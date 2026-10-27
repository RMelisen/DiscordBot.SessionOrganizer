// Her steps in water columns (columnMoves in ProjectSYNCS/MinecraftBody/lib/columns.js), planned by the real pathfinder
// over made-up worlds: up and down each kind of column, never the wrong way, never through a lake, within her air. No
// server needed:
//   node --test column-check.js
// The body's own gentleMovements does the planning, with a stand-in bot that only answers block reads. Its modules load
// without connecting anything (index.js does that).
const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const { createRequire } = require('node:module');

const BodyDir = path.join(__dirname, '../../ProjectSYNCS/MinecraftBody');
const bodyRequire = createRequire(path.join(BodyDir, 'index.js'));
const { Vec3 } = bodyRequire('vec3');
const Move = bodyRequire('mineflayer-pathfinder/lib/move');
const AStar = bodyRequire('mineflayer-pathfinder/lib/astar');
const { goals } = bodyRequire('mineflayer-pathfinder');
const registry = bodyRequire('minecraft-data')('1.21.4');
const Block = bodyRequire('prismarine-block')(registry);
// ES modules, loaded by require (Node 22.12 or later).
const { setBot } = require(path.join(BodyDir, 'lib/state.js'));
const { fillStates } = require(path.join(BodyDir, 'lib/blocks.js'));
const { gentleMovements } = require(path.join(BodyDir, 'lib/movements.js'));

// cells: "x,y,z" -> block name ('lift' and 'drag' for the two bubble columns); stone below y 0, air above.
function world(cells) {
  return (p) => {
    const at = new Vec3(Math.floor(p.x), Math.floor(p.y), Math.floor(p.z));
    const name = cells.get(`${at.x},${at.y},${at.z}`) || (at.y < 0 ? 'stone' : 'air');
    const bubble = registry.blocksByName.bubble_column.minStateId;
    const flowing = /^water(\d+)$/.exec(name); // water with its level: 1-7 spreading, 8 falling
    const state = name === 'drag' ? bubble : name === 'lift' ? bubble + 1
      : flowing ? registry.blocksByName.water.minStateId + Number(flowing[1]) : registry.blocksByName[name].defaultState;
    const block = Block.fromStateId(state, 0);
    block.position = at;
    return block;
  };
}

// The body's spawn fills its state sets once the version is known; the same here, against the stand-in.
function movementsFor(blockAt) {
  setBot({
    registry, version: '1.21.4', blockAt, entities: {}, entity: { position: new Vec3(0, 0, 0) }, game: { minY: -64, height: 384 },
    world: { getBlockStateId: (p) => blockAt(p).stateId }, inventory: { items: () => [], slots: [] },
    pathfinder: { bestHarvestTool: () => null },
  });
  fillStates(registry);
  return gentleMovements();
}

function plan(blockAt, from, to) {
  const start = new Move(from[0], from[1], from[2], 0, 0);
  const result = new AStar(start, movementsFor(blockAt), new goals.GoalBlock(...to), 5000, 5000).compute();
  return { status: result.status, path: result.path.map((n) => `${n.x},${n.y},${n.z}`).join(' ') };
}

// A stone tower H high (x -5..5, z -3..3) with a 1-wide column at x 0, z 0 (y 0..H-1) on `foot`, a doorway into it from
// the west at its foot (x -3..-1, y 0..1; `doorway` fills it, as the water spilling out does), and its top open: she
// stands on the tower at y H, the ledge beside the column at x 1.
function tower(H, fill = 'water', foot = 'stone', doorway = 'air') {
  const cells = new Map();
  for (let x = -5; x <= 5; x++) for (let z = -3; z <= 3; z++) for (let y = 0; y < H; y++) cells.set(`${x},${y},${z}`, 'stone');
  for (let y = 0; y < H; y++) cells.set(`0,${y},0`, fill);
  cells.set('0,-1,0', foot);
  for (let x = -3; x <= -1; x++) {
    cells.set(`${x},0,0`, doorway);
    cells.set(`${x},1,0`, 'air');
  }
  return world(cells);
}
const Foot = [-2, 0, 0];
const top = (H) => [1, H, 0];

test('up a water column, out onto the top', () => {
  for (const H of [8, 20]) assert.equal(plan(tower(H), Foot, top(H)).status, 'success', `${H} high`);
});

test('not up a water column longer than her air', () => {
  assert.equal(plan(tower(30), Foot, top(30)).status, 'noPath');
});

test('up from a doorway the column floods', () => {
  assert.equal(plan(tower(8, 'water', 'stone', 'water'), [-3, 0, 0], top(8)).status, 'success');
});

test('down a short water column (sinking is slow), not a long one', () => {
  assert.equal(plan(tower(4), top(4), Foot).status, 'success');
  assert.equal(plan(tower(8), top(8), Foot).status, 'noPath');
});

test('up a bubble column over soul sand, never down it', () => {
  const lift = tower(30, 'lift', 'soul_sand');
  assert.equal(plan(lift, Foot, top(30)).status, 'success');
  assert.notEqual(plan(lift, top(30), Foot).status, 'success');
});

test('down a bubble column over magma, never up it', () => {
  const drag = tower(30, 'drag', 'magma_block');
  assert.equal(plan(drag, top(30), Foot).status, 'success');
  assert.notEqual(plan(drag, Foot, top(30)).status, 'success');
});

// A cave waterfall: a ledge (x 1..5) H high, a spring on it at x 2 feeding a shallow stream over its edge, falling
// down the cave wall at x 0 to the floor, where it spreads into a puddle (x -4..0; none with `puddle` false, as where
// it drains away); the cave open above and west.
function waterfall(H, puddle = true) {
  const cells = new Map();
  for (let x = 1; x <= 5; x++) for (let z = -3; z <= 3; z++) for (let y = 0; y < H; y++) cells.set(`${x},${y},${z}`, 'stone');
  cells.set(`2,${H},0`, 'water');
  cells.set(`1,${H},0`, 'water1');
  cells.set(`0,${H},0`, 'water2');
  for (let y = 0; y < H; y++) cells.set(`0,${y},0`, 'water8');
  if (puddle) for (let x = -4; x <= 0; x++) for (let z = -4; z <= 4; z++) {
    const d = -x + Math.abs(z);
    if (d > 0 && d <= 7) cells.set(`${x},0,${z}`, `water${d}`);
  }
  return world(cells);
}

test('up a cave waterfall, from the puddle at its foot onto the ledge', () => {
  assert.equal(plan(waterfall(10), [-3, 0, 0], [4, 10, 0]).status, 'success');
  assert.notEqual(plan(waterfall(20), [-3, 0, 0], [4, 20, 0]).status, 'success'); // 10 s up: past her air
});

// With a puddle she jumps off the ledge into it (the pathfinder's own drop into water, which breaks any fall); without
// one, the fall is the way down.
test('down a cave waterfall, from the ledge to the floor', () => {
  assert.equal(plan(waterfall(10), [4, 10, 0], [-3, 0, 0]).status, 'success');
  const r = plan(waterfall(10, false), [4, 10, 0], [-3, 0, 0]);
  assert.equal(r.status, 'success');
  assert.match(r.path, / 0,5,0 /, `down the fall itself: ${r.path}`);
});

test('no way down through a lake', () => {
  const cells = new Map();
  for (let x = -5; x <= 5; x++) for (let z = -3; z <= 3; z++) for (let y = 0; y < 6; y++) cells.set(`${x},${y},${z}`, 'stone');
  for (let x = -1; x <= 1; x++) for (let z = -1; z <= 1; z++) for (let y = 0; y < 6; y++) cells.set(`${x},${y},${z}`, 'water');
  for (let x = -4; x <= -2; x++) {
    cells.set(`${x},0,0`, 'air');
    cells.set(`${x},1,0`, 'air');
  }
  assert.notEqual(plan(world(cells), [3, 6, 0], [-3, 0, 0]).status, 'success');
});

test('a step into a bubble column keeps its height for the walk (columnPoint)', () => {
  const steps = movementsFor(tower(10, 'lift', 'soul_sand')).getNeighbors(new Move(0, 3, 0, 0, 0)).filter((m) => m.x === 0 && m.z === 0);
  assert.ok(steps.length > 0);
  for (const m of steps) assert.equal(m.columnY, m.y);
});
