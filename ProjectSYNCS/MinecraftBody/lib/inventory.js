// What she carries: the kinds of goods, reports to the service (« t'as quoi sur toi ? », /debug minecraft), handing
// things over and putting them away, keeping her kit.

import pathfinderPackage from 'mineflayer-pathfinder';
import { FireResistPotions, HealingPotions, isPotion, Reserved } from './emergency.js';
import { FarmProduce } from './farm.js';
import { setHandedAt } from './needs.js';
import { pausedActivity, playerEntity, resumeActivity, stay } from './orders.js';
import { bot, emit, log, safely, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// What each kind of goods is, by item name. « all » is her pockets; armour includes what she wears.
export const Goods = {
  all: () => true,
  food: (n) => Boolean(bot.registry.foodsByName[n]),
  fish: (n) => ['cod', 'salmon', 'tropical_fish', 'pufferfish', 'cooked_cod', 'cooked_salmon'].includes(n),
  weapons: (n) => n.endsWith('_sword') || (n.endsWith('_axe') && !n.endsWith('_pickaxe')) || ['bow', 'crossbow', 'trident', 'mace'].includes(n),
  tools: (n) => /_(pickaxe|axe|shovel|hoe)$/.test(n) || ['shears', 'fishing_rod', 'flint_and_steel'].includes(n),
  pickaxe: (n) => n.endsWith('_pickaxe'),
  axe: (n) => n.endsWith('_axe') && !n.endsWith('_pickaxe'),
  shovel: (n) => n.endsWith('_shovel'),
  rod: (n) => n === 'fishing_rod',
  bow: (n) => n === 'bow' || n === 'crossbow',
  arrows: (n) => n.endsWith('arrow'),
  armour: (n) => /_(helmet|chestplate|leggings|boots)$/.test(n) || n === 'shield' || n === 'elytra',
  seeds: (n) => n.endsWith('_seeds'),
  produce: (n) => FarmProduce.has(n) && !n.endsWith('_seeds'),
  wood: (n) => (n.endsWith('_log') || n.endsWith('_planks') || n.endsWith('_wood')),
  stone: (n) => ['cobblestone', 'stone', 'cobbled_deepslate', 'deepslate'].includes(n),
  coal: (n) => n === 'coal' || n === 'charcoal',
  iron: (n) => ['raw_iron', 'iron_ingot', 'iron_nugget', 'iron_ore', 'deepslate_iron_ore'].includes(n),
  copper: (n) => ['raw_copper', 'copper_ingot', 'copper_ore', 'deepslate_copper_ore'].includes(n),
  gold: (n) => ['raw_gold', 'gold_ingot', 'gold_nugget', 'gold_ore', 'deepslate_gold_ore'].includes(n),
  diamond: (n) => ['diamond', 'diamond_ore', 'deepslate_diamond_ore'].includes(n),
  dirt: (n) => n === 'dirt' || n === 'coarse_dirt',
  sand: (n) => n === 'sand' || n === 'red_sand',
  gravel: (n) => n === 'gravel',
  clay: (n) => n === 'clay_ball' || n === 'clay',
  redstone: (n) => n === 'redstone',
  lapis: (n) => n === 'lapis_lazuli',
  emerald: (n) => n === 'emerald',
  quartz: (n) => n === 'quartz',
  granite: (n) => n === 'granite',
  diorite: (n) => n === 'diorite',
  andesite: (n) => n === 'andesite',
  torches: (n) => n === 'torch' || n === 'soul_torch',
  sticks: (n) => n === 'stick',
  wool: (n) => n.endsWith('_wool'),
  leather: (n) => n === 'leather' || n === 'rabbit_hide',
  string: (n) => n === 'string',
  feathers: (n) => n === 'feather',
  bones: (n) => n === 'bone' || n === 'bone_meal',
  gunpowder: (n) => n === 'gunpowder',
  flint: (n) => n === 'flint',
  pearls: (n) => n === 'ender_pearl',
  saplings: (n) => n.endsWith('_sapling') || n === 'mangrove_propagule',
  shears: (n) => n === 'shears',
  hoe: (n) => n.endsWith('_hoe'),
  buckets: (n) => n === 'bucket' || n.endsWith('_bucket'),
  glass: (n) => n === 'glass' || n.endsWith('_glass') || n === 'glass_pane',
  eggs: (n) => n === 'egg',
  slime: (n) => n === 'slime_ball',
  blazerods: (n) => n === 'blaze_rod',
  paper: (n) => n === 'paper',
  books: (n) => n === 'book' || n === 'enchanted_book' || n === 'writable_book',
  beds: (n) => n.endsWith('_bed'),
  saddle: (n) => n === 'saddle',
  compass: (n) => n === 'compass',
  map: (n) => n === 'map' || n === 'filled_map',
  ink: (n) => n === 'ink_sac' || n === 'glow_ink_sac',
};
export const WornSlots = ['head', 'torso', 'legs', 'feet'];

export function wornArmour() {
  return WornSlots.filter((slot) => bot.inventory.slots[bot.getEquipmentDestSlot(slot)]);
}

// What she wears that « donne-moi ton armure » and « enlève ton armure » take off: her armour, and a shield in her
// off-hand (left on, it never reached the player).
export function wornGear() {
  const offhand = bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')];
  return offhand && offhand.name === 'shield' ? [...wornArmour(), 'off-hand'] : wornArmour();
}

// A stack as the service reads it. Its kinds of goods are the ones give() would hand over for it, so « t'as du fer ? »
// counts exactly what « donne-moi ton fer » would toss.
function stackReport(item) {
  if (!item) return null;
  return { name: item.name, count: item.count, goods: Object.keys(Goods).filter((key) => key !== 'all' && Goods[key](item.name)) };
}

// Sent on spawn and after her pockets change, at most every InventoryEvery: a gathering run moves a slot per block.
const InventoryEvery = 2000;
let inventoryTimer = null;

function reportInventory() {
  inventoryTimer = null;
  if (!bot) return;
  const slots = bot.inventory.slots;
  emit('inventory', {
    worn: WornSlots.map((slot) => stackReport(slots[bot.getEquipmentDestSlot(slot)])).filter(Boolean),
    held: stackReport(bot.heldItem),
    offhand: stackReport(slots[bot.getEquipmentDestSlot('off-hand')]),
    pockets: bot.inventory.items().map(stackReport),
  });
}

export function scheduleInventory() {
  if (!inventoryTimer) inventoryTimer = setTimeout(safely(reportInventory), InventoryEvery);
}

// « syncs vide tes poches », « donne-moi ton poisson »: she walks up to the player and tosses it at their feet.
// A farm, a fishing spot or a guarded place she was keeping, she goes back to after.
export async function give(player, key) {
  const matches = Goods[key];
  if (!matches) return;
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  const worn = key === 'armour' ? wornGear() : [];
  if (worn.length === 0 && !bot.inventory.items().some((i) => matches(i.name)))
    return emit('cantGive', { player, goods: key, why: 'none' });
  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'give', player };
  setTask(mine);
  let count = 0;
  let delivered = false;
  try {
    if (target.position.distanceTo(bot.entity.position) > 3)
      await bot.pathfinder.goto(new goals.GoalNear(target.position.x, target.position.y, target.position.z, 2));
    const owner = playerEntity(player);
    if (task === mine && owner) {
      for (const slot of worn) await bot.unequip(slot);
      await bot.lookAt(owner.position.offset(0, 1.6, 0), true);
      for (const item of bot.inventory.items().filter((i) => matches(i.name))) {
        await bot.tossStack(item);
        count += item.count;
      }
      delivered = true;
    }
  } catch (err) {
    log('giving failed:', err && err.message ? err.message : String(err));
  }
  setHandedAt(Date.now()); // her tools may be gone now: not broken
  if (task !== mine) return;
  setTask(null);
  emit('given', { player, goods: key, count, delivered });
  resumeActivity(back);
}

// What she keeps when she puts her things away: the best weapon, pickaxe, axe and shovel she carries, a fishing rod,
// shears, and her bow with its arrows, the tools her other orders need. By slot.
const ToolTiers = ['wooden', 'stone', 'golden', 'iron', 'diamond', 'netherite'];

// Her kit, by tool (the names `wornOut` and `broke` report).
export function kitTools(items) {
  const best = (suffix) => items.filter((i) => i.name.endsWith(suffix) && !(suffix === '_axe' && i.name.endsWith('_pickaxe')))
    .sort((a, b) => ToolTiers.indexOf(b.name.split('_')[0]) - ToolTiers.indexOf(a.name.split('_')[0]))[0];
  return {
    sword: best('_sword'), pickaxe: best('_pickaxe'), axe: best('_axe'), shovel: best('_shovel'),
    rod: items.find((i) => i.name === 'fishing_rod'), shears: items.find((i) => i.name === 'shears'),
    bow: items.find((i) => i.name === 'bow'),
  };
}

function kitSlots(items) {
  const kept = new Set(Object.values(kitTools(items)).filter(Boolean).map((i) => i.slot));
  if (items.some((i) => i.name === 'bow'))
    for (const arrow of items.filter((i) => i.name.endsWith('arrow'))) kept.add(arrow.slot);
  // And what keeps her alive: golden apples, a totem, healing and fire resistance potions.
  for (const i of items)
    if (Reserved.has(i.name) || i.name === 'totem_of_undying' || isPotion(i, HealingPotions) || isPotion(i, FireResistPotions)) kept.add(i.slot);
  return kept;
}

const StoreRadius = 16;
const ContainerBlocks = ['chest', 'barrel', 'trapped_chest'];

// « syncs range tes affaires »: everything but her kit, into the closest chest or barrel within StoreRadius.
export async function store(player) {
  const ids = bot.registry.blocksArray.filter((b) => ContainerBlocks.includes(b.name)).map((b) => b.id);
  const at = bot.findBlocks({ matching: ids, maxDistance: StoreRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position))[0];
  if (!at) return emit('cantStore', { player, why: 'noChest' });
  const items = bot.inventory.items();
  const kit = kitSlots(items);
  const totals = new Map();
  for (const item of items.filter((i) => !kit.has(i.slot))) totals.set(item.type, (totals.get(item.type) || 0) + item.count);
  if (totals.size === 0) return emit('cantStore', { player, why: 'empty' });
  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'store', player };
  setTask(mine);
  let count = 0;
  let full = false;
  try {
    await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
    if (task === mine) {
      const box = await bot.openContainer(bot.blockAt(at));
      try {
        for (const [type, n] of totals) {
          try {
            await box.deposit(type, null, n);
            count += n;
          } catch {
            full = true;
            break;
          }
        }
      } finally {
        box.close();
      }
    }
  } catch (err) {
    log('storing failed:', err && err.message ? err.message : String(err));
  }
  setHandedAt(Date.now());
  if (task !== mine) return;
  setTask(null);
  emit('stored', { player, count, full });
  resumeActivity(back);
}
