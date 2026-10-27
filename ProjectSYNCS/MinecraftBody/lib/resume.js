// Going back to what a fight, a run or a defence interrupted (resumeFor), an order started again from its command.

import pathfinderPackage from 'mineflayer-pathfinder';
import { FollowsSomeone } from './flee.js';
import { isActivity, pausedActivity, playerEntity, resumeActivity } from './orders.js';
import { redo } from './restart.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// What running away or defending someone goes back to: an activity, a follow or guard, or an order she can start again
// (Restartable: gathering, harvesting, a walk somewhere). Never her own: autonomy picks again by itself once she's free.
export function resumeFor(previous) {
  if (previous && previous.kind === 'back') return { again: previous.again }; // on her way back to one already
  // Fighting for herself, putting herself out, defending someone: that one's own way back (an order held mid-fight
  // first). Without it, a walk to water taking over a fight lost the follow it was going back to.
  if (previous && previous.kind === 'fight' && previous.held) return { again: () => redo(previous.held) };
  if (previous && ['fight', 'hazard', 'defend'].includes(previous.kind)) return previous.resume;
  if (previous && !previous.own && previous.command && Restartable.has(previous.kind)) {
    // A camp goes on where it stands, with what's on it and the same floor; a craft at the table she set down.
    const command = previous.kind === 'camp'
      ? { ...previous.command, site: previous.site, placed: previous.placed, floor: previous.floor, heights: previous.heights }
      : previous.kind === 'craft' ? { ...previous.command, table: previous.table } : previous.command;
    return { restart: command, at: bot.entity.position.clone(), before: previous.before || null };
  }
  const kept = pausedActivity(previous) || (previous && FollowsSomeone.has(previous.kind) ? previous : null);
  return kept && !kept.own ? kept : null;
}

// After running away or defending someone: back to the activity, to the player she was following or guarding if she
// still sees them, or to the order she was given.
export function resumeAfterBreak(resume) {
  if (!resume) return;
  if (resume.again) return resume.again();
  if (resume.restart) return restartOrder(resume);
  if (isActivity(resume)) return resumeActivity(resume);
  const target = playerEntity(resume.player);
  if (!target) return emit('cantReach', { player: resume.player, why: 'lost' });
  setTask({ ...resume, foe: null, mode: null, modeTarget: null });
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
}

// The orders a fight (or running away) can interrupt and start again, by the command that started them. Gathering and
// harvesting look around where she stands, so she first walks back to where she stopped (BackFor at most), then the
// same command runs again from the same pockets (`since`): what she had already gathered still counts.
export const Restartable = new Set(['collect', 'harvest', 'come', 'goto', 'guide', 'camp', 'uncamp', 'craft']);
const BackFor = 20000;

function restartOrder(r) {
  const from = r.at;
  const again = () => redo({ ...r.restart, since: r.before });
  if (!from || bot.entity.position.distanceTo(from) <= 3 || !['collect', 'harvest'].includes(r.restart.op)) return again();
  setTask({ kind: 'back', player: r.restart.player, again, until: Date.now() + BackFor });
  bot.pathfinder.setGoal(new goals.GoalNear(from.x, from.y, from.z, 2));
}

// Every physics tick on the way back: there (or out of time, or no way), and the order starts again.
export function backTick() {
  const t = task;
  if (!t || t.kind !== 'back') return;
  if (bot.pathfinder.isMoving() && Date.now() < t.until) return;
  setTask(null);
  bot.pathfinder.setGoal(null);
  t.again();
}
