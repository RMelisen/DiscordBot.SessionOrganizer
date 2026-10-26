// How she walks: the pathfinder's Movements made gentle (never digs or places, doors and trapdoors read right, what
// hurts avoided, fields walked round), and no jump over farmland.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { burningStates, farmlandStates, prickly, waterStates, wetStates } from './blocks.js';
import { columnMoves, inColumn } from './columns.js';
import { currentPath, doorKey, isDoor, isTrapdoor, panelSide, stuckDoors } from './doors.js';
import { bot } from './state.js';
import { isDragColumn } from './water.js';

const { Movements } = pathfinderPackage;
const { Vec3 } = vec3;

// She walks and jumps, never digs and never places a block: a path through someone's wall or a
// dirt pillar in the middle of their build is not help. (Felling a tree, she climbs a pillar of her own and takes it
// down after: gatheringMovements' climb.)
export function gentleMovements() {
  const movements = new Movements(bot);
  movements.canDig = false;
  movements.scafoldingBlocks = [];
  movements.allow1by1towers = false;
  // The pathfinder sees every door as a wall (its own door option is off, flaky, and gates only): a wooden door or
  // a fence gate is a way through, whatever its state, and doorTick swings it out of the way as she comes. Not on
  // a diagonal step, where a corner catches the panel, nor one that refused to move: a wall again for a while.
  // A bubble column over magma pulls whoever swims in it to the bottom: the pathfinder takes it for air, so it's
  // made a wall, except in a water column (columnMoves), where any bubble column is water to swim. Trapdoors the
  // pathfinder takes for full blocks: an open one is a thin panel on one edge, passable along that edge and a wall
  // across her way when it faces her (it planned to stand on the panel, and she fell off it, again and again); a closed one in the top half of the block at head height leaves 1.8125 under it, and she's 1.8
  // tall, so she walks under it.
  const getBlock = movements.getBlock.bind(movements);
  const passable = (b) => {
    b.safe = true;
    b.physical = false;
    b.replaceable = false;
    b.openable = false;
    b.height = b.position.y;
  };
  movements.getBlock = (pos, dx = 0, dy = 0, dz = 0) => {
    const b = getBlock(pos, dx, dy, dz);
    if (b && b.name === 'bubble_column') {
      if (b.position && inColumn(b.position)) {
        b.liquid = true;
        b.safe = true;
      } else if (isDragColumn(b)) b.safe = false;
      return b;
    }
    // A cactus is no floor: once she cut a column down to its bottom block, the pathfinder walked her onto it and she
    // stood there being pricked. Not to stand on nor to walk through; beside one costs more (exclusionAreasStep).
    if (b && b.name === 'cactus') {
      b.safe = false;
      b.physical = false;
      return b;
    }
    if (!b || !b.position || (dx !== 0 && dz !== 0)) return b;
    if (isDoor(b) && !stuckDoors.has(doorKey(b))) passable(b);
    else if (isTrapdoor(b)) {
      const p = b.getProperties();
      const side = p.open ? panelSide(b) : null;
      const across = (dx !== 0 && (side === 'e' || side === 'w')) || (dz !== 0 && (side === 'n' || side === 's'));
      const underIt = dy === 1 && p.half === 'top' && getBlock(pos, dx, 0, dz).boundingBox === 'empty'
        && getBlock(pos, dx, -1, dz).height <= pos.y; // a full floor or lower: her head stays under 1.8125
      if ((p.open && !across) || underIt) passable(b);
    }
    return b;
  };
  // Fields cost more to cross than the grass around them (farmlandTick has why), so she walks round when it's not far.
  // Asked for every step the pathfinder weighs: the state id only (a whole block per step slowed the search past its
  // 5 s limit, and a goal out of reach read as « still looking » instead of « no way »).
  // What hurts to walk through (the pathfinder only avoids fire, cobwebs and lava; a berry bush cost her a heart on
  // the local course), and powder snow, which swallows her.
  for (const name of ['sweet_berry_bush', 'wither_rose', 'soul_fire', 'powder_snow'])
    if (bot.registry.blocksByName[name]) movements.blocksToAvoid.add(bot.registry.blocksByName[name].id);
  // A portal is only walked into on purpose (crossAfter): a path cut through one took her to the Nether alone.
  if (bot.registry.blocksByName.nether_portal) movements.blocksToAvoid.add(bot.registry.blocksByName.nether_portal.id);
  movements.exclusionAreasStep.push((block) => {
    if (!block || !block.position) return 0;
    const floor = bot.world.getBlockStateId(block.position.offset(0, -1, 0));
    if (farmlandStates.has(floor)) return FarmlandStep;
    if (burningStates.has(floor)) return BurningFloor;
    if (isFieldWater(block.position)) return FieldWaterStep;
    // Beside what hurts to touch: she walks round it, but a turn cut close clips its block (a heart lost by a bush).
    return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) =>
      prickly.has(bot.world.getBlockStateId(block.position.offset(dx, 0, dz)))) ? PricklyNear : 0;
  });
  // No jump over a gap that takes off from a field or lands on one (farmlandTick would cancel the take-off and she'd
  // walk into the gap), and a drop onto one costs FieldDrop: a fall of a block tramples it one time in two. Up and down
  // a water column are her own steps (columnMoves); a step into a bubble column keeps its height (columnPoint).
  const getNeighbors = movements.getNeighbors.bind(movements);
  movements.getNeighbors = (node) => [...getNeighbors(node), ...columnMoves(movements, node)].filter((move) => {
    if (move.parkour && (onField(node) || onField(move))) return false;
    if (move.y < node.y && onField(move)) move.cost += FieldDrop;
    const id = bot.world.getBlockStateId(move);
    if (wetStates.has(id) && !waterStates.has(id) && inColumn(move)) move.columnY = move.y;
    return true;
  });
  return movements;
}

// Landing on farmland from more than half a block tramples it back to dirt, crop and all; walking never does. Her
// jumps do: the pathfinder sprint-jumps across open ground, and the field's own water hole bends a straight line into
// one. So a field costs FarmlandStep a block to cross (round it when that's shorter), and over farmland she doesn't
// jump unless she's climbing (a step up onto a field lands lower than it took off). Checked on a local server: 2 of
// 120 ripe wheat lost in six crossings before, none after.
const FarmlandStep = 4;
// A field's water hole: walked into, she swims back out with a push that lands her on the farmland beside it, from
// higher than a step (caught on the local server: two blocks of farmland trampled climbing out). So she keeps out.
const FieldWaterStep = 40;
const FieldDrop = 40;

// Whether a path point (a block position, feet inside it) stands on farmland.
const onField = (p) => farmlandStates.has(bot.world.getBlockStateId(new Vec3(p.x, p.y - 1, p.z)));
const isFarmland = (block) => Boolean(block && block.name === 'farmland');
const BurningFloor = 40;
const PricklyNear = 8;

// Water with farmland beside it, at the same height.
function isFieldWater(pos) {
  if (!waterStates.has(bot.world.getBlockStateId(pos))) return false;
  return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => farmlandStates.has(bot.world.getBlockStateId(pos.offset(dx, 0, dz))));
}

export function farmlandTick() {
  if ((!bot.controlState.jump && !bot.jumpQueued) || !bot.entity || bot.entity.isInWater) return;
  const me = bot.entity.position;
  // The block her feet rest on: farmland is 15/16 high, so she stands inside its block (y -60.0625 stands in -61).
  const under = (p) => bot.blockAt(new Vec3(Math.floor(p.x), Math.floor(p.y - 0.1), Math.floor(p.z)));
  const next = currentPath[0];
  if (next && next.y > me.y + 0.3) return; // climbing: the landing is above the take-off
  const ahead = me.offset(-Math.sin(bot.entity.yaw), 0, -Math.cos(bot.entity.yaw));
  if (!isFarmland(under(me)) && !isFarmland(under(ahead)) && !(next && isFarmland(under(next)))) return;
  bot.setControlState('jump', false);
  bot.jumpQueued = false; // pressing jump queues one, and letting go doesn't take it back
}
