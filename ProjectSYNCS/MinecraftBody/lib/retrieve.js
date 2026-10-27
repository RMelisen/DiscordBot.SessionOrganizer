// Getting her things back after a death.

import pathfinderPackage from 'mineflayer-pathfinder';
import { itemCounts } from './gather.js';
import { wornArmour } from './inventory.js';
import { stay } from './orders.js';
import { pickUpDrops } from './pickup.js';
import { dimension } from './seek.js';
import { bot, emit, setTask, task, wait } from './state.js';

const { goals } = pathfinderPackage;

// She dies: where, in which dimension, and whether it was by lava (her things burn). Three seconds after respawning,
// she goes back for them (`retrieve`, ahead of the routine) unless they're in another dimension, over RetrieveFar
// away, in the void or in lava. An order comes first; the walk waits until DespawnAfter (when the items vanish). At the
// spot she sweeps the items within 6 blocks for RetrieveSweep. Armour goes back on by itself (playerCollect).
const RetrieveFar = 500;
const DespawnAfter = 300000;
const RetrieveSweep = 20000;
const RetrieveRadius = 8; // what a death scatters can land 6 blocks off and roll further
export let deathAt = null;
export let pendingRetrieve = null;

export function setPendingRetrieve(value) {
  pendingRetrieve = value;
}

export function minY() {
  return bot.game && Number.isFinite(bot.game.minY) ? bot.game.minY : 0;
}

function lavaNear(pos) {
  const lava = bot.registry.blocksByName.lava;
  return Boolean(lava && bot.findBlocks({ matching: lava.id, point: pos, maxDistance: 2, count: 1 }).length > 0);
}

export function onDeath() {
  pendingRetrieve = null;
  if (!bot.entity) return;
  const pos = bot.entity.position.clone();
  deathAt = { pos, dimension: dimension(), at: Date.now(), lava: lavaNear(pos) };
}

export function armRetrieve() {
  const d = deathAt;
  deathAt = null;
  if (!d || !bot.entity) return;
  // Still carrying things: the server keeps inventories (keepInventory), nothing was dropped.
  if (bot.inventory.items().length > 0 || wornArmour().length > 0) return;
  const here = bot.entity.position;
  const why = d.dimension !== dimension() ? 'otherDimension'
    : d.pos.y < minY() + 1 ? 'void'
    : d.lava ? 'lava'
    : Math.hypot(d.pos.x - here.x, d.pos.z - here.z) > RetrieveFar ? 'tooFar'
    : null;
  if (why) return emit('retrieveLost', { why });
  pendingRetrieve = { pos: d.pos, dimension: d.dimension, until: d.at + DespawnAfter, before: itemCounts(), announced: false };
}

export function startRetrieve() {
  const r = pendingRetrieve;
  if (Date.now() > r.until || r.dimension !== dimension()) {
    pendingRetrieve = null;
    return emit('retrieveLost', { why: Date.now() > r.until ? 'despawned' : 'otherDimension' });
  }
  stay();
  setTask({ kind: 'retrieve', own: true, player: '', pos: r.pos, sweeping: false });
  if (!r.announced) {
    r.announced = true;
    emit('retrieving', {});
  }
  bot.pathfinder.setGoal(new goals.GoalNear(r.pos.x, r.pos.y, r.pos.z, 1));
}

export function retrieveTick(t) {
  if (t.sweeping || !pendingRetrieve || Date.now() <= pendingRetrieve.until) return;
  stay();
  pendingRetrieve = null;
  emit('retrieveLost', { why: 'despawned' });
}

export async function sweepRetrieve(mine) {
  mine.sweeping = true;
  await pickUpDrops(mine.pos, RetrieveRadius, RetrieveSweep, mine);
  if (task !== mine) return; // an order came first: the walk is still pending
  await wait(500); // the last pickup lands
  if (task !== mine) return;
  const before = pendingRetrieve ? pendingRetrieve.before : new Map();
  pendingRetrieve = null;
  setTask(null);
  const items = [...itemCounts()].reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
  emit('retrieved', { count: items + wornArmour().length });
}
