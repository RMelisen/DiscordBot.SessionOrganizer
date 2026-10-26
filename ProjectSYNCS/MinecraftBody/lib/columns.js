// Water columns: her own steps up and down a shaft of water, a waterfall or a bubble column, within her air.

import Move from 'mineflayer-pathfinder/lib/move.js';
import vec3 from 'vec3';
import { airStates, dragStates, liftStates, waterStates, wetStates } from './blocks.js';
import { currentPath } from './doors.js';
import { bot } from './state.js';

const { Vec3 } = vec3;

// A shaft of water players swim up to their base or sink down from (a bubble column over soul sand lifts, one over
// magma drags down). The pathfinder plans no step up or down in water (none under the surface, and no jump out of it),
// so a column is a wall to it. In a column she gets her own steps: a block up, a block down, and out at the top onto a
// ledge a block over her feet. A column is water one block wide: one of its four
// sides at most is water (the doorway at its foot, where it spills out), and every block of a lake has two, a corner
// too. So a lake or the sea never is one: open water stays the dive's, and the sea's magma columns walls.
// The whole way under water must fit in her air, about 9 s before she goes up for more (AirLow), so 8 s of it, at the
// speeds Mineflayer's physics gives her: in still water she swims up 3.3 blocks a second but sinks only 0.5 (no
// crouching down in Mineflayer); falling water beside a wall (a waterfall down a cave wall, or a shaft of flowing water)
// pulls her down, so 2 up and 1.8 down; a bubble column carries her 10 a second up or 4 down, and never the other way.
const ColumnRun = { still: { up: 24, down: 4 }, fall: { up: 16, down: 14 }, lift: { up: 64, down: 0 }, drag: { up: 0, down: 32 } };
const ColumnSides = [[1, 0], [-1, 0], [0, 1], [0, -1]];

// What a column block does to her (ColumnRun's kinds). Falling water (level 8 and up) pulls down only with something
// solid beside it, as Mineflayer's physics has it (getFlow).
function columnKind(pos) {
  const id = bot.world.getBlockStateId(pos);
  if (liftStates.has(id)) return 'lift';
  if (dragStates.has(id)) return 'drag';
  if (!waterStates.has(id) || id - bot.registry.blocksByName.water.minStateId < 8) return 'still';
  return ColumnSides.some(([dx, dz]) => {
    const side = bot.world.getBlockStateId(pos.offset(dx, 0, dz));
    return !airStates.has(side) && !wetStates.has(side);
  }) ? 'fall' : 'still';
}

// State ids only: asked for every step the pathfinder weighs (gentleMovements has why).
export function inColumn(pos) {
  if (!wetStates.has(bot.world.getBlockStateId(pos))) return false;
  return ColumnSides.filter(([dx, dz]) => wetStates.has(bot.world.getBlockStateId(pos.offset(dx, 0, dz)))).length <= 1;
}

// How many blocks of column from `pos` on, going up (dy 1) or down (-1): the way she'd swim under. Stops past `max`.
function columnRun(pos, dy, max) {
  let n = 0;
  for (let p = pos; n <= max && inColumn(p); p = p.offset(0, dy, 0)) n++;
  return n;
}

// Her steps in a column, beside the pathfinder's own. Also in and out at its foot, where it ends in other water: the
// puddle a waterfall spreads into at the bottom (wet all round, so no column), or a lake it falls into.
export function columnMoves(movements, node) {
  const here = new Vec3(node.x, node.y, node.z);
  const above = here.offset(0, 1, 0);
  const inside = inColumn(here);
  if (!inside && !(wetStates.has(bot.world.getBlockStateId(here)) && inColumn(above))) return [];
  const cost = 1 + movements.liquidCost;
  const moves = [];
  const step = (dx, dy, dz, c) => moves.push(new Move(node.x + dx, node.y + dy, node.z + dz, node.remainingBlocks, c));
  // Each way by what the block she moves into does to her: over a waterfall's edge the water still runs level, and
  // falls from the next block down.
  const below = here.offset(0, -1, 0);
  const up = inColumn(above) ? ColumnRun[columnKind(above)].up : 0;
  if (up > 0 && movements.getBlock(node, 0, 2, 0).safe && columnRun(above, 1, up) <= up) step(0, 1, 0, cost);
  if (!inside) return moves;
  const down = wetStates.has(bot.world.getBlockStateId(below)) ? ColumnRun[columnKind(below)].down : 0;
  if (down > 0 && columnRun(here, -1, down) <= down) step(0, -1, 0, cost);
  // Out at the top: open above the surface, and a ledge beside it no higher than a block over her feet, with room to
  // stand. She climbs out as a swimmer does, pushing against it (the pathfinder holds jump in water). Out into the
  // shallow stream feeding a waterfall is the pathfinder's own step (into water, level).
  if (wetStates.has(bot.world.getBlockStateId(above)) || !movements.getBlock(node, 0, 1, 0).safe
    || !movements.getBlock(node, 0, 2, 0).safe) return moves;
  for (const [dx, dz] of ColumnSides) {
    const ledge = movements.getBlock(node, dx, 0, dz);
    if (!ledge.physical || ledge.height > node.y + 1.01) continue;
    if (movements.getBlock(node, dx, 1, dz).safe && movements.getBlock(node, dx, 2, dz).safe) step(dx, 1, dz, cost + 1);
  }
  return moves;
}

// The pathfinder puts a path point on top of what fills its block, or a block lower when that has no top
// (postProcessPath): a bubble column, kelp. In a column she swims to the block itself, as it does for water.
export function columnPoint(node) {
  if (node.columnY === undefined) return;
  node.x = Math.floor(node.x) + 0.5;
  node.y = node.columnY;
  node.z = Math.floor(node.z) + 0.5;
}

// Her next point straight above or below her in a column: the pathfinder holds jump in any water, so going down she
// lets go of it (or she'd never sink), and once over the point she lets go of forward too: pushing on past it would
// take her out of a column in the open air (a waterfall).
export function columnSteer() {
  const next = currentPath[0];
  if (!next || !bot.pathfinder.isMoving()) return;
  const me = bot.entity.position;
  const off = Math.hypot(next.x - me.x, next.z - me.z);
  const cell = me.floored();
  if (off > 0.5 || Math.abs(next.y - me.y) < 0.5 || !(inColumn(cell) || inColumn(cell.offset(0, 1, 0)))) return;
  bot.setControlState('jump', next.y > me.y);
  bot.setControlState('sprint', false);
  bot.setControlState('forward', off > 0.15);
}

// Down a column that drags (magma at its foot) there's no going up for air: the way out is at the bottom, where she's
// headed.
export function dragged() {
  return bot.pathfinder.isMoving() && dragStates.has(bot.world.getBlockStateId(bot.entity.position.floored()));
}
