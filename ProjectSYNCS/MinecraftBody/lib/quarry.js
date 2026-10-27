// Quarrying (stone, for « va chercher de la pierre », the camp's cobblestone and a craft's stone): one face dug into,
// everything in arm's reach dug straight, a walk only when nothing is left in reach.

import vec3 from 'vec3';
import { dropOf, exposed, gather, GatherRadius, Resources } from './gather.js';
import { pickUp, PickUpTry } from './pickup.js';
import { bot, log, task, wait } from './state.js';
import { posKey } from './trees.js';
import { isWater } from './water.js';

const { Vec3 } = vec3;

// Stone, the way a player gets it: one spot, dug into. Block by block, each the closest open stone anywhere within
// GatherRadius and a pathfinder search and a walk apiece, she hopped between surface blocks all over a hillside. Now she
// starts at a face with stone behind it (quarryFace), mines everything she sees in arm's reach from where she stands
// (QuarryReach, straight, no search), and only walks (collectblock, the closest open stone) when nothing is left in
// reach: what she dug opened the next face, so the walk is a step into her own hole. Never under her feet, nor lower
// than QuarryDeep under where the quarry began: at two, she dug a block's floor out and then the one under it, a 1x1
// pit two deep, and spent a 5 s search on getting to its bottom; one deep is a step anyone walks out of. Never a block
// by lava or water (it flows in), under sand or gravel (it falls on her) or over a hole (she'd fall through what she
// dug).
const QuarryReach = 4.4;
const QuarryDeep = 1;
const QuarryMisses = 3; // walks in a row that broke nothing, and she stops
const QuarryFaces = 32; // the open stone weighed for where to start
const Falls = (n) => ['sand', 'red_sand', 'gravel', 'suspicious_sand', 'suspicious_gravel'].includes(n) || n.endsWith('_concrete_powder');
const Sides = [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]];

// Nothing that flows or falls into the hole, and no hole under a block dug at her floor's level.
function quarrySafe(block, floor) {
  const p = block.position;
  if (p.y < floor) return false;
  for (const [dx, dy, dz] of Sides) {
    const near = bot.blockAt(p.offset(dx, dy, dz));
    if (!near || near.name === 'lava' || isWater(near)) return false;
  }
  const above = bot.blockAt(p.offset(0, 1, 0));
  if (Falls(above.name)) return false;
  const feet = Math.floor(bot.entity.position.y);
  // The quarry's bottom too, wherever she stands: from inside her own pit, the ground over a cave was at her feet's
  // level, and she dug through into it.
  if (p.y < feet || p.y === floor) {
    const below = bot.blockAt(p.offset(0, -1, 0));
    if (!below || below.boundingBox !== 'block') return false;
  }
  if (p.y < feet) {
    // What she stands on: her box is 0.6 wide, and may stand on up to four blocks.
    const me = bot.entity.position;
    if (p.x <= me.x + 0.3 && p.x + 1 >= me.x - 0.3 && p.z <= me.z + 0.3 && p.z + 1 >= me.z - 0.3) return false;
  }
  return true;
}

// A face of `block` open to air that her eyes see: what she can mine from here, as a player would.
function seenFrom(eyes, block) {
  const mid = block.position.offset(0.5, 0.5, 0.5);
  return Sides.some(([dx, dy, dz]) => {
    const near = bot.blockAt(block.position.offset(dx, dy, dz));
    if (!near || near.boundingBox !== 'empty') return false;
    const line = mid.offset(dx * 0.49, dy * 0.49, dz * 0.49).minus(eyes);
    const length = line.norm();
    const hit = bot.world.raycast(eyes, line.scaled(1 / length), length + 0.1);
    return hit && hit.position.equals(block.position);
  });
}

// The closest stone in reach that she sees and may dig, or null. Her floor's level last (FloorLast): it's always the
// closest to her eyes, and on stone ground she'd pock the ground round her feet before turning to the face.
const FloorLast = 10;

function stoneInReach(states, floor, skip) {
  const eyes = bot.entity.position.offset(0, 1.62, 0);
  const feet = Math.floor(bot.entity.position.y);
  const r = Math.ceil(QuarryReach);
  let best = null;
  let bestAt = Infinity;
  for (let dx = -r; dx <= r; dx++)
    for (let dy = -1; dy <= r; dy++)
      for (let dz = -r; dz <= r; dz++) {
        const p = new Vec3(Math.floor(eyes.x) + dx, feet + dy, Math.floor(eyes.z) + dz);
        // State ids first: a whole block for every spot of the cube, after every dig, would cost the Pi.
        if (!states.has(bot.world.getBlockStateId(p)) || skip.has(posKey(p))) continue;
        const block = bot.blockAt(p);
        if (!block) continue;
        const reach = block.position.offset(0.5, 0.5, 0.5).distanceTo(eyes);
        const at = reach + (p.y < feet ? FloorLast : 0);
        if (reach > QuarryReach || at >= bestAt || !quarrySafe(block, floor) || !seenFrom(eyes, block)) continue;
        best = block;
        bestAt = at;
      }
  return best;
}

// Where to start: of the closest open stone she sees from here, the one with the most stone around it for the walk to
// it (a cliff 15 blocks off over a lone boulder at 3), as much as she was asked for. Seen: the wall of a cave under the
// ground, sealed, is buried in stone and won over the cliff, and its search ran out of time. None in sight, the closest
// open stone.
function quarryFace(ids, left, floor, skip) {
  const stoneAround = (block) => {
    let n = 0;
    for (let dx = -2; dx <= 2; dx++)
      for (let dy = -2; dy <= 2; dy++)
        for (let dz = -2; dz <= 2; dz++) {
          const near = bot.blockAt(block.position.offset(dx, dy, dz));
          if (near && ids.has(near.type)) n++;
        }
    return n;
  };
  const me = bot.entity.position;
  const eyes = me.offset(0, 1.62, 0);
  let closest = null;
  let best = null;
  let bestScore = -Infinity;
  for (const p of bot.findBlocks({ matching: [...ids], useExtraInfo: exposed, maxDistance: GatherRadius, count: QuarryFaces })) {
    const block = bot.blockAt(p);
    if (!block || skip.has(posKey(p)) || !quarrySafe(block, floor)) continue;
    if (!closest) closest = block;
    if (!seenFrom(eyes, block)) continue;
    const score = Math.min(stoneAround(block), left * 2) - p.distanceTo(me);
    if (score > bestScore) {
      best = block;
      bestScore = score;
    }
  }
  return best || closest;
}

// Until `left` are broken, nothing open is left within GatherRadius, or QuarryMisses walks in a row broke nothing.
export async function quarry(left, mine) {
  const kinds = bot.registry.blocksArray.filter((b) => Resources.stone(b.name));
  const ids = new Set(kinds.map((b) => b.id));
  const states = new Set(kinds.flatMap((b) => Array.from({ length: b.maxStateId - b.minStateId + 1 }, (_, i) => b.minStateId + i)));
  const skip = new Set(); // what she couldn't get at or dig: never tried again
  const drops = new Set();
  const dug = [];
  const noteDrop = (entity) => {
    if (dug.some((b) => dropOf(entity, b))) drops.add(entity.id);
  };
  const collectDrops = async () => {
    if (task === mine && drops.size > 0) await pickUp((e) => drops.has(e.id), Math.min(60000, drops.size * PickUpTry), mine);
    drops.clear();
  };
  // Where the quarry begins: here if there's stone in reach, else at the first face she walks to.
  let floor = Math.floor(bot.entity.position.y) - QuarryDeep;
  let started = stoneInReach(states, floor, skip) !== null;
  let misses = 0;
  bot.on('itemDrop', noteDrop);
  try {
    while (left > 0 && task === mine && misses < QuarryMisses) {
      // In reach: straight, one after another, what each one opened included.
      for (let block; left > 0 && task === mine && (block = stoneInReach(states, floor, skip));) {
        try {
          await bot.tool.equipForBlock(block, {});
          await bot.dig(block, true);
        } catch (err) {
          log(`quarry: dig failed at ${block.position}:`, err && err.message ? err.message : String(err));
        }
        const now = bot.blockAt(block.position);
        if (now && now.type === block.type) skip.add(posKey(block.position));
        else {
          left--;
          dug.push(block);
          if (dug.length > 16) dug.shift();
        }
      }
      if (left <= 0 || task !== mine) break;
      // The drops lie in her hole: picking them up walks her into it, maybe within reach of more.
      if (drops.size > 0) {
        await collectDrops();
        if (stoneInReach(states, floor, skip)) continue;
      }
      // Nothing in reach: the next open stone, the closest once she has started, through collectblock.
      const next = !started ? quarryFace(ids, left, -Infinity, skip)
        : bot.findBlocks({ matching: [...ids], useExtraInfo: exposed, maxDistance: GatherRadius, count: QuarryFaces })
          .map((p) => bot.blockAt(p)).find((b) => b && !skip.has(posKey(b.position)) && quarrySafe(b, floor));
      if (!next) break;
      await gather([next], mine, 'quarry', { drops });
      const now = bot.blockAt(next.position);
      if (now && now.type === next.type) {
        skip.add(posKey(next.position));
        misses++;
        continue;
      }
      misses = 0;
      left--;
      dug.push(next);
      if (!started) floor = Math.floor(bot.entity.position.y) - QuarryDeep;
      started = true;
    }
    await wait(500); // the last drops land
    await collectDrops();
  } finally {
    bot.removeListener('itemDrop', noteDrop);
  }
}
