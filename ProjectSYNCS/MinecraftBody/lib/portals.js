// Through a nether portal after the player she follows or guards, and back.

import pathfinderPackage from 'mineflayer-pathfinder';
import { FollowsSomeone } from './flee.js';
import { guard } from './guard.js';
import { gentleMovements } from './movements.js';
import { follow, playerEntity, stay } from './orders.js';
import { dimension } from './seek.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// The player she follows or guards steps into a nether portal: they vanish from her world, which read as « lost ». Where
// they vanished is a portal, so she goes after them, into it, and stands there until it takes her (4 s in survival);
// on the other side she steps out, looks for them, and goes on as she was, without a word more (the order was
// acknowledged). Back the same way. Only when they vanished by a portal, close to her (further off, they walked out of
// sight first) and still online: a logout by a portal isn't a crossing, and since the server drops the entity before
// the name, that's checked every tick until she's through (the portal takes 4 s, so she never goes alone).
const PortalNear = 2; // how close to a portal block they vanished
const PortalReach = 24; // how far from her that portal can be
const PortalTake = 20000; // to get to it and through
const PortalFind = 15000; // on the other side, to see them again
const PortalSure = 500; // still listed this long after vanishing: through, not logged out

function portalId() {
  const b = bot.registry.blocksByName.nether_portal;
  return b ? b.id : -1;
}

// The portal block someone at `pos` is standing in, the lowest of its column (where feet go), or null.
function portalAt(pos) {
  const id = portalId();
  const b = bot.findBlock({ point: pos, matching: id, maxDistance: PortalNear });
  if (!b) return null;
  let low = b;
  for (let below = bot.blockAt(low.position.offset(0, -1, 0)); below && below.type === id;
    below = bot.blockAt(low.position.offset(0, -1, 0))) low = below;
  return low;
}

// True when the player who just vanished went through a portal and she sets off after them.
export function crossAfter(entity) {
  if (!task || !FollowsSomeone.has(task.kind) || entity.username !== task.player || !bot.players[task.player]) return false;
  const portal = portalAt(entity.position);
  if (!portal || portal.position.distanceTo(bot.entity.position) > PortalReach) return false;
  const { player, kind } = task;
  stay();
  setTask({ kind: 'portal', player, then: kind, from: dimension(), crossed: false, announced: false, at: Date.now(),
    until: Date.now() + PortalTake });
  const movements = gentleMovements();
  movements.blocksToAvoid.delete(portalId());
  bot.pathfinder.setMovements(movements);
  bot.pathfinder.setGoal(new goals.GoalBlock(portal.position.x, portal.position.y, portal.position.z));
  return true;
}

// Every physics tick while crossing: through (out of the arrival portal, then looking), seen again (go on), or gone.
export function portalTick() {
  const t = task;
  if (!t || t.kind !== 'portal') return;
  if (!t.crossed && !bot.players[t.player]) { // logged out, not through
    emit('cantReach', { player: t.player, why: 'lost' });
    return stay();
  }
  // Said once they're surely through, not out: still listed half a second on.
  if (!t.announced && Date.now() > t.at + PortalSure) {
    t.announced = true;
    emit('crossing', { player: t.player });
  }
  if (!t.crossed && dimension() !== t.from) {
    t.crossed = true;
    t.until = Date.now() + PortalFind;
    bot.pathfinder.setMovements(gentleMovements());
    const at = bot.entity.position;
    bot.pathfinder.setGoal(new goals.GoalInvert(new goals.GoalNear(at.x, at.y, at.z, 2))); // out of the portal
  }
  if (playerEntity(t.player)) {
    setTask(null);
    bot.pathfinder.setMovements(gentleMovements());
    return t.then === 'guard' ? guard(t.player, true) : follow(t.player, true);
  }
  if (Date.now() > t.until) {
    emit('cantReach', { player: t.player, why: 'lost' });
    stay();
  }
}
