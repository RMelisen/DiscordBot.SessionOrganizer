// What /debug minecraft shows of the body (« Voir son corps »): what she's doing and why she might be stuck, her
// health, how the process is doing, and the counts of the workarounds in compat.js. Read on demand ({"op":"debug"}),
// never pushed.

import { monitorEventLoopDelay } from 'node:perf_hooks';
import { riding } from './boats.js';
import { bridgeStatus } from './bridges.js';
import { status as compatStatus } from './compat.js';
import { eating } from './food.js';
import { sitting } from './gestures.js';
import { heldUntil } from './orders.js';
import { dimension } from './seek.js';
import { bot, task } from './state.js';
import { swim } from './water.js';

// How late the event loop runs its timers: every physics tick handler, pathfinder search and packet runs on it, so a
// slow Pi shows here first (the pathfinder's 5 s budget, a tick that takes 50 ms and more). Since the last snapshot.
let loop = null;

export function watchLoop() {
  loop = monitorEventLoopDelay({ resolution: 20 });
  loop.enable();
}

const ms = (ns) => Math.round(ns / 1e6);

// What an interrupted task goes back to: an activity or a follow (a task), an order started again (a command), or
// its own way back.
function resumeKind(resume) {
  if (!resume) return null;
  return resume.kind || (resume.restart && resume.restart.op) || (resume.again ? 'again' : null);
}

export function snapshot() {
  const e = bot && bot.entity;
  const loopNow = loop ? { p50: ms(loop.percentile(50)), p99: ms(loop.percentile(99)), max: ms(loop.max) } : null;
  if (loop) loop.reset();
  const goal = bot && bot.pathfinder ? bot.pathfinder.goal : null;
  return {
    pid: process.pid,
    node: process.version,
    uptime: Math.round(process.uptime()),
    rss: Math.round(process.memoryUsage().rss / 1048576),
    version: bot ? bot.version : null,
    pos: e ? { x: Math.floor(e.position.x), y: Math.floor(e.position.y), z: Math.floor(e.position.z) } : null,
    dimension: e ? dimension() : null,
    health: bot && Number.isFinite(bot.health) ? bot.health : null,
    food: bot && Number.isFinite(bot.food) ? bot.food : null,
    oxygen: bot && Number.isFinite(bot.oxygenLevel) ? bot.oxygenLevel : null,
    task: task ? { kind: task.kind, player: task.player || task.who || '', mode: task.mode || null, own: Boolean(task.own),
      resume: resumeKind(task.resume) } : null,
    heldFor: Math.max(0, Math.round((heldUntil - Date.now()) / 1000)),
    sitting,
    eating,
    sleeping: Boolean(bot && bot.isSleeping),
    swim: swim ? swim.mode : null,
    riding: Boolean(riding),
    bridge: bridgeStatus(),
    moving: Boolean(bot && bot.pathfinder && bot.pathfinder.isMoving()),
    goal: goal ? goal.constructor.name : null,
    loop: loopNow,
    compat: compatStatus(),
  };
}
