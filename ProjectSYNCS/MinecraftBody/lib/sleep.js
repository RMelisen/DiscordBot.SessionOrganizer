// Going to bed: when a player does, when asked, and on her own once it's night.

import pathfinderPackage from 'mineflayer-pathfinder';
import { GatherRadius } from './gather.js';
import { sitting } from './gestures.js';
import { isActivity, isFree, pausedActivity, resumeActivity, stay } from './orders.js';
import { dimension } from './seek.js';
import { bot, emit, log, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// The night only passes when everyone on the server sleeps (by default), her included: when a player
// goes to bed she goes too, once per night, or says why she can't.
export let sleptOnDay = null;
// An activity she was at when she went to bed (a farm, fishing, a guarded place): she goes back to it when she
// wakes. Any order clears it (stay()).
let resumeAfterSleep = null;

export function setResumeAfterSleep(value) {
  resumeAfterSleep = value;
}

// The real night, when a bed works without a storm.
function isNightTime() {
  const t = bot.time.timeOfDay;
  return t >= 12542 && t <= 23459;
}

export function isNight() {
  return isNightTime() || bot.thunderState > 0;
}

// A bed anywhere else explodes.
function inOverworld() {
  return !/nether|end/.test(dimension());
}

// Asked, or because a player went to bed (player set), or on her own (auto: player empty, and every failure is
// silent, since nobody is waiting for an answer).
export async function sleep(player, auto = false) {
  if (bot.isSleeping) return;
  if (!isNight()) return auto ? undefined : emit('cantSleep', { player, why: 'day' });
  if (!auto) sleptOnDay = bot.time.day;
  const bedIds = bot.registry.blocksArray.filter((b) => b.name.endsWith('_bed')).map((b) => b.id);
  const beds = !inOverworld() ? [] : bot.findBlocks({ matching: bedIds, maxDistance: GatherRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  if (beds.length === 0) {
    if (!auto) return emit('cantSleep', { player, why: 'noBed' });
    sleptOnDay = bot.time.day; // no bed around: not worth looking again tonight
    return;
  }
  const kept = pausedActivity(task) || resumeAfterSleep;
  stay();
  resumeAfterSleep = kept;
  const mine = { kind: 'sleep', player };
  setTask(mine);
  for (const at of beds) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 1));
      await bot.sleep(bot.blockAt(at));
      if (task === mine) setTask(null);
      sleptOnDay = bot.time.day;
      return emit('sleeping', { player, auto });
    } catch (err) {
      log('bed refused:', err && err.message ? err.message : String(err)); // occupied, too far, monsters near
      if (task !== mine) return;
    }
  }
  setTask(null);
  wakeUp();
  if (!auto) emit('cantSleep', { player, why: 'failed' });
}

// She goes to bed by herself at night, so the night can pass as soon as the players lie down, but only when
// nobody needs her: free, or at an activity she goes back to when she wakes. Not after being told to stay or sit
// tonight, not after an order woke her (sleptOnDay), never in a storm by day. A bed that refused her (monsters
// near, occupied) is tried again two minutes later (a night lasts about seven); no bed at all, and she gives up
// for the night.
const AutoSleepRetry = 120000;
let autoSleepAt = 0;
let stayedOnDay = null;

// Told to stay or sit: she doesn't wander off to bed tonight.
export function stayUpTonight() {
  stayedOnDay = bot.time.day;
}

export function autoSleepTick() {
  if (bot.isSleeping || sitting || !isNightTime() || !inOverworld()) return;
  if (sleptOnDay === bot.time.day || stayedOnDay === bot.time.day) return;
  if (task && !isActivity(task) && !isFree(task)) return;
  if (task && task.kind === 'retrieve') return; // her things despawn: bed can wait
  if (Date.now() < autoSleepAt) return;
  autoSleepAt = Date.now() + AutoSleepRetry;
  sleep('', true).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
}

// Back to what she was at, after a night's sleep or a bed that refused her.
export function wakeUp() {
  const activity = resumeAfterSleep;
  resumeAfterSleep = null;
  resumeActivity(activity);
}
