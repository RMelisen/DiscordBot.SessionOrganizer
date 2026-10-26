// Gathering (« va chercher du bois »): what each resource is, which blocks she may break, in rounds through
// collectblock, by hand for what has no collision box, and the haul handed over in whole stacks.

import pathfinderPackage from 'mineflayer-pathfinder';
import collectTargets from 'mineflayer-collectblock/lib/Targets.js';
import { gentleMovements } from './movements.js';
import { playerEntity, stay } from './orders.js';
import { pickUp, PickUpTry } from './pickup.js';
import { bot, emit, log, setTask, task, wait } from './state.js';
import { fellTrees, pillar, ReachUp, scaffoldIds, treesNear } from './trees.js';

const { goals } = pathfinderPackage;
const { Targets: CollectTargets } = collectTargets;

// For gathering: the same walk, except that the blocks she was sent for may be broken, and so may a tree's own leaves
// next to its logs: a log wrapped in leaves has an open face (its top, the far side) but none she can see from where
// she stands, and the search for one ran its 5 s for every log of a forest, twelve in a row, nothing brought back. A
// player punches through. Leaves someone placed are persistent and stay, like everything else on the way: collectblock
// asks the movements before every break.
const LeavesBy = 2;

// `climb`, felling a tree: she may also stack a pillar under herself (allow1by1towers), from what she carries
// (ScaffoldItems), to reach a top no reach gets to from the ground; straight up only, never a bridge over a gap. What
// she places is noted (gather) and taken down after (unstack). Up the trunk's own column, where its logs were
// (ClimbBeside a block anywhere else): stacked beside it, the logs she cut fell past her to the ground, out of her
// reach until the pillar was down, and she ran out of blocks to stack halfway up a spruce. Under them, each log she
// cuts over her head falls on her and is hers at once.
const ClimbBeside = 10;

function gatheringMovements(targets, climb = false) {
  const movements = gentleMovements();
  movements.canDig = true;
  if (climb) {
    movements.allow1by1towers = true;
    movements.scafoldingBlocks = scaffoldIds();
    const neighbors = movements.getNeighbors.bind(movements);
    movements.getNeighbors = (node) => neighbors(node).filter((move) => move.toPlace.every((p) => p.jump));
    const trunk = new Set(targets.map((b) => `${b.position.x},${b.position.z}`));
    movements.exclusionAreasPlace.push((block) => (trunk.has(`${block.position.x},${block.position.z}`) ? 0 : ClimbBeside));
  }
  const allowed = new Set(targets.map((b) => b.position.toString()));
  const logs = targets.filter((b) => b.name.endsWith('_log'));
  const treeLeaves = (block) => block.name.endsWith('_leaves') && block.getProperties().persistent === false
    && logs.some((l) => Math.abs(l.position.x - block.position.x) <= LeavesBy && Math.abs(l.position.y - block.position.y) <= LeavesBy
      && Math.abs(l.position.z - block.position.z) <= LeavesBy);
  // A chest among the targets is hers to break (her camp's): the pathfinder never breaks one otherwise.
  if (targets.some((b) => b.name === 'chest')) movements.blocksCantBreak.delete(bot.registry.blocksByName.chest.id);
  const safe = movements.safeToBreak.bind(movements);
  // A block in a chunk that isn't loaded comes as a stub with no position: not breakable (reading it threw from the
  // physics tick and took the whole body down).
  movements.safeToBreak = (block) => Boolean(block && block.position)
    && (allowed.has(block.position.toString()) || treeLeaves(block)) && safe(block);
  return movements;
}

// What each resource is, by block name. Only natural blocks: wood is a log with natural leaves near it
// (a log in someone's wall has none, and placed leaves are "persistent"), never a stripped log.
export const Resources = {
  wood: (n) => n.endsWith('_log') && !n.startsWith('stripped_'),
  stone: (n) => n === 'stone',
  coal: (n) => n === 'coal_ore' || n === 'deepslate_coal_ore',
  iron: (n) => n === 'iron_ore' || n === 'deepslate_iron_ore',
  copper: (n) => n === 'copper_ore' || n === 'deepslate_copper_ore',
  gold: (n) => n === 'gold_ore' || n === 'deepslate_gold_ore',
  diamond: (n) => n === 'diamond_ore' || n === 'deepslate_diamond_ore',
  dirt: (n) => n === 'dirt' || n === 'grass_block',
  sand: (n) => n === 'sand',
};
export const GatherRadius = 48;
// What each resource drops, by item name: what she counts as brought back. A tree's leaves drop saplings, sticks and
// apples, handed over too but not counted as wood.
const Yields = {
  wood: (n) => n.endsWith('_log'),
  stone: (n) => n === 'cobblestone' || n === 'stone',
  coal: (n) => n === 'coal',
  iron: (n) => n === 'raw_iron',
  copper: (n) => n === 'raw_copper',
  gold: (n) => n === 'raw_gold',
  diamond: (n) => n === 'diamond',
  dirt: (n) => n === 'dirt',
  sand: (n) => n === 'sand',
};

export function isTreeLog(block) {
  if (!block) return false;
  for (let dy = 0; dy <= 6; dy++)
    for (let dx = -2; dx <= 2; dx++)
      for (let dz = -2; dz <= 2; dz++) {
        const near = bot.blockAt(block.position.offset(dx, dy, dz));
        if (near && near.name.endsWith('_leaves') && near.getProperties().persistent === false) return true;
      }
  return false;
}

// Whether a block has an open face (air, water, a plant: something with no collision box) to break it from.
// Not water either: coal at the bottom of a lake counted as open, she went down for it, mined it at a fifth of the
// speed with her head under, and drowned there (the camp's coal, a local server).
function exposed(block) {
  return [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]].some(([dx, dy, dz]) => {
    const near = bot.blockAt(block.position.offset(dx, dy, dz));
    return near && near.boundingBox === 'empty' && near.name !== 'lava' && near.name !== 'water' && near.name !== 'bubble_column';
  });
}

// Whether anything she carries (or her bare hand) gets a drop from this block.
function canHarvest(block) {
  return [null, ...bot.inventory.items().map((i) => i.type)].some((type) => block.canHarvest(type));
}

export function itemCounts() {
  const counts = new Map();
  for (const item of bot.inventory.items()) counts.set(item.type, (counts.get(item.type) || 0) + item.count);
  return counts;
}

// `since`: started again after a fight (restartOrder), from the pockets as they were when the order first came, so
// what she had gathered before still counts and is handed over; no second « collecting ».
// What she can get at, the closest 256: she never digs, so a block with no open face (stone under the grass) is out
// of reach (collectblock spent its 5 s search on one and stopped the order). Open is part of the search, not a filter
// after it: on a mountain the closest 256 stone were all under her feet, and she said there was none with a cliff face
// 20 blocks off. `all`, for the count she announces, is the closest 256 of the kind. Not wood: trees are felled whole
// (treesNear, fell).
export function gatherCandidates(resource) {
  const matches = Resources[resource];
  const ids = bot.registry.blocksArray.filter((b) => matches(b.name)).map((b) => b.id);
  return {
    all: bot.findBlocks({ matching: ids, maxDistance: GatherRadius, count: 256 }).map((p) => bot.blockAt(p)).filter(Boolean),
    open: bot.findBlocks({ matching: ids, useExtraInfo: exposed, maxDistance: GatherRadius, count: 256 })
      .map((p) => bot.blockAt(p)).filter(Boolean),
  };
}

// In rounds: what's open now, then what that opened (an ore behind the one in front), until `left` are broken,
// nothing is open, or a round broke nothing.
export async function gatherRounds(resource, left, mine, open = gatherCandidates(resource).open) {
  for (let round = 0; round < 4 && left > 0 && open.length > 0 && task === mine; round++) {
    const batch = open.slice(0, left);
    await gather(batch, mine, 'collect');
    const broken = batch.filter((b) => {
      const now = bot.blockAt(b.position);
      return !now || now.type !== b.type;
    }).length;
    left -= broken;
    if (broken === 0 || task !== mine) break;
    open = gatherCandidates(resource).open;
  }
}

export async function collect(player, resource, count, since = null) {
  if (!Resources[resource]) return;
  const wood = resource === 'wood';
  const trees = wood ? treesNear() : null;
  const first = wood ? null : gatherCandidates(resource);
  if (wood ? trees.length === 0 : first.open.length === 0) return emit('cantCollect', { player, resource, why: 'none' });
  if (!wood && !canHarvest(first.open[0])) return emit('cantCollect', { player, resource, why: 'noTool' });
  stay();

  const before = since || itemCounts();
  const counted = (type) => Yields[resource](bot.registry.items[type].name);
  const mine = { kind: 'collect', player, before };
  setTask(mine);
  const available = wood ? trees.reduce((n, t) => n + t.logs.length, 0) : first.all.length;
  if (!since) emit('collecting', { player, resource, count: Math.min(count, available) });
  // Started again, what she already has of it counts towards the count.
  const already = [...itemCounts()].filter(([type]) => counted(type))
    .reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
  if (wood) await fellTrees(trees, count - already, mine);
  else await gatherRounds(resource, count - already, mine, first.open);
  if (task !== mine) return; // told to stop in the meantime

  const { total, delivered } = await deliverGains(player, before, counted);
  if (task === mine) setTask(null);
  emit('collected', { player, resource, count: total, delivered });
}

// collectblock walks into each drop's own block (GoalFollow, range 0) and then waits for it to vanish, with no time
// limit: a drop on leaves, against a fence or under something low kept her there for good. So it only breaks: the
// drops of what it breaks are noted here instead of chased, and picked up after (pickUp, GoalPickUp, PickUpTry each).
let gatheredDrops = null;
const appendTarget = CollectTargets.prototype.appendTarget;
CollectTargets.prototype.appendTarget = function (target) {
  if (target && target.name === 'item') {
    if (gatheredDrops) gatheredDrops.add(target.id);
    return;
  }
  appendTarget.call(this, target);
};

// Whether an item that just appeared fell from breaking `block`: within 2 blocks of it sideways (the item's data can
// come a moment after it started rolling), from a block under it up to ColumnHigh above (a column's pieces drop where
// each piece was, up to 16 high for bamboo; within 2 in all directions, they were left lying).
const ColumnHigh = 17;

function dropOf(entity, block) {
  const p = entity.position;
  const c = block.position.offset(0.5, 0, 0.5);
  return Math.hypot(p.x - c.x, p.z - c.z) <= 2 && p.y >= block.position.y - 1.5 && p.y <= block.position.y + ColumnHigh;
}

// Breaks `blocks`, then picks up what they dropped. `what` names it in the log. Solid blocks go to collectblock (with
// gatheringMovements). Blocks with no collision box (wheat, carrots, potatoes, beetroot) can't: it walks to where a
// ray from her eyes hits the block, and a ray goes through those, so it searched for 5 s and gave up on the harvest.
// `how`, for a tree (fell, unstack): `drops` gets the drops and the caller picks them up (she may be up a pillar);
// `placed`, she may climb, and gets every block she places; `order` 'up' (lowest first, ending at the first she can't
// get to) or 'down' (highest first) instead of the closest first.
export async function gather(blocks, mine, what, how = {}) {
  const drops = how.drops || new Set();
  const solid = blocks.filter((b) => b.shapes && b.shapes.length > 0);
  const soft = blocks.filter((b) => !(b.shapes && b.shapes.length > 0));
  if (solid.length > 0) {
    bot.collectBlock.movements = gatheringMovements(solid, Boolean(how.placed));
    gatheredDrops = drops;
    // The pathfinder places through bot.placeBlock: where each block went, while it stands.
    const placeBlock = bot.placeBlock;
    // It leaves every node from the first block to break or place on at the block's corner (its post-processing stops
    // there), and a step's « arrived » is within 0.35 of the node: on top of each block of her pillar she walked for the
    // corner of the block she stood on. The middle of the block.
    const centred = (result) => {
      for (const node of result.path)
        if (Number.isInteger(node.x) && Number.isInteger(node.z)) {
          node.x += 0.5;
          node.z += 0.5;
        }
    };
    if (how.placed) bot.on('path_update', centred);
    // A step up her pillar is a jump with the block set under her feet, and the pathfinder sets it as soon as her feet
    // are above the block under her, inside the new block's space: the server refused it, two times in three, and
    // each refusal cost a new search and its stuck timer. The block goes in once her feet are above it, at the top of
    // the jump.
    if (how.placed)
      bot.placeBlock = async (ref, face, ...rest) => {
        const at = ref.position.plus(face);
        const p = bot.entity.position;
        if (face.y === 1 && Math.floor(p.x) === at.x && Math.floor(p.z) === at.z)
          for (let i = 0; i < 10 && bot.entity.position.y < at.y + 1.02 && bot.entity.velocity.y > 0; i++) await wait(50);
        // Noted before it's tried: Mineflayer gives up on a block whose update comes late, which the server set all the
        // same (unstack takes down only what stands).
        if (!how.placed.some((q) => q.equals(at))) how.placed.push(at);
        return placeBlock.call(bot, ref, face, ...rest);
      };
    // collectblock only notes a drop that appears within half a block of the block's centre; a lump of coal fell
    // further and stayed lying. Within 2, as for crops.
    const noteDrop = (entity) => {
      if (solid.some((b) => dropOf(entity, b))) drops.add(entity.id);
    };
    bot.on('itemDrop', noteDrop);
    // One at a time, the closest first: collectblock gives up on the whole list when one block can't be reached.
    const left = [...solid];
    try {
      while (left.length > 0 && task === mine) {
        const me = bot.entity.position;
        const closer = (a, b) => a.position.distanceTo(me) - b.position.distanceTo(me);
        left.sort(how.order === 'up' ? (a, b) => a.position.y - b.position.y || closer(a, b)
          : how.order === 'down' ? (a, b) => b.position.y - a.position.y || closer(a, b) : closer);
        const block = left.shift();
        // Out of reach from the ground, with drops lying there: back to the caller, who picks them up before she climbs
        // (they'd wait for the pillar to come down, and a drop is gone after 5 minutes).
        if (how.order === 'up' && how.placed && pillar(how.placed).length === 0 && drops.size > 0
          && block.position.y - Math.floor(bot.entity.position.y) >= ReachUp) break;
        try {
          await bot.collectBlock.collect(block, { ignoreNoPath: true });
        } catch (err) {
          log(`${what}: skipped ${block.name} at ${block.position} (she was at ${bot.entity.position.floored()}):`,
            err && err.message ? err.message : String(err));
        }
        const still = bot.blockAt(block.position);
        if (how.order === 'up' && still && still.type === block.type) break;
      }
    } finally {
      gatheredDrops = null;
      bot.removeListener('itemDrop', noteDrop);
      bot.placeBlock = placeBlock;
      bot.removeListener('path_update', centred);
      bot.pathfinder.setMovements(gentleMovements());
    }
  }
  if (soft.length > 0 && task === mine) await breakSoft(soft, mine, drops);
  if (!how.drops && task === mine && drops.size > 0) await pickUp((e) => drops.has(e.id), Math.min(60000, drops.size * PickUpTry), mine);
}

// No tool in her hand: crops break at a touch, so by hand, the way a player does it (she harvested a field with the
// axe from her last order still out). Holding seeds or bread is as good as a hand. An empty hotbar slot, else one with
// no tool in it; only the hotbar is switched, never the pockets rearranged (unequipping with a full hotbar moved
// things by window clicks, and her inventory went out of step with the server's: no seed to replant, none to hand
// over). Melons and pumpkins are solid and go to collectblock, which takes the axe.
const Cuts = /_(axe|pickaxe|shovel|hoe|sword)$|^(shears|trident|mace)$/;

function emptyHand() {
  if (!bot.heldItem || !Cuts.test(bot.heldItem.name)) return;
  const hotbar = bot.inventory.slots.slice(bot.inventory.hotbarStart, bot.inventory.hotbarStart + 9);
  let i = hotbar.findIndex((item) => !item);
  if (i < 0) i = hotbar.findIndex((item) => !Cuts.test(item.name));
  if (i >= 0) bot.setQuickBarSlot(i);
}

// Set on spawn over bot.tool.equipForBlock, which collectblock calls before every block: a tool only when one breaks
// it faster than a hand does, else the hand (emptyHand). The tool plugin keeps what she holds when nothing beats it,
// so she cut cacti with the pickaxe from her last order and bamboo with an axe; a cactus breaks as fast by hand, and
// bamboo goes to the sword.
export function toolOrHand(equipForBlock) {
  return async (block, options = {}, cb) => {
    await landed();
    const byHand = bot.tool.getDigTime(block, null);
    const faster = bot.inventory.items().some((i) => block.canHarvest(i.type) && bot.tool.getDigTime(block, i) < byHand);
    if (faster || !block.canHarvest(null)) return equipForBlock(block, options, cb);
    emptyHand();
    if (cb) cb();
  };
}

// Off the ground, a block takes five times as long to break, and Mineflayer times the dig from when it starts: right
// after a step up her pillar, still in the jump, a log took 16 s instead of 3. She lands first.
async function landed() {
  bot.setControlState('jump', false);
  for (let i = 0; i < 20 && !bot.entity.onGround; i++) await wait(50);
}

// The blocks with no collision box, the closest first: within 2 blocks of each (PickUpTry to get there), broken by
// hand (emptyHand). What falls from them goes in `drops`.
async function breakSoft(blocks, mine, drops) {
  const left = [...blocks];
  const noteDrop = (entity) => {
    if (blocks.some((b) => dropOf(entity, b))) drops.add(entity.id);
  };
  bot.on('itemDrop', noteDrop);
  try {
    while (task === mine && left.length > 0) {
      const me = bot.entity.position;
      left.sort((a, b) => a.position.distanceTo(me) - b.position.distanceTo(me));
      const target = left.shift();
      const p = target.position;
      try {
        await Promise.race([
          bot.pathfinder.goto(new goals.GoalNear(p.x, p.y, p.z, 2)),
          wait(PickUpTry).then(() => { throw new Error('too slow'); }),
        ]);
        const now = bot.blockAt(p);
        if (task === mine && now && now.type === target.type) {
          emptyHand();
          await bot.dig(now, true);
        }
      } catch {
        if (task === mine) bot.pathfinder.setGoal(null); // out of reach, or too slow: the next one
      }
    }
    await wait(500); // the last drops land
  } finally {
    bot.removeListener('itemDrop', noteDrop);
  }
}

// What she gained since `before` (an itemCounts()), brought back: she walks up to the player and tosses it at
// their feet. Kept on her when the player is out of sight or the walk fails. `counts`: which item types make the
// total (all of them by default); everything gained is handed over.
export async function deliverGains(player, before, counts = () => true) {
  const after = itemCounts();
  const gained = [...after].map(([type, n]) => [type, n - (before.get(type) || 0)]).filter(([, n]) => n > 0);
  const total = gained.filter(([type]) => counts(type)).reduce((sum, [, n]) => sum + n, 0);
  let delivered = false;
  const owner = playerEntity(player);
  if (owner && total > 0) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(owner.position.x, owner.position.y, owner.position.z, 2));
      await bot.lookAt(owner.position.offset(0, 1.6, 0), true);
      // Whole stacks only, the smallest first: toss() with a count drops one item per click, and on 26.1 only the first
      // landed while her inventory went out of step with the server's (1 wheat of 25 reached Papa, then the seeds
      // were « not found »). A stack that's partly hers from before stays whole with her.
      for (const [type, n] of gained) {
        let left = n;
        for (const stack of bot.inventory.items().filter((i) => i.type === type).sort((a, b) => a.count - b.count)) {
          if (stack.count > left) continue;
          await bot.tossStack(stack);
          left -= stack.count;
        }
      }
      delivered = true;
    } catch (err) {
      log('delivery failed:', err && err.message ? err.message : String(err));
    }
  }
  return { total, delivered };
}
