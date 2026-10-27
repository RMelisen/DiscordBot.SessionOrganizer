// Fixes to the pathfinder's own ends (goto, a search out of time, a ladder from the side) and what « no way there »
// means for what she was doing.

import Move from 'mineflayer-pathfinder/lib/move.js';
import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { tryBridge } from './bridges.js';
import { endDefend } from './defend.js';
import { FollowsSomeone } from './flee.js';
import { endHazard } from './hazards.js';
import { playerEntity, stay } from './orders.js';
import { setPendingRetrieve } from './retrieve.js';
import { failSpot } from './routine.js';
import { bot, emit, task } from './state.js';
import { setUnreachable, UnreachableFor } from './strike.js';
import { headUnder } from './water.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// Every search's result. No way there: coming and going give up at once; following and guarding keep trying (the player
// may walk somewhere reachable) but say so only once per order. Gathering and sleeping handle their own paths.
export function onPathUpdate(result) {
  climbFromTheFoot(result.path);
  if (result.status === 'timeout') timedOut = { goal: bot.pathfinder.goal, from: bot.entity.position.clone() };
  if (result.status === 'noPath' || (result.status === 'timeout' && stalledSearch(result))) noWay();
}

// The goal she was walking to can't be reached (noPath, or a search with nothing more to gain): what that means for
// what she was doing.
function noWay() {
  timedOut = null;
  if (!task) return;
  if (task.mode === 'loot') return task.skipLoot.add(task.modeTarget.id); // out of reach: left where it is
  // A monster she can't get to (down a ravine, across a gap): left for a while, not her guard given up.
  if (task.mode === 'fight' && task.modeTarget && ['guard', 'patrol', 'fight'].includes(task.kind)) return setUnreachable(task, task.modeTarget);
  // Her fight's other moves (to her player, a hop away, cover, off a lip, aside to shoot): no way is no failure to tell
  // anyone. Her player out of reach, she hops away from what's on her instead, for a while.
  if (task.kind === 'fight') {
    if (task.mode === 'fallBack') task.noWayToPlayer = Date.now() + UnreachableFor;
    task.mode = null;
    task.modeTarget = null;
    return;
  }
  if (['edge', 'cover', 'aside'].includes(task.mode) && ['guard', 'patrol', 'defend'].includes(task.kind)) {
    task.mode = null;
    task.modeTarget = null;
    return;
  }
  if (task.reported) return;
  // Following someone who dived: no path under water, and the dive (waterTick) goes after them instead.
  if (FollowsSomeone.has(task.kind) && headUnder(playerEntity(task.player))) return;
  // A gap of a block or two in the way of a walk someone asked for: once more, with a bridge she takes down after.
  if (tryBridge(task)) return;
  if (['collect', 'sleep', 'harvest', 'farm', 'give', 'store', 'fetch', 'craft', 'fish', 'smelt', 'patrol', 'forage', 'ranch', 'tagAlong', 'home', 'back']
    .includes(task.kind)) return;
  // Her own: a place she can't walk to is left out for a while; her things, given up.
  if (task.kind === 'trip') {
    const what = task.what;
    stay();
    return failSpot(what, 'no path');
  }
  if (task.kind === 'retrieve') {
    // Sweeping, it's one item out of reach, not the trip: pickUp gives each one its own time and moves on.
    if (task.sweeping) return;
    stay();
    setPendingRetrieve(null);
    return emit('retrieveLost', { why: 'noPath' });
  }
  if (task.kind === 'seek') {
    emit('seek', { player: task.player, outcome: 'missing' });
    return stay();
  }
  if (task.kind === 'flee') {
    task.reported = true;
    return emit('cornered', { player: task.player });
  }
  if (task.kind === 'defend') return endDefend(task); // can't get at them: back to what she was doing
  if (task.kind === 'hazard') return endHazard(task); // no way to the water: she burns it out
  task.reported = true;
  emit('cantReach', { player: task.player, why: 'noPath' });
  if (task.kind === 'come' || task.kind === 'goto' || task.kind === 'guide') stay();
}

// A search that ran out of time (thinkTimeout) and whose best point is where she already stands: nothing more to gain,
// so it counts as « no way ». A goal out of reach, which a fast machine on a small world ends with noPath, runs out of
// time instead on a Pi's budget (MC_CPU_FACTOR 3.5 on the local server) or in a big loaded world. A long trip's
// partial paths still go somewhere, so they go on (timeoutTick).
function stalledSearch(result) {
  const path = result.path;
  if (!path || path.length === 0 || !bot.entity) return true;
  const end = path[path.length - 1];
  return bot.entity.position.distanceTo(new Vec3(end.x, end.y, end.z)) < 1.5;
}

// And once she has walked a timed-out search's partial path, the pathfinder doesn't search again: it only does when a
// path is reset, so she stood there with her goal set, silent, for good. Twice a second: walked it and got somewhere,
// a new search from here; got nowhere, no way.
let timedOut = null; // { goal, from }: the last search for `goal` ran out of time, started from `from`
export const isDynamic = (goal) => goal instanceof goals.GoalFollow || goal instanceof goals.GoalInvert;

export function timeoutTick() {
  const t = timedOut;
  if (!t) return;
  const goal = bot.pathfinder.goal;
  if (!goal || goal !== t.goal) {
    timedOut = null; // reached, dropped or replaced since
    return;
  }
  if (bot.pathfinder.isMoving()) return; // still on its partial path
  timedOut = null;
  if (bot.entity.position.distanceTo(t.from) >= 2) bot.pathfinder.setGoal(goal, isDynamic(goal));
  else noWay();
}

// The pathfinder's goto, except that an unfinished search is not « arrived »: it resolved on any search that came back
// with no path, and a search's first slice (tickTimeout) often does, so she « got there » without moving, stood 2
// blocks from a cull's beef and left it, again and again (local server). Everything awaiting goto had the same lie,
// collectblock included. Resolves on goal_reached or a finished search with nothing to walk; rejects as before on no
// path, a search out of time, another goal, a stop.
export function goto(goal) {
  return new Promise((resolve, reject) => {
    const fail = (name, message) => Object.assign(new Error(message), { name });
    const listeners = {
      goal_reached: () => done(),
      path_update: (r) => {
        if (r.status === 'noPath') done(fail('NoPath', 'No path to the goal!'));
        else if (r.status === 'timeout') done(fail('Timeout', 'Took to long to decide path to goal!'));
        // A finished search with nothing to walk: she's there (its start is her feet on top of what she stands on,
        // where the floored position is inside farmland, 15/16 high, and never matches). A partial one is just the
        // first slice of the search.
        else if (r.path.length === 0 && r.status === 'success') done();
      },
      goal_updated: (g) => { if (g !== goal) done(fail('GoalChanged', 'The goal was changed before it could be completed!')); },
      path_stop: () => done(fail('PathStopped', 'Path was stopped before it could be completed!')),
    };
    function done(err) {
      for (const [ev, fn] of Object.entries(listeners)) bot.removeListener(ev, fn);
      setTimeout(() => (err ? reject(err) : resolve()), 0); // the pathfinder's own cleanup first, as its goto does
    }
    for (const [ev, fn] of Object.entries(listeners)) bot.on(ev, fn);
    bot.pathfinder.setGoal(goal);
  });
}

// The pathfinder lifts a climb's first node to the top of the ladder's plate (getPositionOnTopOf). Walking into the plate
// from the front, she climbs; walking along the wall into the ladder's block from its side she wasn't pushing against
// it, the step up couldn't be simulated, and the pathfinder let go of every key and planned the same again: stuck for
// good beside the ladder. Barring the side entry would close a corridor with a ladder on its wall, so the path gets back
// the step into the ladder's foot instead: in it first, then up, facing the plate as from the front. In place: the
// pathfinder walks the very array it hands out in path_update.
const Facing = { west: [1, 0], east: [-1, 0], north: [0, 1], south: [0, -1] }; // the way into a ladder, towards its wall

function climbFromTheFoot(path) {
  for (let i = 0; i < path.length; i++) {
    const n = path[i];
    const prev = i > 0 ? path[i - 1] : bot.entity.position; // a new search starts where she stands
    const foot = bot.blockAt(new Vec3(Math.floor(n.x), Math.floor(n.y) - 1, Math.floor(n.z)));
    if (!foot || foot.name !== 'ladder' || Math.floor(prev.y) !== foot.position.y) continue;
    const dx = foot.position.x - Math.floor(prev.x);
    const dz = foot.position.z - Math.floor(prev.z);
    const into = Facing[foot.getProperties().facing];
    if (Math.abs(dx) + Math.abs(dz) !== 1 || !into || (into[0] === dx && into[1] === dz)) continue; // head-on: fine
    // The foot's centre: at the climb's own x/z, pressed to the plate, her box overlapped it and the step failed too.
    const step = new Move(foot.position.x, foot.position.y, foot.position.z, n.remainingBlocks, 0);
    step.x = foot.position.x + 0.5;
    step.z = foot.position.z + 0.5;
    path.splice(i, 0, step);
    i++;
  }
}
