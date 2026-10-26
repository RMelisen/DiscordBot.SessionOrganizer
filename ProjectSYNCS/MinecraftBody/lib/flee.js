// Running away from a player trying to kill her.

import pathfinderPackage from 'mineflayer-pathfinder';
import { playerEntity, stay } from './orders.js';
import { resumeAfterBreak, resumeFor } from './resume.js';
import { bot, emit, setTask, task } from './state.js';

const { goals } = pathfinderPackage;

// C# sends `flee` when a player keeps hitting her (who counts, and that Papa never does, is decided there).
// She sprints away until she's FleeDistance blocks from them or FleeTime has passed, then says she's safe.
// An activity (farm, fishing, a guarded place) or a follow or guard is picked up again afterwards; anything
// else is dropped and its player told why (`dropped`). She still never digs or builds to get away: with no
// way out she's `cornered`, once per flight.
const FleeDistance = 20;
const FleeTime = 15000;
export const FollowsSomeone = new Set(['follow', 'guard']);

export function flee(player) {
  const attacker = playerEntity(player);
  if (!attacker) return;
  if (task && task.kind === 'defend' && task.foe === attacker) return; // she's the one fighting them
  if (task && task.kind === 'flee') {
    task.until = Date.now() + FleeTime; // hit again while running: keep running
    return;
  }
  const previous = task;
  stay();
  const resume = resumeFor(previous);
  setTask({ kind: 'flee', player, resume, until: Date.now() + FleeTime, reported: false });
  bot.pathfinder.setGoal(new goals.GoalInvert(new goals.GoalFollow(attacker, FleeDistance)), true);
  if (previous && !resume && previous.player && previous.kind !== 'seek' && previous.kind !== 'sleep')
    emit('dropped', { player: previous.player, kind: previous.kind, by: player });
  emit('fleeing', { player });
}

// Every physics tick while running: far enough, out of sight or out of time, and she stops.
export function fleeTick() {
  if (!task || task.kind !== 'flee') return;
  const attacker = playerEntity(task.player);
  if (attacker && attacker.position.distanceTo(bot.entity.position) < FleeDistance && Date.now() < task.until) return;
  const done = task;
  setTask(null);
  bot.pathfinder.setGoal(null);
  emit('safe', { player: done.player });
  resumeAfterBreak(done.resume);
}
