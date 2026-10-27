// A player's orders to go somewhere (come, follow, go to, guide, stay), the activities an errand pauses and resumes,
// and her free time between orders (held after « reste », the grace after an order, autonomyTick).

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { leaveBoat, riding } from './boats.js';
import { stopBridging } from './bridges.js';
import { fishLoop, reelIn } from './fishing.js';
import { eating } from './food.js';
import { setSitting, sitting } from './gestures.js';
import { gentleMovements } from './movements.js';
import { pendingRetrieve, retrieveTick, startRetrieve } from './retrieve.js';
import { routineTick } from './routine.js';
import { dimension } from './seek.js';
import { stopBow } from './shooting.js';
import { setResumeAfterSleep, stayUpTonight } from './sleep.js';
import { bot, emit, setTask, task } from './state.js';
import { fightPace } from './strike.js';
import { endSlowWalk, tagAlongTick } from './tagalong.js';
import { swim } from './water.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// The ops that are a player's order (the rest are words, questions, or C#'s own business).
export const OrderOps = new Set(['come', 'follow', 'goto', 'guide', 'give', 'store', 'fetch', 'craft', 'fish', 'patrol', 'smelt', 'guard', 'collect', 'camp', 'uncamp',
  'sleep', 'harvest', 'farm', 'ranch']);

// The farthest she agrees to walk to coordinates: past this she would be gone for the evening.
export const MaxTrip = 1500;

// The player's entity, if it's close enough for the server to show it to her (render distance).
export function playerEntity(name) {
  const player = bot.players[name];
  return player && player.entity ? player.entity : null;
}

export function come(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  setTask({ kind: 'come', player, reported: false });
  const at = target.position;
  bot.pathfinder.setGoal(new goals.GoalNear(at.x, at.y, at.z, 2));
  emit('moving', { player, kind: 'come' });
}

// `quiet`: going on after a portal (crossAfter), the order already acknowledged.
export function follow(player, quiet = false) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  setTask({ kind: 'follow', player, reported: false });
  // Dynamic: the goal moves with the player.
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  if (!quiet) emit('moving', { player, kind: 'follow' });
}

// Coordinates: x z (any height) or x y z. Far trips cross chunks she can't see yet; the pathfinder
// walks partial paths towards the goal and plans again as the world loads. A remembered place comes with
// its dimension: from another one, she can't walk there.
// Every order checks it can be done before stay() drops the current one: an order that fails at once
// (out of sight, too far, nothing to gather) leaves her doing what she was doing.
export function goTo(player, x, y, z, dim) {
  const here = bot.entity.position;
  if (dim && dim !== dimension()) return emit('cantReach', { player, why: 'otherDimension' });
  if (Math.hypot(x - here.x, z - here.z) > MaxTrip) return emit('cantReach', { player, why: 'tooFar' });
  stay();
  setTask({ kind: 'goto', player, reported: false });
  bot.pathfinder.setGoal(y === null ? new goals.GoalNearXZ(x, z, 2) : new goals.GoalNear(x, y, z, 1));
  emit('moving', { player, kind: 'goto' });
}

// Leading a player to a place: she walks ahead, stops and waits when they fall more than GuideLag blocks behind,
// and goes on once they're close again. Out of their sight for GuideLost, she gives up.
const GuideLag = 12;
const GuideLost = 30000;

export function guide(player, x, y, z, dim) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  if (dim && dim !== dimension()) return emit('cantReach', { player, why: 'otherDimension' });
  const here = bot.entity.position;
  if (Math.hypot(x - here.x, z - here.z) > MaxTrip) return emit('cantReach', { player, why: 'tooFar' });
  stay();
  setTask({ kind: 'guide', player, dest: new Vec3(x, y, z), waiting: false, lostSince: null, nextCheck: 0, reported: false });
  bot.pathfinder.setGoal(new goals.GoalNear(x, y, z, 2));
  emit('moving', { player, kind: 'guide' });
}

export function guideTick() {
  const t = task;
  if (!t || t.kind !== 'guide' || Date.now() < t.nextCheck) return;
  t.nextCheck = Date.now() + 500;
  const follower = playerEntity(t.player);
  if (!follower) {
    if (!t.lostSince) {
      t.lostSince = Date.now();
      bot.pathfinder.setGoal(null);
    } else if (Date.now() - t.lostSince > GuideLost) {
      emit('cantReach', { player: t.player, why: 'lost' });
      stay();
    }
    return;
  }
  const gap = follower.position.distanceTo(bot.entity.position);
  if ((t.waiting || t.lostSince) && gap < 6) {
    t.waiting = false;
    t.lostSince = null;
    bot.pathfinder.setGoal(new goals.GoalNear(t.dest.x, t.dest.y, t.dest.z, 2));
  } else if (!t.waiting && !t.lostSince && gap > GuideLag) {
    t.waiting = true;
    bot.pathfinder.setGoal(null);
    emit('waiting', { player: t.player });
  }
  if (t.waiting) bot.lookAt(follower.position.offset(0, 1.6, 0)).catch(() => {});
}

export function stay() {
  if (task && ['collect', 'harvest', 'farm', 'forage'].includes(task.kind))
    bot.collectBlock.cancelTask().catch(() => {});
  if (task) reelIn(task); // fishing, or fishing for her supper
  if (task && task.window) { // a furnace she's cooking at
    try { task.window.close(); } catch { /* already closed */ }
  }
  // An order gets her out of bed: lying there, she can't move. And out of a boat (the ride was the old task's).
  if (bot.isSleeping) Promise.resolve().then(() => bot.wake()).catch(() => {});
  if (riding) leaveBoat();
  if (task && task.kind === 'portal') bot.pathfinder.setMovements(gentleMovements()); // portals were allowed
  stopBridging(); // the walk that could bridge is over (what she placed still comes down: bridgeTick)
  setTask(null);
  setResumeAfterSleep(null);
  setSitting(false);
  bot.setControlState('sneak', false);
  stopBow();
  bot.pathfinder.setGoal(null);
  endSlowWalk(); // tagging along walked without sprinting
  fightPace(false);
}

// Farming, fishing, keeping animals and guarding a place last until someone says otherwise. Handing something over
// or putting her things away pauses one, and she goes back to it after; so does a night's sleep. Her own (the
// routine's) count too.
const Activities = new Set(['farm', 'fish', 'patrol', 'ranch']);

export function isActivity(t) {
  return Boolean(t && Activities.has(t.kind));
}

// The activity under way, or the one a meal break (forage) paused: what an errand, a night's sleep or running
// away goes back to.
export function pausedActivity(t) {
  if (isActivity(t)) return t;
  return t && t.kind === 'forage' ? t.resume : null;
}

// A fresh copy, so whatever was still running for the old one (a farm round, a fishing cast) sees it's no longer
// the task and stops.
export function resumeActivity(activity) {
  if (!activity || task) return;
  setTask({ ...activity, busy: false, foe: null, mode: null, modeTarget: null, casting: false, target: null });
  if (task.kind === 'fish') fishLoop(task);
}

// Own tasks (own: true, player ''): the routine, tagging along, getting her things back. Body-only, like eating: no
// claim, no order events, and any order ends one (through stay()). Nobody needs her during one, so she counts as free.
export function isFree(t) {
  return !t || Boolean(t.own);
}

// An order: a task someone gave her (or the walk up to Papa on arriving), as opposed to her own or a body-only one.
export function isOrder(t) {
  return Boolean(t && !t.own && t.player);
}

// « reste » and « assis » hold her for HeldFor: no routine, no tagging along, no retrieval (body language still runs).
// Any other order that gets under way lets her go. And once an order is over she waits OrderGrace before starting
// anything of her own, so « viens » doesn't end with her walking off at once.
const HeldFor = 600000;
const OrderGrace = 45000;
export let heldUntil = 0;

// « reste », « assis »: held here, and up tonight (no going off to bed).
export function holdHere() {
  heldUntil = Date.now() + HeldFor;
  stayUpTonight();
}

// An order under way: free of « reste ».
export function letGo() {
  heldUntil = 0;
}
let freeSince = 0;
let wasOrder = false;
let autonomyAt = 0;

// « quartier libre »: her own life starts on the next autonomy tick, with no grace after the order she was on.
export function skipGrace() {
  wasOrder = false;
  freeSince = 0;
}

export function freeTick() {
  const order = isOrder(task);
  if (wasOrder && !order) freeSince = Date.now();
  wasOrder = order;
}

// Once a second, when nobody needs her: getting her things back after a death, else the routine (named places), else
// tagging along with Papa or Tata, else nothing but body language.
export function autonomyTick() {
  if (Date.now() < autonomyAt || !bot.entity) return;
  autonomyAt = Date.now() + 1000;
  if (bot.isSleeping || eating || sitting || swim || !isFree(task)) return;
  if (Date.now() < freeSince + OrderGrace || Date.now() < heldUntil) return;
  if (task && task.kind === 'retrieve') return retrieveTick(task);
  if (pendingRetrieve) return startRetrieve();
  if (routineTick()) return;
  tagAlongTick();
}
