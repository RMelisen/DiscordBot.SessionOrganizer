// Swimming, which the pathfinder can't: up for air, diving after someone, treading water, shaking off on land.

import pathfinderPackage from 'mineflayer-pathfinder';
import { columnSteer, dragged, inColumn } from './columns.js';
import { eating } from './food.js';
import { eyes, greeting, setGreeting } from './gaze.js';
import { sitting } from './gestures.js';
import { RetreatHealth } from './guard.js';
import { playerEntity } from './orders.js';
import { drawing } from './shooting.js';
import { bot, emit, task, wait } from './state.js';

const { goals } = pathfinderPackage;

// The pathfinder swims on the surface (it holds jump in water while it walks her) but never plans a step under it,
// and nothing keeps her up once it stops. So whenever she's in water with nowhere to walk she treads it, head up, and
// her air is watched: under water with little left she swims up, whatever she's doing, around a ceiling if there is
// one. Following someone who dives (or tagging along with them), she goes down after them on her own steering, back up
// to breathe when her air runs low, and down again. Following someone who sits in a boat with a free seat, she climbs
// in as their passenger: they steer, and she places nothing. Out of the water after a swim, she shakes herself off.
const WaterBlocks = new Set(['water', 'bubble_column', 'kelp', 'kelp_plant', 'seagrass', 'tall_seagrass']);
// bot.oxygenLevel, out of AirMax: 15 seconds of air in all.
const AirMax = 20;
// Under water with this little left, she goes up, whatever she's doing.
const AirLow = 8;
// Diving: back up with this much left, and down again once she has breathed this much back.
const AirDiveUp = 7;
const AirDiveAgain = 19;
const DiveReach = 24;
const DiveAgainAfter = 3000;
// Badly hurt on a dive: up, and no more diving for a while.
const DiveHurtPause = 60000;
// Wet for this long, she shakes herself off once back on dry land; once a minute at most.
const ShakeAfter = 3000;
const ShakeEvery = 60000;

// What her own swimming is doing: null, a dive ({ mode: 'dive', phase: down|up }) or going up for air
// ({ mode: 'surface' }). The pathfinder is left alone by the second, which only takes the controls.
export let swim = null;
let treading = false;
let drowning = false;
let wetSince = null;
let shakeAt = 0;
let diveAgainAt = 0;

function isWater(block) {
  if (!block) return false;
  if (WaterBlocks.has(block.name)) return true;
  try {
    return block.getProperties().waterlogged === true;
  } catch {
    return false;
  }
}

// A bubble column over magma (drag) pulls down; one over soul sand lifts.
export function isDragColumn(block) {
  return Boolean(block && block.name === 'bubble_column' && block.getProperties().drag === true);
}

// Whether someone's eyes are under water: hers (she needs air), or theirs (they dived), or a drowned's (out of reach).
export function headUnder(entity) {
  return Boolean(entity && entity.position && bot.blockAt && isWater(bot.blockAt(entity.position.offset(0, (entity.height || 1.8) * 0.9, 0))));
}

function air() {
  return Number.isFinite(bot.oxygenLevel) ? bot.oxygenLevel : AirMax;
}

// Water straight up to the open air above her head: nothing solid, no column pulling down.
function clearAbove() {
  const head = bot.entity.position.offset(0, 1.62, 0).floored();
  for (let dy = 0; dy <= 16; dy++) {
    const b = bot.blockAt(head.offset(0, dy, 0));
    if (!b || isDragColumn(b)) return false;
    if (isWater(b)) continue;
    return b.boundingBox === 'empty';
  }
  return false;
}

// The closest bit of surface (water with air above it) within 8 blocks around and 12 up, none in a column pulling
// down. Only looked for under a ceiling, so the cost (a few thousand block reads) is rare.
function surfaceSpot() {
  const eye = bot.entity.position.offset(0, 1.62, 0);
  const base = eye.floored();
  let best = null;
  let bestD = Infinity;
  for (let dx = -8; dx <= 8; dx++)
    for (let dz = -8; dz <= 8; dz++)
      for (let dy = -2; dy <= 12; dy++) {
        const p = base.offset(dx, dy, dz);
        const b = bot.blockAt(p);
        if (!isWater(b) || isDragColumn(b)) continue;
        const above = bot.blockAt(p.offset(0, 1, 0));
        if (!above || isWater(above) || above.boundingBox !== 'empty') continue;
        const d = p.offset(0.5, 0.5, 0.5).distanceTo(eye);
        if (d < bestD) {
          best = p;
          bestD = d;
        }
        break;
      }
  return best;
}

function releaseSwim() {
  for (const control of ['forward', 'jump', 'sprint']) bot.setControlState(control, false);
}

// Seated in a boat: no swimming, no treading, her keys let go.
export function stopSwimming() {
  swim = null;
  treading = false;
  releaseSwim();
}

// Dead: whatever the water was doing with her is over.
export function forgetSwim() {
  swim = null;
  drowning = false;
  wetSince = null;
}

// Up for air: straight up when it's open, else towards the closest surface. `s` keeps the spot for a second.
function surfaceTick(s) {
  if (clearAbove()) {
    bot.setControlState('forward', false);
    bot.setControlState('jump', true);
    return;
  }
  if (Date.now() >= (s.spotAt || 0)) {
    s.spot = surfaceSpot();
    s.spotAt = Date.now() + 1000;
  }
  bot.setControlState('jump', true);
  bot.setControlState('sprint', false);
  if (!s.spot) return bot.setControlState('forward', false);
  const to = s.spot.offset(0.5, 0.9, 0.5);
  const me = bot.entity.position;
  bot.lookAt(to, true).catch(() => {});
  bot.setControlState('forward', Math.hypot(to.x - me.x, to.z - me.z) > 0.6);
}

// Whom she'd dive after: the player she follows or tags along with, under water, in reach, not in a boat.
function diveTarget() {
  const t = task;
  if (!t || (t.kind !== 'follow' && t.kind !== 'tagAlong')) return null;
  const who = t.kind === 'follow' ? t.player : t.who;
  const target = playerEntity(who);
  if (!target || target.vehicle || !headUnder(target) || target.position.distanceTo(bot.entity.position) > DiveReach) return null;
  return who;
}

function startDive(who) {
  swim = { mode: 'dive', phase: 'down', task, who, hurt: false };
  bot.pathfinder.setGoal(null); // her own steering now: a follow goal would fight it, and report no path
  if (!task.dived) {
    task.dived = true; // a word the first time per order, not on every breath
    emit('diving', { player: who });
  }
}

// Down after them: towards their eyes, rising when they're above her and sinking when they're below; she stops beside
// them. Up when her air runs low, when they come up or out of reach, when she's badly hurt, or in a column pulling down.
function diveTick(s, under) {
  const target = playerEntity(s.who);
  if (task !== s.task || !target || target.vehicle || target.position.distanceTo(bot.entity.position) > DiveReach) return endDive(s);
  if (s.phase === 'down') {
    const hurt = bot.health <= RetreatHealth;
    const theyAreUnder = headUnder(target);
    if (air() <= AirDiveUp || hurt || !theyAreUnder || isDragColumn(bot.blockAt(bot.entity.position.floored()))) {
      s.phase = 'up';
      s.hurt = hurt;
      if (theyAreUnder && !hurt) emit('surfacing', { player: s.who });
    } else {
      const me = bot.entity.position;
      const aim = eyes(target);
      bot.lookAt(aim, true).catch(() => {});
      bot.setControlState('sprint', false);
      bot.setControlState('forward', Math.hypot(aim.x - me.x, aim.z - me.z) > 1.5);
      bot.setControlState('jump', target.position.y > me.y + 0.3);
      return;
    }
  }
  if (!under) return endDive(s);
  surfaceTick(s);
}

// The dive is over (her head is out, or the order changed): back to the follow or the tagging along, if still hers.
function endDive(s) {
  swim = null;
  releaseSwim();
  diveAgainAt = Date.now() + (s.hurt ? DiveHurtPause : DiveAgainAfter);
  if (task !== s.task) return;
  if (task.kind === 'tagAlong') {
    task.mode = null;
    return;
  }
  const target = playerEntity(task.player);
  if (target) bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
}

// Every physics tick, after the pathfinder's own (registered on spawn, so it overrides it): air first, then a dive
// or a column, then treading water, then shaking herself off on dry land.
export function waterTick() {
  if (!bot.entity || bot.vehicle || bot.isSleeping) return;
  const me = bot.entity;
  const under = headUnder(me);
  if (under && air() <= 0 && !drowning) {
    drowning = true;
    emit('drowning', {});
  } else if (!under && drowning) {
    drowning = false;
    emit('breathing', {});
  }

  if (swim && swim.mode === 'dive') return diveTick(swim, under);
  if (swim && swim.mode === 'surface') {
    if (under) return surfaceTick(swim);
    swim = null;
    releaseSwim();
  }
  if (under && air() <= AirLow && !dragged()) {
    // A dig under way lets go: it goes on under water whatever her keys say, and the pathfinder would steer her back to
    // it. Gathering skips that block (collectblock's dig fails) and goes on with the next.
    if (bot.targetDigBlock) {
      bot.stopDigging();
      bot.pathfinder.setGoal(null);
    }
    swim = { mode: 'surface' };
    return surfaceTick(swim);
  }
  const wet = me.isInWater;
  // In a column the path takes her after them (columnMoves): a dive there would steer against it, and in a drag would
  // only try to go up.
  const column = wet && inColumn(me.position.floored());
  if (wet && !column && air() >= AirDiveAgain && Date.now() >= diveAgainAt && bot.health > RetreatHealth) {
    const who = diveTarget();
    if (who) return startDive(who);
  }
  if (column) columnSteer();

  // Treading: in water, off the bottom or with her head under, and the pathfinder not swimming her anywhere. Standing
  // in shallow water she stays put.
  if (wet && (!me.onGround || under) && !bot.pathfinder.isMoving()) {
    bot.setControlState('jump', true);
    treading = true;
  } else if (treading) {
    treading = false;
    if (!bot.pathfinder.isMoving()) bot.setControlState('jump', false);
  }

  if (wet) {
    if (!wetSince) wetSince = Date.now();
  } else if (wetSince && me.onGround) {
    const swam = Date.now() - wetSince >= ShakeAfter;
    wetSince = null;
    if (swam) shakeOff().catch(() => {});
  }
}

// Three quick crouches, the way a wet dog shakes. Not in a fight, on the run, sitting, eating or greeting.
async function shakeOff() {
  if (Date.now() < shakeAt || greeting || eating || sitting || drawing) return;
  if (task && ['defend', 'flee', 'guard', 'patrol'].includes(task.kind)) return;
  shakeAt = Date.now() + ShakeEvery;
  setGreeting(true);
  try {
    for (let i = 0; i < 3; i++) {
      bot.setControlState('sneak', true);
      await wait(120);
      bot.setControlState('sneak', false);
      await wait(120);
    }
  } finally {
    setGreeting(false);
  }
  emit('shook', {});
}
