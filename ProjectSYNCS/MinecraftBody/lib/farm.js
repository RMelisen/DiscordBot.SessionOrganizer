// Harvesting ripe crops and replanting, and the farm she keeps (« occupe-toi de la ferme »): a round every 15 s,
// the produce in a chest.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { deliverGains, gather, itemCounts } from './gather.js';
import { stay } from './orders.js';
import { pickUp } from './pickup.js';
import { bot, emit, log, setTask, task } from './state.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// What she harvests, by crop: the block, its ripe age, and the seed she replants it with. Melons and
// pumpkins have no age and need no replanting (the stem stays); only a fruit next to an attached stem is
// a crop, so a pumpkin someone placed as decoration is never touched.
export const Crops = {
  wheat: { block: 'wheat', ripe: 7, seed: 'wheat_seeds' },
  carrot: { block: 'carrots', ripe: 7, seed: 'carrot' },
  potato: { block: 'potatoes', ripe: 7, seed: 'potato' },
  beetroot: { block: 'beetroots', ripe: 3, seed: 'beetroot_seeds' },
  melon: { block: 'melon', stem: 'attached_melon_stem' },
  pumpkin: { block: 'pumpkin', stem: 'attached_pumpkin_stem' },
  // Columns: what grows on top of itself. She cuts the second block of each, which brings down everything above it, and
  // the bottom one stays to grow again: no replanting. Only when named or on a farm, never in « everything ripe »: a
  // bamboo jungle, a desert's cacti and a river's cane are mostly wild.
  bamboo: { block: 'bamboo', column: true },
  sugarcane: { block: 'sugar_cane', column: true },
  cactus: { block: 'cactus', column: true },
};
export const HarvestRadius = 24;
export const FarmRadius = 16;
const ChestRadius = 10;
export const FarmRound = 15000;
// What goes in the farm's chest, and how much of each she keeps back to replant with.
export const FarmProduce = new Set(['wheat', 'wheat_seeds', 'carrot', 'potato', 'poisonous_potato', 'beetroot', 'beetroot_seeds',
  'melon_slice', 'pumpkin', 'bamboo', 'sugar_cane', 'cactus']);
const SeedsKept = { wheat_seeds: 16, carrot: 16, potato: 16, beetroot_seeds: 16 };
// And what she keeps to eat: the carrots and potatoes kept as seeds are food too, so only these two are extra.
const FoodKept = { beetroot: 8, melon_slice: 8 };
// And wheat for the cows and sheep: the seeds, carrots and potatoes kept to replant already feed the rest of the pen.
const FeedKept = { wheat: 16 };

// What lies in a field is the field's: produce (FarmProduce) on or beside farmland or a crop. She only ever picked up
// the drops she saw fall from what she broke, within 2 blocks of it, so a carrot that rolled further, or one someone
// trampled out, stayed on the ground round after round. Anything else lying about stays where it is: a player's dropped
// things are theirs (lootSpots).
export const FieldSweep = 20000;

function isFieldBlock(block) {
  return Boolean(block) && (block.name === 'farmland' || Object.values(Crops).some((c) => c.block === block.name));
}

function fieldDrop(entity) {
  if (entity.name !== 'item' || typeof entity.getDroppedItem !== 'function') return false;
  const item = entity.getDroppedItem();
  if (!item || !FarmProduce.has(item.name)) return false;
  const at = entity.position.floored();
  for (let dx = -1; dx <= 1; dx++)
    for (let dz = -1; dz <= 1; dz++)
      for (let dy = -1; dy <= 0; dy++)
        if (isFieldBlock(bot.blockAt(at.offset(dx, dy, dz)))) return true;
  return false;
}

function cropsFor(key) {
  return key === 'any' ? Object.values(Crops).filter((c) => !c.column) : Crops[key] ? [Crops[key]] : [];
}

function isRipe(block, crop) {
  // A column's second block: the same below it, something else below that.
  if (crop.column) {
    const below = bot.blockAt(block.position.offset(0, -1, 0));
    const base = bot.blockAt(block.position.offset(0, -2, 0));
    return Boolean(below && below.name === crop.block && base && base.name !== crop.block);
  }
  if (crop.stem) {
    return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => {
      const near = bot.blockAt(block.position.offset(dx, 0, dz));
      return near && near.name === crop.stem;
    });
  }
  // prismarine-block gives number states as strings on recent versions (« 7 » on 26.1), booleans as booleans.
  const age = Number(block.getProperties().age);
  return Number.isFinite(age) && age >= crop.ripe;
}

// Ripe crops of these kinds within the radius of a point, with the crop each one is.
export function findRipe(crops, point, radius) {
  const byName = new Map(crops.map((c) => [c.block, c]));
  const ids = bot.registry.blocksArray.filter((b) => byName.has(b.name)).map((b) => b.id);
  return bot.findBlocks({ matching: ids, maxDistance: radius, point, count: 256 })
    .map((p) => bot.blockAt(p)).filter(Boolean)
    .map((block) => ({ block, crop: byName.get(block.name) }))
    .filter(({ block, crop }) => isRipe(block, crop));
}

// Breaks the ripe crops and picks up what drops (gather), then puts a seed back on each patch of farmland
// it left bare. Seeds on farmland are the one thing she ever places: nothing else, nowhere else.
export async function harvestBlocks(found, mine) {
  await gather(found.map((f) => f.block), mine, 'harvest');
  let replanted = 0;
  for (const { block, crop } of found) {
    if (task !== mine) break;
    if (!crop.seed) continue;
    const soil = bot.blockAt(block.position.offset(0, -1, 0));
    const above = bot.blockAt(block.position);
    if (!soil || soil.name !== 'farmland' || !above || above.name !== 'air') continue;
    if (!bot.inventory.items().some((i) => i.name === crop.seed)) continue;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(block.position.x, block.position.y, block.position.z, 3));
      // The stack as it is now: picked before the walk, it had moved (what she picked up on the way), and she placed
      // with an empty hand (« must be holding an item to place »).
      const seed = bot.inventory.items().find((i) => i.name === crop.seed);
      if (!seed) continue;
      await bot.equip(seed, 'hand');
      await bot.placeBlock(soil, new Vec3(0, 1, 0));
      replanted++;
    } catch (err) {
      log('replant failed:', err && err.message ? err.message : String(err));
    }
  }
  return replanted;
}

// « syncs récolte le blé »: harvest what's ripe around her, replant, and bring it to the player.
// `since`: as for collect.
export async function harvest(player, key, since = null) {
  const found = findRipe(cropsFor(key), bot.entity.position, HarvestRadius);
  if (found.length === 0) return emit('cantHarvest', { player, crop: key, why: 'none' });
  stay();
  const before = since || itemCounts();
  const mine = { kind: 'harvest', player, before };
  setTask(mine);
  if (!since) emit('harvesting', { player, crop: key, count: found.length });
  const replanted = await harvestBlocks(found, mine);
  // And what lies in the field she cut, near what she cut.
  if (task === mine) await pickUp((e) => fieldDrop(e) && found.some((f) => f.block.position.distanceTo(e.position) <= 4), FieldSweep, mine);
  if (task !== mine) return; // told to stop in the meantime

  const { total, delivered } = await deliverGains(player, before);
  if (task === mine) setTask(null);
  emit('harvested', { player, crop: key, count: total, replanted, delivered });
}

// « syncs occupe-toi de la ferme »: she stays on this spot and, every round, harvests what ripened within
// FarmRadius, replants, and stores the produce in the chest by the field. A farm needs crops and a chest:
// without one she says so instead of filling her pockets all evening.
export function farm(player) {
  const anchor = bot.entity.position.clone();
  if (!cropsNear(anchor)) return emit('cantFarm', { player, why: 'none' });
  const chest = chestNear(anchor);
  if (!chest) return emit('cantFarm', { player, why: 'noChest' });
  stay();
  setTask({ kind: 'farm', player, anchor, chest, busy: false });
  emit('farming', { player });
  farmRound();
}

export function cropsNear(point) {
  const cropIds = bot.registry.blocksArray.filter((b) => Object.values(Crops).some((c) => c.block === b.name)).map((b) => b.id);
  return bot.findBlocks({ matching: cropIds, maxDistance: FarmRadius, point, count: 1 }).length > 0;
}

// The chest or barrel a farm (or a pen) stores into.
export function chestNear(point) {
  return chestsNear(point, 1)[0] || null;
}

// The chests and barrels within ChestRadius of a spot, closest first.
export function chestsNear(point, count) {
  const chestIds = bot.registry.blocksArray.filter((b) => b.name === 'chest' || b.name === 'barrel').map((b) => b.id);
  return bot.findBlocks({ matching: chestIds, maxDistance: ChestRadius, point, count });
}

export async function farmRound() {
  if (!bot || !task || task.kind !== 'farm' || task.busy) return;
  const mine = task;
  mine.busy = true;
  try {
    const found = findRipe(Object.values(Crops), mine.anchor, FarmRadius);
    const before = itemCounts();
    if (found.length > 0) await harvestBlocks(found, mine);
    // Every round, ripe or not: what lies in the field comes in too.
    if (task === mine) await pickUp((e) => fieldDrop(e) && e.position.distanceTo(mine.anchor) <= FarmRadius, FieldSweep, mine);
    // What the round brought in, counted as a harvest order counts it (items gained, net of the seeds replanted).
    const gained = [...itemCounts()].reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
    if (gained > 0) emit('farmed', { count: gained });
    if (task === mine && gained > 0) await storeProduce(mine);
    if (task === mine && bot.entity.position.distanceTo(mine.anchor) > 4)
      await bot.pathfinder.goto(new goals.GoalNear(mine.anchor.x, mine.anchor.y, mine.anchor.z, 2));
  } catch (err) {
    log('farm round failed:', err && err.message ? err.message : String(err));
  } finally {
    mine.busy = false;
  }
}

// Puts the produce in the farm's chest, keeping back enough seeds to replant, a little to eat and wheat for the
// animals. A full chest keeps the rest on her.
async function storeProduce(mine) {
  const totals = new Map();
  for (const item of bot.inventory.items())
    if (FarmProduce.has(item.name)) totals.set(item.name, (totals.get(item.name) || 0) + item.count);
  await storeIn(mine, [...totals].map(([name, n]) => [name, n - (SeedsKept[name] || FoodKept[name] || FeedKept[name] || 0)]));
}

// [name, count] pairs into the task's chest; a full chest keeps the rest on her.
export async function storeIn(mine, pairs) {
  const toStore = pairs.filter(([, n]) => n > 0);
  const block = bot.blockAt(mine.chest);
  if (toStore.length === 0 || !block || (block.name !== 'chest' && block.name !== 'barrel')) return;
  await bot.pathfinder.goto(new goals.GoalNear(mine.chest.x, mine.chest.y, mine.chest.z, 2));
  if (task !== mine) return;
  const box = await bot.openContainer(block);
  try {
    for (const [name, n] of toStore) await box.deposit(bot.registry.itemsByName[name].id, null, n);
  } catch (err) {
    log('chest full or refused:', err && err.message ? err.message : String(err));
  } finally {
    box.close();
  }
}
