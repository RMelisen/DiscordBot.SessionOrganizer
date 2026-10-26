// Defending a player a monster hurts near her, and Papa and Tata against players.

import { isFamily } from './family.js';
import { FollowsSomeone } from './flee.js';
import { eatIfHungry } from './food.js';
import { RetreatHealth } from './guard.js';
import { isActivity, isFree, stay } from './orders.js';
import { rangedStep } from './ranged.js';
import { Restartable, resumeAfterBreak, resumeFor } from './resume.js';
import { equipWeapon } from './shooting.js';
import { bot, setTask, task } from './state.js';
import { chase, FightLeash, fightPace, strike } from './strike.js';
import { headUnder } from './water.js';

// A monster hitting a player within DefendRadius of her, anyone, she fights it herself, saying nothing: someone near her
// getting hurt is hers to help, whatever she was doing. A player hitting her family (C# sends their in-game names on
// spawn, `family`) is reported (`familyHurt`) and C# decides what comes of it: a word, one hit back (`defend` with
// swat), then a fight. She goes when free, during an activity, following or guarding anyone, or busy with an order she
// can start again (Restartable), and she goes back to it after (resumeFor). Only what's over in seconds or can't be
// left (smelting at a furnace, handing over, storing, a bed, a portal, running away) keeps her, and C# keeps to words
// (`cantDefend`). She never hits family (family.js) or a creeper, and breaks off below RetreatHealth.
export const DefendRadius = 16;
const DefendMobFor = 20000;
const DefendFightFor = 15000;
const SwatFor = 6000;

export function canDefend() {
  if (bot.isSleeping) return false;
  if (task && task.kind === 'fight' && task.stance === 'getOut') return false; // never back towards a warden
  if (isFree(task) || ['defend', 'back', 'fight'].includes(task.kind) || isActivity(task) || FollowsSomeone.has(task.kind)) return true;
  return Boolean(task.command) && Restartable.has(task.kind);
}

// True when she takes it on.
export function defend(foe, victim, swat) {
  if (!foe || foe.isValid === false || foe.name === 'creeper' || (foe.type === 'player' && isFamily(foe.username))) return false;
  if (!canDefend() || foe.position.distanceTo(bot.entity.position) > DefendRadius || headUnder(foe)) return false;
  const lasts = swat ? SwatFor : foe.type === 'player' ? DefendFightFor : DefendMobFor;
  if (task && task.kind === 'defend') {
    // Already at it: a new attacker takes over, and a fight never shrinks back to one hit.
    task.swat = task.swat && swat && task.foe === foe;
    if (task.foe !== foe) task.fightFrom = foe.position.clone();
    task.foe = foe;
    task.until = Date.now() + lasts;
    return true;
  }
  const resume = resumeFor(task);
  stay();
  setTask({ kind: 'defend', player: '', victim, foe, swat, resume, until: Date.now() + lasts, mode: null, modeTarget: null,
    fightFrom: foe.position.clone() });
  equipWeapon().catch(() => {});
  return true;
}

// Every physics tick while defending: chase the attacker, hit it on the weapon's cooldown; one hit is enough for a
// swat. Over when it's dead or gone, too far, out of time, or she's badly hurt.
export function defendTick() {
  const t = task;
  if (!t || t.kind !== 'defend') return;
  const foe = t.foe;
  const me = bot.entity.position;
  if (!foe || foe.isValid === false || !bot.entities[foe.id] || foe.position.distanceTo(me) > DefendRadius
      || Date.now() > t.until || bot.health <= RetreatHealth || headUnder(foe) || foe.position.distanceTo(t.fightFrom) > FightLeash)
    return endDefend(t);
  if (rangedStep(t, foe)) return;
  chase(t, foe);
  if (strike(foe, t) && t.swat) endDefend(t);
}

export function endDefend(t) {
  if (task !== t) return;
  setTask(null);
  fightPace(false);
  bot.pathfinder.setGoal(null);
  if (bot.health <= RetreatHealth) eatIfHungry(20).catch(() => {});
  resumeAfterBreak(t.resume);
}
