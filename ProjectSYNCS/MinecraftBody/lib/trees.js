// Felling whole trees, closest first, climbing a pillar of her own up the trunk and taking it down after.

import { isLog } from './camp.js';
import { gather, GatherRadius, isTreeLog, Resources } from './gather.js';
import { pickUp, PickUpTry } from './pickup.js';
import { bot, log, task } from './state.js';

// A tree is felled whole, to its last log: cutting the closest logs anywhere left trunks floating half cut all over a
// forest, and a 2x2 spruce's top out of any reach. A tree is the logs of one kind touching each other (corners too:
// acacia and dark oak branches), from the bottom of its trunk, within TreeReach of it sideways and TreeHigh above, with
// natural leaves near its top (isTreeLog on its TreeTop highest logs): a big spruce's trunk has none for its first ten
// blocks, and a log in someone's wall has none at all.
const TreeReach = 6;
const TreeHigh = 40;
const TreeMost = 400;
const TreeTop = 8;
const LogSearch = 512;
export const posKey = (p) => `${p.x},${p.y},${p.z}`;

// The tree `block` belongs to, or null if it isn't one. Every log it reaches goes in `seen`, tree or not.
function treeOf(block, seen) {
  let base = block.position;
  while (base.y > block.position.y - TreeHigh) {
    const below = bot.blockAt(base.offset(0, -1, 0));
    if (!below || below.name !== block.name) break;
    base = below.position;
  }
  const logs = [];
  const queue = [base];
  const marked = new Set([posKey(base)]);
  while (queue.length > 0 && logs.length < TreeMost) {
    const p = queue.shift();
    logs.push(p);
    for (let dx = -1; dx <= 1; dx++)
      for (let dy = -1; dy <= 1; dy++)
        for (let dz = -1; dz <= 1; dz++) {
          const q = p.offset(dx, dy, dz);
          const k = posKey(q);
          // A few blocks under the base: the other trunks of a 2x2 on a slope go lower.
          if (marked.has(k) || Math.abs(q.x - base.x) > TreeReach || Math.abs(q.z - base.z) > TreeReach
            || q.y < base.y - 3 || q.y > base.y + TreeHigh) continue;
          const near = bot.blockAt(q);
          if (!near || near.name !== block.name) continue;
          marked.add(k);
          queue.push(q);
        }
  }
  for (const k of marked) seen.add(k);
  const top = [...logs].sort((a, b) => b.y - a.y).slice(0, TreeTop);
  if (!top.some((p) => isTreeLog(bot.blockAt(p)))) return null;
  return { name: block.name, base: logs.reduce((low, p) => (p.y < low.y ? p : low), base), logs };
}

// The trees within GatherRadius, from their closest LogSearch logs.
export function treesNear() {
  const ids = bot.registry.blocksArray.filter((b) => Resources.wood(b.name)).map((b) => b.id);
  const seen = new Set();
  const trees = [];
  for (const p of bot.findBlocks({ matching: ids, maxDistance: GatherRadius, count: LogSearch })) {
    if (seen.has(posKey(p))) continue;
    const block = bot.blockAt(p);
    const tree = block && treeOf(block, seen);
    if (tree) trees.push(tree);
  }
  return trees;
}

// What's left of it; not her pillar, which may stand where its trunk was (unstack takes that down, from the top).
const standingLogs = (tree, placed = []) => tree.logs.filter((p) => !placed.some((q) => q.equals(p)))
  .map((p) => bot.blockAt(p)).filter((b) => b && b.name === tree.name);

// One tree after another, the closest to where she stands first, until `left` logs are cut. Each to its last log, so
// she brings more than asked rather than leave a tree standing half cut.
export async function fellTrees(trees, left, mine) {
  const todo = [...trees];
  while (left > 0 && todo.length > 0 && task === mine) {
    const me = bot.entity.position;
    todo.sort((a, b) => a.base.distanceTo(me) - b.base.distanceTo(me));
    left -= await fell(todo.shift(), mine);
  }
}

// Bottom up, in rounds: each round cuts what she gets at, from the ground or from a pillar she stacks under herself
// (gatheringMovements' climb: a 2x2 spruce stands 30 high), and ends at the first log she can't get to, so a tree's top
// never costs a 5 s search per log. The next round has the logs the last one brought to stack with. The drops are
// picked up on the ground: between rounds while there's no pillar, else once it's down (unstack). Returns how many of
// its logs are gone.
const FellRounds = 8;
export const ReachUp = 3; // a block this high above her feet needs a climb

async function fell(tree, mine) {
  const placed = [];
  const drops = new Set();
  const collectDrops = async () => {
    if (task === mine && drops.size > 0) await pickUp((e) => drops.has(e.id), Math.min(60000, drops.size * PickUpTry), mine);
    drops.clear();
  };
  let left = standingLogs(tree);
  const start = left.length;
  for (let round = 0; round < FellRounds && left.length > 0 && task === mine; round++) {
    await gather(left, mine, 'fell', { placed, drops, order: 'up' });
    const now = standingLogs(tree, placed);
    const broke = now.length < left.length;
    left = now;
    if (pillar(placed).length === 0) await collectDrops();
    if (!broke) break;
  }
  if (left.length > 0) log(`fell: ${left.length} of ${tree.logs.length} ${tree.name} left standing at ${tree.base}`);
  if (pillar(placed).length > 0 && task === mine) await unstack(placed, mine, drops);
  await collectDrops();
  return start - standingLogs(tree, placed).length;
}

// Her pillar, from the top down: she breaks the block she stands on, drops onto the next, and so on to the ground.
// Nothing she placed stays in the forest; what it was made of comes back to her pockets with the drops.
async function unstack(placed, mine, drops) {
  const blocks = pillar(placed);
  if (blocks.length > 0) await gather(blocks, mine, 'unstack', { drops, order: 'down' });
  const left = pillar(placed);
  if (left.length > 0) log(`unstack: ${left.length} of her pillar's blocks left, the top at ${left[0].position}`);
}

// What stands of her pillar: `placed` is where she tried to set a block.
export const pillar = (placed) => placed.map((p) => bot.blockAt(p)).filter((b) => b && b.boundingBox === 'block');

// What a pillar is stacked from, in that order: what she'd least miss first, a log last (it comes back when the pillar
// comes down).
const ScaffoldItems = ['dirt', 'coarse_dirt', 'cobblestone', 'cobbled_deepslate', 'netherrack', 'andesite', 'diorite', 'granite'];

export function scaffoldIds() {
  const named = ScaffoldItems.map((n) => bot.registry.itemsByName[n]).filter(Boolean);
  const logs = bot.registry.itemsArray.filter((i) => isLog(i.name));
  return [...named, ...logs].map((i) => i.id);
}
