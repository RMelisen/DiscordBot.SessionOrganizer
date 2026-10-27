// « ramène-moi du fer du coffre »: goods from the chests and barrels near her or the asker, to the asker's feet. Any
// chest, no owner check (the owner's call); a chest is opened, emptied of what was asked and closed, never broken.
// An errand like handing over: an activity pauses and comes back after.

import pathfinderPackage from 'mineflayer-pathfinder';
import { handOver } from './handover.js';
import { Goods } from './inventory.js';
import { setHandedAt } from './needs.js';
import { pausedActivity, playerEntity, resumeActivity, stay } from './orders.js';
import { bot, emit, log, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// Within this of her or of the asker (the radius `store` uses), the closest to her first.
const FetchRadius = 16;
const Containers = ['chest', 'barrel', 'trapped_chest'];
// No number asked: everything of that kind she finds, up to a stack.
const FetchMax = 64;

function chestsNear(target) {
  const ids = bot.registry.blocksArray.filter((b) => Containers.includes(b.name)).map((b) => b.id);
  const seen = new Set();
  return [
    ...bot.findBlocks({ matching: ids, maxDistance: FetchRadius, count: 8 }),
    ...bot.findBlocks({ matching: ids, maxDistance: FetchRadius, count: 8, point: target.position }),
  ].filter((p) => {
    const key = `${p.x},${p.y},${p.z}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  }).sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
}

// `count` 0: up to FetchMax. Everything that can fail at once (out of sight, no chest) is checked before stay().
export async function fetchFromChests(player, key, count) {
  const matches = Goods[key];
  if (!matches || key === 'all') return;
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  const chests = chestsNear(target);
  if (chests.length === 0) return emit('cantFetch', { player, goods: key, why: 'noChest' });
  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'fetch', player };
  setTask(mine);
  emit('fetching', { player, goods: key });

  const want = count > 0 ? Math.min(count, FetchMax) : FetchMax;
  const taken = new Map();
  let total = 0;
  let full = false;
  for (const at of chests) {
    if (total >= want || full || task !== mine) break;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
    } catch (err) {
      log('fetch: no way to a chest:', err && err.message ? err.message : String(err));
      continue;
    }
    const block = bot.blockAt(at);
    if (task !== mine || !block || !Containers.includes(block.name)) continue;
    let box = null;
    try {
      box = await bot.openContainer(block);
      for (const item of box.containerItems().filter((i) => matches(i.name))) {
        if (total >= want) break;
        const take = Math.min(item.count, want - total);
        try {
          await box.withdraw(item.type, null, take);
        } catch {
          full = true; // her pockets
          break;
        }
        taken.set(item.type, (taken.get(item.type) || 0) + take);
        total += take;
      }
    } catch (err) {
      log('fetch: a chest failed:', err && err.message ? err.message : String(err));
    } finally {
      if (box) box.close();
    }
  }
  if (task !== mine) return; // a fight took her: it drops the errand and tells them (combat.js)

  let delivered = false;
  if (total > 0) {
    try {
      delivered = await handOver(player, taken);
    } catch (err) {
      log('fetch: handing over failed:', err && err.message ? err.message : String(err));
    }
  }
  setHandedAt(Date.now());
  if (task !== mine) return;
  setTask(null);
  if (total === 0) emit('cantFetch', { player, goods: key, why: 'none' });
  else emit('fetched', { player, goods: key, count: total, asked: count, delivered });
  resumeActivity(back);
}
