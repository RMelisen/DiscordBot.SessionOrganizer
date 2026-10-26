// Creepers, whatever she's doing: shot from a distance or kept away from, and players warned of one behind them.

import pathfinderPackage from 'mineflayer-pathfinder';
import { canDefend, DefendRadius } from './defend.js';
import { CreeperRange, setMode } from './guard.js';
import { stay } from './orders.js';
import { resumeAfterBreak, resumeFor } from './resume.js';
import { bowReady, eyesOf, inSight, playerInLine, shoot, sideStep, stopBow } from './shooting.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// Guarding, a creeper was always guardTick's. Busy otherwise (a field, fishing, gathering, following, her own errands)
// she paid it no mind, and one walked up and blew up beside her (local server: a crater, 7 hearts left of 20). Now one
// closer than CreeperNear takes her off whatever can be left and picked up again (canDefend's rule, resumeFor's way
// back): shot from CreeperRange or more when she has a bow and arrows, else kept out of its reach, never hit by hand.
// Dead, gone or CreeperClear off, she goes back to what she was doing. What can't be left (a furnace, handing over, a
// bed, a portal, running away) is over in seconds.
const CreeperNear = 10;
const CreeperClear = 16;
const DodgeFor = 30000;
const CreeperDraw = 12;
// This close to a player near her, a creeper is hers to deal with too: at 5 it was a second from its fuse, and three
// arrows at 1.1 s a draw came too late (it blew up by the player at her camp).
const CreeperByThem = 10;

export function creeperTick() {
  if (!bot.entity || bot.isSleeping || !canDefend()) return;
  if (task && ['guard', 'patrol', 'dodge'].includes(task.kind)) return;
  const me = bot.entity.position;
  // Near her, or close to someone near her: one walked past her to the player by her camp and blew up there.
  const others = Object.values(bot.players).map((p) => p.entity)
    .filter((e) => e && e !== bot.entity && e.position.distanceTo(me) < DefendRadius);
  // And in sight, hers or theirs: one in a cave under the ground can't get at anyone, and she shot into the ground at it.
  const creeper = bot.nearestEntity((e) => e.name === 'creeper' && e.isValid !== false && e.position.distanceTo(me) < DefendRadius
    && ((e.position.distanceTo(me) < CreeperNear && inSight(e))
      || others.some((o) => o.position.distanceTo(e.position) < CreeperByThem && inSight(e, eyesOf(o)))));
  if (!creeper) return;
  // Fighting for someone, the way back is that fight's.
  const resume = task && task.kind === 'defend' ? task.resume : resumeFor(task);
  stay();
  setTask({ kind: 'dodge', player: '', creeper, resume, until: Date.now() + DodgeFor, mode: null, modeTarget: null, modeSince: 0,
    seenAt: Date.now() });
}

// A creeper out of everyone's sight this long (gone down a hole, behind a hill) ends the dodge.
const CreeperUnseen = 3000;

function seenByAnyone(c) {
  if (inSight(c)) return true;
  const me = bot.entity.position;
  return Object.values(bot.players).some((p) => p.entity && p.entity !== bot.entity
    && p.entity.position.distanceTo(me) < DefendRadius && p.entity.position.distanceTo(c.position) < CreeperByThem
    && inSight(c, eyesOf(p.entity)));
}

// Away from it to a point (8 blocks straight off, again when she gets there): an inverted follow (« further than 12 »)
// is a goal the pathfinder often can't solve, and she stood still while the creeper walked up and blew. With a bow,
// back off to CreeperDraw, then stand and shoot: a creeper after her closes about 4 blocks in the 1.1 s a draw takes,
// and backing off only to 9 she fled again before every shot, for 30 s and 60 blocks away from her work.
export function dodgeTick() {
  const t = task;
  if (!t || t.kind !== 'dodge') return;
  const c = t.creeper;
  const me = bot.entity.position;
  if (c && c.isValid !== false && seenByAnyone(c)) t.seenAt = Date.now();
  if (!c || c.isValid === false || !bot.entities[c.id] || c.position.distanceTo(me) > CreeperClear || Date.now() > t.until
    || Date.now() - t.seenAt > CreeperUnseen) {
    setTask(null);
    stopBow();
    bot.pathfinder.setGoal(null);
    return resumeAfterBreak(t.resume);
  }
  const d = c.position.distanceTo(me);
  const bow = bowReady();
  const fleeing = t.mode === 'flee';
  if (d < CreeperRange || !bow || (fleeing && d < CreeperDraw)) {
    if (!fleeing || !bot.pathfinder.isMoving()) {
      const dx = me.x - c.position.x;
      const dz = me.z - c.position.z;
      const n = Math.hypot(dx, dz) || 1;
      t.mode = 'flee';
      t.modeTarget = c;
      bot.pathfinder.setGoal(new goals.GoalNear(me.x + (dx / n) * 8, me.y, me.z + (dz / n) * 8, 2));
    }
    return;
  }
  // Out of her own sight (behind a tree, someone else sees it): no arrow into the bark, she waits for it to show.
  if (!inSight(c)) return setMode(t, 'wait', c, null);
  const inTheWay = playerInLine(c);
  if (inTheWay) return setMode(t, 'aside', c, sideStep(c), false);
  setMode(t, 'shoot', c, null);
  shoot(c);
}

// A creeper within 4 blocks of a player (within 32 of her), behind them (over 100° off where they look): she warns them,
// once per creeper. The player she guards is guardTick's.
const CreeperBehindAngle = Math.cos((100 * Math.PI) / 180);
export const warnedCreepers = new Set();
let creeperWatchAt = 0;

export function creeperWatchTick() {
  if (Date.now() < creeperWatchAt || !bot.entity) return;
  creeperWatchAt = Date.now() + 250;
  const guarded = task && task.kind === 'guard' ? task.player : null;
  const me = bot.entity.position;
  for (const player of Object.values(bot.players)) {
    const e = player.entity;
    if (!e || player.username === bot.username || player.username === guarded || e.position.distanceTo(me) > 32) continue;
    // Not one under their floor: a creeper in a cave below is no creeper behind them.
    const creeper = bot.nearestEntity((c) => c.name === 'creeper' && c.isValid !== false && !warnedCreepers.has(c.id)
      && c.position.distanceTo(e.position) < 4 && inSight(c, eyesOf(e)));
    if (!creeper) continue;
    const vx = creeper.position.x - e.position.x;
    const vz = creeper.position.z - e.position.z;
    const len = Math.hypot(vx, vz);
    if (len < 0.01) continue;
    // Where they look: Mineflayer's yaw is 0 facing -z.
    if ((-Math.sin(e.yaw) * vx - Math.cos(e.yaw) * vz) / len > CreeperBehindAngle) continue;
    warnedCreepers.add(creeper.id);
    emit('creeperBehind', { player: player.username });
  }
}
