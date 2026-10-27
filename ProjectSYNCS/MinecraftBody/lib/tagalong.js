// Tagging along with Papa or Tata when nothing else is on.

import pathfinderPackage from 'mineflayer-pathfinder';
import { boardable } from './boats.js';
import { familyOrder } from './family.js';
import { gentleMovements } from './movements.js';
import { playerEntity, stay } from './orders.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// Nothing in the routine: she keeps near Papa, else Tata, when one is in sight. Over TagFar blocks away she walks up to
// TagNear, without sprinting unless they're over TagSprint off; close by she takes a few slow steps around them now
// and then. They go out of sight, and it ends, silently.
const TagFar = 8;
const TagNear = 4;
const TagSprint = 16;
let slowWalk = false;
let walkSprint = null;

function familyInSight() {
  for (const name of familyOrder) {
    const player = Object.values(bot.players).find((p) => p.username.toLowerCase() === name && p.entity);
    if (player) return player.username;
  }
  return null;
}

const stepPause = () => 15000 + Math.random() * 25000;

export function tagAlongTick() {
  let t = task;
  const who = familyInSight();
  if (!who) {
    if (t && t.kind === 'tagAlong') stay();
    return;
  }
  if (!t || t.kind !== 'tagAlong' || t.who !== who) {
    stay();
    t = { kind: 'tagAlong', own: true, player: '', who, mode: null, stepAt: Date.now() + stepPause() };
    setTask(t);
    emit('tagAlong', { player: who });
  }
  const target = playerEntity(who);
  const d = target.position.distanceTo(bot.entity.position);
  // In a boat with room for her: right up to it, so boardTick can climb in.
  if (boardable(target)) {
    if (t.mode !== 'board') {
      t.mode = 'board';
      walkPace(d > TagSprint);
      bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
    }
    return;
  }
  if (t.mode === 'board') t.mode = null;
  if (d > TagFar) {
    walkPace(d > TagSprint);
    if (t.mode !== 'follow') {
      t.mode = 'follow';
      bot.pathfinder.setGoal(new goals.GoalFollow(target, TagNear), true);
    }
    return;
  }
  if (t.mode === 'follow' && d <= TagNear + 1) {
    t.mode = null;
    t.stepAt = Date.now() + stepPause();
    bot.pathfinder.setGoal(null);
  }
  if (t.mode === 'step' && !bot.pathfinder.goal) t.mode = null;
  if (t.mode === null && Date.now() >= t.stepAt) {
    walkPace(false);
    const angle = Math.random() * 2 * Math.PI;
    const r = 2 + Math.random() * 2;
    const p = target.position;
    t.mode = 'step';
    t.stepAt = Date.now() + stepPause();
    bot.pathfinder.setGoal(new goals.GoalNear(p.x + Math.cos(angle) * r, p.y, p.z + Math.sin(angle) * r, 1));
  }
}

// Her walk while tagging along: sprinting only to catch up from far. stay() puts the usual one back (endSlowWalk).
function walkPace(sprint) {
  if (slowWalk && walkSprint === sprint) return;
  const movements = gentleMovements();
  movements.allowSprinting = sprint;
  bot.pathfinder.setMovements(movements);
  slowWalk = true;
  walkSprint = sprint;
}

export function endSlowWalk() {
  if (!slowWalk) return;
  slowWalk = false;
  walkSprint = null;
  bot.pathfinder.setMovements(gentleMovements());
}
