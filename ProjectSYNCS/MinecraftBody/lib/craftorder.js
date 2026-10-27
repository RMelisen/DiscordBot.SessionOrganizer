// « fabrique-moi une pioche en fer »: an item made for a player and handed over at their feet. Wood and stone she gets
// herself (trees felled whole, stone from open faces with a wooden pickaxe she makes if she has none); iron, gold,
// diamonds, coal and string she must already carry. At a crafting table within TableReach, else at one of her own,
// crafted and set down beside her, then taken back: the one block she places and breaks outside the camp. What she
// makes along the way (planks, sticks, a wooden pickaxe) stays hers. An errand like handing over: an activity pauses
// and comes back after. A fight starts it again from where it was (resume.js: her table and her pockets at the start).

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { held, isLog, isPlanks } from './camp.js';
import { craftOne, craftPlanks } from './crafting.js';
import { gatherCandidates, gatherRounds, itemCounts } from './gather.js';
import { handOver } from './handover.js';
import { setHandedAt } from './needs.js';
import { pausedActivity, playerEntity, resumeActivity, stay } from './orders.js';
import { pickUp, PickUpTry } from './pickup.js';
import { bot, emit, log, setTask, task, wait } from './state.js';
import { fellTrees, treesNear } from './trees.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// A crafting table this close is used where it stands, whoever's it is.
const TableReach = 16;

// What a material is, by item name: `log`, `planks`, `stick` and `stone` she can get herself; the rest she must carry
// (cantCraft's `missing`, named by C#'s McCraftMaterialNames). Stone is what stone tools and furnaces take.
const Has = {
  log: isLog,
  planks: isPlanks,
  stick: (n) => n === 'stick',
  stone: (n) => n === 'cobblestone' || n === 'cobbled_deepslate' || n === 'blackstone',
  iron: (n) => n === 'iron_ingot',
  gold: (n) => n === 'gold_ingot',
  diamond: (n) => n === 'diamond',
  coal: (n) => n === 'coal' || n === 'charcoal',
  string: (n) => n === 'string',
};
const Carried = ['iron', 'gold', 'diamond', 'coal', 'string'];

// Tools in materials: the head's material and how much of it, then sticks.
const Heads = { wooden: 'planks', stone: 'stone', iron: 'iron', golden: 'gold', diamond: 'diamond' };
const ToolShapes = { pickaxe: [3, 2], axe: [3, 2], shovel: [1, 2], hoe: [2, 2], sword: [2, 1] };
// A bare tool (« une pioche ») is the best she can afford, in this order. Never gold unasked: a golden tool wears out
// faster than a stone one, and it would spend the player's gold on it.
const BareOrder = ['diamond', 'iron', 'stone', 'wooden'];

// Per craft: what it takes, how many it makes, and whether it needs a table (a 3x3 grid).
const Recipes = {
  bow: { takes: { stick: 3, string: 3 }, yield: 1, table: true },
  fishing_rod: { takes: { stick: 3, string: 2 }, yield: 1, table: true },
  shears: { takes: { iron: 2 }, yield: 1, table: false },
  planks: { takes: { log: 1 }, yield: 4, table: false },
  stick: { takes: { planks: 2 }, yield: 4, table: false },
  crafting_table: { takes: { planks: 4 }, yield: 1, table: false },
  chest: { takes: { planks: 8 }, yield: 1, table: true },
  furnace: { takes: { stone: 8 }, yield: 1, table: true },
  torch: { takes: { coal: 1, stick: 1 }, yield: 4, table: false },
  ladder: { takes: { stick: 7 }, yield: 3, table: true },
};

function recipeFor(item) {
  const tool = /^(wooden|stone|iron|golden|diamond)_(pickaxe|axe|shovel|hoe|sword)$/.exec(item);
  if (!tool) return Recipes[item] || null;
  const [head, sticks] = ToolShapes[tool[2]];
  return { takes: { [Heads[tool[1]]]: head, stick: sticks }, yield: 1, table: true };
}

const crafts = (recipe, count) => Math.ceil(count / recipe.yield);

// A bare tool, in the best material she has enough of (stone also when there's some open to gather: the wood for a
// pickaxe is checked after). Anything else as it is.
function resolve(item, count) {
  if (!ToolShapes[item]) return recipeFor(item) ? item : null;
  const [head] = ToolShapes[item];
  return `${BareOrder.find((m) => m === 'wooden' || held(Has[Heads[m]]) >= head * count
    || (m === 'stone' && gatherCandidates('stone').open.length > 0))}_${item}`;
}

function tableNear() {
  const id = bot.registry.blocksByName.crafting_table.id;
  return bot.findBlock({ matching: id, maxDistance: TableReach });
}

// What's still short for `n` crafts, from what she carries now: logs to cut, stone to gather, and the first material
// she can't get herself. A wooden pickaxe first when stone is short and she has none, a table of her own when the
// recipe or that pickaxe needs one and none stands near (`table`: the one she set down, if it still stands). Sticks
// short become planks, planks short become logs.
function shortfall(recipe, n, table) {
  const need = {};
  const add = (k, x) => { need[k] = (need[k] || 0) + x; };
  for (const [k, x] of Object.entries(recipe.takes)) add(k, x * n);
  const stone = Math.max(0, (need.stone || 0) - held(Has.stone));
  const pickaxe = stone > 0 && held((x) => x.endsWith('_pickaxe')) === 0;
  if (pickaxe) { add('planks', 3); add('stick', 2); }
  const ownTable = (recipe.table || pickaxe) && !table && held((x) => x === 'crafting_table') === 0;
  if (ownTable) add('planks', 4);
  add('planks', Math.ceil(Math.max(0, (need.stick || 0) - held(Has.stick)) / 4) * 2);
  add('log', Math.ceil(Math.max(0, (need.planks || 0) - held(Has.planks)) / 4));
  return {
    need,
    logs: Math.max(0, need.log - held(Has.log)),
    stone,
    pickaxe,
    missing: Carried.find((k) => (need[k] || 0) > held(Has[k])) || null,
  };
}

// Her own table, set down beside her: in an empty cell over firm ground, two blocks off first (one block off, her own
// box can be in the way), with nobody standing there. Null when no spot took it.
async function setTable(mine) {
  const item = bot.inventory.items().find((i) => i.name === 'crafting_table');
  if (!item) return null;
  const feet = bot.entity.position.floored();
  const offsets = [];
  for (let dx = -2; dx <= 2; dx++)
    for (let dz = -2; dz <= 2; dz++) if (dx !== 0 || dz !== 0) offsets.push([dx, dz]);
  offsets.sort((a, b) => Math.max(Math.abs(b[0]), Math.abs(b[1])) - Math.max(Math.abs(a[0]), Math.abs(a[1])));
  const someoneOn = (p) => Object.values(bot.entities).some((e) => e.type === 'player' && e.position.floored().equals(p));
  for (const [dx, dz] of offsets) {
    const cell = feet.offset(dx, 0, dz);
    const here = bot.blockAt(cell);
    const below = bot.blockAt(cell.offset(0, -1, 0));
    if (!here || !['air', 'cave_air'].includes(here.name)) continue;
    if (!below || below.boundingBox !== 'block' || someoneOn(cell)) continue;
    for (let attempt = 0; attempt < 2; attempt++) {
      if (task !== mine) return null;
      try {
        await bot.equip(item, 'hand');
        await bot.placeBlock(below, new Vec3(0, 1, 0));
      } catch { /* checked below: it often lands before its event */ }
      await wait(150);
      const now = bot.blockAt(cell);
      if (now && now.name === 'crafting_table') {
        mine.table = { x: cell.x, y: cell.y, z: cell.z };
        return now;
      }
      await wait(300);
    }
    // Refused there (a slab under it, someone walking in): the next spot.
  }
  return null;
}

// Her table back in her pockets once she's done: broken by hand or axe, the drop picked up.
async function takeTableBack(mine) {
  if (!mine.table) return;
  const at = new Vec3(mine.table.x, mine.table.y, mine.table.z);
  const block = bot.blockAt(at);
  if (!block || block.name !== 'crafting_table') return;
  try {
    if (bot.entity.position.distanceTo(at) > 4) await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
    if (bot.tool) await bot.tool.equipForBlock(block, {});
    await bot.dig(block, true);
    await pickUp((e) => e.position.distanceTo(at) < 3, PickUpTry, mine);
  } catch (err) {
    log('craft: taking her table back failed:', err && err.message ? err.message : String(err));
  }
}

// What she made for them: the item's stacks she has more of than at the start (planks: of any wood).
function made(item, before) {
  const counts = itemCounts();
  const gained = new Map();
  for (const [type, n] of counts) {
    const name = bot.registry.items[type] && bot.registry.items[type].name;
    if (!name || (item === 'planks' ? !isPlanks(name) : name !== item)) continue;
    const more = n - (before.get(type) || 0);
    if (more > 0) gained.set(type, more);
  }
  return gained;
}

// `count`: items, crafted in whole recipes. Everything that can fail at once (out of sight, an unknown item, a
// material she can't get, no tree or stone in reach) is checked before stay(). `resume`: started again after a fight
// ({ before, table }), with no second « crafting ».
export async function craftFor(player, item, count, resume = null) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  const wanted = resolve(item, count);
  if (!wanted) return emit('cantCraft', { player, item, why: 'unknown' });
  const recipe = recipeFor(wanted);
  const n = crafts(recipe, count);
  // The table she set down, while it still stands: before a restart, the one the craft had.
  const standing = (t) => {
    const b = t && bot.blockAt(new Vec3(t.x, t.y, t.z));
    return b && b.name === 'crafting_table' ? b : null;
  };
  const ownTable = () => standing(resume && resume.table);
  const first = shortfall(recipe, n, tableNear() || ownTable());
  if (first.missing) return emit('cantCraft', { player, item: wanted, why: 'missing', missing: first.missing });
  if (first.logs > 0 && treesNear().length === 0) return emit('cantCraft', { player, item: wanted, why: 'noWood' });
  if (first.stone > 0 && gatherCandidates('stone').open.length === 0) return emit('cantCraft', { player, item: wanted, why: 'noStone' });

  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'craft', player, before: (resume && resume.before) || itemCounts(), table: (resume && resume.table) || null };
  setTask(mine);
  if (!resume) emit('crafting', { player, item: wanted, count });
  const end = (ev, data) => {
    if (task !== mine) return;
    setTask(null);
    emit(ev, { player, item: wanted, ...data });
    resumeActivity(back);
  };
  // Taken away mid-craft by another order: her table stays where it is (its spot in the log), as the tree pillar does.
  const leftBehind = () => {
    if (mine.table) log(`craft: her table left standing at ${mine.table.x} ${mine.table.y} ${mine.table.z}`);
  };
  const table = () => tableNear() || standing(mine.table);
  const plan = () => shortfall(recipe, n, table());
  const toTable = async (b) => {
    if (bot.entity.position.distanceTo(b.position) > 3)
      try { await bot.pathfinder.goto(new goals.GoalNear(b.position.x, b.position.y, b.position.z, 2)); } catch { /* crafted from where she is, if in reach */ }
  };

  // Nothing escapes: a throw left her on a dead task and the order unanswered.
  try {
    // Wood, all of it at once, again while there are trees in reach (a pass whose drops she missed brought nothing).
    for (let pass = 0; pass < 4 && task === mine && plan().logs > 0; pass++) {
      const trees = treesNear();
      if (trees.length === 0) break;
      await fellTrees(trees, plan().logs, mine);
    }
    if (task !== mine) return leftBehind();
    if (plan().logs > 0) return end('cantCraft', { why: 'noWood' });

    const { need, pickaxe } = plan();
    // Planks made from the logs, keeping those the recipe itself takes (planks asked for).
    await craftPlanks(need.planks || 0, (recipe.takes.log || 0) * n, mine);
    while (task === mine && held(Has.stick) < (need.stick || 0) && (await craftOne('stick')));
    if (task !== mine) return leftBehind();

    let at = null;
    if (recipe.table || pickaxe) {
      at = table();
      if (!at && held((x) => x === 'crafting_table') === 0 && !(await craftOne('crafting_table'))) return end('cantCraft', { why: 'failed' });
      if (!at) at = await setTable(mine);
      if (task !== mine) return leftBehind();
      if (!at) return end('cantCraft', { why: 'failed' });
    }
    if (pickaxe) {
      await toTable(at);
      if (!(await craftOne('wooden_pickaxe', at))) return end('cantCraft', { why: 'failed' });
    }
    if (plan().stone > 0) await gatherRounds('stone', plan().stone, mine);
    if (task !== mine) return leftBehind();
    if (plan().stone > 0) return end('cantCraft', { why: 'noStone' });

    if (recipe.table) {
      at = table();
      if (!at) return end('cantCraft', { why: 'failed' });
      await toTable(at);
    }
    for (let i = 0; i < n && task === mine; i++) {
      // Planks of the wood she has most logs of.
      const name = wanted === 'planks'
        ? (bot.inventory.items().filter((x) => isLog(x.name)).sort((a, b) => b.count - a.count)[0] || { name: 'oak_log' }).name.replace(/_log$/, '_planks')
        : wanted;
      if (!(await craftOne(name, recipe.table ? at : null))) break;
    }
    if (task !== mine) return leftBehind();

    await takeTableBack(mine);
    if (task !== mine) return leftBehind();
    const gained = made(wanted, mine.before);
    const total = [...gained.values()].reduce((a, b) => a + b, 0);
    if (total === 0) return end('cantCraft', { why: 'failed' });
    let delivered = false;
    try {
      delivered = await handOver(player, gained);
    } catch (err) {
      log('craft: handing over failed:', err && err.message ? err.message : String(err));
    }
    setHandedAt(Date.now());
    end('crafted', { count: total, delivered });
  } catch (err) {
    log('craft failed:', err && err.message ? err.message : String(err));
    if (task === mine) leftBehind();
    end('cantCraft', { why: 'failed' });
  }
}
