// What she's doing, reported to C# each time it changes (`doing {kind,player,what}`), for « tu fais quoi ? » and
// /minecraft status. C# knows the orders it sent; only the body knows her own life. Checked once a second.

import { sitting } from './gestures.js';
import { heldUntil } from './orders.js';
import { bot, emit, task } from './state.js';

// Keeping herself alive: whatever she was doing waits.
const Dangers = new Set(['fight', 'flee', 'hazard', 'dodge', 'defend']);

let last = '';
let doingAt = 0;

// On spawn: the first report always goes, whatever the last body said.
export function forgetDoing() {
  last = '';
}

export function doingTick() {
  if (Date.now() < doingAt || !bot.entity) return;
  doingAt = Date.now() + 1000;
  const now = current();
  const key = `${now.kind}|${now.player}|${now.what}`;
  if (key === last) return;
  last = key;
  emit('doing', now);
}

// kind: an order's task kind (`follow`, `fish`…), else her own life: routine (what: farm|fish|ranch|home, on her way
// there included), tagAlong and seek (player: who), forage, retrieve, danger (what: the task's kind), sleep, held
// (what: sitting or nothing), idle.
function current() {
  const t = task;
  if (t && Dangers.has(t.kind)) return { kind: 'danger', player: '', what: t.kind };
  if (bot.isSleeping) return { kind: 'sleep', player: '', what: '' };
  if (t && t.own) {
    if (t.kind === 'tagAlong') return { kind: 'tagAlong', player: t.who || '', what: '' };
    if (t.kind === 'retrieve') return { kind: 'retrieve', player: '', what: '' };
    return { kind: 'routine', player: '', what: t.what || '' };
  }
  if (t && t.kind === 'forage') return { kind: 'forage', player: '', what: '' };
  if (t && t.player) return { kind: t.kind, player: t.player, what: '' };
  if (sitting || Date.now() < heldUntil) return { kind: 'held', player: '', what: sitting ? 'sitting' : '' };
  return { kind: 'idle', player: '', what: '' };
}
