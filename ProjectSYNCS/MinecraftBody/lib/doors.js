// Doors, fence gates and trapdoors on her way: which edge their panel stands on, clicked out of her way as she comes
// and put back as she found them once she's through. Also the path she's walking (setPath), which the doors are read
// from.

import vec3 from 'vec3';
import { columnPoint } from './columns.js';
import { bot } from './state.js';

const { Vec3 } = vec3;

export const isTrapdoor = (block) => (block.name || '').endsWith('_trapdoor');

// Wooden doors (copper ones too) and fence gates: what a hand opens. Never iron, which needs redstone, nor
// trapdoors, which are as often floors as doors.
export function isDoor(block) {
  const n = block.name || '';
  return (n.endsWith('_door') && n !== 'iron_door') || n.endsWith('_fence_gate');
}

// One key per door: a door's two halves are the lower one.
export function doorKey(block) {
  const lower = block.name.endsWith('_door') && block.getProperties().half === 'upper' ? block.position.offset(0, -1, 0) : block.position;
  return lower.toString();
}

// A door is a thin panel along one edge of its block, and clicking it swings the panel to the next edge: « open »
// or « closed » says nothing about whether the way is clear (a door left open across a corridor is a wall). What
// counts is the edge she's about to cross. The panel's edge comes from its collision box: 'n' (-z), 's' (+z),
// 'w' (-x), 'e' (+x). A fence gate is a bar across its block when closed and nothing when open.
export function panelSide(block) {
  const box = block.shapes && block.shapes[0];
  if (!box) return null;
  const [x0, , z0, x1, , z1] = box;
  if (z1 - z0 < 0.5) return z0 < 0.01 ? 'n' : 's';
  if (x1 - x0 < 0.5) return x0 < 0.01 ? 'w' : 'e';
  return null;
}

// Whether a step between this door's block and a neighbouring one (`other`, on her path) runs into it.
function doorBlocks(block, other) {
  if (block.name.endsWith('_fence_gate')) return !block.getProperties().open;
  const side = panelSide(block);
  if (!side) return false;
  const at = block.position;
  return (side === 'n' && other.z < at.z) || (side === 's' && other.z > at.z)
    || (side === 'w' && other.x < at.x) || (side === 'e' && other.x > at.x);
}

// The path the pathfinder is walking (path_update) and how far along it she is; the doors she moved, with the state
// she found them in (put back once she's through); the ones that wouldn't move (a wall until DoorStuckFor passes).
export let currentPath = [];
let pathIndex = 0;
const movedDoors = new Map();
export const stuckDoors = new Map();
const DoorReach = 2.5;
const DoorPast = 1.5; // from the door's centre: her box is out of its block
const DoorStuckFor = 120000;
let doorTickAt = 0;

export function setPath(path) {
  currentPath = Array.isArray(path) ? path : [];
  pathIndex = 0;
  for (const node of currentPath) {
    floorDoorNode(node);
    columnPoint(node);
  }
}

// The pathfinder puts each point of its path on top of whatever stands in that block, once planned: a closed fence gate
// lifts it 1.5 (the top of its bar), a door or an open trapdoor 1 and onto its panel. She walks through those, not
// over them, so such a point goes back down, centred, before she walks it: otherwise she never reaches it (a block or
// more below it, sent back towards it until the pathfinder gives up), and doorTick looks for the door a block too
// high. The path is the pathfinder's own (path_update hands it over before it walks it), so this is the path she walks.
function floorDoorNode(node) {
  // What the point stands on: a gate (nobody stands on one), a door's lower half (the point is in its upper half), or
  // an open trapdoor (its panel, which gentleMovements walks past).
  const below = bot.blockAt(new Vec3(Math.floor(node.x), Math.floor(node.y) - 1, Math.floor(node.z)));
  if (!below) return;
  if (isTrapdoor(below)) {
    if (!below.getProperties().open) return; // a closed one is a floor
  } else if (!isDoor(below) || stuckDoors.has(doorKey(below))) return;
  else if (below.name.endsWith('_door') && below.getProperties().half !== 'lower') return; // on top of a door: a real floor
  node.x = below.position.x + 0.5;
  node.y = below.position.y;
  node.z = below.position.z + 0.5;
}

// The next few steps on her path, from where she stands: [from, to] pairs of block positions.
function stepsAhead(here) {
  // Path points are block centres (x.5): compared as blocks, or a point in the door's own row reads as beside it.
  const cell = (n) => new Vec3(Math.floor(n.x), Math.floor(n.y), Math.floor(n.z));
  for (let i = pathIndex; i < Math.min(currentPath.length, pathIndex + 4); i++) {
    const n = cell(currentPath[i]);
    if (n.x === here.x && n.z === here.z && Math.abs(n.y - here.y) <= 1) pathIndex = i + 1;
  }
  const cells = [here, ...currentPath.slice(pathIndex, pathIndex + 3).map(cell)];
  const steps = [];
  for (let i = 0; i + 1 < cells.length; i++) steps.push([cells[i], cells[i + 1]]);
  return steps;
}

// A few times a second: a door in the way of her next steps gets clicked out of the way once she's close, and the
// doors she moved are put back as she found them once she's through, unless someone stands in the doorway.
export function doorTick() {
  if (Date.now() < doorTickAt || !bot.entity) return;
  doorTickAt = Date.now() + 200;
  const me = bot.entity.position;
  for (const [key, until] of stuckDoors) if (Date.now() > until) stuckDoors.delete(key);
  for (const [from, to] of stepsAhead(me.floored())) {
    for (const [cell, other] of [[to, from], [from, to]]) {
      if (cell.offset(0.5, 0, 0.5).distanceTo(me) > DoorReach + 0.5) continue;
      for (const dy of [0, 1]) {
        const block = bot.blockAt(cell.offset(0, dy, 0));
        if (block && isDoor(block) && doorBlocks(block, other)) clearDoor(block);
      }
    }
  }
  for (const [key, door] of movedDoors) {
    const block = bot.blockAt(door.pos);
    if (!block || !isDoor(block) || Date.now() - door.at > 60000) {
      movedDoors.delete(key); // gone, or she never got through: leave it be
      continue;
    }
    if (block.stateId === door.found) {
      if (Date.now() - door.at > 1500) movedDoors.delete(key); // back as it was (moved twice, or by someone)
      continue;
    }
    // Through: out of the doorway on the far side from where she clicked it, or well away from it either way (a
    // destination just past a door would otherwise keep her « not through » and leave it open).
    const centre = door.pos.offset(0.5, 0, 0.5);
    const away = centre.distanceTo(me);
    const past = (me.x - centre.x) * (door.from.x - centre.x) + (me.z - centre.z) * (door.from.z - centre.z) < 0;
    if (!(past && away > DoorPast) && away <= DoorReach + 0.5) continue; // not through yet
    const someone = bot.nearestEntity((e) => e !== bot.entity && ['player', 'animal', 'passive'].includes(e.type)
      && e.position.distanceTo(centre) < 2);
    if (someone) continue;
    movedDoors.delete(key);
    if (centre.distanceTo(me) < 4.5) bot.activateBlock(block).catch(() => {});
  }
}

// One click on a door in her way. A click the server ignored twice (spawn protection, a plugin) makes it a wall:
// the pathfinder's own stuck check re-plans in a few seconds, and getBlock then sees one.
function clearDoor(seen) {
  // Always the lower half of a door, so both halves are one door with one remembered state.
  const block = seen.name.endsWith('_door') && seen.getProperties().half === 'upper'
    ? bot.blockAt(seen.position.offset(0, -1, 0)) : seen;
  if (!block || !isDoor(block)) return;
  const key = doorKey(block);
  const door = movedDoors.get(key);
  if (door && Date.now() - door.at < 1500) return; // just clicked: the server hasn't answered yet
  if (door && block.stateId === door.clicked && ++door.ignored >= 2) {
    stuckDoors.set(key, Date.now() + DoorStuckFor);
    movedDoors.delete(key);
    return;
  }
  movedDoors.set(key, {
    pos: block.position, found: door ? door.found : block.stateId, clicked: block.stateId,
    ignored: door ? door.ignored : 0, at: Date.now(), from: door ? door.from : bot.entity.position.clone(),
  });
  bot.activateBlock(block).catch(() => {});
}
