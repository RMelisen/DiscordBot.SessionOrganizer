// Her bow (never through a player, aimed a little high), what's in sight, and getting her weapon in hand.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { attackReadyAt, setAttackReadyAt, Weapons } from './guard.js';
import { shieldDown } from './ranged.js';
import { bot, log, wait } from './state.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

export function arrows() {
  return bot.inventory.items().some((i) => i.name.endsWith('arrow'));
}

export function bowReady() {
  return arrows() && bot.inventory.items().some((i) => i.name === 'bow');
}

// One arrow: drawn for a second (full power), aimed a little above the target to make up for the drop, aimed again
// just before letting go, since the target moves meanwhile.
export let drawing = false;

// A player (not her) within a block of the line from her eyes to `target`, between the two.
export function playerInLine(target) {
  const from = bot.entity.position.offset(0, 1.62, 0);
  const to = target.position.offset(0, (target.height || 1.7) * 0.6, 0);
  const line = to.minus(from);
  const length = line.norm();
  if (length === 0) return null;
  const dir = line.scaled(1 / length);
  return Object.values(bot.players).map((p) => p.entity).find((e) => {
    if (!e || e === bot.entity) return false;
    const mid = e.position.offset(0, 0.9, 0).minus(from);
    const along = mid.dot(dir);
    return along > 0.5 && along < length && mid.minus(dir.scaled(along)).norm() < 1;
  }) || null;
}

// Nothing solid between `from` (eyes) and `entity`'s head or chest: a monster she can see, and shoot or hit. A creeper
// in a cave under her feet was « near », and she shot arrow after arrow into the ground at it.
export function inSight(entity, from = bot.entity.position.offset(0, 1.62, 0)) {
  const h = entity.height || 1.7;
  return [0.9, 0.6].some((k) => {
    const line = entity.position.offset(0, h * k, 0).minus(from);
    const length = line.norm();
    return length < 0.01 || !bot.world.raycast(from, line.scaled(1 / length), length);
  });
}

// What a player sees from: their eyes.
export const eyesOf = (e) => e.position.offset(0, 1.62, 0);

// Three blocks to her side of the line to `target`, the side with more room: a clear shot from there.
export function sideStep(target) {
  const me = bot.entity.position;
  const dx = target.position.x - me.x;
  const dz = target.position.z - me.z;
  const n = Math.hypot(dx, dz) || 1;
  const left = new Vec3(me.x - (dz / n) * 3, me.y, me.z + (dx / n) * 3);
  const right = new Vec3(me.x + (dz / n) * 3, me.y, me.z - (dx / n) * 3);
  const near = (p) => bot.nearestEntity((e) => e.type === 'player' && e !== bot.entity && e.position.distanceTo(p) < 2);
  const spot = near(left) && !near(right) ? right : left;
  return new goals.GoalNear(spot.x, spot.y, spot.z, 1);
}

export async function shoot(target) {
  if (drawing || Date.now() < attackReadyAt) return;
  shieldDown();
  const bow = bot.inventory.items().find((i) => i.name === 'bow');
  if (!bow) return;
  drawing = true;
  const aim = () => {
    const d = target.position.distanceTo(bot.entity.position);
    return target.position.offset(0, (target.height || 1.7) * 0.6 + 0.003 * d * d, 0);
  };
  try {
    if (!bot.heldItem || bot.heldItem.name !== 'bow') await bot.equip(bow, 'hand');
    await bot.lookAt(aim(), true);
    bot.activateItem();
    await wait(1100);
    if (target.isValid) await bot.lookAt(aim(), true);
    // Someone stepped into the line while she drew: a moment for them to pass, then the arrow goes up, not into them.
    for (let i = 0; i < 10 && target.isValid && playerInLine(target); i++) {
      await wait(100);
      await bot.lookAt(aim(), true);
    }
    // And not into a wall or the ground: gone behind something while she drew, the arrow goes up too.
    if (playerInLine(target) || target.isValid === false || !inSight(target)) await bot.look(bot.entity.yaw, Math.PI / 2, true);
    bot.deactivateItem();
  } catch (err) {
    log('shot failed:', err && err.message ? err.message : String(err));
  } finally {
    drawing = false;
    setAttackReadyAt(Date.now() + 300);
  }
}

export function stopBow() {
  if (!drawing) return;
  try { bot.deactivateItem(); } catch { /* not drawing after all */ }
}

let equipping = false;

export async function equipWeapon() {
  if (equipping) return;
  const items = bot.inventory.items();
  for (const name of Weapons) {
    const item = items.find((i) => i.name === name);
    if (!item) continue;
    if (bot.heldItem && bot.heldItem.name === name) return;
    equipping = true;
    try { await bot.equip(item, 'hand'); } catch { /* keep fighting bare-handed */ } finally { equipping = false; }
    return;
  }
}
