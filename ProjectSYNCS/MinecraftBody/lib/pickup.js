// Picking items up within a player's reach, and the loot of her kills.

import pathfinderPackage from 'mineflayer-pathfinder';
import { bot, log, task, wait } from './state.js';

const { goals } = pathfinderPackage;

// Where she can pick an item up from. A player picks up what touches their box (0.6 wide, 1.8 tall) grown by a block
// sideways and half a block up and down. GoalNear(item, 1) wanted her within a block of it, counted in whole blocks,
// which an item on a fence post, on a pillar's edge or under a slab too low to stand under never allows: the
// pathfinder found no way, or kept looking. Each item gets PickUpTry, then she leaves it. A node counts within
// PickUpReach of the item sideways: her box reaches 1.3 from where she stands, and leaning against what's in the way
// (pickUp) takes her 0.2 further. At 1.2, coal at the back of the hole its ore left in a quarry face had no node at all.
export const PickUpTry = 8000;
const PickUpReach = 1.5;

export class GoalPickUp extends goals.Goal {
  constructor(pos) {
    super();
    this.x = pos.x;
    this.y = pos.y;
    this.z = pos.z;
  }

  heuristic(node) {
    return Math.hypot(this.x - (node.x + 0.5), this.z - (node.z + 0.5)) + Math.abs(this.y - node.y);
  }

  isEnd(node) {
    const up = this.y - node.y;
    return Math.abs(this.x - (node.x + 0.5)) <= PickUpReach && Math.abs(this.z - (node.z + 0.5)) <= PickUpReach && up >= -0.4 && up <= 2.2;
  }
}

// Picks up the dropped items within `radius` of a spot (pickUp).
export async function pickUpDrops(center, radius, timeout, mine) {
  return pickUp((e) => e.position.distanceTo(center) <= radius, timeout, mine);
}

// Walks to the dropped items `choose` accepts, the closest first, until none is left, the time is up or she's no
// longer doing `mine`. Each item gets a try of PickUpTry, and another (three in all) if it has rolled more than a
// block since: aimed at while still rolling, it was left lying beside where she went.
export async function pickUp(choose, timeout, mine) {
  const end = Date.now() + timeout;
  const tried = new Map(); // item id -> { at, tries }
  const retry = (e) => {
    const t = tried.get(e.id);
    return !t || (t.tries < 3 && e.position.distanceTo(t.at) > 1);
  };
  while (task === mine && Date.now() < end) {
    const item = bot.nearestEntity((e) => e.name === 'item' && retry(e) && choose(e));
    if (!item) return;
    tried.set(item.id, { at: item.position.clone(), tries: (tried.get(item.id) || { tries: 0 }).tries + 1 });
    const at = item.position;
    try {
      await Promise.race([
        bot.pathfinder.goto(new GoalPickUp(at)),
        wait(Math.min(PickUpTry, Math.max(0, end - Date.now()))).then(() => { throw new Error('too slow'); }),
      ]);
      // In reach is not picked up: a fresh drop waits half a second before anyone can take it, and she had already
      // moved on to the next one, leaving it where it fell.
      for (let i = 0; i < 15 && bot.entities[item.id]; i++) await wait(100);
      // Still there, close: in a hole the ore left in a quarry face, a block up and at its back, just past the reach
      // of where she stood. She leans in against the face, as a player does, and it comes.
      if (bot.entities[item.id] && item.position.distanceTo(bot.entity.position) < 2.5 && task === mine) {
        await bot.lookAt(item.position, true);
        bot.setControlState('forward', true);
        for (let i = 0; i < 6 && bot.entities[item.id]; i++) await wait(100);
        bot.setControlState('forward', false);
      }
    } catch (err) {
      if (task === mine) bot.pathfinder.setGoal(null); // out of reach, or too slow: the next one
      if (process.env.MC_TRACE_PATHS === 'all') log(`pickUp: ${at} out of reach: ${err && err.message}`);
    }
    if (process.env.MC_TRACE_PATHS === 'all' && bot.entities[item.id]) log(`pickUp: ${item.position} still lying (she is at ${bot.entity.position.floored()})`);
    await wait(300);
  }
}

// Where something she was fighting (guarding, defending) or culling died, for LootFresh: what drops there is hers to
// pick up. Nothing else is: a player's dropped items stay where they are.
const LootFresh = 30000;
export const lootSpots = [];

export function pruneLoot() {
  while (lootSpots.length > 0 && Date.now() - lootSpots[0].at > LootFresh) lootSpots.shift();
}

// A dropped item near one of her kills, within 6 blocks of her and within `radius` of what she guards.
export function lootNear(center, radius, skip) {
  pruneLoot();
  if (lootSpots.length === 0) return null;
  const me = bot.entity.position;
  return bot.nearestEntity((e) => e.name === 'item' && !(skip && skip.has(e.id)) && e.position.distanceTo(me) <= 6
    && e.position.distanceTo(center) <= radius && lootSpots.some((s) => s.pos.distanceTo(e.position) <= 3));
}
