// Ranged foes (shot back, closed in on, or hidden from), her shield, and fireballs punched back.

import pathfinderPackage from 'mineflayer-pathfinder';
import { RangedKinds } from './fight.js';
import { awayFrom, ClearBeyond, herPlayer } from './combat.js';
import { eating } from './food.js';
import { attackReadyAt, isFoe, MeleeReach, setMode } from './guard.js';
import { playerEntity } from './orders.js';
import { meta } from './ranch.js';
import { bowReady, drawing, inSight, playerInLine, shoot, sideStep } from './shooting.js';
import { bot, task } from './state.js';

const { goals } = pathfinderPackage;

// Shot from where she stands when she has a bow and it's this far or more; closer, or without a bow, closed in on
// behind her shield. A ghast or a blaze she can't get at (no bow, out of reach above her): out of its sight.
const ShootFrom = 6;
const ReachAbove = 3;
const CoverAway = 16;
// Her shield goes up against a ranged foe out to get her (aggressive: the server says no more, not even when it draws)
// and looking her way (within ShieldAim of straight at her), this close. Not while she runs in at that one: held up, it
// slows her to a crawl.
export const ShieldSight = 16;
const ShieldAim = Math.cos((30 * Math.PI) / 180);
// An axe hit on her shield disables it this long (the game's rule).
const ShieldDisabled = 5000;
let shielding = false;
let shieldDisabledUntil = 0;

// An axe hit on her shield (entity_status 30): down, and no use raising it for a while.
export function shieldBroken() {
  shieldDisabledUntil = Date.now() + ShieldDisabled;
  shielding = false;
}

export function isRanged(e) {
  return RangedKinds.has(e.name) || (e.name === 'drowned' && Boolean(e.heldItem) && e.heldItem.name === 'trident');
}

function hasShield() {
  const off = bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')];
  return Boolean(off && off.name === 'shield');
}

function shieldUp() {
  if (shielding || drawing || eating || !hasShield() || Date.now() < shieldDisabledUntil) return;
  bot.activateItem(true);
  shielding = true;
}

export function shieldDown() {
  if (!shielding) return;
  shielding = false;
  try { bot.deactivateItem(); } catch { /* not raised after all */ }
}

// `e` looks her way: its head within ShieldAim of the line to her (yaw 0 faces north, as Mineflayer turns her).
function facing(e, me) {
  const yaw = Number.isFinite(e.headYaw) ? e.headYaw : e.yaw;
  const dx = me.x - e.position.x;
  const dz = me.z - e.position.z;
  const n = Math.hypot(dx, dz) || 1;
  return (-Math.sin(yaw) * dx - Math.cos(yaw) * dz) / n > ShieldAim;
}

// A ranged foe: shot from where she stands (a player in the line: a step aside first), or out of the sight of one she
// can't get at; true when that's what she does this tick. Otherwise she closes in, shield up, through the usual chase.
export function rangedStep(t, foe) {
  if (!isRanged(foe)) return false;
  const me = bot.entity.position;
  const d = foe.position.distanceTo(me);
  if (bowReady() && d >= ShootFrom && inSight(foe)) {
    shieldDown();
    if (playerInLine(foe)) setMode(t, 'aside', foe, sideStep(foe), false);
    else {
      setMode(t, 'shoot', foe, null);
      shoot(foe);
    }
    return true;
  }
  if ((foe.name === 'ghast' || foe.name === 'blaze') && foe.position.y - me.y > ReachAbove) {
    if (!inSight(foe)) setMode(t, 'cover', foe, null);
    else if ((t.mode !== 'cover' || !bot.pathfinder.isMoving()) && Date.now() >= (t.replanAt || 0)) {
      // By the one she guards or falls back to, on their far side from it: away from them, a blaze kept her hopping
      // off, 60 blocks from Papa. Alone, well away from it.
      const by = coverBy(t);
      const spot = by ? awayFrom(by, foe.position, ClearBeyond) : awayFrom(me, foe.position, CoverAway);
      t.mode = null;
      t.replanAt = Date.now() + Replan;
      setMode(t, 'cover', foe, new goals.GoalNear(spot.x, spot.y, spot.z, 2));
    }
    return true;
  }
  return false;
}

// Where she takes cover near: the player she guards, the spot she patrols, or the player she'd fall back to.
function coverBy(t) {
  if (t.kind === 'guard') return (playerEntity(t.player) || {}).position || null;
  if (t.kind === 'patrol') return t.anchor;
  const p = t.kind === 'fight' ? herPlayer(t) : null;
  return p ? p.position : null;
}

// A spot she's sent to (a hop away, cover) is set again at most this often: with no way there, it was set on every tick,
// a whole search each time.
export const Replan = 1000;

// Every physics tick in a fight: her shield up against a ranged foe looking her way while nothing is in reach, while
// she closes in on one, and between her hits on what's in reach; down to hit, eat, draw, or once the fight's over.
export function shieldTick() {
  const t = task;
  if (!hasShield() || !t || !['guard', 'patrol', 'defend', 'fight'].includes(t.kind) || eating || drawing) return shieldDown();
  const me = bot.entity.position;
  const melee = bot.nearestEntity((e) => e !== bot.entity && e.name !== 'creeper' && (isFoe(e) || (t.foes && t.foes.has(e.id)))
    && e.position.distanceTo(me) < MeleeReach + 0.5);
  const target = t.mode === 'fight' ? t.modeTarget : null;
  const aimed = bot.nearestEntity((e) => isRanged(e) && aiming(e) && e.position.distanceTo(me) < ShieldSight && facing(e, me)
    && inSight(e) && !(e === target && e.position.distanceTo(me) >= MeleeReach));
  // Between her hits on what's in reach, when there's time for it to come up (it guards a quarter second after).
  const between = melee && Date.now() < attackReadyAt - ShieldRaise;
  if (aimed || between) shieldUp();
  else shieldDown();
}

// Out to get someone: the aggressive bit of its mob flags, set while it has a target (the server sends no bow drawn: a
// skeleton's hand-active bit never comes). A ghast, a blaze or a breeze counts whenever it faces her.
const ShieldRaise = 300;

function aiming(e) {
  if (e.name === 'ghast' || e.name === 'blaze' || e.name === 'breeze') return true;
  return (Number(meta(e, 'mob_flags')) & 0x04) !== 0;
}

// A ghast's fireball coming at her is hit back: a punch sends it the way she looks, and she looks at it, so it goes
// back where it came from. Her client hears where a fireball is only every few ticks (it moves 5 blocks in between), so
// where it is now is guessed from its last two positions; she swings at the guess from FireballReach on, every tick
// (FireballEvery: it covers 1.6 blocks in two), for FireballSwing at most. A swing out of reach is ignored by the
// server, and a second one that lands sends it on the same way. Swinging only from 4 blocks, the one swing in reach
// landed at under a block from her, too late.
const FireballNear = 16;
const FireballReach = 7;
const FireballEvery = 50;
const FireballSwing = 1000;
const fireballs = new Map();

export function fireballTick() {
  if (!bot.entity) return;
  for (const id of fireballs.keys()) if (!bot.entities[id]) fireballs.delete(id);
  const me = bot.entity.position.offset(0, 1, 0);
  const fb = bot.nearestEntity((e) => e.name === 'fireball' && e.position.distanceTo(me) < FireballNear);
  if (!fb) return;
  const now = Date.now();
  if (!fireballs.has(fb.id)) fireballs.set(fb.id, { last: fb.position.clone(), lastAt: now, speed: null, since: null, at: 0 });
  const seen = fireballs.get(fb.id);
  if (!fb.position.equals(seen.last)) {
    const dt = (now - seen.lastAt) / 1000;
    if (dt > 0) seen.speed = fb.position.minus(seen.last).scaled(1 / dt);
    seen.last = fb.position.clone();
    seen.lastAt = now;
  }
  const guess = seen.speed ? seen.last.plus(seen.speed.scaled((now - seen.lastAt) / 1000)) : fb.position;
  if (seen.since === null) {
    if (guess.distanceTo(me) >= FireballReach) return;
    seen.since = now;
  }
  if (now - seen.since > FireballSwing || now - seen.at < FireballEvery) return;
  seen.at = now;
  shieldDown();
  bot.lookAt(guess, true).catch(() => {});
  bot.attack(fb);
}
