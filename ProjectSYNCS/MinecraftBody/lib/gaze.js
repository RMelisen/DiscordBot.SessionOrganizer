// Body language: where she looks, turning her back after a hit, greeting family who walk up.

import { eating } from './food.js';
import { sitting } from './gestures.js';
import { playerEntity } from './orders.js';
import { drawing } from './shooting.js';
import { bot, task, wait } from './state.js';
import { familyOrder } from './family.js';
import { swim } from './water.js';

// When her gaze is free (not asleep, eating, drawing her bow or walking; nothing to do, at home, at her own farm or pen
// between rounds, tagging along, following someone, or guarding them between fights) she looks the way a person would:
// at someone for a few seconds, away for a moment, then back. Who: whoever just talked within 12 blocks (no glancing
// away while they're talking), else the one she follows or guards, else the closest player within 8. A glance goes to
// another player close by, an animal or a monster in sight, or just somewhere, now and then down or up; guarding, she
// glances away more often (she's watching for monsters). With nobody around she looks somewhere new every 4–9 s, mostly
// near where she already looks rather than spinning round. Walking, the pathfinder turns her where she goes (Mineflayer
// walks where she looks), so none of this runs then. For 10 s after a player hits her, she turns her back on them
// instead. When Papa or Tata walks up from afar, she looks at them and crouches twice, once every GreetEvery at most.
const TalkLook = 8000;
export const HitSulk = 10000;
const GreetEvery = 120000;
const PersonSight = 8;
const Creatures = new Set(['hostile', 'mob', 'animal', 'passive', 'water_creature', 'ambient']);
export const lastChat = new Map();
const familyAway = new Map();
const greetedAt = new Map();
let hitBy = null;

export function setHitBy(value) {
  hitBy = value;
}
let idleAt = 0;
export let greeting = false;

export function setGreeting(value) {
  greeting = value;
}
// Whom she's looking at (an entity id), when she next glances away, and the glance (an entity, or a yaw and a pitch)
// with when it ends.
const gaze = { person: null, awayAt: 0, away: null, backAt: 0 };

function lookFree() {
  if (bot.isSleeping || eating || drawing || greeting || swim || bot.pathfinder.isMoving()) return false;
  if (!task) return true;
  if (task.kind === 'tagAlong' || task.kind === 'home' || task.kind === 'follow') return true;
  if (task.kind === 'guard') return task.mode === 'home' && !task.foe && !task.retreatUntil;
  return Boolean(task.own && (task.kind === 'farm' || task.kind === 'ranch') && !task.busy);
}

export const eyes = (entity) => entity.position.offset(0, (entity.height || 1.8) * 0.9, 0);

export function idleTick() {
  if (Date.now() < idleAt || !bot.entity) return;
  idleAt = Date.now() + 250;
  greetTick();
  if (!lookFree()) return;
  const me = bot.entity.position;
  const now = Date.now();
  const hitter = hitBy && now < hitBy.until ? playerEntity(hitBy.name) : null;
  if (hitter) {
    const towards = Math.atan2(-(hitter.position.x - me.x), -(hitter.position.z - me.z));
    bot.look(towards + Math.PI, 0).catch(() => {});
    return;
  }
  const speaker = talker(me);
  const person = speaker || companion(me)
    || bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username && e.position.distanceTo(me) <= PersonSight);
  if (!person) {
    gaze.person = null;
    if (now >= gaze.backAt || !lookAtGlance()) {
      gaze.away = glance(me, null);
      gaze.backAt = now + 4000 + Math.random() * 5000;
      lookAtGlance();
    }
    return;
  }
  if (gaze.person !== person.id) {
    gaze.person = person.id;
    gaze.backAt = 0;
    gaze.awayAt = now + dwell();
  }
  if (speaker) {
    // Listening: no glancing away, and a while longer on them once they've finished.
    gaze.backAt = 0;
    gaze.awayAt = Math.max(gaze.awayAt, now + 2000);
  } else if (now >= gaze.awayAt) {
    gaze.away = glance(me, person);
    gaze.backAt = now + 700 + Math.random() * 1800;
    gaze.awayAt = gaze.backAt + dwell();
  }
  if (now < gaze.backAt && lookAtGlance()) return;
  bot.lookAt(eyes(person)).catch(() => {});
}

// How long she keeps her eyes on someone before glancing away: shorter on guard.
function dwell() {
  return task && task.kind === 'guard' ? 1500 + Math.random() * 2500 : 2500 + Math.random() * 4500;
}

// The player she follows or guards, when in sight.
function companion(me) {
  if (!task || (task.kind !== 'follow' && task.kind !== 'guard')) return null;
  const entity = playerEntity(task.player);
  return entity && entity.position.distanceTo(me) <= 16 ? entity : null;
}

// Somewhere to look for a moment: another player close by, a creature, or a direction off to one side (now and then
// down at her feet or up at the sky). Away from `person`, never back at them.
function glance(me, person) {
  const near = (e, d) => e !== person && e !== bot.entity && e.isValid !== false && e.position.distanceTo(me) <= d;
  const roll = Math.random();
  if (roll < 0.35) {
    const other = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username && near(e, 12));
    if (other) return { entity: other };
  }
  if (roll < 0.7) {
    const creature = bot.nearestEntity((e) => Creatures.has(e.type) && near(e, person ? 12 : 16));
    if (creature) return { entity: creature };
  }
  const turn = (Math.random() < 0.5 ? -1 : 1) * (0.5 + Math.random() * (person ? 1.5 : 2.2));
  const r = Math.random();
  const pitch = r < 0.2 ? -(0.5 + Math.random() * 0.5) : r < 0.3 ? 0.2 + Math.random() * 0.4 : (Math.random() - 0.5) * 0.3;
  return { yaw: bot.entity.yaw + turn, pitch };
}

// Looks where the current glance goes; false when there's none or what she glanced at is gone.
function lookAtGlance() {
  const g = gaze.away;
  if (!g) return false;
  if (g.entity) {
    if (!g.entity.isValid) return false;
    bot.lookAt(eyes(g.entity)).catch(() => {});
  } else bot.look(g.yaw, g.pitch).catch(() => {});
  return true;
}

// Whoever chatted last, within TalkLook and 12 blocks.
function talker(me) {
  let best = null;
  let bestAt = 0;
  for (const [name, at] of lastChat) {
    if (Date.now() - at > TalkLook) {
      lastChat.delete(name);
      continue;
    }
    const entity = playerEntity(name);
    if (entity && at > bestAt && entity.position.distanceTo(me) <= 12) {
      best = entity;
      bestAt = at;
    }
  }
  return best;
}

// Away is out of sight or over 10 blocks off; back within 5 is walking up.
function greetTick() {
  if (greeting || swim || bot.isSleeping || eating || (task &&['defend', 'flee', 'guard', 'patrol'].includes(task.kind))) return;
  const me = bot.entity.position;
  for (const name of familyOrder) {
    const player = Object.values(bot.players).find((p) => p.username.toLowerCase() === name);
    const entity = player && player.entity;
    const d = entity ? entity.position.distanceTo(me) : Infinity;
    if (d > 10) {
      familyAway.set(name, true);
      continue;
    }
    if (d > 5 || familyAway.get(name) === false || Date.now() - (greetedAt.get(name) || 0) < GreetEvery) continue;
    familyAway.set(name, false);
    greetedAt.set(name, Date.now());
    crouchFor(entity).catch(() => {});
    return;
  }
}

// Two crouches; sitting, she stands up and back down instead.
async function crouchFor(entity) {
  greeting = true;
  try {
    await bot.lookAt(eyes(entity)).catch(() => {});
    for (let i = 0; i < 2; i++) {
      bot.setControlState('sneak', !sitting);
      await wait(250);
      bot.setControlState('sneak', sitting);
      await wait(250);
    }
  } finally {
    greeting = false;
  }
}
