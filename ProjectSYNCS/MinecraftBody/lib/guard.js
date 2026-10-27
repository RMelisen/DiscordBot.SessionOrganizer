// Guarding a player and patrolling a spot: the closest monster in sight taken on, a creeper kept away from, loot
// picked up between fights (guardTick).

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { eatIfHungry } from './food.js';
import { MaxTrip, playerEntity, stay } from './orders.js';
import { GoalPickUp, lootNear, PickUpTry } from './pickup.js';
import { rangedStep } from './ranged.js';
import { dimension } from './seek.js';
import { bowReady, eyesOf, inSight, playerInLine, shoot, sideStep, stopBow } from './shooting.js';
import { bot, emit, setTask, task } from './state.js';
import { chase, fightPace, foeOnHer, reachable, strike } from './strike.js';
import { headUnder } from './water.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// Monsters she fights: what minecraft-data files as hostile, and the few it files elsewhere (slimes, magma cubes and
// phantoms as « mob », hoglins as « animal »). Neutral ones that only turn hostile when hit (endermen, piglins) are
// left alone: starting that fight would be the opposite of protecting anyone. So are the warden (two hits kill her,
// and a fight only angers it more) and the creaking (nothing hurts it while its heart stands).
const Peaceful = new Set(['enderman', 'zombified_piglin', 'piglin', 'warden', 'creaking']);
const AlsoFoes = new Set(['slime', 'magma_cube', 'phantom', 'hoglin', 'ghast']);
const GuardRadius = 10;
export const MeleeReach = 3.2;
export const Weapons = ['netherite_sword', 'diamond_sword', 'iron_sword', 'stone_sword', 'golden_sword', 'wooden_sword',
  'netherite_axe', 'diamond_axe', 'iron_axe', 'stone_axe', 'golden_axe', 'wooden_axe'];
export let attackReadyAt = 0;

export function setAttackReadyAt(value) {
  attackReadyAt = value;
}

export function isFoe(entity) {
  return Boolean(entity) && (entity.type === 'hostile' || AlsoFoes.has(entity.name)) && !Peaceful.has(entity.name)
    && entity.isValid !== false;
}

// Guarding a place rather than a player: monsters within PatrolRadius of the spot.
const PatrolRadius = 16;
// Under this much health (3 hearts) she stops fighting and backs off until she has healed, or RetreatFor has passed.
export const RetreatHealth = 6;
const RetreatFor = 20000;
// A creeper is never hit by hand: its blast would land on the player she guards. With a bow and arrows she shoots it
// from at least CreeperRange; without, she keeps out of its reach and lets the player deal with it.
export const CreeperRange = 6;
const CreeperSight = 16;
// A monster she chases that stays out of sight this long (gone round a corner for good, down a hole) is let go.
const FoeUnseen = 4000;

export function guard(player, quiet = false) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  setTask({ kind: 'guard', player, foe: null, mode: null, modeTarget: null, warned: new Set(), skipLoot: new Set(), reported: false });
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  if (!quiet) emit('moving', { player, kind: 'guard' });
}

// « syncs surveille la base »: where she stands, or the remembered spot C# sends.
export function patrol(player, spot) {
  let anchor = bot.entity.position.floored();
  if (spot) {
    if (spot.dimension !== dimension()) return emit('cantPatrol', { player, why: 'otherDimension' });
    if (Math.hypot(spot.x - anchor.x, spot.z - anchor.z) > MaxTrip) return emit('cantPatrol', { player, why: 'tooFar' });
    anchor = new Vec3(spot.x, spot.y, spot.z);
  }
  stay();
  setTask({ kind: 'patrol', player, anchor, foe: null, mode: null, modeTarget: null, warned: new Set(), skipLoot: new Set(), reported: false });
  emit('patrolling', { player });
}

// Changes where she's heading only when what she's doing changes: a goal set on every tick would never be walked.
export function setMode(t, mode, target, goal, dynamic) {
  if (t.mode === mode && t.modeTarget === target) return;
  t.mode = mode;
  t.modeTarget = target;
  t.modeSince = Date.now();
  bot.pathfinder.setGoal(goal, dynamic);
}

// Every physics tick while guarding a player or a place, in this order: back off when badly hurt; deal with a
// creeper (shoot it, or keep out of its reach); otherwise pick the closest monster near the player or the spot (or
// near her), chase it, hit it when in reach, then go back to following or to the spot.
export function guardTick() {
  const t = task;
  if (!t || (t.kind !== 'guard' && t.kind !== 'patrol')) return;
  const owner = t.kind === 'guard' ? playerEntity(t.player) : null;
  if (t.kind === 'guard' && !owner) {
    emit('cantReach', { player: t.player, why: 'lost' });
    stay();
    return;
  }
  const me = bot.entity.position;
  const center = owner ? owner.position : t.anchor;
  const radius = owner ? GuardRadius : PatrolRadius;
  const goHome = () => {
    fightPace(false);
    // What dropped from her kills first, when it's close and inside what she guards.
    const loot = lootNear(center, radius, t.skipLoot);
    if (loot && t.mode === 'loot' && t.modeTarget === loot && Date.now() - t.modeSince > PickUpTry) {
      t.skipLoot.add(loot.id); // a way there, but never close enough: left where it is
      return goHome();
    }
    if (loot) return setMode(t, 'loot', loot, new GoalPickUp(loot.position));
    if (owner) return setMode(t, 'home', owner, new goals.GoalFollow(owner, 2), true);
    // Back to the spot; again if pushed off it after getting there (a reached goal is dropped).
    if (t.mode !== 'home' || (!bot.pathfinder.goal && me.distanceTo(t.anchor) > 3)) {
      t.mode = 'home';
      t.modeTarget = null;
      bot.pathfinder.setGoal(new goals.GoalNear(t.anchor.x, t.anchor.y, t.anchor.z, 2));
    }
  };

  // Badly hurt: away from the closest monster until healed, eating to heal faster.
  if (!t.retreatUntil && bot.health <= RetreatHealth && Date.now() >= (t.retreatAgainAt || 0)) {
    t.retreatUntil = Date.now() + RetreatFor;
    t.foe = null;
    stopBow();
    emit('retreat', { player: t.player });
    eatIfHungry(20).catch(() => {});
  }
  if (t.retreatUntil) {
    if (bot.health >= 14 || Date.now() > t.retreatUntil) {
      t.retreatUntil = null;
      t.retreatAgainAt = Date.now() + 30000;
    } else {
      const threat = bot.nearestEntity((e) => isFoe(e) && e.position.distanceTo(me) < 10);
      if (threat) setMode(t, 'retreat', threat, new goals.GoalInvert(new goals.GoalFollow(threat, 12)), true);
      else goHome();
      // Backing off, she still hits what keeps up with her: with nowhere to go (a ledge), she stood and took it.
      const onHer = foeOnHer();
      if (onHer) strike(onHer);
      return;
    }
  }

  // Creepers: warned about once each, shot from range, never hit by hand. Seen from CreeperSight: one walks 10 blocks
  // in about 3 s, two shots at most, and it takes three to kill (it blew up by Papa that way on the local server).
  // Only one in sight, hers or the guarded player's: one in a cave under them is no threat, and can't be shot.
  const creeper = bot.nearestEntity((e) => e.name === 'creeper' && e.isValid !== false
    && (e.position.distanceTo(center) < CreeperSight || e.position.distanceTo(me) < CreeperSight)
    && (inSight(e) || (owner && inSight(e, eyesOf(owner)))));
  if (creeper) {
    if (!t.warned.has(creeper.id)) {
      t.warned.add(creeper.id);
      emit('creeper', { player: t.player });
    }
    const d = creeper.position.distanceTo(me);
    if (d < CreeperRange) return setMode(t, 'flee', creeper, new goals.GoalInvert(new goals.GoalFollow(creeper, CreeperRange + 3)), true);
    if (bowReady() && inSight(creeper)) {
      // Never through a player: standing behind the one she guards, she put four arrows in his back while the
      // creeper walked up to him. With someone in the way she steps aside first.
      const inTheWay = playerInLine(creeper);
      if (inTheWay) return setMode(t, 'aside', creeper, sideStep(creeper), false);
      setMode(t, 'shoot', creeper, null);
      shoot(creeper);
      return;
    }
  }

  // Every other monster: chased and hit. Not one under water (a drowned at the bottom): she can't reach it there. Nor
  // one she can't see (in a cave under the ground): taken on only in sight, and let go once out of it for FoeUnseen.
  // One on her comes first, whatever she was after: chasing a zombie she couldn't reach, she let the one behind her
  // kill her. One she found no way to is left for a while (unreachable).
  if (t.foe && inSight(t.foe)) t.foeSeenAt = Date.now();
  if (t.foe && (!t.foe.isValid || t.foe.position.distanceTo(center) > radius * 1.6 || headUnder(t.foe)
    || Date.now() - (t.foeSeenAt || 0) > FoeUnseen)) t.foe = null;
  const onHer = foeOnHer();
  if (onHer && onHer !== t.foe) {
    t.foe = onHer;
    t.foeSeenAt = Date.now();
  }
  if (!t.foe) {
    t.foe = bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e) && reachable(t, e)
      && (e.position.distanceTo(center) < radius || e.position.distanceTo(me) < 6) && inSight(e));
    if (!t.foe) return goHome();
    t.foeSeenAt = Date.now();
  }
  if (rangedStep(t, t.foe)) return;
  chase(t, t.foe);
  strike(t.foe, t);
}
