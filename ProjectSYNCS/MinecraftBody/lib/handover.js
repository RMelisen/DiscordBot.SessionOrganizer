// Handing a player what she just got for them (from the chests, from her crafting): at their feet, whole stacks only.

import pathfinderPackage from 'mineflayer-pathfinder';
import { playerEntity } from './orders.js';
import { bot } from './state.js';

const { goals } = pathfinderPackage;

// `gained`: item type → how many she got for them. Whole stacks, the smallest first: toss() with a count drops one item
// per click, and on 26.1 put her inventory out of step with the server's. The last stack goes whole even when it's
// bigger than what's left: what she got merged with what she carried, and she hands over the lot (the spec's call).
// True when she reached them.
export async function handOver(player, gained) {
  const owner = playerEntity(player);
  if (!owner) return false;
  if (owner.position.distanceTo(bot.entity.position) > 3)
    await bot.pathfinder.goto(new goals.GoalNear(owner.position.x, owner.position.y, owner.position.z, 2));
  const there = playerEntity(player);
  if (!there) return false;
  await bot.lookAt(there.position.offset(0, 1.6, 0), true);
  for (const [type, n] of gained) {
    let left = n;
    for (const stack of bot.inventory.items().filter((i) => i.type === type).sort((a, b) => a.count - b.count)) {
      if (left <= 0) break;
      await bot.tossStack(stack);
      left -= stack.count;
    }
  }
  return true;
}
