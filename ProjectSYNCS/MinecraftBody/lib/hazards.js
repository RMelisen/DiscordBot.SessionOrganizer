// Fire and lava: out of them by her own keys, to water when she burns.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { fireStates, lavaStates, waterStates } from './blocks.js';
import { riding } from './boats.js';
import { canSelfDefend } from './combat.js';
import { consumeNow, FireResistPotions, isPotion } from './emergency.js';
import { eating } from './food.js';
import { isOrder, stay } from './orders.js';
import { meta } from './ranch.js';
import { resumeAfterBreak, resumeFor } from './resume.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// In lava or fire she steers herself out (the pathfinder avoids both and won't plan from inside them: in a ring of fire
// she had no way to the water), to the closest standable block within LavaExit, by her own keys, like waterTick: what
// she was doing stays hers. On fire on dry land, she
// makes for water within WaterFind (HazardFor at most), through resumeFor like a fight: an errand that can't wait is
// dropped. A fire resistance potion she carries is drunk at once in either. Not asleep or in a boat.
const LavaExit = 4;
const WaterFind = 8;
const HazardFor = 10000;
const FireResistDrinkEvery = 10000;
let lavaSteering = false;
let fireResistAt = 0;

function onFire() {
  return (Number(meta(bot.entity, 'shared_flags')) & 0x01) !== 0;
}

function stateAt(pos, dy = 0) {
  return bot.world.getBlockStateId(new Vec3(Math.floor(pos.x), Math.floor(pos.y) + dy, Math.floor(pos.z)));
}

function inLava() {
  const p = bot.entity.position;
  return lavaStates.has(stateAt(p)) || lavaStates.has(stateAt(p, 1));
}

function inFire() {
  const p = bot.entity.position;
  return fireStates.has(stateAt(p)) || fireStates.has(stateAt(p, 1));
}

function inWater() {
  const p = bot.entity.position;
  return waterStates.has(stateAt(p)) || waterStates.has(stateAt(p, 1));
}

// A fire resistance potion she carries, once (FireResistDrinkEvery), unless she has the effect already.
function drinkFireResist() {
  if (eating || Date.now() < fireResistAt || bot.entity.effects[11]) return;
  const potion = bot.inventory.items().find((i) => isPotion(i, FireResistPotions));
  if (!potion) return;
  fireResistAt = Date.now() + FireResistDrinkEvery;
  consumeNow(potion).catch(() => {});
}

// The closest block within LavaExit she can stand on: solid, with room above it, no lava or fire in that room.
function lavaExit() {
  const me = bot.entity.position;
  let best = null;
  for (let dx = -LavaExit; dx <= LavaExit; dx++)
    for (let dz = -LavaExit; dz <= LavaExit; dz++)
      for (let dy = -1; dy <= 1; dy++) {
        const at = me.offset(dx, dy, dz).floored();
        const floor = bot.blockAt(at.offset(0, -1, 0));
        if (!floor || floor.boundingBox !== 'block' || lavaStates.has(floor.stateId)) continue;
        const room = [0, 1].map((h) => bot.blockAt(at.offset(0, h, 0)));
        if (room.some((b) => !b || b.boundingBox !== 'empty' || lavaStates.has(b.stateId) || fireStates.has(b.stateId))) continue;
        const d = at.offset(0.5, 0, 0.5).distanceTo(me);
        if (!best || d < best.d) best = { at, d };
      }
  return best && best.at;
}

function waterSpot() {
  const water = bot.registry.blocksByName.water;
  if (!water) return null;
  return bot.findBlocks({ matching: water.id, point: bot.entity.position, maxDistance: WaterFind, count: 16 })
    .map((p) => bot.blockAt(p.offset(0, 1, 0)) && bot.blockAt(p.offset(0, 1, 0)).boundingBox === 'empty' ? p : null)
    .find(Boolean) || null;
}

// Every physics tick, after her swimming.
export function hazardTick() {
  if (!bot.entity || bot.isSleeping || riding || bot.health <= 0) return;
  const lava = inLava();
  const burning = lava || onFire();
  if (burning) drinkFireResist();
  if (lava || inFire()) {
    const out = lavaExit();
    if (!out) return;
    lavaSteering = true;
    bot.lookAt(out.offset(0.5, 0.5, 0.5), true).catch(() => {});
    bot.setControlState('forward', true);
    bot.setControlState('jump', true);
    bot.setControlState('sprint', true);
    return;
  }
  if (lavaSteering) {
    lavaSteering = false;
    for (const key of ['forward', 'jump', 'sprint']) bot.setControlState(key, false);
  }
  const t = task;
  if (t && t.kind === 'hazard') {
    if (!burning || inWater() || Date.now() > t.until) endHazard(t);
    return;
  }
  if (!burning || inWater() || !canSelfDefend() || (t && t.kind === 'dodge')) return;
  const water = waterSpot();
  if (!water) return;
  const resume = resumeFor(t);
  if (t && !resume && isOrder(t) && t.kind !== 'seek' && t.kind !== 'sleep') emit('dropped', { player: t.player, kind: t.kind, by: '' });
  stay();
  setTask({ kind: 'hazard', player: '', resume, until: Date.now() + HazardFor });
  bot.pathfinder.setGoal(new goals.GoalNear(water.x, water.y, water.z, 0));
}

export function endHazard(t) {
  if (task !== t) return;
  setTask(null);
  bot.pathfinder.setGoal(null);
  resumeAfterBreak(t.resume);
}
