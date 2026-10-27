// Defending herself and sizing a fight up: fight it, fall back to her player, or get out; orders held mid-fight. The
// decisions that need no bot are in fight.js.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { GetOutKinds, sizeUp } from './fight.js';
import { riding } from './boats.js';
import { familyOrder } from './family.js';
import { eatIfHungry, eating } from './food.js';
import { isFoe, MeleeReach, RetreatHealth, setMode, Weapons } from './guard.js';
import { WornSlots } from './inventory.js';
import { isOrder, OrderOps, playerEntity, stay } from './orders.js';
import { isRanged, rangedStep, Replan, ShieldSight } from './ranged.js';
import { redo } from './restart.js';
import { resumeAfterBreak, resumeFor } from './resume.js';
import { equipWeapon, inSight } from './shooting.js';
import { bot, emit, setTask, task } from './state.js';
import { chase, FightLeash, fightPace, foeOnHer, reachable, strike } from './strike.js';
import { headUnder } from './water.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// Anything that hurts her, or a monster coming at her up close, is dealt with whatever she's doing: she stops, deals
// with it, and goes back to what she was doing (resumeFor, as after defending someone). Her life comes before an
// errand: one that can't be started again is dropped, and its player told (`dropped`, nobody to blame). Not asleep, in
// a boat (her physics is off) or crossing a portal, and not while running from a player. Guarding, patrolling,
// defending someone or dodging a creeper, the fight under way is already hers (guardTick takes the monster on her
// first). The stance (fight it, or only get away from it: answerTo) is kept on the task; getting away is Task 4's
// walk, and meanwhile she never hits what she should only get away from.
// A monster this close and coming at her is a fight before its first hit; one further off, or standing still (a
// skeleton on a hill), leaves her to her work.
const SelfThreat = 4;
// Nothing on her or near her for this long, and the fight's over.
const FightQuiet = 2000;
// How far a foe of the fight still counts, in sight.
const FightSight = 8;
// Getting away from something she never hits ends once it's this far, or hasn't come near her for GetOutQuiet.
const GetOutClear = 20;
const GetOutQuiet = 8000;

export function canSelfDefend() {
  if (!bot.entity || bot.isSleeping || riding) return false;
  return !(task && ['portal', 'flee'].includes(task.kind));
}

// True when she takes it on.
export function selfDefend(foe, stance = 'fight') {
  if (!foe || foe.isValid === false || !canSelfDefend()) return false;
  if (task && task.kind === 'fight') {
    if (stance === 'fight') task.foes.add(foe.id);
    else {
      // Never a target, even mid-fight (an iron golem's hit made it one): she gets clear of it.
      task.foes.delete(foe.id);
      task.threat = foe;
      task.threatAt = Date.now();
      task.stance = 'getOut';
      task.stanceSince = Date.now();
      task.mode = null;
      task.modeTarget = null;
    }
    return true;
  }
  // Putting herself out comes first; whatever is on her waits a few seconds (a fight and the walk to water replaced
  // each other on every tick).
  if (task && task.kind === 'hazard') return false;
  // Their own fight, except that getting out of a warden's way comes first: guarding, she gets clear and guards on.
  if (task && (task.kind === 'dodge' || (stance !== 'getOut' && ['defend', 'guard', 'patrol'].includes(task.kind)))) return false;
  const previous = task;
  const resume = resumeFor(previous);
  if (previous && !resume && isOrder(previous) && previous.kind !== 'seek' && previous.kind !== 'sleep')
    emit('dropped', { player: previous.player, kind: previous.kind, by: '' });
  stay();
  setTask({ kind: 'fight', player: '', foes: new Set([foe.id]), foe: null, stance, stanceSince: Date.now(),
    threat: stance === 'getOut' ? foe : null, threatAt: Date.now(), resume, fightFrom: foe.position.clone(), quietSince: null,
    startedAt: Date.now(), held: null, toward: null, mode: null, modeTarget: null, modeSince: 0 });
  if (stance === 'fight') equipWeapon();
  return true;
}

// Where each monster near her was a moment ago, to tell one coming at her from one standing about.
const approach = new Map();
const ApproachEvery = 500;

function comingAt(e, me) {
  const d = e.position.distanceTo(me);
  if (d < 2) return true;
  const seen = approach.get(e.id);
  if (!seen || Date.now() - seen.at > ApproachEvery * 2) {
    approach.set(e.id, { d, at: Date.now() });
    return false;
  }
  if (Date.now() - seen.at < ApproachEvery) return false;
  approach.set(e.id, { d, at: Date.now() });
  return d < seen.d - 0.2;
}

// Every physics tick: a monster (not a creeper: creeperTick's) within SelfThreat, in sight and coming at her, while
// nothing else is fighting it out.
export function threatTick() {
  if (!canSelfDefend() || (task && ['dodge', 'fight', 'hazard'].includes(task.kind))) return;
  const me = bot.entity.position;
  // What she never fights (a warden, a creaking), in sight: she gets clear of it, whatever she's doing.
  const danger = bot.nearestEntity((e) => GetOutKinds.has(e.name) && e.isValid !== false
    && e.position.distanceTo(me) < DangerSight && inSight(e));
  if (danger) return void selfDefend(danger, 'getOut');
  if (task && ['guard', 'patrol', 'defend'].includes(task.kind)) return;
  const foe = bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e)
    && e.position.distanceTo(me) < SelfThreat && comingAt(e, me) && inSight(e));
  if (foe) selfDefend(foe);
}

// A warden or a creaking this close and in sight: she gets clear of it.
const DangerSight = 16;
// How far she sizes a fight up: every monster in sight this close counts against her (fight.js weighs them).
const SizeSight = 12;
// A stance is held at least this long, so she doesn't flip between fighting and falling back every tick.
const StanceHold = 1500;
// Falling back with nobody to fall back to, this long, and she turns and fights what still chases her.
const FallBackAlone = 20000;
// Who she falls back to is within this, and seen; beside them (Helped) counts as help.
const HerPlayerReach = 32;
const Helped = 4;
// Getting clear of a warden: this far beyond her player, on their far side from it; alone, this far from it.
export const ClearBeyond = 6;
const ClearAlone = 16;
// Out of her sight this long, and what she was getting clear of is behind her.
const DangerUnseen = 3000;
const ArmourPoints = {
  leather: [1, 3, 2, 1], copper: [2, 4, 3, 1], golden: [2, 5, 3, 1], chainmail: [2, 5, 4, 1], iron: [2, 6, 5, 2],
  diamond: [3, 8, 6, 3], netherite: [3, 8, 6, 3], turtle: [2, 0, 0, 0],
};

function armourPoints() {
  return WornSlots.reduce((sum, slot, i) => {
    const item = bot.inventory.slots[bot.getEquipmentDestSlot(slot)];
    const m = item && /^([a-z]+)_(helmet|chestplate|leggings|boots)$/.exec(item.name);
    return sum + ((m && ArmourPoints[m[1]] && ArmourPoints[m[1]][i]) || 0);
  }, 0);
}

export function bestWeapon() {
  const names = new Set(bot.inventory.items().map((i) => i.name));
  return Weapons.find((w) => names.has(w)) || null;
}

// Who she falls back to: whoever she was told to come to mid-fight, else the player of what she was doing, else Papa,
// else Tata, else the closest player; seen, within HerPlayerReach.
export function herPlayer(t) {
  const me = bot.entity.position;
  const seen = (e) => (e && e !== bot.entity && e.position.distanceTo(me) <= HerPlayerReach ? e : null);
  const named = (name) => (name ? seen(playerEntity(name)) : null);
  const r = t.resume;
  const byFamily = familyOrder.map((f) => Object.values(bot.players).find((p) => p.username.toLowerCase() === f))
    .map((p) => p && seen(p.entity)).find(Boolean);
  return named(t.toward) || named(r && (r.player || (r.restart && r.restart.player))) || byFamily
    || bot.nearestEntity((e) => e.type === 'player' && e !== bot.entity && e.position.distanceTo(me) <= HerPlayerReach);
}

// A point `far` blocks from `from`, away from `away` (horizontally).
export function awayFrom(from, away, far) {
  const dx = from.x - away.x;
  const dz = from.z - away.z;
  const n = Math.hypot(dx, dz) || 1;
  return new Vec3(from.x + (dx / n) * far, from.y, from.z + (dz / n) * far);
}

// Every physics tick while defending herself. Sized up (fight.js) every StanceHold at most: fight the closest foe;
// fall back to her player hitting whatever keeps up, eating when nothing is in reach; or get clear of what she never
// fights. Over once nothing is left for FightQuiet (a warden: once it's far or out of sight).
export function fightTick() {
  const t = task;
  if (!t || t.kind !== 'fight') return;
  const me = bot.entity.position;
  const onHer = foeOnHer();
  if (onHer && t.stance !== 'getOut') t.foes.add(onHer.id);
  const foes = [...t.foes].map((id) => bot.entities[id])
    .filter((e) => e && e.isValid !== false && !headUnder(e) && reachable(t, e) && e.position.distanceTo(me) < (isRanged(e) ? ShieldSight : FightSight)
      && e.position.distanceTo(t.fightFrom) <= FightLeash && inSight(e))
    .sort((a, b) => a.position.distanceTo(me) - b.position.distanceTo(me));
  const p = herPlayer(t);
  if (t.stance !== 'getOut' && Date.now() - t.stanceSince >= StanceHold) {
    const around = Object.values(bot.entities).filter((e) => e !== bot.entity && e.isValid !== false
      && (t.foes.has(e.id) || (isFoe(e) && e.name !== 'creeper') || GetOutKinds.has(e.name))
      && e.position.distanceTo(me) < SizeSight && inSight(e));
    let next = sizeUp({ foes: around.map((e) => ({ name: e.name, distance: e.position.distanceTo(me) })), health: bot.health,
      armour: armourPoints(), weapon: bestWeapon(), helped: Boolean(p && p.position.distanceTo(me) < Helped), previous: t.stance });
    if (next === 'fight' && t.toward) next = 'fallBack'; // told to come: to them first, fighting on the way
    if (next === 'fallBack' && (t.lastStand || (!p && t.fallSince && Date.now() - t.fallSince > FallBackAlone))) {
      next = 'fight'; // nobody to fall back to, still chased: she turns and fights it out
      t.lastStand = true;
    }
    if (next === 'getOut') {
      t.threat = around.find((e) => GetOutKinds.has(e.name));
      t.threatAt = Date.now();
    }
    if (next !== t.stance) {
      t.stance = next;
      t.stanceSince = Date.now();
      t.mode = null;
      t.modeTarget = null;
      if (next === 'fallBack') {
        t.fallSince = Date.now();
        emit('retreat', { player: t.toward || '' });
      }
    }
  }
  if (t.stance === 'getOut') return getOutTick(t, p);
  if (foes.length === 0) {
    if (!t.quietSince) {
      t.quietSince = Date.now();
      t.mode = null;
      t.modeTarget = null;
      bot.pathfinder.setGoal(null);
    }
    if (Date.now() - t.quietSince > FightQuiet) endFight(t);
    return;
  }
  t.quietSince = null;
  t.foe = foes[0];
  if (t.stance === 'fight') {
    if (rangedStep(t, t.foe)) return;
    chase(t, t.foe);
    strike(t.foe, t);
    return;
  }
  // Falling back: to her player, else off away from them, again once there. Whatever keeps up gets hit.
  fightPace(true);
  if (p && Date.now() >= (t.noWayToPlayer || 0)) setMode(t, 'fallBack', p, new goals.GoalFollow(p, 2), true);
  else if ((t.mode !== 'hop' || !bot.pathfinder.isMoving()) && Date.now() >= (t.replanAt || 0)) {
    const spot = awayFrom(me, t.foe.position, 8);
    t.mode = 'hop';
    t.modeTarget = null;
    t.replanAt = Date.now() + Replan;
    bot.pathfinder.setGoal(new goals.GoalNear(spot.x, spot.y, spot.z, 2));
  }
  if (onHer) {
    if (!eating && !(bot.heldItem && Weapons.includes(bot.heldItem.name)) && bestWeapon()) equipWeapon();
    strike(onHer);
  } else if (!eating && bot.health < 20) eatIfHungry(20).catch(() => {});
}

// Getting clear of what she never fights: beyond her player on their far side from it, else well away from it. Over
// once it's far, gone, or out of her sight for DangerUnseen (and, for an animal that hit her, once it leaves her alone).
function getOutTick(t, p) {
  const me = bot.entity.position;
  const th = t.threat;
  if (th && th.isValid !== false && inSight(th)) t.threatSeenAt = Date.now();
  if (!th || th.isValid === false || !bot.entities[th.id] || th.position.distanceTo(me) > GetOutClear
    || Date.now() - (t.threatSeenAt || t.startedAt) > DangerUnseen
    || (!GetOutKinds.has(th.name) && Date.now() - t.threatAt > GetOutQuiet)) return endFight(t);
  if (th.position.distanceTo(me) < MeleeReach + 1) t.threatAt = Date.now();
  if (Date.now() < (t.clearAt || 0)) return;
  t.clearAt = Date.now() + 1000; // her player moves: the spot moves with them
  const near = p && p.position.distanceTo(me) < DangerSight ? p : null;
  const spot = near ? awayFrom(near.position, th.position, ClearBeyond) : awayFrom(me, th.position, ClearAlone);
  t.mode = 'clear';
  t.modeTarget = th;
  fightPace(true);
  bot.pathfinder.setGoal(new goals.GoalNear(spot.x, spot.y, spot.z, 1));
}

function endFight(t) {
  if (task !== t) return;
  setTask(null);
  fightPace(false);
  bot.pathfinder.setGoal(null);
  approach.clear();
  if (bot.health <= RetreatHealth) eatIfHungry(20).catch(() => {});
  if (t.held) return redo(t.held); // an order given mid-fight replaces what she was doing
  resumeAfterBreak(t.resume);
}

// Orders while she fights for herself (Papa's call). « viens » and « suis-moi » bring her to that player at once,
// falling back as she fights; any other order, « reste » included (she can't stand still under attack), waits for the
// fight and then starts, a newer one replacing it. Answered at once as ever: words, where she is, a wave. C# keeps a
// held order pending until the body reports it under way. True when the order was held.
export function holdForFight(command) {
  const t = task;
  if (!bot || !t || t.kind !== 'fight' || !(OrderOps.has(command.op) || command.op === 'stay' || command.op === 'sit' || command.op === 'free')) return false;
  t.held = command;
  if ((command.op === 'come' || command.op === 'follow') && typeof command.player === 'string' && playerEntity(command.player)) {
    t.toward = command.player;
    if (t.stance === 'fight') {
      t.stance = 'fallBack';
      t.stanceSince = Date.now();
      t.fallSince = Date.now();
      t.mode = null;
      t.modeTarget = null;
    }
  }
  return true;
}

// Who hurt someone, for sure: since 1.19.4 the server says (damage_event, the shooter for an arrow). Before that,
// Mineflayer gives no source and her own hits fall back to a guess.
export function exactHits() {
  const v = bot.registry && bot.registry.version;
  return Boolean(v && typeof v['>='] === 'function' && v['>=']('1.19.4'));
}
