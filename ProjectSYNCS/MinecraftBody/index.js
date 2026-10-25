// SYNCS's body on the Minecraft server: a Mineflayer client the C# bot starts and drives.
//
// The body owns no policy and no words. It reports what happens as one JSON object per line on
// stdout, and does what it is told by one JSON object per line on stdin. Which line she says,
// when she joins and when she leaves are all decided in C# (MinecraftBodyService): her voice
// lives in BotResponses, behind ResponsePicker, never here. Logs go to stderr.
//
// Environment (the C# side clears everything else, so the Discord token never reaches us):
//   MC_HOST, MC_PORT, MC_USERNAME
//   MC_TRACE_MOVES=1: every server correction logged (tools/mc-pathtest only)
//   MC_TRACE_PATHS=1: every slow or unfinished path search logged; =all: every path, node by node (tools/mc-pathtest only)
//   MC_CPU_FACTOR=n: the pathfinder's time budgets divided by n, as on a CPU n times slower (tools/mc-pathtest only)
//
// In:  {"op":"say","text":"…"} · {"op":"whisper","player":"…","text":"…"} · {"op":"quit"}
//      {"op":"come","player":"…"} · {"op":"follow","player":"…"} · {"op":"stay"}
//      {"op":"goto","player":"…","x":0,"y":null,"z":0} · {"op":"guard","player":"…"}
//      {"op":"collect","player":"…","resource":"wood","count":10} · {"op":"sleep","player":"…"}
//      {"op":"seek","player":"…","spot":{"x":0,"y":64,"z":0,"dimension":"…"}|null}
//      {"op":"flee","player":"…"} · {"op":"family","names":["…"]} · {"op":"defend","player":"…","victim":"…","swat":true}
//      {"op":"harvest","player":"…","crop":"any|wheat|carrot|potato|beetroot|melon|pumpkin"} · {"op":"farm","player":"…"}
//      {"op":"goto",…,"dimension":"…"|null} · {"op":"guide","player":"…","x":0,"y":64,"z":0,"dimension":"…"}
//      {"op":"give","player":"…","goods":"all|food|fish|…"} · {"op":"store","player":"…"} · {"op":"fish","player":"…"}
//      {"op":"dress","player":"…"} · {"op":"undress","player":"…"}
//      {"op":"patrol","player":"…","spot":{x,y,z,dimension}|null} · {"op":"smelt","player":"…","what":"food|ore|sand"}
//      {"op":"where","player":"…"} · {"op":"mark","player":"…"} · {"op":"sit"} · {"op":"jump"} · {"op":"wave","player":"…"}
//      {"op":"ranch","player":"…"} · {"op":"routine","farm":spot|null,"fish":spot|null,"ranch":spot|null,"home":spot|null}
//      {"op":"camp","player":"…"} · {"op":"uncamp","player":"…","blocks":[{x,y,z,name}],"dimension":"…","center":{x,y,z}}
//        (a spot is {x,y,z,dimension})
// Out: version {server,using,fallback} · spawned · players {names} · chat {player,text,private}
//      system {key,args:[{text,key}]} · hurt {by} · died · night · kicked {reason} · end {reason}
//      moving {player,kind: come|follow|goto|guard|guide} · arrived {player,kind} · waiting {player}
//      crossing {player} (going after them through a portal)
//      cantReach {player,why: notVisible|noPath|lost|tooFar|otherDimension} · killed {player}
//      creeper {player} · retreat {player}
//      collecting {player,resource,count} · collected {player,resource,count,delivered}
//      cantCollect {player,resource,why: none|noTool} · sleeping {player,auto} · cantSleep {player,why: day|noBed|failed}
//      seek {player,outcome: met|missing} · seen {player,x,y,z,dimension}
//      harvesting {player,crop,count} · harvested {player,crop,count,replanted,delivered} · cantHarvest {player,crop,why: none}
//      farming {player} · cantFarm {player,why: none|noChest} · farmed {count} (a farm round's produce, for her stats)
//      fleeing {player} · safe {player} · cornered {player} · dropped {player,kind,by}
//      familyHurt {victim,by} · defending {player,victim,swat} · cantDefend {player,victim}
//      hungry {food} · fed {by}
//      given {player,goods,count,delivered} · cantGive {player,goods,why: none} · stored {player,count,full}
//      dressed {player,count} · cantDress {player,why: none|worn} · undressed {player,count,full}
//      cantUndress {player,why: none|full} · armoured {by} (put on a piece someone gave her)
//      cantStore {player,why: noChest|empty} · fishing {player} · cantFish {player,why: noRod|noWater|broke}
//      caught {player,kind: fish|treasure|junk} · patrolling {player} · cantPatrol {player,why: tooFar|otherDimension}
//      smelting {player,what,count} · smelted {player,what,count,delivered}
//      cantSmelt {player,what,why: nothing|noFuel|noFurnace|busy}
//      position {player,x,y,z,dimension} · marked {player,x,y,z,dimension} · cantMark {player}
//      ranching {player} · cantRanch {player,why: none|noChest}
//      camping {player} · camped {player,blocks:[{x,y,z,name}],complete,x,y,z,dimension}
//      cantCamp {player,why: noRoom|missing|craft|place,missing:[wool|wood|stone|…],blocks?} · uncamping {player}
//      uncamped {player,count,left} · cantUncamp {player,why: none|otherDimension|tooFar}
//      Her own (no player, no claim): routine {what: farm|fish|ranch|home} · tagAlong {player} · retrieving
//      retrieved {count} · retrieveLost {why: otherDimension|tooFar|void|lava|noPath|despawned} · creeperBehind {player}
//      wornOut {tool} · broke {tool} · lacking {what: rod|shears|breedFood|arrows|space}
//      (tool: sword|pickaxe|axe|shovel|rod|shears|bow)
//      Water: diving {player} · surfacing {player} · drowning · breathing · boarded {player} · shook
//      inventory {worn:[stack],held:stack|null,offhand:stack|null,pockets:[stack]} (stack: {name,count,goods:[…]}),
//        on spawn and after her pockets change, at most every 2 s

'use strict';

const readline = require('node:readline');

// stdout is the event channel and carries nothing else. The protocol libraries print their own
// errors and warnings through console.*, so every console method goes to stderr (the log) before
// any of them is loaded.
for (const method of ['log', 'info', 'warn', 'error', 'debug', 'trace']) {
  console[method] = (...parts) => process.stderr.write(parts.map(String).join(' ') + '\n');
}

const mineflayer = require('mineflayer');
const { pathfinder, Movements, goals } = require('mineflayer-pathfinder');
const { plugin: collectBlock } = require('mineflayer-collectblock');
const { Targets: CollectTargets } = require('mineflayer-collectblock/lib/Targets');
const Move = require('mineflayer-pathfinder/lib/move');
const mc = require('minecraft-protocol');
const minecraftData = require('minecraft-data');
const { Vec3 } = require('vec3');
const { cooldownFor, answerTo, sizeUp, GetOutKinds } = require('./fight');

// Collisions with the game's own tolerance: a box less than 1e-7 into a block still touches its face, and on the other
// two axes overlaps by less than that don't count (vanilla's VoxelShape epsilon). Mineflayer compares exactly, so a box
// rounded a hair past a face (a step after being put flush against it: -127.99999999999999 for -128) no longer finds
// the block in front of it and walks straight in, and the server puts her back, again and again.
const AABB = require('prismarine-physics/lib/aabb');
const Touch = 1e-7;
const across = (a0, a1, b0, b1) => a1 > b0 + Touch && a0 < b1 - Touch;
AABB.prototype.computeOffsetX = function computeOffsetX(other, offsetX) {
  if (across(other.minY, other.maxY, this.minY, this.maxY) && across(other.minZ, other.maxZ, this.minZ, this.maxZ)) {
    if (offsetX > 0 && other.maxX <= this.minX + Touch) offsetX = Math.min(this.minX - other.maxX, offsetX);
    else if (offsetX < 0 && other.minX >= this.maxX - Touch) offsetX = Math.max(this.maxX - other.minX, offsetX);
  }
  return offsetX;
};
AABB.prototype.computeOffsetY = function computeOffsetY(other, offsetY) {
  if (across(other.minX, other.maxX, this.minX, this.maxX) && across(other.minZ, other.maxZ, this.minZ, this.maxZ)) {
    if (offsetY > 0 && other.maxY <= this.minY + Touch) offsetY = Math.min(this.minY - other.maxY, offsetY);
    else if (offsetY < 0 && other.minY >= this.maxY - Touch) offsetY = Math.max(this.maxY - other.minY, offsetY);
  }
  return offsetY;
};
AABB.prototype.computeOffsetZ = function computeOffsetZ(other, offsetZ) {
  if (across(other.minX, other.maxX, this.minX, this.maxX) && across(other.minY, other.maxY, this.minY, this.maxY)) {
    if (offsetZ > 0 && other.maxZ <= this.minZ + Touch) offsetZ = Math.min(this.minZ - other.maxZ, offsetZ);
    else if (offsetZ < 0 && other.minZ >= this.maxZ - Touch) offsetZ = Math.max(this.maxZ - other.minZ, offsetZ);
  }
  return offsetZ;
};

const host = process.env.MC_HOST;
const port = Number.parseInt(process.env.MC_PORT || '25565', 10);
const username = process.env.MC_USERNAME || 'SYNCS';

function emit(ev, data) {
  process.stdout.write(JSON.stringify({ ev, ...data }) + '\n');
}

function log(...parts) {
  process.stderr.write(parts.join(' ') + '\n');
}

if (!host) {
  log('MC_HOST is not set');
  process.exit(2);
}

let bot = null;

let ended = false;
function finish(reason) {
  if (ended) return;
  ended = true;
  emit('end', { reason: String(reason ?? '') });
  // Let stdout drain before leaving.
  setTimeout(() => process.exit(0), 100);
}

function playerNames() {
  return Object.keys(bot.players).filter((name) => name !== bot.username);
}

function reportPlayers() {
  emit('players', { names: playerNames() });
}

// A chat component's text and translate key, the key being what C# classifies on: it is the
// same in every language, whereas the text is whatever Mineflayer's language data renders.
function part(component) {
  if (component === null || component === undefined) return { text: '', key: null };
  if (typeof component !== 'object') return { text: String(component), key: null };
  return { text: component.toString(), key: component.translate ?? null };
}

// ---- Orders: come, follow, go to, guard, collect, sleep, stay -----------------------------------

// What she was asked to do, for the events that report how it went. Null when she stands still.
let task = null;

// The farthest she agrees to walk to coordinates: past this she would be gone for the evening.
const MaxTrip = 1500;

// She walks and jumps, never digs and never places a block: a path through someone's wall or a
// dirt pillar in the middle of their build is not help. (Felling a tree, she climbs a pillar of her own and takes it
// down after: gatheringMovements' climb.)
function gentleMovements() {
  const movements = new Movements(bot);
  movements.canDig = false;
  movements.scafoldingBlocks = [];
  movements.allow1by1towers = false;
  // The pathfinder sees every door as a wall (its own door option is off, flaky, and gates only): a wooden door or
  // a fence gate is a way through, whatever its state, and doorTick swings it out of the way as she comes. Not on
  // a diagonal step, where a corner catches the panel, nor one that refused to move: a wall again for a while.
  // A bubble column over magma pulls whoever swims in it to the bottom: the pathfinder takes it for air, so it's
  // made a wall. Trapdoors the pathfinder takes for full blocks: an open one is a thin panel on one edge, passable
  // along that edge and a wall across her way when it faces her (it planned to stand on the panel, and she fell off it,
  // again and again); a closed one in the top half of the block at head height leaves 1.8125 under it, and she's 1.8
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
    if (isDragColumn(b)) {
      b.safe = false;
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
  // walk into the gap), and a drop onto one costs FieldDrop: a fall of a block tramples it one time in two.
  const getNeighbors = movements.getNeighbors.bind(movements);
  movements.getNeighbors = (node) => getNeighbors(node).filter((move) => {
    if (move.parkour && (onField(node) || onField(move))) return false;
    if (move.y < node.y && onField(move)) move.cost += FieldDrop;
    return true;
  });
  return movements;
}

// ---- Fields ---------------------------------------------------------------------------------------------

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
let farmlandStates = new Set(); // its state ids, filled on the first spawn, once the version is known
let waterStates = new Set(); // the same for water
// Floors that burn whoever stands on them: magma, a lit campfire. Not forbidden: a way across may need one.
let burningStates = new Set();
const BurningFloor = 40;
// What hurts to brush against, and the cost of a step beside it.
let prickly = new Set();
const PricklyNear = 8;
// Lava, and what a fall goes straight through (air): fightMovements keeps her off the lip of a drop or a lava pool.
let lavaStates = new Set();
let airStates = new Set();

// Water with farmland beside it, at the same height.
function isFieldWater(pos) {
  if (!waterStates.has(bot.world.getBlockStateId(pos))) return false;
  return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => farmlandStates.has(bot.world.getBlockStateId(pos.offset(dx, 0, dz))));
}

function farmlandTick() {
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

// ---- Doors ---------------------------------------------------------------------------------------------

const isTrapdoor = (block) => (block.name || '').endsWith('_trapdoor');

// Wooden doors (copper ones too) and fence gates: what a hand opens. Never iron, which needs redstone, nor
// trapdoors, which are as often floors as doors.
function isDoor(block) {
  const n = block.name || '';
  return (n.endsWith('_door') && n !== 'iron_door') || n.endsWith('_fence_gate');
}

// One key per door: a door's two halves are the lower one.
function doorKey(block) {
  const lower = block.name.endsWith('_door') && block.getProperties().half === 'upper' ? block.position.offset(0, -1, 0) : block.position;
  return lower.toString();
}

// A door is a thin panel along one edge of its block, and clicking it swings the panel to the next edge: « open »
// or « closed » says nothing about whether the way is clear (a door left open across a corridor is a wall). What
// counts is the edge she's about to cross. The panel's edge comes from its collision box: 'n' (-z), 's' (+z),
// 'w' (-x), 'e' (+x). A fence gate is a bar across its block when closed and nothing when open.
function panelSide(block) {
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
let currentPath = [];
let pathIndex = 0;
const movedDoors = new Map();
const stuckDoors = new Map();
const DoorReach = 2.5;
const DoorPast = 1.5; // from the door's centre: her box is out of its block
const DoorStuckFor = 120000;
let doorTickAt = 0;

function setPath(path) {
  currentPath = Array.isArray(path) ? path : [];
  pathIndex = 0;
  for (const node of currentPath) floorDoorNode(node);
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
function doorTick() {
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

// For gathering: the same walk, except that the blocks she was sent for may be broken, and so may a tree's own leaves
// next to its logs: a log wrapped in leaves has an open face (its top, the far side) but none she can see from where
// she stands, and the search for one ran its 5 s for every log of a forest, twelve in a row, nothing brought back. A
// player punches through. Leaves someone placed are persistent and stay, like everything else on the way: collectblock
// asks the movements before every break.
const LeavesBy = 2;

// `climb`, felling a tree: she may also stack a pillar under herself (allow1by1towers), from what she carries
// (ScaffoldItems), to reach a top no reach gets to from the ground; straight up only, never a bridge over a gap. What
// she places is noted (gather) and taken down after (unstack). Up the trunk's own column, where its logs were
// (ClimbBeside a block anywhere else): stacked beside it, the logs she cut fell past her to the ground, out of her
// reach until the pillar was down, and she ran out of blocks to stack halfway up a spruce. Under them, each log she
// cuts over her head falls on her and is hers at once.
const ClimbBeside = 10;

function gatheringMovements(targets, climb = false) {
  const movements = gentleMovements();
  movements.canDig = true;
  if (climb) {
    movements.allow1by1towers = true;
    movements.scafoldingBlocks = scaffoldIds();
    const neighbors = movements.getNeighbors.bind(movements);
    movements.getNeighbors = (node) => neighbors(node).filter((move) => move.toPlace.every((p) => p.jump));
    const trunk = new Set(targets.map((b) => `${b.position.x},${b.position.z}`));
    movements.exclusionAreasPlace.push((block) => (trunk.has(`${block.position.x},${block.position.z}`) ? 0 : ClimbBeside));
  }
  const allowed = new Set(targets.map((b) => b.position.toString()));
  const logs = targets.filter((b) => b.name.endsWith('_log'));
  const treeLeaves = (block) => block.name.endsWith('_leaves') && block.getProperties().persistent === false
    && logs.some((l) => Math.abs(l.position.x - block.position.x) <= LeavesBy && Math.abs(l.position.y - block.position.y) <= LeavesBy
      && Math.abs(l.position.z - block.position.z) <= LeavesBy);
  // A chest among the targets is hers to break (her camp's): the pathfinder never breaks one otherwise.
  if (targets.some((b) => b.name === 'chest')) movements.blocksCantBreak.delete(bot.registry.blocksByName.chest.id);
  const safe = movements.safeToBreak.bind(movements);
  // A block in a chunk that isn't loaded comes as a stub with no position: not breakable (reading it threw from the
  // physics tick and took the whole body down).
  movements.safeToBreak = (block) => Boolean(block && block.position)
    && (allowed.has(block.position.toString()) || treeLeaves(block)) && safe(block);
  return movements;
}

// The player's entity, if it's close enough for the server to show it to her (render distance).
function playerEntity(name) {
  const player = bot.players[name];
  return player && player.entity ? player.entity : null;
}

function come(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'come', player, reported: false };
  const at = target.position;
  bot.pathfinder.setGoal(new goals.GoalNear(at.x, at.y, at.z, 2));
  emit('moving', { player, kind: 'come' });
}

// `quiet`: going on after a portal (crossAfter), the order already acknowledged.
function follow(player, quiet = false) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'follow', player, reported: false };
  // Dynamic: the goal moves with the player.
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  if (!quiet) emit('moving', { player, kind: 'follow' });
}

// Coordinates: x z (any height) or x y z. Far trips cross chunks she can't see yet; the pathfinder
// walks partial paths towards the goal and plans again as the world loads. A remembered place comes with
// its dimension: from another one, she can't walk there.
// Every order checks it can be done before stay() drops the current one: an order that fails at once
// (out of sight, too far, nothing to gather) leaves her doing what she was doing.
function goTo(player, x, y, z, dim) {
  const here = bot.entity.position;
  if (dim && dim !== dimension()) return emit('cantReach', { player, why: 'otherDimension' });
  if (Math.hypot(x - here.x, z - here.z) > MaxTrip) return emit('cantReach', { player, why: 'tooFar' });
  stay();
  task = { kind: 'goto', player, reported: false };
  bot.pathfinder.setGoal(y === null ? new goals.GoalNearXZ(x, z, 2) : new goals.GoalNear(x, y, z, 1));
  emit('moving', { player, kind: 'goto' });
}

// Leading a player to a place: she walks ahead, stops and waits when they fall more than GuideLag blocks behind,
// and goes on once they're close again. Out of their sight for GuideLost, she gives up.
const GuideLag = 12;
const GuideLost = 30000;

function guide(player, x, y, z, dim) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  if (dim && dim !== dimension()) return emit('cantReach', { player, why: 'otherDimension' });
  const here = bot.entity.position;
  if (Math.hypot(x - here.x, z - here.z) > MaxTrip) return emit('cantReach', { player, why: 'tooFar' });
  stay();
  task = { kind: 'guide', player, dest: new Vec3(x, y, z), waiting: false, lostSince: null, nextCheck: 0, reported: false };
  bot.pathfinder.setGoal(new goals.GoalNear(x, y, z, 2));
  emit('moving', { player, kind: 'guide' });
}

function guideTick() {
  const t = task;
  if (!t || t.kind !== 'guide' || Date.now() < t.nextCheck) return;
  t.nextCheck = Date.now() + 500;
  const follower = playerEntity(t.player);
  if (!follower) {
    if (!t.lostSince) {
      t.lostSince = Date.now();
      bot.pathfinder.setGoal(null);
    } else if (Date.now() - t.lostSince > GuideLost) {
      emit('cantReach', { player: t.player, why: 'lost' });
      stay();
    }
    return;
  }
  const gap = follower.position.distanceTo(bot.entity.position);
  if ((t.waiting || t.lostSince) && gap < 6) {
    t.waiting = false;
    t.lostSince = null;
    bot.pathfinder.setGoal(new goals.GoalNear(t.dest.x, t.dest.y, t.dest.z, 2));
  } else if (!t.waiting && !t.lostSince && gap > GuideLag) {
    t.waiting = true;
    bot.pathfinder.setGoal(null);
    emit('waiting', { player: t.player });
  }
  if (t.waiting) bot.lookAt(follower.position.offset(0, 1.6, 0)).catch(() => {});
}

function stay() {
  if (task && ['collect', 'harvest', 'farm', 'forage'].includes(task.kind))
    bot.collectBlock.cancelTask().catch(() => {});
  if (task) reelIn(task); // fishing, or fishing for her supper
  if (task && task.window) { // a furnace she's cooking at
    try { task.window.close(); } catch { /* already closed */ }
  }
  // An order gets her out of bed: lying there, she can't move. And out of a boat (the ride was the old task's).
  if (bot.isSleeping) Promise.resolve().then(() => bot.wake()).catch(() => {});
  if (riding) leaveBoat();
  if (task && task.kind === 'portal') bot.pathfinder.setMovements(gentleMovements()); // portals were allowed
  task = null;
  resumeAfterSleep = null;
  sitting = false;
  bot.setControlState('sneak', false);
  stopBow();
  bot.pathfinder.setGoal(null);
  if (slowWalk) { // tagging along walked without sprinting
    slowWalk = false;
    walkSprint = null;
    bot.pathfinder.setMovements(gentleMovements());
  }
  fightPace(false);
}

const wait =(ms) => new Promise((resolve) => setTimeout(resolve, ms));

// ---- Activities: what she goes back to after a short errand or a night's sleep ----------------------

// Farming, fishing, keeping animals and guarding a place last until someone says otherwise. Handing something over
// or putting her things away pauses one, and she goes back to it after; so does a night's sleep. Her own (the
// routine's) count too.
const Activities = new Set(['farm', 'fish', 'patrol', 'ranch']);

function isActivity(t) {
  return Boolean(t && Activities.has(t.kind));
}

// The activity under way, or the one a meal break (forage) paused: what an errand, a night's sleep or running
// away goes back to.
function pausedActivity(t) {
  if (isActivity(t)) return t;
  return t && t.kind === 'forage' ? t.resume : null;
}

// A fresh copy, so whatever was still running for the old one (a farm round, a fishing cast) sees it's no longer
// the task and stops.
function resumeActivity(activity) {
  if (!activity || task) return;
  task = { ...activity, busy: false, foe: null, mode: null, modeTarget: null, casting: false, target: null };
  if (task.kind === 'fish') fishLoop(task);
}

// ---- Free time: what she does on her own between orders -----------------------------------------------

// Own tasks (own: true, player ''): the routine, tagging along, getting her things back. Body-only, like eating: no
// claim, no order events, and any order ends one (through stay()). Nobody needs her during one, so she counts as free.
function isFree(t) {
  return !t || Boolean(t.own);
}

// An order: a task someone gave her (or the walk up to Papa on arriving), as opposed to her own or a body-only one.
function isOrder(t) {
  return Boolean(t && !t.own && t.player);
}

// « reste » and « assis » hold her for HeldFor: no routine, no tagging along, no retrieval (body language still runs).
// Any other order that gets under way lets her go. And once an order is over she waits OrderGrace before starting
// anything of her own, so « viens » doesn't end with her walking off at once.
const HeldFor = 600000;
const OrderGrace = 45000;
let heldUntil = 0;
let freeSince = 0;
let wasOrder = false;
let autonomyAt = 0;

function freeTick() {
  const order = isOrder(task);
  if (wasOrder && !order) freeSince = Date.now();
  wasOrder = order;
}

// Once a second, when nobody needs her: getting her things back after a death, else the routine (named places), else
// tagging along with Papa or Tata, else nothing but body language.
function autonomyTick() {
  if (Date.now() < autonomyAt || !bot.entity) return;
  autonomyAt = Date.now() + 1000;
  if (bot.isSleeping || eating || sitting || swim || !isFree(task)) return;
  if (Date.now() < freeSince + OrderGrace || Date.now() < heldUntil) return;
  if (task && task.kind === 'retrieve') return retrieveTick(task);
  if (pendingRetrieve) return startRetrieve();
  if (routineTick()) return;
  tagAlongTick();
}

// A tick handler that can't take the process down: an exception thrown from a physics tick or a timer is uncaught.
function safely(fn) {
  return () => {
    try {
      fn();
    } catch (err) {
      log(`${fn.name} failed:`, err && err.message ? err.message : String(err));
    }
  };
}

// ---- Small things: where she is, a marked spot, sitting, hopping, waving ---------------------------------

let sitting = false;

function where(player) {
  const p = (bot.vehicle || bot.entity).position; // riding, her own position isn't updated: the boat's is
  emit('position', { player, x: Math.floor(p.x), y: Math.floor(p.y), z: Math.floor(p.z), dimension: dimension() });
}

// Where the player stands, for a place they want remembered.
function mark(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantMark', { player });
  const p = target.position;
  emit('marked', { player, x: Math.floor(p.x), y: Math.floor(p.y), z: Math.floor(p.z), dimension: dimension() });
}

function sit() {
  stay();
  sitting = true;
  bot.setControlState('sneak', true);
}

async function hop(times) {
  for (let i = 0; i < times; i++) {
    bot.setControlState('jump', true);
    await wait(250);
    bot.setControlState('jump', false);
    await wait(350);
  }
}

// The players' hello: look at them, wave, and crouch up and down.
async function wave(player) {
  const target = playerEntity(player);
  if (target) await bot.lookAt(target.position.offset(0, 1.6, 0)).catch(() => {});
  for (let i = 0; i < 3; i++) {
    bot.swingArm('right');
    bot.setControlState('sneak', true);
    await wait(200);
    bot.setControlState('sneak', sitting);
    await wait(200);
  }
}

// ---- Guarding --------------------------------------------------------------------------------------

// Monsters she fights: what minecraft-data files as hostile, and the few it files elsewhere (slimes, magma cubes and
// phantoms as « mob », hoglins as « animal »). Neutral ones that only turn hostile when hit (endermen, piglins) are
// left alone: starting that fight would be the opposite of protecting anyone. So are the warden (two hits kill her,
// and a fight only angers it more) and the creaking (nothing hurts it while its heart stands).
const Peaceful = new Set(['enderman', 'zombified_piglin', 'piglin', 'warden', 'creaking']);
const AlsoFoes = new Set(['slime', 'magma_cube', 'phantom', 'hoglin']);
const GuardRadius = 10;
const MeleeReach = 3.2;
const Weapons = ['netherite_sword', 'diamond_sword', 'iron_sword', 'stone_sword', 'golden_sword', 'wooden_sword',
  'netherite_axe', 'diamond_axe', 'iron_axe', 'stone_axe', 'golden_axe', 'wooden_axe'];
let attackReadyAt = 0;

function isFoe(entity) {
  return Boolean(entity) && (entity.type === 'hostile' || AlsoFoes.has(entity.name)) && !Peaceful.has(entity.name)
    && entity.isValid !== false;
}

// Guarding a place rather than a player: monsters within PatrolRadius of the spot.
const PatrolRadius = 16;
// Under this much health (3 hearts) she stops fighting and backs off until she has healed, or RetreatFor has passed.
const RetreatHealth = 6;
const RetreatFor = 20000;
// A creeper is never hit by hand: its blast would land on the player she guards. With a bow and arrows she shoots it
// from at least CreeperRange; without, she keeps out of its reach and lets the player deal with it.
const CreeperRange = 6;
const CreeperSight = 16;
// A monster she chases that stays out of sight this long (gone round a corner for good, down a hole) is let go.
const FoeUnseen = 4000;

function guard(player, quiet = false) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'guard', player, foe: null, mode: null, modeTarget: null, warned: new Set(), skipLoot: new Set(), reported: false };
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  if (!quiet) emit('moving', { player, kind: 'guard' });
}

// ---- Portals -----------------------------------------------------------------------------------

// The player she follows or guards steps into a nether portal: they vanish from her world, which read as « lost ». Where
// they vanished is a portal, so she goes after them, into it, and stands there until it takes her (4 s in survival);
// on the other side she steps out, looks for them, and goes on as she was, without a word more (the order was
// acknowledged). Back the same way. Only when they vanished by a portal, close to her (further off, they walked out of
// sight first) and still online: a logout by a portal isn't a crossing, and since the server drops the entity before
// the name, that's checked every tick until she's through (the portal takes 4 s, so she never goes alone).
const PortalNear = 2; // how close to a portal block they vanished
const PortalReach = 24; // how far from her that portal can be
const PortalTake = 20000; // to get to it and through
const PortalFind = 15000; // on the other side, to see them again
const PortalSure = 500; // still listed this long after vanishing: through, not logged out

function portalId() {
  const b = bot.registry.blocksByName.nether_portal;
  return b ? b.id : -1;
}

// The portal block someone at `pos` is standing in, the lowest of its column (where feet go), or null.
function portalAt(pos) {
  const id = portalId();
  const b = bot.findBlock({ point: pos, matching: id, maxDistance: PortalNear });
  if (!b) return null;
  let low = b;
  for (let below = bot.blockAt(low.position.offset(0, -1, 0)); below && below.type === id;
    below = bot.blockAt(low.position.offset(0, -1, 0))) low = below;
  return low;
}

// True when the player who just vanished went through a portal and she sets off after them.
function crossAfter(entity) {
  if (!task || !FollowsSomeone.has(task.kind) || entity.username !== task.player || !bot.players[task.player]) return false;
  const portal = portalAt(entity.position);
  if (!portal || portal.position.distanceTo(bot.entity.position) > PortalReach) return false;
  const { player, kind } = task;
  stay();
  task = { kind: 'portal', player, then: kind, from: dimension(), crossed: false, announced: false, at: Date.now(),
    until: Date.now() + PortalTake };
  const movements = gentleMovements();
  movements.blocksToAvoid.delete(portalId());
  bot.pathfinder.setMovements(movements);
  bot.pathfinder.setGoal(new goals.GoalBlock(portal.position.x, portal.position.y, portal.position.z));
  return true;
}

// Every physics tick while crossing: through (out of the arrival portal, then looking), seen again (go on), or gone.
function portalTick() {
  const t = task;
  if (!t || t.kind !== 'portal') return;
  if (!t.crossed && !bot.players[t.player]) { // logged out, not through
    emit('cantReach', { player: t.player, why: 'lost' });
    return stay();
  }
  // Said once they're surely through, not out: still listed half a second on.
  if (!t.announced && Date.now() > t.at + PortalSure) {
    t.announced = true;
    emit('crossing', { player: t.player });
  }
  if (!t.crossed && dimension() !== t.from) {
    t.crossed = true;
    t.until = Date.now() + PortalFind;
    bot.pathfinder.setMovements(gentleMovements());
    const at = bot.entity.position;
    bot.pathfinder.setGoal(new goals.GoalInvert(new goals.GoalNear(at.x, at.y, at.z, 2))); // out of the portal
  }
  if (playerEntity(t.player)) {
    task = null;
    bot.pathfinder.setMovements(gentleMovements());
    return t.then === 'guard' ? guard(t.player, true) : follow(t.player, true);
  }
  if (Date.now() > t.until) {
    emit('cantReach', { player: t.player, why: 'lost' });
    stay();
  }
}

// « syncs surveille la base »: where she stands, or the remembered spot C# sends.
function patrol(player, spot) {
  let anchor = bot.entity.position.floored();
  if (spot) {
    if (spot.dimension !== dimension()) return emit('cantPatrol', { player, why: 'otherDimension' });
    if (Math.hypot(spot.x - anchor.x, spot.z - anchor.z) > MaxTrip) return emit('cantPatrol', { player, why: 'tooFar' });
    anchor = new Vec3(spot.x, spot.y, spot.z);
  }
  stay();
  task = { kind: 'patrol', player, anchor, foe: null, mode: null, modeTarget: null, warned: new Set(), skipLoot: new Set(), reported: false };
  emit('patrolling', { player });
}

// Changes where she's heading only when what she's doing changes: a goal set on every tick would never be walked.
function setMode(t, mode, target, goal, dynamic) {
  if (t.mode === mode && t.modeTarget === target) return;
  t.mode = mode;
  t.modeTarget = target;
  t.modeSince = Date.now();
  bot.pathfinder.setGoal(goal, dynamic);
}

function arrows() {
  return bot.inventory.items().some((i) => i.name.endsWith('arrow'));
}

function bowReady() {
  return arrows() && bot.inventory.items().some((i) => i.name === 'bow');
}

// One arrow: drawn for a second (full power), aimed a little above the target to make up for the drop, aimed again
// just before letting go, since the target moves meanwhile.
let drawing = false;

// A player (not her) within a block of the line from her eyes to `target`, between the two.
function playerInLine(target) {
  const from = bot.entity.position.offset(0, 1.62, 0);
  const to = target.position.offset(0, (target.height || 1.7) * 0.6, 0);
  const line = to.minus(from);
  const length = line.norm();
  if (length === 0) return null;
  const dir = line.scaled(1 / length);
  return Object.values(bot.players).map((p) => p.entity).find((e) => {
    if (!e || e === bot.entity) return false;
    const mid = e.position.offset(0, 0.9, 0).minus(from);
    const along = mid.dot(dir);
    return along > 0.5 && along < length && mid.minus(dir.scaled(along)).norm() < 1;
  }) || null;
}

// Nothing solid between `from` (eyes) and `entity`'s head or chest: a monster she can see, and shoot or hit. A creeper
// in a cave under her feet was « near », and she shot arrow after arrow into the ground at it.
function inSight(entity, from = bot.entity.position.offset(0, 1.62, 0)) {
  const h = entity.height || 1.7;
  return [0.9, 0.6].some((k) => {
    const line = entity.position.offset(0, h * k, 0).minus(from);
    const length = line.norm();
    return length < 0.01 || !bot.world.raycast(from, line.scaled(1 / length), length);
  });
}

// What a player sees from: their eyes.
const eyesOf = (e) => e.position.offset(0, 1.62, 0);

// Three blocks to her side of the line to `target`, the side with more room: a clear shot from there.
function sideStep(target) {
  const me = bot.entity.position;
  const dx = target.position.x - me.x;
  const dz = target.position.z - me.z;
  const n = Math.hypot(dx, dz) || 1;
  const left = new Vec3(me.x - (dz / n) * 3, me.y, me.z + (dx / n) * 3);
  const right = new Vec3(me.x + (dz / n) * 3, me.y, me.z - (dx / n) * 3);
  const near = (p) => bot.nearestEntity((e) => e.type === 'player' && e !== bot.entity && e.position.distanceTo(p) < 2);
  const spot = near(left) && !near(right) ? right : left;
  return new goals.GoalNear(spot.x, spot.y, spot.z, 1);
}

async function shoot(target) {
  if (drawing || Date.now() < attackReadyAt) return;
  const bow = bot.inventory.items().find((i) => i.name === 'bow');
  if (!bow) return;
  drawing = true;
  const aim = () => {
    const d = target.position.distanceTo(bot.entity.position);
    return target.position.offset(0, (target.height || 1.7) * 0.6 + 0.003 * d * d, 0);
  };
  try {
    if (!bot.heldItem || bot.heldItem.name !== 'bow') await bot.equip(bow, 'hand');
    await bot.lookAt(aim(), true);
    bot.activateItem();
    await wait(1100);
    if (target.isValid) await bot.lookAt(aim(), true);
    // Someone stepped into the line while she drew: a moment for them to pass, then the arrow goes up, not into them.
    for (let i = 0; i < 10 && target.isValid && playerInLine(target); i++) {
      await wait(100);
      await bot.lookAt(aim(), true);
    }
    // And not into a wall or the ground: gone behind something while she drew, the arrow goes up too.
    if (playerInLine(target) || target.isValid === false || !inSight(target)) await bot.look(bot.entity.yaw, Math.PI / 2, true);
    bot.deactivateItem();
  } catch (err) {
    log('shot failed:', err && err.message ? err.message : String(err));
  } finally {
    drawing = false;
    attackReadyAt = Date.now() + 300;
  }
}

function stopBow() {
  if (!drawing) return;
  try { bot.deactivateItem(); } catch { /* not drawing after all */ }
}

async function equipWeapon() {
  const items = bot.inventory.items();
  for (const name of Weapons) {
    const item = items.find((i) => i.name === name);
    if (!item) continue;
    if (bot.heldItem && bot.heldItem.name === name) return;
    try { await bot.equip(item, 'hand'); } catch { /* keep fighting bare-handed */ }
    return;
  }
}

// ---- Striking: the one way every fight hits ----------------------------------------------------------------

// A hit taken with a drop or lava behind her throws her in (knockback). Before swinging she steps off such a lip for
// EdgeStep, towards what she fights or along the edge.
const EdgeStep = 1000;
// A foe this far from where it stood when the fight started has run: let go, or she's drawn deep into a cave.
const FightLeash = 12;
// A sword's sweep lands on whoever stands within a block of what it hits. With a player that close she hits on the way
// down from a jump: a critical hit, which never sweeps (the game sweeps only from the ground). Not hitting at all left a
// zombie on Papa until it killed him.
const SweepReach = 1.6;

// A drop of more than 3 blocks, lava, or the void at (x, y, z), y being the level of her feet. Unloaded is no hazard
// (the step costs ask in the middle of a search, where a guess must not throw).
function hazardAt(x, y, z) {
  const at = (dy) => bot.world.getBlockStateId(new Vec3(Math.floor(x), y + dy, Math.floor(z)));
  const here = at(0);
  if (here == null) return false;
  if (lavaStates.has(here)) return true;
  for (let d = 1; d <= 4; d++) {
    if (y - d < minY()) return true;
    const below = at(-d);
    if (lavaStates.has(below)) return true;
    if (!airStates.has(below)) return false;
  }
  return true;
}

// Where to step before swinging at `foe`, when the block behind her (away from it) is a hazard: towards it, else
// along the edge either way. Null when her back is safe.
function edgeBehind(foe) {
  const me = bot.entity.position;
  const dx = me.x - foe.position.x;
  const dz = me.z - foe.position.z;
  const n = Math.hypot(dx, dz);
  if (n < 0.01) return null;
  const ux = dx / n;
  const uz = dz / n;
  const y = Math.floor(me.y + 0.01);
  if (!hazardAt(me.x + ux, y, me.z + uz)) return null;
  const spots = [[-ux, -uz], [-uz, ux], [uz, -ux]].map(([sx, sz]) => new Vec3(me.x + sx * 1.5, y, me.z + sz * 1.5));
  return spots.find((p) => !hazardAt(p.x, y, p.z)) || spots[0];
}

function playerBeside(foe) {
  return Object.values(bot.players).some((p) => p.entity && p.entity !== bot.entity
    && Math.hypot(p.entity.position.x - foe.position.x, p.entity.position.z - foe.position.z) < SweepReach
    && Math.abs(p.entity.position.y - foe.position.y) < 1.5);
}

// Her walk in a fight: no jumping a gap, no drop deeper than 2, and the lip of a drop or a lava pool costs what a
// detour does, so she never chases along a ravine's edge. State ids only: it's asked for every step weighed.
function fightMovements() {
  const movements = gentleMovements();
  movements.allowParkour = false;
  movements.maxDropDown = 2;
  movements.exclusionAreasStep.push((block) => {
    if (!block || !block.position) return 0;
    const p = block.position;
    return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => hazardAt(p.x + dx, p.y, p.z + dz)) ? 40 : 0;
  });
  return movements;
}

let fightWalk = false;

function fightPace(on) {
  if (fightWalk === on) return;
  fightWalk = on;
  bot.pathfinder.setMovements(on ? fightMovements() : gentleMovements());
}

// A foe with no way to it is left alone this long (noWay), then tried again.
const UnreachableFor = 10000;

function reachable(t, foe) {
  return !(t.unreachable && t.unreachable.get(foe.id) > Date.now());
}

function setUnreachable(t, foe) {
  if (foe.position.distanceTo(bot.entity.position) < MeleeReach) return; // in reach: hit it where it stands
  if (!t.unreachable) t.unreachable = new Map();
  t.unreachable.set(foe.id, Date.now() + UnreachableFor);
  if (t.foe === foe) t.foe = null;
  t.mode = null;
  t.modeTarget = null;
}

// The closest monster (not a creeper) in reach of her and in sight: the one hitting her, or about to. Never one set
// aside as unreachable: in reach, it can be hit whatever the pathfinder found.
function foeOnHer() {
  const me = bot.entity.position;
  return bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e)
    && e.position.distanceTo(me) < MeleeReach + 0.5 && inSight(e));
}

// After `foe`, weapon in hand, with her fight walk. Not while she steps off a lip (strike).
function chase(t, foe) {
  if (Date.now() < (t.edgeUntil || 0)) return;
  if (t.mode !== 'fight' || t.modeTarget !== foe) equipWeapon();
  fightPace(true);
  setMode(t, 'fight', foe, new goals.GoalFollow(foe, 1), true);
}

// One hit on `foe` when she can: in reach and in sight, her weapon ready (its own cooldown), and no hazard behind her
// (then she steps off the lip first, when there's a task `t` to move for). With a player beside it and a sword in hand,
// from a jump (SweepReach). True when she swung.
function strike(foe, t = null) {
  const me = bot.entity.position;
  if (!foe || foe.isValid === false || foe.position.distanceTo(me) >= MeleeReach || Date.now() < attackReadyAt
    || !inSight(foe)) return false;
  if (bot.heldItem && bot.heldItem.name.endsWith('_sword') && playerBeside(foe)) {
    if (bot.entity.onGround) {
      bot.setControlState('jump', true);
      return false;
    }
    bot.setControlState('jump', false);
    if (bot.entity.velocity.y >= 0) return false; // still going up: the hit lands on the way down
  }
  const step = edgeBehind(foe);
  if (step) {
    if (t && Date.now() >= (t.edgeUntil || 0)) {
      t.edgeUntil = Date.now() + EdgeStep;
      t.mode = 'edge';
      t.modeTarget = foe;
      t.modeSince = Date.now();
      bot.pathfinder.setGoal(new goals.GoalNear(step.x, step.y, step.z, 0));
    }
    return false;
  }
  attackReadyAt = Date.now() + cooldownFor(bot.heldItem ? bot.heldItem.name : null);
  bot.lookAt(foe.position.offset(0, (foe.height || 1.6) * 0.8, 0), true).catch(() => {});
  bot.attack(foe);
  return true;
}

// Every physics tick while guarding a player or a place, in this order: back off when badly hurt; deal with a
// creeper (shoot it, or keep out of its reach); otherwise pick the closest monster near the player or the spot (or
// near her), chase it, hit it when in reach, then go back to following or to the spot.
function guardTick() {
  const t = task;
  if (!t || (t.kind !== 'guard' && t.kind !== 'patrol')) return;
  const owner = t.kind === 'guard' ? playerEntity(t.player) : null;
  if (t.kind === 'guard' && !owner) {
    emit('cantReach', { player: t.player, why: 'lost' });
    stay();
    return;
  }
  const me = bot.entity.position;
  const center = owner ? owner.position : t.anchor;
  const radius = owner ? GuardRadius : PatrolRadius;
  const goHome = () => {
    fightPace(false);
    // What dropped from her kills first, when it's close and inside what she guards.
    const loot = lootNear(center, radius, t.skipLoot);
    if (loot && t.mode === 'loot' && t.modeTarget === loot && Date.now() - t.modeSince > PickUpTry) {
      t.skipLoot.add(loot.id); // a way there, but never close enough: left where it is
      return goHome();
    }
    if (loot) return setMode(t, 'loot', loot, new GoalPickUp(loot.position));
    if (owner) return setMode(t, 'home', owner, new goals.GoalFollow(owner, 2), true);
    // Back to the spot; again if pushed off it after getting there (a reached goal is dropped).
    if (t.mode !== 'home' || (!bot.pathfinder.goal && me.distanceTo(t.anchor) > 3)) {
      t.mode = 'home';
      t.modeTarget = null;
      bot.pathfinder.setGoal(new goals.GoalNear(t.anchor.x, t.anchor.y, t.anchor.z, 2));
    }
  };

  // Badly hurt: away from the closest monster until healed, eating to heal faster.
  if (!t.retreatUntil && bot.health <= RetreatHealth && Date.now() >= (t.retreatAgainAt || 0)) {
    t.retreatUntil = Date.now() + RetreatFor;
    t.foe = null;
    stopBow();
    emit('retreat', { player: t.player });
    eatIfHungry(20).catch(() => {});
  }
  if (t.retreatUntil) {
    if (bot.health >= 14 || Date.now() > t.retreatUntil) {
      t.retreatUntil = null;
      t.retreatAgainAt = Date.now() + 30000;
    } else {
      const threat = bot.nearestEntity((e) => isFoe(e) && e.position.distanceTo(me) < 10);
      if (threat) setMode(t, 'retreat', threat, new goals.GoalInvert(new goals.GoalFollow(threat, 12)), true);
      else goHome();
      // Backing off, she still hits what keeps up with her: with nowhere to go (a ledge), she stood and took it.
      const onHer = foeOnHer();
      if (onHer) strike(onHer);
      return;
    }
  }

  // Creepers: warned about once each, shot from range, never hit by hand. Seen from CreeperSight: one walks 10 blocks
  // in about 3 s, two shots at most, and it takes three to kill (it blew up by Papa that way on the local server).
  // Only one in sight, hers or the guarded player's: one in a cave under them is no threat, and can't be shot.
  const creeper = bot.nearestEntity((e) => e.name === 'creeper' && e.isValid !== false
    && (e.position.distanceTo(center) < CreeperSight || e.position.distanceTo(me) < CreeperSight)
    && (inSight(e) || (owner && inSight(e, eyesOf(owner)))));
  if (creeper) {
    if (!t.warned.has(creeper.id)) {
      t.warned.add(creeper.id);
      emit('creeper', { player: t.player });
    }
    const d = creeper.position.distanceTo(me);
    if (d < CreeperRange) return setMode(t, 'flee', creeper, new goals.GoalInvert(new goals.GoalFollow(creeper, CreeperRange + 3)), true);
    if (bowReady() && inSight(creeper)) {
      // Never through a player: standing behind the one she guards, she put four arrows in his back while the
      // creeper walked up to him. With someone in the way she steps aside first.
      const inTheWay = playerInLine(creeper);
      if (inTheWay) return setMode(t, 'aside', creeper, sideStep(creeper), false);
      setMode(t, 'shoot', creeper, null);
      shoot(creeper);
      return;
    }
  }

  // Every other monster: chased and hit. Not one under water (a drowned at the bottom): she can't reach it there. Nor
  // one she can't see (in a cave under the ground): taken on only in sight, and let go once out of it for FoeUnseen.
  // One on her comes first, whatever she was after: chasing a zombie she couldn't reach, she let the one behind her
  // kill her. One she found no way to is left for a while (unreachable).
  if (t.foe && inSight(t.foe)) t.foeSeenAt = Date.now();
  if (t.foe && (!t.foe.isValid || t.foe.position.distanceTo(center) > radius * 1.6 || headUnder(t.foe)
    || Date.now() - (t.foeSeenAt || 0) > FoeUnseen)) t.foe = null;
  const onHer = foeOnHer();
  if (onHer && onHer !== t.foe) {
    t.foe = onHer;
    t.foeSeenAt = Date.now();
  }
  if (!t.foe) {
    t.foe = bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e) && reachable(t, e)
      && (e.position.distanceTo(center) < radius || e.position.distanceTo(me) < 6) && inSight(e));
    if (!t.foe) return goHome();
    t.foeSeenAt = Date.now();
  }
  chase(t, t.foe);
  strike(t.foe, t);
}

// ---- Gathering ---------------------------------------------------------------------------------------

// What each resource is, by block name. Only natural blocks: wood is a log with natural leaves near it
// (a log in someone's wall has none, and placed leaves are "persistent"), never a stripped log.
const Resources = {
  wood: (n) => n.endsWith('_log') && !n.startsWith('stripped_'),
  stone: (n) => n === 'stone',
  coal: (n) => n === 'coal_ore' || n === 'deepslate_coal_ore',
  iron: (n) => n === 'iron_ore' || n === 'deepslate_iron_ore',
  copper: (n) => n === 'copper_ore' || n === 'deepslate_copper_ore',
  gold: (n) => n === 'gold_ore' || n === 'deepslate_gold_ore',
  diamond: (n) => n === 'diamond_ore' || n === 'deepslate_diamond_ore',
  dirt: (n) => n === 'dirt' || n === 'grass_block',
  sand: (n) => n === 'sand',
};
const GatherRadius = 48;
// What each resource drops, by item name: what she counts as brought back. A tree's leaves drop saplings, sticks and
// apples, handed over too but not counted as wood.
const Yields = {
  wood: (n) => n.endsWith('_log'),
  stone: (n) => n === 'cobblestone' || n === 'stone',
  coal: (n) => n === 'coal',
  iron: (n) => n === 'raw_iron',
  copper: (n) => n === 'raw_copper',
  gold: (n) => n === 'raw_gold',
  diamond: (n) => n === 'diamond',
  dirt: (n) => n === 'dirt',
  sand: (n) => n === 'sand',
};

function isTreeLog(block) {
  if (!block) return false;
  for (let dy = 0; dy <= 6; dy++)
    for (let dx = -2; dx <= 2; dx++)
      for (let dz = -2; dz <= 2; dz++) {
        const near = bot.blockAt(block.position.offset(dx, dy, dz));
        if (near && near.name.endsWith('_leaves') && near.getProperties().persistent === false) return true;
      }
  return false;
}

// Whether a block has an open face (air, water, a plant: something with no collision box) to break it from.
// Not water either: coal at the bottom of a lake counted as open, she went down for it, mined it at a fifth of the
// speed with her head under, and drowned there (the camp's coal, a local server).
function exposed(block) {
  return [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]].some(([dx, dy, dz]) => {
    const near = bot.blockAt(block.position.offset(dx, dy, dz));
    return near && near.boundingBox === 'empty' && near.name !== 'lava' && near.name !== 'water' && near.name !== 'bubble_column';
  });
}

// Whether anything she carries (or her bare hand) gets a drop from this block.
function canHarvest(block) {
  return [null, ...bot.inventory.items().map((i) => i.type)].some((type) => block.canHarvest(type));
}

function itemCounts() {
  const counts = new Map();
  for (const item of bot.inventory.items()) counts.set(item.type, (counts.get(item.type) || 0) + item.count);
  return counts;
}

// `since`: started again after a fight (restartOrder), from the pockets as they were when the order first came, so
// what she had gathered before still counts and is handed over; no second « collecting ».
// What she can get at, the closest 256: she never digs, so a block with no open face (stone under the grass) is out
// of reach (collectblock spent its 5 s search on one and stopped the order). Open is part of the search, not a filter
// after it: on a mountain the closest 256 stone were all under her feet, and she said there was none with a cliff face
// 20 blocks off. `all`, for the count she announces, is the closest 256 of the kind. Not wood: trees are felled whole
// (treesNear, fell).
function gatherCandidates(resource) {
  const matches = Resources[resource];
  const ids = bot.registry.blocksArray.filter((b) => matches(b.name)).map((b) => b.id);
  return {
    all: bot.findBlocks({ matching: ids, maxDistance: GatherRadius, count: 256 }).map((p) => bot.blockAt(p)).filter(Boolean),
    open: bot.findBlocks({ matching: ids, useExtraInfo: exposed, maxDistance: GatherRadius, count: 256 })
      .map((p) => bot.blockAt(p)).filter(Boolean),
  };
}

// ---- Trees -------------------------------------------------------------------------------------------

// A tree is felled whole, to its last log: cutting the closest logs anywhere left trunks floating half cut all over a
// forest, and a 2x2 spruce's top out of any reach. A tree is the logs of one kind touching each other (corners too:
// acacia and dark oak branches), from the bottom of its trunk, within TreeReach of it sideways and TreeHigh above, with
// natural leaves near its top (isTreeLog on its TreeTop highest logs): a big spruce's trunk has none for its first ten
// blocks, and a log in someone's wall has none at all.
const TreeReach = 6;
const TreeHigh = 40;
const TreeMost = 400;
const TreeTop = 8;
const LogSearch = 512;
const posKey = (p) => `${p.x},${p.y},${p.z}`;

// The tree `block` belongs to, or null if it isn't one. Every log it reaches goes in `seen`, tree or not.
function treeOf(block, seen) {
  let base = block.position;
  while (base.y > block.position.y - TreeHigh) {
    const below = bot.blockAt(base.offset(0, -1, 0));
    if (!below || below.name !== block.name) break;
    base = below.position;
  }
  const logs = [];
  const queue = [base];
  const marked = new Set([posKey(base)]);
  while (queue.length > 0 && logs.length < TreeMost) {
    const p = queue.shift();
    logs.push(p);
    for (let dx = -1; dx <= 1; dx++)
      for (let dy = -1; dy <= 1; dy++)
        for (let dz = -1; dz <= 1; dz++) {
          const q = p.offset(dx, dy, dz);
          const k = posKey(q);
          // A few blocks under the base: the other trunks of a 2x2 on a slope go lower.
          if (marked.has(k) || Math.abs(q.x - base.x) > TreeReach || Math.abs(q.z - base.z) > TreeReach
            || q.y < base.y - 3 || q.y > base.y + TreeHigh) continue;
          const near = bot.blockAt(q);
          if (!near || near.name !== block.name) continue;
          marked.add(k);
          queue.push(q);
        }
  }
  for (const k of marked) seen.add(k);
  const top = [...logs].sort((a, b) => b.y - a.y).slice(0, TreeTop);
  if (!top.some((p) => isTreeLog(bot.blockAt(p)))) return null;
  return { name: block.name, base: logs.reduce((low, p) => (p.y < low.y ? p : low), base), logs };
}

// The trees within GatherRadius, from their closest LogSearch logs.
function treesNear() {
  const ids = bot.registry.blocksArray.filter((b) => Resources.wood(b.name)).map((b) => b.id);
  const seen = new Set();
  const trees = [];
  for (const p of bot.findBlocks({ matching: ids, maxDistance: GatherRadius, count: LogSearch })) {
    if (seen.has(posKey(p))) continue;
    const block = bot.blockAt(p);
    const tree = block && treeOf(block, seen);
    if (tree) trees.push(tree);
  }
  return trees;
}

// What's left of it; not her pillar, which may stand where its trunk was (unstack takes that down, from the top).
const standingLogs = (tree, placed = []) => tree.logs.filter((p) => !placed.some((q) => q.equals(p)))
  .map((p) => bot.blockAt(p)).filter((b) => b && b.name === tree.name);

// One tree after another, the closest to where she stands first, until `left` logs are cut. Each to its last log, so
// she brings more than asked rather than leave a tree standing half cut.
async function fellTrees(trees, left, mine) {
  const todo = [...trees];
  while (left > 0 && todo.length > 0 && task === mine) {
    const me = bot.entity.position;
    todo.sort((a, b) => a.base.distanceTo(me) - b.base.distanceTo(me));
    left -= await fell(todo.shift(), mine);
  }
}

// Bottom up, in rounds: each round cuts what she gets at, from the ground or from a pillar she stacks under herself
// (gatheringMovements' climb: a 2x2 spruce stands 30 high), and ends at the first log she can't get to, so a tree's top
// never costs a 5 s search per log. The next round has the logs the last one brought to stack with. The drops are
// picked up on the ground: between rounds while there's no pillar, else once it's down (unstack). Returns how many of
// its logs are gone.
const FellRounds = 8;
const ReachUp = 3; // a block this high above her feet needs a climb

async function fell(tree, mine) {
  const placed = [];
  const drops = new Set();
  const collectDrops = async () => {
    if (task === mine && drops.size > 0) await pickUp((e) => drops.has(e.id), Math.min(60000, drops.size * PickUpTry), mine);
    drops.clear();
  };
  let left = standingLogs(tree);
  const start = left.length;
  for (let round = 0; round < FellRounds && left.length > 0 && task === mine; round++) {
    await gather(left, mine, 'fell', { placed, drops, order: 'up' });
    const now = standingLogs(tree, placed);
    const broke = now.length < left.length;
    left = now;
    if (pillar(placed).length === 0) await collectDrops();
    if (!broke) break;
  }
  if (left.length > 0) log(`fell: ${left.length} of ${tree.logs.length} ${tree.name} left standing at ${tree.base}`);
  if (pillar(placed).length > 0 && task === mine) await unstack(placed, mine, drops);
  await collectDrops();
  return start - standingLogs(tree, placed).length;
}

// Her pillar, from the top down: she breaks the block she stands on, drops onto the next, and so on to the ground.
// Nothing she placed stays in the forest; what it was made of comes back to her pockets with the drops.
async function unstack(placed, mine, drops) {
  const blocks = pillar(placed);
  if (blocks.length > 0) await gather(blocks, mine, 'unstack', { drops, order: 'down' });
  const left = pillar(placed);
  if (left.length > 0) log(`unstack: ${left.length} of her pillar's blocks left, the top at ${left[0].position}`);
}

// What stands of her pillar: `placed` is where she tried to set a block.
const pillar = (placed) => placed.map((p) => bot.blockAt(p)).filter((b) => b && b.boundingBox === 'block');

// What a pillar is stacked from, in that order: what she'd least miss first, a log last (it comes back when the pillar
// comes down).
const ScaffoldItems = ['dirt', 'coarse_dirt', 'cobblestone', 'cobbled_deepslate', 'netherrack', 'andesite', 'diorite', 'granite'];

function scaffoldIds() {
  const named = ScaffoldItems.map((n) => bot.registry.itemsByName[n]).filter(Boolean);
  const logs = bot.registry.itemsArray.filter((i) => isLog(i.name));
  return [...named, ...logs].map((i) => i.id);
}

// In rounds: what's open now, then what that opened (an ore behind the one in front), until `left` are broken,
// nothing is open, or a round broke nothing.
async function gatherRounds(resource, left, mine, open = gatherCandidates(resource).open) {
  for (let round = 0; round < 4 && left > 0 && open.length > 0 && task === mine; round++) {
    const batch = open.slice(0, left);
    await gather(batch, mine, 'collect');
    const broken = batch.filter((b) => {
      const now = bot.blockAt(b.position);
      return !now || now.type !== b.type;
    }).length;
    left -= broken;
    if (broken === 0 || task !== mine) break;
    open = gatherCandidates(resource).open;
  }
}

async function collect(player, resource, count, since = null) {
  if (!Resources[resource]) return;
  const wood = resource === 'wood';
  const trees = wood ? treesNear() : null;
  const first = wood ? null : gatherCandidates(resource);
  if (wood ? trees.length === 0 : first.open.length === 0) return emit('cantCollect', { player, resource, why: 'none' });
  if (!wood && !canHarvest(first.open[0])) return emit('cantCollect', { player, resource, why: 'noTool' });
  stay();

  const before = since || itemCounts();
  const counted = (type) => Yields[resource](bot.registry.items[type].name);
  const mine = { kind: 'collect', player, before };
  task = mine;
  const available = wood ? trees.reduce((n, t) => n + t.logs.length, 0) : first.all.length;
  if (!since) emit('collecting', { player, resource, count: Math.min(count, available) });
  // Started again, what she already has of it counts towards the count.
  const already = [...itemCounts()].filter(([type]) => counted(type))
    .reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
  if (wood) await fellTrees(trees, count - already, mine);
  else await gatherRounds(resource, count - already, mine, first.open);
  if (task !== mine) return; // told to stop in the meantime

  const { total, delivered } = await deliverGains(player, before, counted);
  if (task === mine) task = null;
  emit('collected', { player, resource, count: total, delivered });
}

// collectblock walks into each drop's own block (GoalFollow, range 0) and then waits for it to vanish, with no time
// limit: a drop on leaves, against a fence or under something low kept her there for good. So it only breaks: the
// drops of what it breaks are noted here instead of chased, and picked up after (pickUp, GoalPickUp, PickUpTry each).
let gatheredDrops = null;
const appendTarget = CollectTargets.prototype.appendTarget;
CollectTargets.prototype.appendTarget = function (target) {
  if (target && target.name === 'item') {
    if (gatheredDrops) gatheredDrops.add(target.id);
    return;
  }
  appendTarget.call(this, target);
};

// Whether an item that just appeared fell from breaking `block`: within 2 blocks of it sideways (the item's data can
// come a moment after it started rolling), from a block under it up to ColumnHigh above (a column's pieces drop where
// each piece was, up to 16 high for bamboo; within 2 in all directions, they were left lying).
const ColumnHigh = 17;

function dropOf(entity, block) {
  const p = entity.position;
  const c = block.position.offset(0.5, 0, 0.5);
  return Math.hypot(p.x - c.x, p.z - c.z) <= 2 && p.y >= block.position.y - 1.5 && p.y <= block.position.y + ColumnHigh;
}

// Breaks `blocks`, then picks up what they dropped. `what` names it in the log. Solid blocks go to collectblock (with
// gatheringMovements). Blocks with no collision box (wheat, carrots, potatoes, beetroot) can't: it walks to where a
// ray from her eyes hits the block, and a ray goes through those, so it searched for 5 s and gave up on the harvest.
// `how`, for a tree (fell, unstack): `drops` gets the drops and the caller picks them up (she may be up a pillar);
// `placed`, she may climb, and gets every block she places; `order` 'up' (lowest first, ending at the first she can't
// get to) or 'down' (highest first) instead of the closest first.
async function gather(blocks, mine, what, how = {}) {
  const drops = how.drops || new Set();
  const solid = blocks.filter((b) => b.shapes && b.shapes.length > 0);
  const soft = blocks.filter((b) => !(b.shapes && b.shapes.length > 0));
  if (solid.length > 0) {
    bot.collectBlock.movements = gatheringMovements(solid, Boolean(how.placed));
    gatheredDrops = drops;
    // The pathfinder places through bot.placeBlock: where each block went, while it stands.
    const placeBlock = bot.placeBlock;
    // It leaves every node from the first block to break or place on at the block's corner (its post-processing stops
    // there), and a step's « arrived » is within 0.35 of the node: on top of each block of her pillar she walked for the
    // corner of the block she stood on. The middle of the block.
    const centred = (result) => {
      for (const node of result.path)
        if (Number.isInteger(node.x) && Number.isInteger(node.z)) {
          node.x += 0.5;
          node.z += 0.5;
        }
    };
    if (how.placed) bot.on('path_update', centred);
    // A step up her pillar is a jump with the block set under her feet, and the pathfinder sets it as soon as her feet
    // are above the block under her, inside the new block's space: the server refused it, two times in three, and
    // each refusal cost a new search and its stuck timer. The block goes in once her feet are above it, at the top of
    // the jump.
    if (how.placed)
      bot.placeBlock = async (ref, face, ...rest) => {
        const at = ref.position.plus(face);
        const p = bot.entity.position;
        if (face.y === 1 && Math.floor(p.x) === at.x && Math.floor(p.z) === at.z)
          for (let i = 0; i < 10 && bot.entity.position.y < at.y + 1.02 && bot.entity.velocity.y > 0; i++) await wait(50);
        // Noted before it's tried: Mineflayer gives up on a block whose update comes late, which the server set all the
        // same (unstack takes down only what stands).
        if (!how.placed.some((q) => q.equals(at))) how.placed.push(at);
        return placeBlock.call(bot, ref, face, ...rest);
      };
    // collectblock only notes a drop that appears within half a block of the block's centre; a lump of coal fell
    // further and stayed lying. Within 2, as for crops.
    const noteDrop = (entity) => {
      if (solid.some((b) => dropOf(entity, b))) drops.add(entity.id);
    };
    bot.on('itemDrop', noteDrop);
    // One at a time, the closest first: collectblock gives up on the whole list when one block can't be reached.
    const left = [...solid];
    try {
      while (left.length > 0 && task === mine) {
        const me = bot.entity.position;
        const closer = (a, b) => a.position.distanceTo(me) - b.position.distanceTo(me);
        left.sort(how.order === 'up' ? (a, b) => a.position.y - b.position.y || closer(a, b)
          : how.order === 'down' ? (a, b) => b.position.y - a.position.y || closer(a, b) : closer);
        const block = left.shift();
        // Out of reach from the ground, with drops lying there: back to the caller, who picks them up before she climbs
        // (they'd wait for the pillar to come down, and a drop is gone after 5 minutes).
        if (how.order === 'up' && how.placed && pillar(how.placed).length === 0 && drops.size > 0
          && block.position.y - Math.floor(bot.entity.position.y) >= ReachUp) break;
        try {
          await bot.collectBlock.collect(block, { ignoreNoPath: true });
        } catch (err) {
          log(`${what}: skipped ${block.name} at ${block.position} (she was at ${bot.entity.position.floored()}):`,
            err && err.message ? err.message : String(err));
        }
        const still = bot.blockAt(block.position);
        if (how.order === 'up' && still && still.type === block.type) break;
      }
    } finally {
      gatheredDrops = null;
      bot.removeListener('itemDrop', noteDrop);
      bot.placeBlock = placeBlock;
      bot.removeListener('path_update', centred);
      bot.pathfinder.setMovements(gentleMovements());
    }
  }
  if (soft.length > 0 && task === mine) await breakSoft(soft, mine, drops);
  if (!how.drops && task === mine && drops.size > 0) await pickUp((e) => drops.has(e.id), Math.min(60000, drops.size * PickUpTry), mine);
}

// No tool in her hand: crops break at a touch, so by hand, the way a player does it (she harvested a field with the
// axe from her last order still out). Holding seeds or bread is as good as a hand. An empty hotbar slot, else one with
// no tool in it; only the hotbar is switched, never the pockets rearranged (unequipping with a full hotbar moved
// things by window clicks, and her inventory went out of step with the server's: no seed to replant, none to hand
// over). Melons and pumpkins are solid and go to collectblock, which takes the axe.
const Cuts = /_(axe|pickaxe|shovel|hoe|sword)$|^(shears|trident|mace)$/;

function emptyHand() {
  if (!bot.heldItem || !Cuts.test(bot.heldItem.name)) return;
  const hotbar = bot.inventory.slots.slice(bot.inventory.hotbarStart, bot.inventory.hotbarStart + 9);
  let i = hotbar.findIndex((item) => !item);
  if (i < 0) i = hotbar.findIndex((item) => !Cuts.test(item.name));
  if (i >= 0) bot.setQuickBarSlot(i);
}

// Set on spawn over bot.tool.equipForBlock, which collectblock calls before every block: a tool only when one breaks
// it faster than a hand does, else the hand (emptyHand). The tool plugin keeps what she holds when nothing beats it,
// so she cut cacti with the pickaxe from her last order and bamboo with an axe; a cactus breaks as fast by hand, and
// bamboo goes to the sword.
function toolOrHand(equipForBlock) {
  return async (block, options = {}, cb) => {
    await landed();
    const byHand = bot.tool.getDigTime(block, null);
    const faster = bot.inventory.items().some((i) => block.canHarvest(i.type) && bot.tool.getDigTime(block, i) < byHand);
    if (faster || !block.canHarvest(null)) return equipForBlock(block, options, cb);
    emptyHand();
    if (cb) cb();
  };
}

// Off the ground, a block takes five times as long to break, and Mineflayer times the dig from when it starts: right
// after a step up her pillar, still in the jump, a log took 16 s instead of 3. She lands first.
async function landed() {
  bot.setControlState('jump', false);
  for (let i = 0; i < 20 && !bot.entity.onGround; i++) await wait(50);
}

// The blocks with no collision box, the closest first: within 2 blocks of each (PickUpTry to get there), broken by
// hand (emptyHand). What falls from them goes in `drops`.
async function breakSoft(blocks, mine, drops) {
  const left = [...blocks];
  const noteDrop = (entity) => {
    if (blocks.some((b) => dropOf(entity, b))) drops.add(entity.id);
  };
  bot.on('itemDrop', noteDrop);
  try {
    while (task === mine && left.length > 0) {
      const me = bot.entity.position;
      left.sort((a, b) => a.position.distanceTo(me) - b.position.distanceTo(me));
      const target = left.shift();
      const p = target.position;
      try {
        await Promise.race([
          bot.pathfinder.goto(new goals.GoalNear(p.x, p.y, p.z, 2)),
          wait(PickUpTry).then(() => { throw new Error('too slow'); }),
        ]);
        const now = bot.blockAt(p);
        if (task === mine && now && now.type === target.type) {
          emptyHand();
          await bot.dig(now, true);
        }
      } catch {
        if (task === mine) bot.pathfinder.setGoal(null); // out of reach, or too slow: the next one
      }
    }
    await wait(500); // the last drops land
  } finally {
    bot.removeListener('itemDrop', noteDrop);
  }
}

// What she gained since `before` (an itemCounts()), brought back: she walks up to the player and tosses it at
// their feet. Kept on her when the player is out of sight or the walk fails. `counts`: which item types make the
// total (all of them by default); everything gained is handed over.
async function deliverGains(player, before, counts = () => true) {
  const after = itemCounts();
  const gained = [...after].map(([type, n]) => [type, n - (before.get(type) || 0)]).filter(([, n]) => n > 0);
  const total = gained.filter(([type]) => counts(type)).reduce((sum, [, n]) => sum + n, 0);
  let delivered = false;
  const owner = playerEntity(player);
  if (owner && total > 0) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(owner.position.x, owner.position.y, owner.position.z, 2));
      await bot.lookAt(owner.position.offset(0, 1.6, 0), true);
      // Whole stacks only, the smallest first: toss() with a count drops one item per click, and on 26.1 only the first
      // landed while her inventory went out of step with the server's (1 wheat of 25 reached Papa, then the seeds
      // were « not found »). A stack that's partly hers from before stays whole with her.
      for (const [type, n] of gained) {
        let left = n;
        for (const stack of bot.inventory.items().filter((i) => i.type === type).sort((a, b) => a.count - b.count)) {
          if (stack.count > left) continue;
          await bot.tossStack(stack);
          left -= stack.count;
        }
      }
      delivered = true;
    } catch (err) {
      log('delivery failed:', err && err.message ? err.message : String(err));
    }
  }
  return { total, delivered };
}

// ---- Farming -----------------------------------------------------------------------------------------

// What she harvests, by crop: the block, its ripe age, and the seed she replants it with. Melons and
// pumpkins have no age and need no replanting (the stem stays); only a fruit next to an attached stem is
// a crop, so a pumpkin someone placed as decoration is never touched.
const Crops = {
  wheat: { block: 'wheat', ripe: 7, seed: 'wheat_seeds' },
  carrot: { block: 'carrots', ripe: 7, seed: 'carrot' },
  potato: { block: 'potatoes', ripe: 7, seed: 'potato' },
  beetroot: { block: 'beetroots', ripe: 3, seed: 'beetroot_seeds' },
  melon: { block: 'melon', stem: 'attached_melon_stem' },
  pumpkin: { block: 'pumpkin', stem: 'attached_pumpkin_stem' },
  // Columns: what grows on top of itself. She cuts the second block of each, which brings down everything above it, and
  // the bottom one stays to grow again: no replanting. Only when named or on a farm, never in « everything ripe »: a
  // bamboo jungle, a desert's cacti and a river's cane are mostly wild.
  bamboo: { block: 'bamboo', column: true },
  sugarcane: { block: 'sugar_cane', column: true },
  cactus: { block: 'cactus', column: true },
};
const HarvestRadius = 24;
const FarmRadius = 16;
const ChestRadius = 10;
const FarmRound = 15000;
// What goes in the farm's chest, and how much of each she keeps back to replant with.
const FarmProduce = new Set(['wheat', 'wheat_seeds', 'carrot', 'potato', 'poisonous_potato', 'beetroot', 'beetroot_seeds',
  'melon_slice', 'pumpkin', 'bamboo', 'sugar_cane', 'cactus']);
const SeedsKept = { wheat_seeds: 16, carrot: 16, potato: 16, beetroot_seeds: 16 };
// And what she keeps to eat: the carrots and potatoes kept as seeds are food too, so only these two are extra.
const FoodKept = { beetroot: 8, melon_slice: 8 };
// And wheat for the cows and sheep: the seeds, carrots and potatoes kept to replant already feed the rest of the pen.
const FeedKept = { wheat: 16 };

// What lies in a field is the field's: produce (FarmProduce) on or beside farmland or a crop. She only ever picked up
// the drops she saw fall from what she broke, within 2 blocks of it, so a carrot that rolled further, or one someone
// trampled out, stayed on the ground round after round. Anything else lying about stays where it is: a player's dropped
// things are theirs (lootSpots).
const FieldSweep = 20000;

function isFieldBlock(block) {
  return Boolean(block) && (block.name === 'farmland' || Object.values(Crops).some((c) => c.block === block.name));
}

function fieldDrop(entity) {
  if (entity.name !== 'item' || typeof entity.getDroppedItem !== 'function') return false;
  const item = entity.getDroppedItem();
  if (!item || !FarmProduce.has(item.name)) return false;
  const at = entity.position.floored();
  for (let dx = -1; dx <= 1; dx++)
    for (let dz = -1; dz <= 1; dz++)
      for (let dy = -1; dy <= 0; dy++)
        if (isFieldBlock(bot.blockAt(at.offset(dx, dy, dz)))) return true;
  return false;
}

function cropsFor(key) {
  return key === 'any' ? Object.values(Crops).filter((c) => !c.column) : Crops[key] ? [Crops[key]] : [];
}

function isRipe(block, crop) {
  // A column's second block: the same below it, something else below that.
  if (crop.column) {
    const below = bot.blockAt(block.position.offset(0, -1, 0));
    const base = bot.blockAt(block.position.offset(0, -2, 0));
    return Boolean(below && below.name === crop.block && base && base.name !== crop.block);
  }
  if (crop.stem) {
    return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => {
      const near = bot.blockAt(block.position.offset(dx, 0, dz));
      return near && near.name === crop.stem;
    });
  }
  // prismarine-block gives number states as strings on recent versions (« 7 » on 26.1), booleans as booleans.
  const age = Number(block.getProperties().age);
  return Number.isFinite(age) && age >= crop.ripe;
}

// Ripe crops of these kinds within the radius of a point, with the crop each one is.
function findRipe(crops, point, radius) {
  const byName = new Map(crops.map((c) => [c.block, c]));
  const ids = bot.registry.blocksArray.filter((b) => byName.has(b.name)).map((b) => b.id);
  return bot.findBlocks({ matching: ids, maxDistance: radius, point, count: 256 })
    .map((p) => bot.blockAt(p)).filter(Boolean)
    .map((block) => ({ block, crop: byName.get(block.name) }))
    .filter(({ block, crop }) => isRipe(block, crop));
}

// Breaks the ripe crops and picks up what drops (gather), then puts a seed back on each patch of farmland
// it left bare. Seeds on farmland are the one thing she ever places: nothing else, nowhere else.
async function harvestBlocks(found, mine) {
  await gather(found.map((f) => f.block), mine, 'harvest');
  let replanted = 0;
  for (const { block, crop } of found) {
    if (task !== mine) break;
    if (!crop.seed) continue;
    const soil = bot.blockAt(block.position.offset(0, -1, 0));
    const above = bot.blockAt(block.position);
    if (!soil || soil.name !== 'farmland' || !above || above.name !== 'air') continue;
    if (!bot.inventory.items().some((i) => i.name === crop.seed)) continue;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(block.position.x, block.position.y, block.position.z, 3));
      // The stack as it is now: picked before the walk, it had moved (what she picked up on the way), and she placed
      // with an empty hand (« must be holding an item to place »).
      const seed = bot.inventory.items().find((i) => i.name === crop.seed);
      if (!seed) continue;
      await bot.equip(seed, 'hand');
      await bot.placeBlock(soil, new Vec3(0, 1, 0));
      replanted++;
    } catch (err) {
      log('replant failed:', err && err.message ? err.message : String(err));
    }
  }
  return replanted;
}

// « syncs récolte le blé »: harvest what's ripe around her, replant, and bring it to the player.
// `since`: as for collect.
async function harvest(player, key, since = null) {
  const found = findRipe(cropsFor(key), bot.entity.position, HarvestRadius);
  if (found.length === 0) return emit('cantHarvest', { player, crop: key, why: 'none' });
  stay();
  const before = since || itemCounts();
  const mine = { kind: 'harvest', player, before };
  task = mine;
  if (!since) emit('harvesting', { player, crop: key, count: found.length });
  const replanted = await harvestBlocks(found, mine);
  // And what lies in the field she cut, near what she cut.
  if (task === mine) await pickUp((e) => fieldDrop(e) && found.some((f) => f.block.position.distanceTo(e.position) <= 4), FieldSweep, mine);
  if (task !== mine) return; // told to stop in the meantime

  const { total, delivered } = await deliverGains(player, before);
  if (task === mine) task = null;
  emit('harvested', { player, crop: key, count: total, replanted, delivered });
}

// « syncs occupe-toi de la ferme »: she stays on this spot and, every round, harvests what ripened within
// FarmRadius, replants, and stores the produce in the chest by the field. A farm needs crops and a chest:
// without one she says so instead of filling her pockets all evening.
function farm(player) {
  const anchor = bot.entity.position.clone();
  if (!cropsNear(anchor)) return emit('cantFarm', { player, why: 'none' });
  const chest = chestNear(anchor);
  if (!chest) return emit('cantFarm', { player, why: 'noChest' });
  stay();
  task = { kind: 'farm', player, anchor, chest, busy: false };
  emit('farming', { player });
  farmRound();
}

function cropsNear(point) {
  const cropIds = bot.registry.blocksArray.filter((b) => Object.values(Crops).some((c) => c.block === b.name)).map((b) => b.id);
  return bot.findBlocks({ matching: cropIds, maxDistance: FarmRadius, point, count: 1 }).length > 0;
}

// The chest or barrel a farm (or a pen) stores into.
function chestNear(point) {
  return chestsNear(point, 1)[0] || null;
}

// The chests and barrels within ChestRadius of a spot, closest first.
function chestsNear(point, count) {
  const chestIds = bot.registry.blocksArray.filter((b) => b.name === 'chest' || b.name === 'barrel').map((b) => b.id);
  return bot.findBlocks({ matching: chestIds, maxDistance: ChestRadius, point, count });
}

async function farmRound() {
  if (!bot || !task || task.kind !== 'farm' || task.busy) return;
  const mine = task;
  mine.busy = true;
  try {
    const found = findRipe(Object.values(Crops), mine.anchor, FarmRadius);
    const before = itemCounts();
    if (found.length > 0) await harvestBlocks(found, mine);
    // Every round, ripe or not: what lies in the field comes in too.
    if (task === mine) await pickUp((e) => fieldDrop(e) && e.position.distanceTo(mine.anchor) <= FarmRadius, FieldSweep, mine);
    // What the round brought in, counted as a harvest order counts it (items gained, net of the seeds replanted).
    const gained = [...itemCounts()].reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
    if (gained > 0) emit('farmed', { count: gained });
    if (task === mine && gained > 0) await storeProduce(mine);
    if (task === mine && bot.entity.position.distanceTo(mine.anchor) > 4)
      await bot.pathfinder.goto(new goals.GoalNear(mine.anchor.x, mine.anchor.y, mine.anchor.z, 2));
  } catch (err) {
    log('farm round failed:', err && err.message ? err.message : String(err));
  } finally {
    mine.busy = false;
  }
}

// Puts the produce in the farm's chest, keeping back enough seeds to replant, a little to eat and wheat for the
// animals. A full chest keeps the rest on her.
async function storeProduce(mine) {
  const totals = new Map();
  for (const item of bot.inventory.items())
    if (FarmProduce.has(item.name)) totals.set(item.name, (totals.get(item.name) || 0) + item.count);
  await storeIn(mine, [...totals].map(([name, n]) => [name, n - (SeedsKept[name] || FoodKept[name] || FeedKept[name] || 0)]));
}

// [name, count] pairs into the task's chest; a full chest keeps the rest on her.
async function storeIn(mine, pairs) {
  const toStore = pairs.filter(([, n]) => n > 0);
  const block = bot.blockAt(mine.chest);
  if (toStore.length === 0 || !block || (block.name !== 'chest' && block.name !== 'barrel')) return;
  await bot.pathfinder.goto(new goals.GoalNear(mine.chest.x, mine.chest.y, mine.chest.z, 2));
  if (task !== mine) return;
  const box = await bot.openContainer(block);
  try {
    for (const [name, n] of toStore) await box.deposit(bot.registry.itemsByName[name].id, null, n);
  } catch (err) {
    log('chest full or refused:', err && err.message ? err.message : String(err));
  } finally {
    box.close();
  }
}

// ---- Handing things over and putting them away -------------------------------------------------------

// What each kind of goods is, by item name. « all » is her pockets; armour includes what she wears.
const Goods = {
  all: () => true,
  food: (n) => Boolean(bot.registry.foodsByName[n]),
  fish: (n) => ['cod', 'salmon', 'tropical_fish', 'pufferfish', 'cooked_cod', 'cooked_salmon'].includes(n),
  weapons: (n) => n.endsWith('_sword') || (n.endsWith('_axe') && !n.endsWith('_pickaxe')) || ['bow', 'crossbow', 'trident', 'mace'].includes(n),
  tools: (n) => /_(pickaxe|axe|shovel|hoe)$/.test(n) || ['shears', 'fishing_rod', 'flint_and_steel'].includes(n),
  pickaxe: (n) => n.endsWith('_pickaxe'),
  axe: (n) => n.endsWith('_axe') && !n.endsWith('_pickaxe'),
  shovel: (n) => n.endsWith('_shovel'),
  rod: (n) => n === 'fishing_rod',
  bow: (n) => n === 'bow' || n === 'crossbow',
  arrows: (n) => n.endsWith('arrow'),
  armour: (n) => /_(helmet|chestplate|leggings|boots)$/.test(n) || n === 'shield' || n === 'elytra',
  seeds: (n) => n.endsWith('_seeds'),
  produce: (n) => FarmProduce.has(n) && !n.endsWith('_seeds'),
  wood: (n) => (n.endsWith('_log') || n.endsWith('_planks') || n.endsWith('_wood')),
  stone: (n) => ['cobblestone', 'stone', 'cobbled_deepslate', 'deepslate'].includes(n),
  coal: (n) => n === 'coal' || n === 'charcoal',
  iron: (n) => ['raw_iron', 'iron_ingot', 'iron_nugget', 'iron_ore', 'deepslate_iron_ore'].includes(n),
  copper: (n) => ['raw_copper', 'copper_ingot', 'copper_ore', 'deepslate_copper_ore'].includes(n),
  gold: (n) => ['raw_gold', 'gold_ingot', 'gold_nugget', 'gold_ore', 'deepslate_gold_ore'].includes(n),
  diamond: (n) => ['diamond', 'diamond_ore', 'deepslate_diamond_ore'].includes(n),
  dirt: (n) => n === 'dirt' || n === 'coarse_dirt',
  sand: (n) => n === 'sand' || n === 'red_sand',
};
const WornSlots = ['head', 'torso', 'legs', 'feet'];

function wornArmour() {
  return WornSlots.filter((slot) => bot.inventory.slots[bot.getEquipmentDestSlot(slot)]);
}

// What she wears that « donne-moi ton armure » and « enlève ton armure » take off: her armour, and a shield in her
// off-hand (left on, it never reached the player).
function wornGear() {
  const offhand = bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')];
  return offhand && offhand.name === 'shield' ? [...wornArmour(), 'off-hand'] : wornArmour();
}

// ---- What she carries, for the service (« t'as quoi sur toi ? », /debug minecraft) --------------------------------

// A stack as the service reads it. Its kinds of goods are the ones give() would hand over for it, so « t'as du fer ? »
// counts exactly what « donne-moi ton fer » would toss.
function stackReport(item) {
  if (!item) return null;
  return { name: item.name, count: item.count, goods: Object.keys(Goods).filter((key) => key !== 'all' && Goods[key](item.name)) };
}

// Sent on spawn and after her pockets change, at most every InventoryEvery: a gathering run moves a slot per block.
const InventoryEvery = 2000;
let inventoryTimer = null;

function reportInventory() {
  inventoryTimer = null;
  if (!bot) return;
  const slots = bot.inventory.slots;
  emit('inventory', {
    worn: WornSlots.map((slot) => stackReport(slots[bot.getEquipmentDestSlot(slot)])).filter(Boolean),
    held: stackReport(bot.heldItem),
    offhand: stackReport(slots[bot.getEquipmentDestSlot('off-hand')]),
    pockets: bot.inventory.items().map(stackReport),
  });
}

function scheduleInventory() {
  if (!inventoryTimer) inventoryTimer = setTimeout(safely(reportInventory), InventoryEvery);
}

// « syncs vide tes poches », « donne-moi ton poisson »: she walks up to the player and tosses it at their feet.
// A farm, a fishing spot or a guarded place she was keeping, she goes back to after.
async function give(player, key) {
  const matches = Goods[key];
  if (!matches) return;
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  const worn = key === 'armour' ? wornGear() : [];
  if (worn.length === 0 && !bot.inventory.items().some((i) => matches(i.name)))
    return emit('cantGive', { player, goods: key, why: 'none' });
  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'give', player };
  task = mine;
  let count = 0;
  let delivered = false;
  try {
    if (target.position.distanceTo(bot.entity.position) > 3)
      await bot.pathfinder.goto(new goals.GoalNear(target.position.x, target.position.y, target.position.z, 2));
    const owner = playerEntity(player);
    if (task === mine && owner) {
      for (const slot of worn) await bot.unequip(slot);
      await bot.lookAt(owner.position.offset(0, 1.6, 0), true);
      for (const item of bot.inventory.items().filter((i) => matches(i.name))) {
        await bot.tossStack(item);
        count += item.count;
      }
      delivered = true;
    }
  } catch (err) {
    log('giving failed:', err && err.message ? err.message : String(err));
  }
  handedAt = Date.now(); // her tools may be gone now: not broken
  if (task !== mine) return;
  task = null;
  emit('given', { player, goods: key, count, delivered });
  resumeActivity(back);
}

// What she keeps when she puts her things away: the best weapon, pickaxe, axe and shovel she carries, a fishing rod,
// shears, and her bow with its arrows, the tools her other orders need. By slot.
const ToolTiers = ['wooden', 'stone', 'golden', 'iron', 'diamond', 'netherite'];

// Her kit, by tool (the names `wornOut` and `broke` report).
function kitTools(items) {
  const best = (suffix) => items.filter((i) => i.name.endsWith(suffix) && !(suffix === '_axe' && i.name.endsWith('_pickaxe')))
    .sort((a, b) => ToolTiers.indexOf(b.name.split('_')[0]) - ToolTiers.indexOf(a.name.split('_')[0]))[0];
  return {
    sword: best('_sword'), pickaxe: best('_pickaxe'), axe: best('_axe'), shovel: best('_shovel'),
    rod: items.find((i) => i.name === 'fishing_rod'), shears: items.find((i) => i.name === 'shears'),
    bow: items.find((i) => i.name === 'bow'),
  };
}

function kitSlots(items) {
  const kept = new Set(Object.values(kitTools(items)).filter(Boolean).map((i) => i.slot));
  if (items.some((i) => i.name === 'bow'))
    for (const arrow of items.filter((i) => i.name.endsWith('arrow'))) kept.add(arrow.slot);
  return kept;
}

const StoreRadius = 16;
const ContainerBlocks = ['chest', 'barrel', 'trapped_chest'];

// « syncs range tes affaires »: everything but her kit, into the closest chest or barrel within StoreRadius.
async function store(player) {
  const ids = bot.registry.blocksArray.filter((b) => ContainerBlocks.includes(b.name)).map((b) => b.id);
  const at = bot.findBlocks({ matching: ids, maxDistance: StoreRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position))[0];
  if (!at) return emit('cantStore', { player, why: 'noChest' });
  const items = bot.inventory.items();
  const kit = kitSlots(items);
  const totals = new Map();
  for (const item of items.filter((i) => !kit.has(i.slot))) totals.set(item.type, (totals.get(item.type) || 0) + item.count);
  if (totals.size === 0) return emit('cantStore', { player, why: 'empty' });
  const back = pausedActivity(task);
  stay();
  const mine = { kind: 'store', player };
  task = mine;
  let count = 0;
  let full = false;
  try {
    await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
    if (task === mine) {
      const box = await bot.openContainer(bot.blockAt(at));
      try {
        for (const [type, n] of totals) {
          try {
            await box.deposit(type, null, n);
            count += n;
          } catch {
            full = true;
            break;
          }
        }
      } finally {
        box.close();
      }
    }
  } catch (err) {
    log('storing failed:', err && err.message ? err.message : String(err));
  }
  handedAt = Date.now();
  if (task !== mine) return;
  task = null;
  emit('stored', { player, count, full });
  resumeActivity(back);
}

// ---- Fishing -------------------------------------------------------------------------------------------

// « syncs va pêcher »: she walks to open water within FishRadius and fishes until told otherwise, keeping the catch
// (« donne-moi ton poisson », « range tes affaires »). She needs a rod; a rod that breaks ends it.
const FishRadius = 16;
const FishCatch = new Set(['cod', 'salmon', 'tropical_fish', 'pufferfish']);
const FishTreasure = new Set(['bow', 'enchanted_book', 'fishing_rod', 'name_tag', 'nautilus_shell', 'saddle']);
// No bite in this long (the float landed on the bank, say): reel in and cast again.
const BiteWait = 45000;

function fishingRod() {
  return bot.inventory.items().find((i) => i.name === 'fishing_rod');
}

// Water with air above it: somewhere a float can land. The closest.
function openWater() {
  const water = bot.registry.blocksByName.water;
  if (!water) return null;
  return bot.findBlocks({ matching: water.id, maxDistance: FishRadius, count: 64 })
    .filter((p) => {
      const above = bot.blockAt(p.offset(0, 1, 0));
      return above && above.name === 'air';
    })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position))[0] || null;
}

function fish(player) {
  if (!fishingRod()) return emit('cantFish', { player, why: 'noRod' });
  const water = openWater();
  if (!water) return emit('cantFish', { player, why: 'noWater' });
  stay();
  task = { kind: 'fish', player, water, casting: false };
  emit('fishing', { player });
  fishLoop(task);
}

// Fishing for her own supper (mine.forage) or on her own (mine.quiet, the routine) is quiet: no catch reported, and a
// broken rod just ends it. Either stops at mine.until; her supper, also once she has something to eat.
async function fishLoop(mine) {
  try {
    await bot.pathfinder.goto(new goals.GoalNear(mine.water.x, mine.water.y + 1, mine.water.z, 3));
  } catch (err) {
    log('fishing walk:', err && err.message ? err.message : String(err));
  }
  while (task === mine && !(mine.forage && hasFood()) && !(mine.until && Date.now() > mine.until)) {
    const rod = fishingRod();
    if (!rod) {
      if (mine.forage) return;
      task = null;
      if (mine.quiet) return;
      return emit('cantFish', { player: mine.player, why: 'broke' });
    }
    while (eating && task === mine) await wait(500);
    if (task !== mine) return;
    try {
      if (!bot.heldItem || bot.heldItem.name !== 'fishing_rod') await bot.equip(rod, 'hand');
      await bot.lookAt(new Vec3(mine.water.x + 0.5, mine.water.y + 0.9, mine.water.z + 0.5), true);
      const before = itemCounts();
      mine.casting = true;
      const cast = bot.fish();
      cast.catch(() => {});
      await Promise.race([cast, wait(BiteWait).then(() => { throw new Error('no bite'); })]);
      mine.casting = false;
      await wait(1500); // the catch flies to her
      if (task !== mine) return;
      const gained = [...itemCounts()].find(([type, n]) => n > (before.get(type) || 0));
      const caught = gained ? bot.registry.items[gained[0]] : null;
      if (caught && !mine.forage && !mine.quiet) {
        const kind = FishCatch.has(caught.name) ? 'fish' : FishTreasure.has(caught.name) ? 'treasure' : 'junk';
        emit('caught', { player: mine.player, kind });
      }
    } catch (err) {
      if (task !== mine) return;
      // No bite in time: the float is still out, so reel it in. Any other end (eating swapped the rod out, the
      // float went) already took it back, and a click now would cast again.
      if (err && err.message === 'no bite') reelIn(mine);
      else mine.casting = false;
      log('fishing:', err && err.message ? err.message : String(err));
      await wait(2000);
    }
  }
}

// Pulls the float back in (the same click as casting), which ends the cast under way.
function reelIn(mine) {
  if (!mine.casting) return;
  mine.casting = false;
  try { bot.activateItem(); } catch { /* nothing in hand to reel */ }
}

// ---- Cooking -----------------------------------------------------------------------------------------

// « syncs cuis la viande », « fais fondre le fer »: what she carries of it goes in a free furnace nearby, with her own
// fuel; she waits by it and brings the result back. A furnace someone else is using (anything in its input or output,
// or another fuel) is left alone. She never places one.
const SmeltInputs = {
  food: (n) => ['beef', 'porkchop', 'chicken', 'mutton', 'rabbit', 'cod', 'salmon', 'potato', 'kelp'].includes(n),
  ore: (n) => ['raw_iron', 'raw_gold', 'raw_copper', 'iron_ore', 'deepslate_iron_ore', 'gold_ore', 'deepslate_gold_ore',
    'copper_ore', 'deepslate_copper_ore'].includes(n),
  sand: (n) => n === 'sand' || n === 'red_sand',
};
// Which blocks cook what: smokers only food, blast furnaces only ore, both twice as fast as a furnace.
const SmeltBlocks = { food: ['furnace', 'smoker'], ore: ['furnace', 'blast_furnace'], sand: ['furnace'] };
const SmeltRadius = 16;

// How many items one of these burns for.
function fuelValue(name) {
  if (name === 'coal' || name === 'charcoal') return 8;
  if (name === 'coal_block') return 80;
  if (name === 'blaze_rod') return 12;
  if (name === 'dried_kelp_block') return 20;
  if (name === 'lava_bucket') return 100;
  if (name.endsWith('_log') || name.endsWith('_planks') || name.endsWith('_wood') || name.endsWith('_stem')) return 1.5;
  return 0;
}

async function smelt(player, key) {
  const matches = SmeltInputs[key];
  if (!matches) return;
  const items = bot.inventory.items();
  const inputs = new Map();
  for (const item of items.filter((i) => matches(i.name))) inputs.set(item.type, (inputs.get(item.type) || 0) + item.count);
  if (inputs.size === 0) return emit('cantSmelt', { player, what: key, why: 'nothing' });
  const fuels = new Map();
  for (const item of items.filter((i) => fuelValue(i.name) > 0 && !matches(i.name)))
    fuels.set(item.type, { name: item.name, count: (fuels.get(item.type)?.count || 0) + item.count });
  const capacity = [...fuels.values()].reduce((sum, f) => sum + fuelValue(f.name) * f.count, 0);
  if (capacity < 1) return emit('cantSmelt', { player, what: key, why: 'noFuel' });
  const ids = bot.registry.blocksArray.filter((b) => SmeltBlocks[key].includes(b.name)).map((b) => b.id);
  const furnaces = bot.findBlocks({ matching: ids, maxDistance: SmeltRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  if (furnaces.length === 0) return emit('cantSmelt', { player, what: key, why: 'noFurnace' });

  stay();
  const mine = { kind: 'smelt', player, window: null };
  task = mine;
  const before = itemCounts();
  for (const at of furnaces) {
    let box;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
      if (task !== mine) return;
      box = await bot.openFurnace(bot.blockAt(at));
    } catch (err) {
      if (task !== mine) return;
      log('furnace refused:', err && err.message ? err.message : String(err));
      continue;
    }
    const burning = box.fuelItem();
    if (box.inputItem() || box.outputItem() || (burning && !fuels.has(burning.type))) {
      box.close();
      continue;
    }
    mine.window = box;
    const planned = Math.min(Math.floor(capacity), [...inputs.values()].reduce((a, b) => a + b, 0));
    emit('smelting', { player, what: key, count: planned });
    // A furnace takes 10 s an item (half that in a smoker or blast furnace): past twice that, something is wrong.
    const deadline = Date.now() + planned * 20000 + 60000;
    let left = planned;
    let starved = false;
    try {
      for (const [type, total] of inputs) {
        let ofType = Math.min(total, left);
        while (ofType > 0 && !starved && task === mine) {
          const batch = Math.min(ofType, 64);
          await box.putInput(type, null, batch);
          ofType -= batch;
          left -= batch;
          // Until the batch is through: fuel added when it runs out, the result taken as it comes.
          while (task === mine && (box.inputItem() || box.outputItem()) && Date.now() < deadline) {
            if (box.outputItem()) await box.takeOutput();
            if (box.inputItem() && !box.fuelItem() && !(box.fuel > 0)) {
              const next = [...fuels].find(([t, f]) => f.count > 0 && bot.inventory.count(t) > 0);
              if (!next) {
                starved = true;
                break;
              }
              const [fuelType, fuel] = next;
              const needed = Math.min(fuel.count, Math.ceil(box.inputItem().count / fuelValue(fuel.name)), 64);
              await box.putFuel(fuelType, null, needed);
              fuel.count -= needed;
            }
            await wait(1000);
          }
        }
        if (task !== mine || starved || left <= 0) break;
      }
    } catch (err) {
      log('smelting stopped:', err && err.message ? err.message : String(err));
    }
    if (task !== mine) return;
    // Out of fuel halfway, or the deadline passed: what's still in the input comes back to her.
    try {
      if (box.inputItem()) await box.takeInput();
    } catch { /* leave it */ }
    box.close();
    mine.window = null;
    // The inputs and the fuel only went down, so what she gained is what came out of the furnace.
    const { total, delivered } = await deliverGains(player, before);
    if (task === mine) task = null;
    return emit('smelted', { player, what: key, count: total, delivered });
  }
  if (task === mine) task = null;
  emit('cantSmelt', { player, what: key, why: 'busy' });
}

// ---- Feeding herself ---------------------------------------------------------------------------------

// Hungry with nothing on her, and free (or at an activity, which this pauses and goes back to): before asking
// anyone she looks for food herself, and says nothing about it (the owner's call). Sometimes she picks a few ripe
// crops nearby (replanted, like any harvest), sometimes she fishes, whichever is around, then the other if the
// first gave nothing. With time to spare (not starving) and a free furnace or smoker close, she cooks it first.
// Nothing found: she asks, as before, and doesn't look again for a while.
const EdibleCrops = [Crops.carrot, Crops.potato, Crops.beetroot, Crops.melon];
const ForagePlants = 3;
const ForageFishTime = 120000;
const ForageRetry = 300000;
// Her food bar: at this or below she eats what she has, raw, rather than wait by a furnace.
const CookAbove = 6;
const CookMax = 8;
let forageAt = 0;

// True when she sets off (or already has): reportHunger then doesn't ask.
function startForage() {
  if (task && task.kind === 'forage') return true;
  if (eating || bot.isSleeping || Date.now() < forageAt) return false;
  if (task && !isFree(task) && !isActivity(task)) return false; // someone's order
  if (task && (task.kind === 'fish' || task.kind === 'retrieve')) return false; // fishing feeds her; her things won't wait
  const sources = [];
  if (findRipe(EdibleCrops, bot.entity.position, HarvestRadius).length > 0) sources.push('crops');
  if (fishingRod() && openWater()) sources.push('fish');
  if (sources.length === 0) return false;
  if (Math.random() < 0.5) sources.reverse();
  forage(sources).catch((err) => log('forage failed:', err && err.message ? err.message : String(err)));
  return true;
}

async function forage(sources) {
  const resume = isActivity(task) ? task : null; // her own trip or tagging along just ends: she picks again after
  stay();
  waitingForFood = false; // what she picks up now isn't someone feeding her
  const mine = { kind: 'forage', player: '', resume, forage: true, casting: false, window: null };
  task = mine;
  for (const source of sources) {
    if (source === 'crops') {
      const here = bot.entity.position;
      const found = findRipe(EdibleCrops, here, HarvestRadius)
        .sort((a, b) => a.block.position.distanceTo(here) - b.block.position.distanceTo(here))
        .slice(0, ForagePlants);
      if (found.length > 0) await harvestBlocks(found, mine);
    } else {
      mine.water = openWater();
      mine.until = Date.now() + ForageFishTime;
      if (mine.water && fishingRod()) await fishLoop(mine);
    }
    if (task !== mine) return; // an order came: it's hers now
    if (hasFood()) break;
  }
  if (hasFood() && bot.food > CookAbove) await cookForHerself(mine);
  if (task !== mine) return;
  task = null;
  if (!hasFood()) forageAt = Date.now() + ForageRetry;
  resumeActivity(resume); // before eating: free for a moment, bedtime would take her and the activity would be lost
  await eatIfHungry();
  reportHunger(); // still nothing: now she asks
}

// The raw food she carries (meat, fish, potatoes), up to CookMax of one kind, into the closest free furnace or smoker
// with her own fuel. Free means empty: nothing in it at all. She waits, and takes back what's left if it runs long.
async function cookForHerself(mine) {
  const items = bot.inventory.items();
  const raw = items.find((i) => SmeltInputs.food(i.name) && bot.registry.foodsByName[i.name]);
  const fuel = items.filter((i) => fuelValue(i.name) > 0 && !SmeltInputs.food(i.name))
    .sort((a, b) => fuelValue(b.name) - fuelValue(a.name))[0];
  if (!raw || !fuel) return;
  const count = Math.min(raw.count, CookMax, Math.floor(fuelValue(fuel.name) * fuel.count));
  if (count < 1) return;
  const ids = bot.registry.blocksArray.filter((b) => SmeltBlocks.food.includes(b.name)).map((b) => b.id);
  const furnaces = bot.findBlocks({ matching: ids, maxDistance: SmeltRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  for (const at of furnaces) {
    let box;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
      if (task !== mine) return;
      box = await bot.openFurnace(bot.blockAt(at));
    } catch (err) {
      if (task !== mine) return;
      log('furnace refused:', err && err.message ? err.message : String(err));
      continue;
    }
    if (box.inputItem() || box.outputItem() || box.fuelItem()) {
      box.close();
      continue;
    }
    mine.window = box;
    try {
      await box.putInput(raw.type, null, count);
      await box.putFuel(fuel.type, null, Math.ceil(count / fuelValue(fuel.name)));
      const deadline = Date.now() + count * 12000 + 15000;
      while (task === mine && box.inputItem() && Date.now() < deadline) {
        if (box.outputItem()) await box.takeOutput();
        await wait(1000);
      }
      if (task === mine) {
        if (box.outputItem()) await box.takeOutput();
        if (box.inputItem()) await box.takeInput();
        if (box.fuelItem()) await box.takeFuel(); // an empty bucket, or fuel left over
      }
    } catch (err) {
      log('cooking stopped:', err && err.message ? err.message : String(err));
    }
    if (task !== mine) return; // stay() closed the window
    box.close();
    mine.window = null;
    return;
  }
}

// ---- Sleeping ----------------------------------------------------------------------------------------

// The night only passes when everyone on the server sleeps (by default), her included: when a player
// goes to bed she goes too, once per night, or says why she can't.
let sleptOnDay = null;
// An activity she was at when she went to bed (a farm, fishing, a guarded place): she goes back to it when she
// wakes. Any order clears it (stay()).
let resumeAfterSleep = null;

// The real night, when a bed works without a storm.
function isNightTime() {
  const t = bot.time.timeOfDay;
  return t >= 12542 && t <= 23459;
}

function isNight() {
  return isNightTime() || bot.thunderState > 0;
}

// A bed anywhere else explodes.
function inOverworld() {
  return !/nether|end/.test(dimension());
}

// Asked, or because a player went to bed (player set), or on her own (auto: player empty, and every failure is
// silent, since nobody is waiting for an answer).
async function sleep(player, auto = false) {
  if (bot.isSleeping) return;
  if (!isNight()) return auto ? undefined : emit('cantSleep', { player, why: 'day' });
  if (!auto) sleptOnDay = bot.time.day;
  const bedIds = bot.registry.blocksArray.filter((b) => b.name.endsWith('_bed')).map((b) => b.id);
  const beds = !inOverworld() ? [] : bot.findBlocks({ matching: bedIds, maxDistance: GatherRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  if (beds.length === 0) {
    if (!auto) return emit('cantSleep', { player, why: 'noBed' });
    sleptOnDay = bot.time.day; // no bed around: not worth looking again tonight
    return;
  }
  const kept = pausedActivity(task) || resumeAfterSleep;
  stay();
  resumeAfterSleep = kept;
  const mine = { kind: 'sleep', player };
  task = mine;
  for (const at of beds) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 1));
      await bot.sleep(bot.blockAt(at));
      if (task === mine) task = null;
      sleptOnDay = bot.time.day;
      return emit('sleeping', { player, auto });
    } catch (err) {
      log('bed refused:', err && err.message ? err.message : String(err)); // occupied, too far, monsters near
      if (task !== mine) return;
    }
  }
  task = null;
  wakeUp();
  if (!auto) emit('cantSleep', { player, why: 'failed' });
}

// She goes to bed by herself at night, so the night can pass as soon as the players lie down, but only when
// nobody needs her: free, or at an activity she goes back to when she wakes. Not after being told to stay or sit
// tonight, not after an order woke her (sleptOnDay), never in a storm by day. A bed that refused her (monsters
// near, occupied) is tried again two minutes later (a night lasts about seven); no bed at all, and she gives up
// for the night.
const AutoSleepRetry = 120000;
let autoSleepAt = 0;
let stayedOnDay = null;

function autoSleepTick() {
  if (bot.isSleeping || sitting || !isNightTime() || !inOverworld()) return;
  if (sleptOnDay === bot.time.day || stayedOnDay === bot.time.day) return;
  if (task && !isActivity(task) && !isFree(task)) return;
  if (task && task.kind === 'retrieve') return; // her things despawn: bed can wait
  if (Date.now() < autoSleepAt) return;
  autoSleepAt = Date.now() + AutoSleepRetry;
  sleep('', true).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
}

// Back to what she was at, after a night's sleep or a bed that refused her.
function wakeUp() {
  const activity = resumeAfterSleep;
  resumeAfterSleep = null;
  resumeActivity(activity);
}

// ---- Running away ------------------------------------------------------------------------------------

// C# sends `flee` when a player keeps hitting her (who counts, and that Papa never does, is decided there).
// She sprints away until she's FleeDistance blocks from them or FleeTime has passed, then says she's safe.
// An activity (farm, fishing, a guarded place) or a follow or guard is picked up again afterwards; anything
// else is dropped and its player told why (`dropped`). She still never digs or builds to get away: with no
// way out she's `cornered`, once per flight.
const FleeDistance = 20;
const FleeTime = 15000;
const FollowsSomeone = new Set(['follow', 'guard']);

function flee(player) {
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
  task = { kind: 'flee', player, resume, until: Date.now() + FleeTime, reported: false };
  bot.pathfinder.setGoal(new goals.GoalInvert(new goals.GoalFollow(attacker, FleeDistance)), true);
  if (previous && !resume && previous.player && previous.kind !== 'seek' && previous.kind !== 'sleep')
    emit('dropped', { player: previous.player, kind: previous.kind, by: player });
  emit('fleeing', { player });
}

// Every physics tick while running: far enough, out of sight or out of time, and she stops.
function fleeTick() {
  if (!task || task.kind !== 'flee') return;
  const attacker = playerEntity(task.player);
  if (attacker && attacker.position.distanceTo(bot.entity.position) < FleeDistance && Date.now() < task.until) return;
  const done = task;
  task = null;
  bot.pathfinder.setGoal(null);
  emit('safe', { player: done.player });
  resumeAfterBreak(done.resume);
}

// What running away or defending someone goes back to: an activity, a follow or guard, or an order she can start again
// (Restartable: gathering, harvesting, a walk somewhere). Never her own: autonomy picks again by itself once she's free.
function resumeFor(previous) {
  if (previous && previous.kind === 'back') return { again: previous.again }; // on her way back to one already
  if (previous && previous.kind === 'fight') return previous.resume; // fighting for herself: that fight's way back
  if (previous && !previous.own && previous.command && Restartable.has(previous.kind)) {
    // A camp goes on where it stands, with what's on it and the same floor.
    const command = previous.kind === 'camp'
      ? { ...previous.command, site: previous.site, placed: previous.placed, floor: previous.floor, heights: previous.heights } : previous.command;
    return { restart: command, at: bot.entity.position.clone(), before: previous.before || null };
  }
  const kept = pausedActivity(previous) || (previous && FollowsSomeone.has(previous.kind) ? previous : null);
  return kept && !kept.own ? kept : null;
}

// After running away or defending someone: back to the activity, to the player she was following or guarding if she
// still sees them, or to the order she was given.
function resumeAfterBreak(resume) {
  if (!resume) return;
  if (resume.again) return resume.again();
  if (resume.restart) return restartOrder(resume);
  if (isActivity(resume)) return resumeActivity(resume);
  const target = playerEntity(resume.player);
  if (!target) return emit('cantReach', { player: resume.player, why: 'lost' });
  task = { ...resume, foe: null, mode: null, modeTarget: null };
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
}

// The orders a fight (or running away) can interrupt and start again, by the command that started them. Gathering and
// harvesting look around where she stands, so she first walks back to where she stopped (BackFor at most), then the
// same command runs again from the same pockets (`since`): what she had already gathered still counts.
const Restartable = new Set(['collect', 'harvest', 'come', 'goto', 'guide', 'camp', 'uncamp']);
const BackFor = 20000;

function restartOrder(r) {
  const from = r.at;
  const again = () => dispatch({ ...r.restart, since: r.before });
  if (!from || bot.entity.position.distanceTo(from) <= 3 || !['collect', 'harvest'].includes(r.restart.op)) return again();
  task = { kind: 'back', player: r.restart.player, again, until: Date.now() + BackFor };
  bot.pathfinder.setGoal(new goals.GoalNear(from.x, from.y, from.z, 2));
}

// Every physics tick on the way back: there (or out of time, or no way), and the order starts again.
function backTick() {
  const t = task;
  if (!t || t.kind !== 'back') return;
  if (bot.pathfinder.isMoving() && Date.now() < t.until) return;
  task = null;
  bot.pathfinder.setGoal(null);
  t.again();
}

// ---- The camp -----------------------------------------------------------------------------------------

// « monte le camp » (Papa or Tata: C# checks): an open camp round a campfire, as CampLayout draws it: the fire in a pit
// of four trapdoors, two benches (a log on its side and two slabs) on two of its sides, the workstation (table, furnace,
// chest) on the third, a bed in a back corner, four torches on fence posts, and a floor of path and plain dirt trodden
// round it all. Nothing square on purpose (the owner's design, docs/superpowers/specs/2026-10-22-camp-design.md). All of
// it crafted, one craft at a time, from what she carries; what's missing she gathers: wood, stone (with a wooden pickaxe
// she makes), coal (else charcoal from logs, in the camp's own furnace). Wool only from shearing, never a sheep killed.
// Only on her own cells, never over anything; « démonte le camp » breaks only what she placed, still what she placed:
// the list goes to C#, which keeps it across restarts and sends it back. The floor is never in it, and stays. She keeps
// the tools she made (a wooden axe, so taking the camp down doesn't take minutes; a wooden shovel, for the path).
const CampSearch = 12; // how far from her a site may be
const CampTrapdoors = 4;
const CampSlabs = 4;
const CampBenchLogs = 2;
const CampFences = 4;
const CampTorches = 4;

// The camp from above, the back to the north: x east, z south from its north-west corner at her feet' level. F the
// campfire, d a trapdoor of its pit, L a bench's log, S its slabs, T the crafting table, K the furnace, C the chest, B
// the bed (its head to the back), l a torch on a fence post, p and D the floor (path or plain dirt, drawn anew for each
// camp: campFloor), . left as it is, not even checked: the camp sits into uneven ground.
const CampLayout = [
  '.l.......',
  '...LSS.Bl',
  '..pDpp.B.',
  '.LpDdppD.',
  '.SpdFdpT.',
  '.SDpdpDK.',
  '..pDppDC.',
  'l..pDp...',
  '....p..l.',
];

function campCells(marks) {
  const cells = [];
  CampLayout.forEach((row, z) => [...row].forEach((m, x) => { if (marks.includes(m)) cells.push([x, z]); }));
  return cells;
}

const At = (o, dx, dy, dz) => o.offset(dx, dy, dz);
const CampFire = [4, 0, 4];
const CampTable = [7, 0, 4];
const CampFurnace = [7, 0, 5];
const CampChest = [7, 0, 6];
const CampBed = [7, 0, 2]; // the foot: a bed is laid towards where she looks, so from CampBedStand its head is at (7, 1)
const CampBedStand = [7, 0, 3];
const CampWork = [6, 0, 5]; // by the workstation, where she crafts from; each of its blocks is set from its west, to face the fire
const CampWays = ['4,6', '4,7', '4,8']; // from the fire to the way in: always path
const CampPathShare = 0.6; // of the other floor cells
// What a full camp places, the floor and the bed aside (a torch post is two blocks).
const CampBlocks = campCells('FdLSTKC').length + 2 * campCells('l').length;

// What the game lets a block be placed over (its own « replaceable »): grass, leaf litter, bushes, a snow layer. Flowers,
// mushrooms and saplings are not, and a site with one is passed over: they may be someone's.
const CampOver = new Set(['air', 'cave_air', 'short_grass', 'tall_grass', 'fern', 'large_fern', 'dead_bush', 'leaf_litter', 'bush',
  'short_dry_grass', 'tall_dry_grass', 'vine', 'snow', 'glow_lichen']);

function campReplaceable(block) {
  return Boolean(block) && CampOver.has(block.name);
}

// Where a cell's ground is, from the site's level: 0, a block up or a block down (that order), with `high` blocks of
// room above it (0: the ground alone); null: none. Firm full ground only.
function campHeight(o, x, z, high) {
  for (const h of [0, 1, -1]) {
    const ground = bot.blockAt(At(o, x, h - 1, z));
    if (!ground || ground.boundingBox !== 'block' || /leaves|farmland|ice|_log$/.test(ground.name)) continue;
    let room = true;
    for (let dy = 0; dy < high && room; dy++) room = campReplaceable(bot.blockAt(At(o, x, h + dy, z)));
    if (room) return h;
  }
  return null;
}

// A site: one level, with room above, under the fire, its pit, the benches, the workstation and the bed; the torch
// posts and the floor follow the ground, a block up or down (campHeights), and the floor has no deeper drop: she stood
// in a two-deep hole in it to reach something, and never got out. The cells left alone aren't looked at. All of it at
// one level, a 9x9 with its corner posts, was rare outside a flat world: at a forest's edge, none in reach.
function campFits(o, standing = []) {
  if (standing.some(([x, z]) => /[FdLSTKCBl]/.test((CampLayout[z - o.z] || '')[x - o.x] || ''))) return false;
  if (!campCells('FdLSTKCB').every(([x, z]) => campHeight(o, x, z, 2) === 0)) return false;
  if (!campCells('l').every(([x, z]) => campHeight(o, x, z, 3) !== null)) return false;
  return campCells('pD').every(([x, z]) => campHeight(o, x, z, 0) !== null);
}

// Each post's and floor cell's ground, by "x,z", as found when the site is chosen (kept on the task: a fence set down
// would read as ground a block up). A floor cell with none (a flower on it, a drop) is left as it is: null.
function campHeights(o) {
  const heights = {};
  for (const [x, z] of campCells('l')) heights[`${x},${z}`] = campHeight(o, x, z, 3);
  for (const [x, z] of campCells('pD')) heights[`${x},${z}`] = campHeight(o, x, z, 1);
  return heights;
}

// The closest site within CampSearch, its fire nearest her, with nobody standing where a block goes: centred on her,
// its fire came under the feet of the one who asked, standing beside her, and the server refused it (a block isn't set
// in a player); null when there's none.
function campSite() {
  const me = bot.entity.position.floored();
  const standing = Object.values(bot.players).map((p) => p.entity).filter((e) => e && e !== bot.entity)
    .map((e) => [Math.floor(e.position.x), Math.floor(e.position.z)]);
  const sites = [];
  for (let dx = -CampSearch; dx <= CampSearch; dx++)
    for (let dz = -CampSearch; dz <= CampSearch; dz++)
      for (const dy of [0, -1, 1, -2, 2]) sites.push(me.offset(dx - CampFire[0], dy, dz - CampFire[2]));
  sites.sort((a, b) => At(a, ...CampFire).distanceTo(me) - At(b, ...CampFire).distanceTo(me));
  return sites.find((o) => campFits(o, standing)) || null;
}

// The floor's pattern: which cells are path and which plain dirt, about CampPathShare path, the way in always. Drawn
// once per camp and kept on the task, so a camp taken up again goes on with the same one.
function campFloor() {
  const cells = campCells('pD');
  const free = cells.filter(([x, z]) => !CampWays.includes(`${x},${z}`));
  for (let i = free.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [free[i], free[j]] = [free[j], free[i]];
  }
  const paths = new Set(free.slice(0, Math.round(free.length * CampPathShare)).map(([x, z]) => `${x},${z}`));
  return cells.map(([x, z]) => [x, z, CampWays.includes(`${x},${z}`) || paths.has(`${x},${z}`) ? 'path' : 'dirt']);
}

const held = (match) => bot.inventory.items().filter((i) => match(i.name)).reduce((n, i) => n + i.count, 0);
const isPlanks = (n) => n.endsWith('_planks');
const isLog = (n) => n.endsWith('_log') && !n.startsWith('stripped_');
const isCoal = (n) => n === 'coal' || n === 'charcoal';
const hasItem = (match) => held(match) > 0;

// The colour she has three wool of, if any.
function woolColour() {
  const by = new Map();
  for (const i of bot.inventory.items()) if (i.name.endsWith('_wool')) by.set(i.name, (by.get(i.name) || 0) + i.count);
  const best = [...by].sort((a, b) => b[1] - a[1])[0];
  return best && best[1] >= 3 ? best[0].replace(/_wool$/, '') : null;
}

const campSheep = () => bot.nearestEntity((e) => e.name === 'sheep' && !isNamed(e) && !isShorn(e)
  && e.position.distanceTo(bot.entity.position) < 32);

// The bed she can have, best first: from wool she has, from wool sheared on the spot, a straw bed from hay or wheat she
// carries (never fetched: the wheat near her is someone's field); null: none, and the camp goes up without one (a
// camp is still a place to craft, cook and warm up). A straw bed is 3 hay bales (27 wheat) for 4 beds; her 26.1 client
// has no straw bed and sees it, in hand and set down, as a yellow bed (ViaBackwards), so that's the name it's crafted
// and placed by, through her recipe book only (minecraft-data doesn't know the recipe).
const StrawHay = 3;

function campBed() {
  if (woolColour()) return 'wool';
  if (hasItem((n) => n === 'shears') && campSheep()) return 'shear';
  if (held((n) => n === 'hay_block') * 9 + held((n) => n === 'wheat') >= StrawHay * 9) return 'straw';
  return null;
}

// Wooden things of any wood (an iron trapdoor or a nether brick fence is no camp's).
const WoodKind = /^(oak|spruce|birch|jungle|acacia|dark_oak|mangrove|cherry|pale_oak|bamboo|crimson|warped)_/;
const isWooden = (suffix) => (n) => n.endsWith(suffix) && WoodKind.test(n);

// What the camp still needs, from what she carries: the items to craft (a count where there are several) and the raw
// materials short. `placed`: what's already set down (a camp taken up again) counts as done.
function campNeeds(placed = [], bed = campBed()) {
  const on = (match) => placed.filter((b) => match(b.name)).length;
  const has = (match) => hasItem(match) || on(match) > 0;
  const left = (match, total) => Math.max(0, total - held(match) - on(match));
  const need = {
    table: !has((n) => n === 'crafting_table'),
    chest: !has((n) => n === 'chest'),
    furnace: !has((n) => n === 'furnace'),
    campfire: !has((n) => n === 'campfire'),
    trapdoors: left(isWooden('_trapdoor'), CampTrapdoors),
    slabs: left(isWooden('_slab'), CampSlabs),
    fences: left(isWooden('_fence'), CampFences),
    torches: left((n) => n === 'torch', CampTorches),
    bed: bed !== null && !has((n) => n.endsWith('_bed')),
    pickaxe: !hasItem((n) => n.endsWith('_pickaxe')),
    axe: !hasItem((n) => n.endsWith('_axe') && !n.endsWith('_pickaxe')),
    shovel: !hasItem((n) => n.endsWith('_shovel')),
  };
  // Crafts: 6 planks make 2 trapdoors, 3 make 6 slabs, 4 and 2 sticks make 3 fences, a coal and a stick 4 torches.
  const trapCrafts = Math.ceil(need.trapdoors / 2);
  const slabCrafts = Math.ceil(need.slabs / 6);
  const fenceCrafts = Math.ceil(need.fences / 3);
  const torchCrafts = Math.ceil(need.torches / 4);
  const woolBed = need.bed && bed !== 'straw';
  const sticks = fenceCrafts * 2 + torchCrafts + (need.campfire ? 3 : 0) + (need.pickaxe ? 2 : 0) + (need.axe ? 2 : 0) + (need.shovel ? 2 : 0);
  const stickCrafts = Math.ceil(Math.max(0, sticks - held((n) => n === 'stick')) / 4);
  const planks = (need.table ? 4 : 0) + (need.chest ? 8 : 0) + trapCrafts * 6 + slabCrafts * 3 + fenceCrafts * 4 + (woolBed ? 3 : 0)
    + (need.pickaxe ? 3 : 0) + (need.axe ? 3 : 0) + (need.shovel ? 1 : 0) + stickCrafts * 2;
  const coal = torchCrafts + (need.campfire ? 1 : 0);
  const coalShort = Math.max(0, coal - held(isCoal));
  const benchLogs = Math.max(0, CampBenchLogs - on(isLog));
  const logs = Math.ceil(Math.max(0, planks - held(isPlanks)) / 4) + benchLogs + (need.campfire ? 3 : 0) + coalShort; // charcoal at worst
  return {
    need,
    bed,
    planks,
    benchLogs,
    logs: Math.max(0, logs - held(isLog)),
    cobble: need.furnace ? Math.max(0, 8 - held((n) => n === 'cobblestone')) : 0,
    coalShort,
  };
}

// One craft, checked: the item must show up in her pockets. Through her recipe book when the recipe is in it
// (bookCraft): the server fills the grid itself and one shift-click takes the result. Mineflayer's bot.craft places
// every ingredient by clicks the server checks one by one, and on 26.1 it failed again and again (the first table craft
// of a mixed recipe always: no bed, no campfire, no pickaxe, so the furnace was dug by hand and lost; with a count it
// made one and put her inventory out of step with the server's), whatever the delays. It stays the way for a recipe
// not in her book yet. Each craft waits for its item, then CraftGap; one that made nothing is tried again once her
// inventory has settled (CraftSettle).
const CraftTries = 3;
const CraftGap = 300;
const CraftSettle = 1500;

// Her recipe book: the display ids of the crafting recipes she knows, by the item they make. The server sends them at
// login and as recipes unlock (as she gets their ingredients), so this listens from the start (main).
const recipeBook = new Map();

function watchRecipes(client) {
  client.on('recipe_book_add', (packet) => {
    if (packet.replaceAll) recipeBook.clear();
    for (const entry of packet.entries || []) {
      const display = entry.recipe && entry.recipe.display;
      const id = display && display.data && display.data.result && display.data.result.data && display.data.result.data.itemId;
      if (id == null || !/^crafting/.test(String(display.type))) continue;
      if (!recipeBook.has(id)) recipeBook.set(id, []);
      if (!recipeBook.get(id).includes(entry.recipe.displayId)) recipeBook.get(id).push(entry.recipe.displayId);
    }
  });
  client.on('recipe_book_remove', (packet) => {
    for (const ids of recipeBook.values())
      for (const gone of packet.recipeIds || []) if (ids.includes(gone)) ids.splice(ids.indexOf(gone), 1);
  });
}

async function bookCraft(item, table) {
  const window = table ? await bot.openBlock(table) : bot.inventory;
  try {
    for (const displayId of recipeBook.get(item.id) || []) {
      bot._client.write('craft_recipe_request', { windowId: window.id, recipeId: displayId, makeAll: false });
      for (let i = 0; i < 15 && !(window.slots[0] && window.slots[0].type === item.id); i++) await wait(100);
      if (window.slots[0] && window.slots[0].type === item.id) {
        await bot.clickWindow(0, 0, 1); // the result, shift-clicked into her pockets
        return;
      }
    }
  } finally {
    bot.closeWindow(window); // what's left in the grid goes back to her pockets
  }
}

async function craftOne(name, table) {
  const item = bot.registry.itemsByName[name];
  if (!item) return false;
  for (let attempt = 0; attempt < CraftTries; attempt++) {
    const before = held((n) => n === name);
    // The book first: the server knows its own tags (a campfire from charcoal and acacia logs), minecraft-data's
    // recipe list didn't, and said there was no recipe.
    const inBook = (recipeBook.get(item.id) || []).length > 0;
    const recipe = inBook ? null : bot.recipesFor(item.id, null, 1, table || null)[0];
    if (!inBook && !recipe) {
      log(`camp: no recipe for ${name} with ${bot.inventory.items().map((i) => `${i.name}x${i.count}`).join(' ')}`);
      return false;
    }
    try {
      if (inBook) await bookCraft(item, table);
      else await bot.craft(recipe, 1, table || undefined);
    } catch (err) {
      log(`camp: crafting ${name} failed:`, err && err.message ? err.message : String(err));
    }
    for (let i = 0; i < 20; i++) {
      if (held((n) => n === name) > before) {
        await wait(CraftGap);
        return true;
      }
      await wait(100);
    }
    log(`camp: crafting ${name} made nothing, ${attempt + 1 < CraftTries ? 'again' : 'giving up'}`);
    await wait(CraftSettle);
  }
  return false;
}

// Planks from her logs, the kind she has most of, until she has `want` (keeping `keepLogs` logs).
async function craftPlanks(want, keepLogs, mine) {
  while (task === mine && held(isPlanks) < want) {
    const logs = bot.inventory.items().filter((i) => isLog(i.name)).sort((a, b) => b.count - a.count);
    if (logs.length === 0 || held(isLog) <= keepLogs) return;
    if (!(await craftOne(logs[0].name.replace(/_log$/, '_planks')))) return;
  }
}

// In reach of `pos` to place there, and not standing in it.
function campInReach(pos) {
  const me = bot.entity.position;
  const inIt = Math.floor(me.x) === pos.x && Math.floor(me.z) === pos.z && pos.y - Math.floor(me.y) >= 0 && pos.y - Math.floor(me.y) <= 1;
  return !inIt && me.offset(0, 1.62, 0).distanceTo(pos.offset(0.5, 0.5, 0.5)) <= CampReach;
}

// Places `match` (an item) at `cell` (from the camp's corner): from `stand` when given (the bed is laid the way she
// looks, the workstation turns to face her), else from wherever she can reach it. Against the block under it, or
// `against` (an offset): a log lies along the face it's set against, so a bench's log goes against its slab's side.
// True when the block there is then what was placed (or already was: started again).
async function campPlace(o, mine, cell, match, placed, stand = null, against = null) {
  const pos = At(o, ...cell);
  const now = bot.blockAt(pos);
  if (now && match(now.name)) return true;
  try {
    if (stand) {
      const s = At(o, ...stand);
      if (bot.entity.position.distanceTo(s.offset(0.5, 0, 0.5)) > 0.6) await bot.pathfinder.goto(new goals.GoalBlock(s.x, s.y, s.z));
    } else if (!campInReach(pos)) {
      await bot.pathfinder.goto(new goals.GoalPlaceBlock(pos, bot.world, { range: CampReach - 0.4 }));
    }
  } catch { /* placed from where she is, if in reach */ }
  if (task !== mine) return false;
  const item = bot.inventory.items().find((i) => match(i.name));
  const why = (what) => { log(`camp: nothing placed at ${cell.join(' ')}: ${what}`); return false; };
  if (!item) return why('nothing to place');
  const faces = against ? [against] : [[0, -1, 0], [-1, 0, 0], [1, 0, 0], [0, 0, -1], [0, 0, 1], [0, 1, 0]];
  const ref = faces.map(([dx, dy, dz]) => ({ block: bot.blockAt(pos.offset(dx, dy, dz)), face: new Vec3(-dx, -dy, -dz) }))
    .find((r) => r.block && r.block.boundingBox === 'block' && !/door|bed|chest|furnace|crafting|campfire|trapdoor/.test(r.block.name));
  if (!ref) return why('nothing to place it against');
  let failed = '';
  // Twice: just after a fight the sword or the bow can still be in her hand when the block is placed, and it's refused.
  for (let attempt = 0; attempt < 2; attempt++) {
    failed = '';
    try {
      await bot.equip(item, 'hand');
      await bot.placeBlock(ref.block, ref.face);
    } catch (err) { failed = err && err.message ? err.message : String(err); } // checked below: it often lands before its event
    await wait(150);
    const now2 = bot.blockAt(pos);
    if (now2 && match(now2.name)) break;
    await wait(300);
  }
  const after = bot.blockAt(pos);
  if (!after || !match(after.name)) return why(`${after ? after.name : '?'} there after placing ${item.name}${failed ? ` (${failed})` : ''}`);
  placed.push({ x: pos.x, y: pos.y, z: pos.z, name: after.name });
  return true;
}

// What fills a floor cell found dug out, best first: earth, else stone, else planks. And what she sets on a path for a
// moment to turn it to plain dirt (the game's rule: a path under a full block is dirt again), then takes back.
const CampFiller = (n) => ['dirt', 'coarse_dirt', 'rooted_dirt', 'cobblestone'].includes(n) || isPlanks(n);

// The ground the floor is trodden into: a path is made from any of these, plain dirt only from grass.
const Treadable = new Set(['grass_block', 'dirt', 'coarse_dirt', 'podzol', 'mycelium', 'rooted_dirt', 'dirt_path']);

// The floor, last, the nearest cell first: a path with the shovel, nothing placed; plain dirt the same, then a block
// set on it and taken back (CampFiller). Dug out and its dirt set back instead, the drop sometimes popped onto the table
// out of reach, and a dip's hole was two deep (she stepped in for the dirt and stayed down there): nothing is dug now.
// None of it goes in `placed` (a block set on a path is, while it's there: a fight in between, it comes down with the
// camp): the floor stays when the camp comes down. Ground that isn't earth (sand, stone, snow) is left as it is. A cell
// found dug out is filled, first and once more at the end.
async function campTread(o, mine) {
  const h = (x, z) => (mine.heights && `${x},${z}` in mine.heights ? mine.heights[`${x},${z}`] : 0);
  const cells = mine.floor.filter(([x, z]) => h(x, z) !== null)
    .map(([x, z, kind]) => ({ cell: [x, h(x, z) - 1, z], pos: At(o, x, h(x, z) - 1, z), kind }))
    .sort((a, b) => a.pos.distanceTo(bot.entity.position) - b.pos.distanceTo(bot.entity.position));
  const isDirt = (n) => n === 'dirt';
  // Hers only: whatever else lies about may be someone's, and a player's dropped things stay where they are.
  const lying = (pos, r, match) => (e) => e.name === 'item' && typeof e.getDroppedItem === 'function'
    && match((e.getDroppedItem() || {}).name || '') && e.position.distanceTo(pos.offset(0.5, 0.5, 0.5)) < r;
  const hole = (pos) => { const g = bot.blockAt(pos); return Boolean(g) && campReplaceable(g); };
  // Someone standing on it: nothing can be set there, a path it stays.
  const stoodOn = (pos) => Object.values(bot.players).some(({ entity: e }) => e && e !== bot.entity
    && Math.floor(e.position.x) === pos.x && Math.floor(e.position.z) === pos.z && Math.abs(e.position.y - (pos.y + 1)) < 2);
  const fill = async ({ cell, pos }) => {
    if (!hasItem(isDirt)) await pickUp(lying(pos, 6, isDirt), 8000, mine);
    if (task === mine && !(await campPlace(o, mine, cell, isDirt, [])) && task === mine && hole(pos))
      await campPlace(o, mine, cell, CampFiller, []);
  };
  const capOf = (above) => mine.placed.findIndex((b) => b.x === above.x && b.y === above.y && b.z === above.z);
  for (const c of cells) {
    if (task !== mine) return;
    const { pos } = c;
    const kind = c.kind === 'dirt' && stoodOn(pos) ? 'path' : c.kind;
    if (hole(pos)) { await fill(c); continue; }
    const ground = bot.blockAt(pos);
    const above = pos.offset(0, 1, 0);
    if (!ground || !Treadable.has(ground.name)) continue;
    const capped = capOf(above) >= 0; // set down before a fight took her off
    if (!capped && (kind === 'path' ? ground.name === 'dirt_path' : ground.name === 'dirt')) continue;
    if (!campInReach(above)) {
      try { await bot.pathfinder.goto(new goals.GoalPlaceBlock(above, bot.world, { range: CampReach - 0.4 })); } catch { /* from here */ }
      if (task !== mine) return;
    }
    try {
      const over = bot.blockAt(above);
      if (!capped && over && over.name !== 'air') await bot.dig(over, true); // short grass: a shovel needs the top bare
      if (bot.blockAt(pos).name !== 'dirt_path' && bot.blockAt(pos).name !== 'dirt') {
        const shovel = bot.inventory.items().find((i) => i.name.endsWith('_shovel'));
        if (!shovel) continue;
        await bot.equip(shovel, 'hand');
        await bot.activateBlock(bot.blockAt(pos));
      }
      if (kind !== 'dirt' || task !== mine) continue;
      if (!capped) {
        await wait(150);
        if (!(await campPlace(o, mine, [c.cell[0], c.cell[1] + 1, c.cell[2]], CampFiller, mine.placed))) continue;
      }
      for (let i = 0; i < 10 && bot.blockAt(pos).name !== 'dirt'; i++) await wait(100);
      const cap = bot.blockAt(above);
      if (task !== mine || !cap || !CampFiller(cap.name)) continue;
      const name = cap.name;
      if (bot.tool && bot.tool.equipForBlock) await bot.tool.equipForBlock(cap, {});
      await bot.dig(cap, true);
      const at = capOf(above);
      if (at >= 0) mine.placed.splice(at, 1);
      await pickUp(lying(above, 3, (n) => n === name), 3000, mine);
    } catch (err) {
      log('camp: floor:', err && err.message ? err.message : String(err));
    }
  }
  for (const c of cells) if (task === mine && hole(c.pos)) await fill(c);
}

// Charcoal from her logs in the camp's furnace, planks burning: `n` of them.
async function campCharcoal(o, mine, n) {
  const at = At(o, ...CampFurnace);
  let box;
  try {
    box = await bot.openFurnace(bot.blockAt(at));
    mine.window = box;
    const logs = bot.inventory.items().find((i) => isLog(i.name));
    const planks = bot.inventory.items().find((i) => isPlanks(i.name));
    if (!logs || !planks) return;
    await box.putFuel(planks.type, null, Math.min(planks.count, Math.ceil(n / 1.5)));
    await box.putInput(logs.type, null, Math.min(logs.count, n));
    const until = Date.now() + n * 12000 + 5000;
    let got = 0;
    while (task === mine && got < n && Date.now() < until) {
      await wait(1000);
      if (box.outputItem()) {
        got += box.outputItem().count;
        await box.takeOutput();
      }
    }
  } catch (err) {
    log('camp: charcoal failed:', err && err.message ? err.message : String(err));
  } finally {
    if (box) try { box.close(); } catch { /* closed */ }
    mine.window = null;
  }
}

// One wooden thing (`suffix`: '_trapdoor', '_slab', '_fence'), `per` planks of one kind a craft: of the camp's wood when
// there's enough of it, else of the kind she has most of, planks made for it from its logs when short (never the logs
// she keeps: `keep`). With logs of several kinds her planks come out mixed, and a recipe takes one kind only: tied to
// one wood, the camp lost a fence post with planks to spare.
async function craftWooden(suffix, per, prefer, table, keep) {
  const planksOf = (k) => held((n) => n === `${k}_planks`);
  const kinds = [...new Set(bot.inventory.items().filter((i) => isPlanks(i.name) || isLog(i.name))
    .map((i) => i.name.replace(/_(planks|log)$/, '')))];
  const order = [prefer, ...kinds.filter((k) => k !== prefer).sort((a, b) => planksOf(b) - planksOf(a))];
  for (const k of order) {
    while (planksOf(k) < per && held((n) => n === `${k}_log`) > 0 && held(isLog) > keep() && (await craftOne(`${k}_planks`)));
    if (planksOf(k) >= per && (await craftOne(`${k}${suffix}`, table))) return true;
  }
  return false;
}

// `resume`: taken up again after a fight or a creeper (restartOrder), at the same site with what was already placed and
// the same floor: a new search would have started a second camp beside the half-built one. Everything placed is skipped
// (campPlace).
async function makeCamp(player, resume = null) {
  const again = resume && resume.site;
  // What's still there of what she placed: a creeper may have blown some of it away, and counted as done it was never
  // made again (a table gone ended the camp).
  const stands = (b) => { const now = bot.blockAt(new Vec3(b.x, b.y, b.z)); return !now || now.name === b.name; };
  const placed = again && Array.isArray(resume.placed) ? resume.placed.filter(stands) : [];
  if (!again && campNeeds(placed).logs > 0 && treesNear().length === 0) return emit('cantCamp', { player, why: 'missing', missing: ['wood'] });
  const o = again ? new Vec3(resume.site.x, resume.site.y, resume.site.z) : campSite();
  if (!o) return emit('cantCamp', { player, why: 'noRoom' });
  stay();
  // Interrupted, what to take up again: this site, what's on it and the floor's pattern (resumeFor).
  const mine = { kind: 'camp', player, window: null, site: { x: o.x, y: o.y, z: o.z }, placed,
    floor: again && Array.isArray(resume.floor) ? resume.floor : campFloor(),
    heights: again && resume.heights && typeof resume.heights === 'object' ? resume.heights : campHeights(o) };
  task = mine;
  if (!again) emit('camping', { player });
  const fail = (why, missing = []) => {
    if (task !== mine) return;
    task = null;
    emit('cantCamp', { player, why, missing, blocks: placed });
  };
  const work = async () => {
    const s = At(o, ...CampWork);
    try { await bot.pathfinder.goto(new goals.GoalBlock(s.x, s.y, s.z)); } catch { /* close enough */ }
  };

  // Nothing escapes: a throw left her on a dead task, the order unanswered, and the blocks she had placed unknown to C#
  // (nothing to take down later). It ends the camp as a failed craft would, with what's on the ground.
  try {
    // Wool, sheared, before anything else is cut or crafted.
    for (let n = 0; n < 6 && task === mine && campNeeds(placed).need.bed && campBed() === 'shear'; n++) {
      const sheep = campSheep();
      await useOn(sheep, (x) => x === 'shears');
      await wait(600);
      await pickUp((e) => e.position.distanceTo(sheep.position) < 4, 6000, mine);
    }
    // Wood, all of it at once: planks for the crafting, logs for the benches, the campfire and charcoal. Again while
    // there are trees in reach: a plain's trees are scattered, and a pass whose drops she missed brought nothing in, which
    // ended the camp (« missing wood ») one log short at a forest's edge.
    for (let pass = 0; pass < 6 && task === mine && campNeeds(placed).logs > 0; pass++) {
      const trees = treesNear();
      if (trees.length === 0) break;
      await fellTrees(trees, campNeeds(placed).logs, mine);
    }
    if (task !== mine) return;
    if (campNeeds(placed).logs > 0) return fail('missing', ['wood']);
    await work();
    const plan = campNeeds(placed);
    await craftPlanks(plan.planks + 4, plan.benchLogs + (plan.need.campfire ? 3 : 0) + plan.coalShort, mine);
    if (task !== mine) return;
    // The wood she has most of: the trapdoors, the slabs, the fences, and the benches' logs when she has them, all one.
    const kind = bot.inventory.items().filter((i) => isPlanks(i.name)).sort((a, b) => b.count - a.count)[0];
    const wood = kind ? kind.name.replace(/_planks$/, '') : 'oak';
    const benchLog = (n) => (hasItem((x) => x === `${wood}_log`) || placed.some((b) => b.name === `${wood}_log`) ? n === `${wood}_log` : isLog(n));
    if (plan.need.table && !(await craftOne('crafting_table'))) return fail('craft', ['crafting_table']);
    if (!(await campPlace(o, mine, CampTable, (n) => n === 'crafting_table', placed, [6, 0, 4]))) return fail('place', ['crafting_table']);
    const table = bot.blockAt(At(o, ...CampTable));
    const sticks = async (n) => { while (task === mine && held((x) => x === 'stick') < n && (await craftOne('stick'))); };
    const several = async (name, crafts) => { for (let i = 0; i < crafts && task === mine; i++) if (!(await craftOne(name, table))) return; };
    // Tools, then stone and coal.
    if (campNeeds(placed).need.pickaxe) { await sticks(2); await craftOne('wooden_pickaxe', table); }
    if (campNeeds(placed).need.axe) { await sticks(2); await craftOne('wooden_axe', table); }
    if (campNeeds(placed).need.shovel) { await sticks(2); await craftOne('wooden_shovel', table); }
    if (task === mine && campNeeds(placed).cobble > 0) await gatherRounds('stone', campNeeds(placed).cobble, mine);
    if (task === mine && campNeeds(placed).coalShort > 0 && gatherCandidates('coal').open.length > 0) await gatherRounds('coal', campNeeds(placed).coalShort, mine);
    if (task !== mine) return;
    if (campNeeds(placed).cobble > 0) return fail('missing', ['stone']);
    await work();
    // What's crafted at the table.
    const now = campNeeds(placed);
    if (now.need.chest) await craftOne('chest', table);
    if (now.need.furnace) await craftOne('furnace', table);
    if (now.need.bed && woolColour()) await craftOne(`${woolColour()}_bed`, table);
    else if (now.need.bed && now.bed === 'straw') {
      while (task === mine && held((n) => n === 'hay_block') < StrawHay && (await craftOne('hay_block', table)));
      await craftOne('yellow_bed', table);
    }
    const keep = () => { const n = campNeeds(placed); return n.benchLogs + (n.need.campfire ? 3 : 0) + n.coalShort; };
    const wooden = async (suffix, per, crafts) => {
      for (let i = 0; i < crafts && task === mine; i++) if (!(await craftWooden(suffix, per, wood, table, keep))) return;
    };
    await wooden('_trapdoor', 6, Math.ceil(now.need.trapdoors / 2));
    await wooden('_slab', 3, Math.ceil(now.need.slabs / 6));
    if (now.need.fences > 0) { await sticks(2 * Math.ceil(now.need.fences / 3)); await wooden('_fence', 4, Math.ceil(now.need.fences / 3)); }
    if (task !== mine) return;
    // The rest of the workstation, then charcoal in its furnace when there was no coal.
    await campPlace(o, mine, CampFurnace, (n) => n === 'furnace', placed, [6, 0, 5]);
    await campPlace(o, mine, CampChest, (n) => n === 'chest', placed, [6, 0, 6]);
    if (task === mine && campNeeds(placed).coalShort > 0) await campCharcoal(o, mine, campNeeds(placed).coalShort);
    if (task !== mine) return;
    const lit = campNeeds(placed).need;
    if (lit.torches > 0 || lit.campfire) await sticks(Math.ceil(lit.torches / 4) + (lit.campfire ? 3 : 0));
    if (lit.torches > 0) await several('torch', Math.ceil(lit.torches / 4));
    if (lit.campfire) await craftOne('campfire', table);
    if (task !== mine) return;
    // The bed, the fire in its pit, the benches (slabs first: the log lies along the slab it's set against), the posts.
    // Twice: a block the server refused (a sheep standing in its cell, in the wild) is tried again once the rest is
    // down; what's already there is passed over.
    const isBed = (n) => n.endsWith('_bed');
    const post = (x, z) => mine.heights[`${x},${z}`] || 0;
    let bedOk = false;
    for (let round = 0; round < 2 && task === mine; round++) {
      if (round > 0) await wait(2000);
      bedOk = (hasItem(isBed) || placed.some((b) => isBed(b.name))) && (await campPlace(o, mine, CampBed, isBed, placed, CampBedStand));
      for (const [x, z] of campCells('d')) await campPlace(o, mine, [x, 0, z], isWooden('_trapdoor'), placed);
      await campPlace(o, mine, CampFire, (n) => n === 'campfire', placed);
      for (const [x, z] of campCells('S')) await campPlace(o, mine, [x, 0, z], isWooden('_slab'), placed);
      for (const [x, z] of campCells('L')) {
        const slab = campCells('S').find(([sx, sz]) => Math.abs(sx - x) + Math.abs(sz - z) === 1);
        await campPlace(o, mine, [x, 0, z], benchLog, placed, null, slab ? [slab[0] - x, 0, slab[1] - z] : null);
      }
      for (const [x, z] of campCells('l')) await campPlace(o, mine, [x, post(x, z), z], isWooden('_fence'), placed);
      for (const [x, z] of campCells('l')) await campPlace(o, mine, [x, post(x, z) + 1, z], (n) => n === 'torch', placed);
      if (placed.filter((b) => !isBed(b.name)).length >= CampBlocks && (bedOk || !hasItem(isBed))) break;
    }
    if (task !== mine) return;
    await campTread(o, mine);
    if (task !== mine) return;
    task = null;
    emit('camped', { player, blocks: placed, complete: placed.filter((b) => !b.name.endsWith('_bed')).length >= CampBlocks,
      bed: bedOk && placed.some((b) => b.name.endsWith('_bed')), x: o.x + CampFire[0], y: o.y, z: o.z + CampFire[2], dimension: dimension() });
  } catch (err) {
    log('camp failed:', err && err.message ? err.message : String(err));
    fail('craft');
  }
}

// « démonte le camp »: the blocks C# kept, each broken only if it's still what she placed there: the torches first
// (they'd drop off their posts), then whatever is nearest from where she stands, out of reach through collectblock;
// then what fell, the chest's contents with it. The floor isn't in the list, and stays. She keeps it all (a campfire
// broken without silk touch gives two charcoal, not itself: the game's rule).
const CampReach = 4.4;

async function campDig(block, mine) {
  if (task !== mine) return false;
  const eyes = bot.entity.position.offset(0, 1.62, 0);
  if (eyes.distanceTo(block.position.offset(0.5, 0.5, 0.5)) > CampReach) {
    await gather([block], mine, 'uncamp'); // out of reach from here: collectblock finds a way
  } else {
    try {
      if (bot.tool && bot.tool.equipForBlock) await bot.tool.equipForBlock(block, {});
      await bot.dig(block, true);
    } catch (err) {
      log('uncamp: dig failed:', err && err.message ? err.message : String(err));
    }
  }
  const now = bot.blockAt(block.position);
  return !now || now.name !== block.name;
}

async function breakCamp(player, blocks, dim, center) {
  if (!Array.isArray(blocks) || blocks.length === 0) return emit('cantUncamp', { player, why: 'none' });
  if (dim && dim !== dimension()) return emit('cantUncamp', { player, why: 'otherDimension' });
  const mid = center && Number.isFinite(center.x) ? new Vec3(center.x, center.y, center.z)
    : new Vec3(Math.floor(blocks.reduce((a, b) => a + b.x, 0) / blocks.length), blocks[0].y, Math.floor(blocks.reduce((a, b) => a + b.z, 0) / blocks.length));
  if (Math.hypot(mid.x - bot.entity.position.x, mid.z - bot.entity.position.z) > MaxTrip) return emit('cantUncamp', { player, why: 'tooFar' });
  stay();
  const mine = { kind: 'uncamp', player };
  task = mine;
  emit('uncamping', { player });
  const still = () => blocks.map((b) => bot.blockAt(new Vec3(b.x, b.y, b.z))).filter((now, i) => now && now.name === blocks[i].name);
  const was = still().length;
  for (const b of still().filter((x) => x.name === 'torch')) await campDig(b, mine);
  const near = (a, b) => a.position.distanceTo(bot.entity.position) - b.position.distanceTo(bot.entity.position);
  for (let i = 0; i < was * 2 && task === mine; i++) {
    const next = still().sort(near)[0];
    if (!next) break;
    await campDig(next, mine);
  }
  // Whatever is left, any way it can be got at.
  for (let round = 0; round < 2 && task === mine && still().length > 0; round++) await gather(still(), mine, 'uncamp');
  if (task !== mine) return;
  await wait(800); // the last drops land
  await pickUp((e) => e.name === 'item' && e.position.distanceTo(mid) <= 9, FieldSweep, mine);
  if (task !== mine) return;
  task = null;
  emit('uncamped', { player, count: was - still().length, left: still().length });
}

// ---- Creepers, whatever she's doing -------------------------------------------------------------------

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

function creeperTick() {
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
  task = { kind: 'dodge', player: '', creeper, resume, until: Date.now() + DodgeFor, mode: null, modeTarget: null, modeSince: 0,
    seenAt: Date.now() };
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
function dodgeTick() {
  const t = task;
  if (!t || t.kind !== 'dodge') return;
  const c = t.creeper;
  const me = bot.entity.position;
  if (c && c.isValid !== false && seenByAnyone(c)) t.seenAt = Date.now();
  if (!c || c.isValid === false || !bot.entities[c.id] || c.position.distanceTo(me) > CreeperClear || Date.now() > t.until
    || Date.now() - t.seenAt > CreeperUnseen) {
    task = null;
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

// ---- Defending ---------------------------------------------------------------------------------------

// A monster hitting a player within DefendRadius of her, anyone, she fights it herself, saying nothing: someone near her
// getting hurt is hers to help, whatever she was doing. A player hitting her family (C# sends their in-game names on
// spawn, `family`) is reported (`familyHurt`) and C# decides what comes of it: a word, one hit back (`defend` with
// swat), then a fight. She goes when free, during an activity, following or guarding anyone, or busy with an order she
// can start again (Restartable), and she goes back to it after (resumeFor). Only what's over in seconds or can't be
// left (smelting at a furnace, handing over, storing, a bed, a portal, running away) keeps her, and C# keeps to words
// (`cantDefend`). She never hits family or a creeper, and breaks off below RetreatHealth.
let family = new Set();
const DefendRadius = 16;
const DefendMobFor = 20000;
const DefendFightFor = 15000;
const SwatFor = 6000;

function isFamily(name) {
  return Boolean(name) && family.has(String(name).toLowerCase());
}

function canDefend() {
  if (bot.isSleeping) return false;
  if (isFree(task) || ['defend', 'back', 'fight'].includes(task.kind) || isActivity(task) || FollowsSomeone.has(task.kind)) return true;
  return Boolean(task.command) && Restartable.has(task.kind);
}

// True when she takes it on.
function defend(foe, victim, swat) {
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
  task = { kind: 'defend', player: '', victim, foe, swat, resume, until: Date.now() + lasts, mode: null, modeTarget: null,
    fightFrom: foe.position.clone() };
  equipWeapon().catch(() => {});
  return true;
}

// Every physics tick while defending: chase the attacker, hit it on the weapon's cooldown; one hit is enough for a
// swat. Over when it's dead or gone, too far, out of time, or she's badly hurt.
function defendTick() {
  const t = task;
  if (!t || t.kind !== 'defend') return;
  const foe = t.foe;
  const me = bot.entity.position;
  if (!foe || foe.isValid === false || !bot.entities[foe.id] || foe.position.distanceTo(me) > DefendRadius
      || Date.now() > t.until || bot.health <= RetreatHealth || headUnder(foe) || foe.position.distanceTo(t.fightFrom) > FightLeash)
    return endDefend(t);
  chase(t, foe);
  if (strike(foe, t) && t.swat) endDefend(t);
}

function endDefend(t) {
  if (task !== t) return;
  task = null;
  fightPace(false);
  bot.pathfinder.setGoal(null);
  if (bot.health <= RetreatHealth) eatIfHungry(20).catch(() => {});
  resumeAfterBreak(t.resume);
}

// ---- Defending herself -------------------------------------------------------------------------------

// Anything that hurts her, or a monster coming at her up close, is dealt with whatever she's doing: she stops, deals
// with it, and goes back to what she was doing (resumeFor, as after defending someone). Her life comes before an
// errand: one that can't be started again is dropped, and its player told (`dropped`, nobody to blame). Not asleep, in
// a boat (her physics is off) or crossing a portal, and not while running from a player. Guarding, patrolling,
// defending someone or dodging a creeper, the fight under way is already hers (guardTick takes the monster on her
// first). The stance (fight it, or only get away from it: answerTo) is kept on the task; getting away is Task 4's
// walk, and meanwhile she never hits what she should only get away from.
// A monster this close and coming at her is a fight before its first hit; one further off, or standing still (a
// skeleton on a hill), leaves her to her work.
const SelfThreat = 4;
// Nothing on her or near her for this long, and the fight's over.
const FightQuiet = 2000;
// How far a foe of the fight still counts, in sight.
const FightSight = 8;
// Getting away from something she never hits ends once it's this far, or hasn't come near her for GetOutQuiet.
const GetOutClear = 20;
const GetOutQuiet = 8000;

function canSelfDefend() {
  if (!bot.entity || bot.isSleeping || riding) return false;
  return !(task && ['portal', 'flee'].includes(task.kind));
}

// True when she takes it on.
function selfDefend(foe, stance = 'fight') {
  if (!foe || foe.isValid === false || !canSelfDefend()) return false;
  if (task && task.kind === 'fight') {
    task.foes.add(foe.id);
    if (stance === 'getOut') {
      task.threat = foe;
      task.threatAt = Date.now();
    }
    return true;
  }
  // Their own fight, except that getting out of a warden's way comes first: guarding, she gets clear and guards on.
  if (task && (task.kind === 'dodge' || (stance !== 'getOut' && ['defend', 'guard', 'patrol'].includes(task.kind)))) return false;
  const previous = task;
  const resume = resumeFor(previous);
  if (previous && !resume && isOrder(previous) && previous.kind !== 'seek' && previous.kind !== 'sleep')
    emit('dropped', { player: previous.player, kind: previous.kind, by: '' });
  stay();
  task = { kind: 'fight', player: '', foes: new Set([foe.id]), foe: null, stance, stanceSince: Date.now(),
    threat: stance === 'getOut' ? foe : null, threatAt: Date.now(), resume, fightFrom: foe.position.clone(), quietSince: null,
    startedAt: Date.now(), held: null, toward: null, mode: null, modeTarget: null, modeSince: 0 };
  if (stance === 'fight') equipWeapon();
  return true;
}

// Where each monster near her was a moment ago, to tell one coming at her from one standing about.
const approach = new Map();
const ApproachEvery = 500;

function comingAt(e, me) {
  const d = e.position.distanceTo(me);
  if (d < 2) return true;
  const seen = approach.get(e.id);
  if (!seen || Date.now() - seen.at > ApproachEvery * 2) {
    approach.set(e.id, { d, at: Date.now() });
    return false;
  }
  if (Date.now() - seen.at < ApproachEvery) return false;
  approach.set(e.id, { d, at: Date.now() });
  return d < seen.d - 0.2;
}

// Every physics tick: a monster (not a creeper: creeperTick's) within SelfThreat, in sight and coming at her, while
// nothing else is fighting it out.
function threatTick() {
  if (!canSelfDefend() || (task && ['dodge', 'fight'].includes(task.kind))) return;
  const me = bot.entity.position;
  // What she never fights (a warden, a creaking), in sight: she gets clear of it, whatever she's doing.
  const danger = bot.nearestEntity((e) => GetOutKinds.has(e.name) && e.isValid !== false
    && e.position.distanceTo(me) < DangerSight && inSight(e));
  if (danger) return void selfDefend(danger, 'getOut');
  if (task && ['guard', 'patrol', 'defend'].includes(task.kind)) return;
  const foe = bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e)
    && e.position.distanceTo(me) < SelfThreat && comingAt(e, me) && inSight(e));
  if (foe) selfDefend(foe);
}

// ---- Sizing a fight up: fight it, fall back to her player, or get out ----------------------------------

// A warden or a creaking this close and in sight: she gets clear of it.
const DangerSight = 16;
// How far she sizes a fight up: every monster in sight this close counts against her (fight.js weighs them).
const SizeSight = 12;
// A stance is held at least this long, so she doesn't flip between fighting and falling back every tick.
const StanceHold = 1500;
// Falling back with nobody to fall back to, this long, and she turns and fights what still chases her.
const FallBackAlone = 20000;
// Who she falls back to is within this, and seen; beside them (Helped) counts as help.
const HerPlayerReach = 32;
const Helped = 4;
// Getting clear of a warden: this far beyond her player, on their far side from it; alone, this far from it.
const ClearBeyond = 6;
const ClearAlone = 16;
// Out of her sight this long, and what she was getting clear of is behind her.
const DangerUnseen = 3000;
const ArmourPoints = {
  leather: [1, 3, 2, 1], copper: [2, 4, 3, 1], golden: [2, 5, 3, 1], chainmail: [2, 5, 4, 1], iron: [2, 6, 5, 2],
  diamond: [3, 8, 6, 3], netherite: [3, 8, 6, 3], turtle: [2, 0, 0, 0],
};

function armourPoints() {
  return WornSlots.reduce((sum, slot, i) => {
    const item = bot.inventory.slots[bot.getEquipmentDestSlot(slot)];
    const m = item && /^([a-z]+)_(helmet|chestplate|leggings|boots)$/.exec(item.name);
    return sum + ((m && ArmourPoints[m[1]] && ArmourPoints[m[1]][i]) || 0);
  }, 0);
}

function bestWeapon() {
  const names = new Set(bot.inventory.items().map((i) => i.name));
  return Weapons.find((w) => names.has(w)) || null;
}

// Who she falls back to: whoever she was told to come to mid-fight, else the player of what she was doing, else Papa,
// else Tata, else the closest player; seen, within HerPlayerReach.
function herPlayer(t) {
  const me = bot.entity.position;
  const seen = (e) => (e && e !== bot.entity && e.position.distanceTo(me) <= HerPlayerReach ? e : null);
  const named = (name) => (name ? seen(playerEntity(name)) : null);
  const r = t.resume;
  const byFamily = familyOrder.map((f) => Object.values(bot.players).find((p) => p.username.toLowerCase() === f))
    .map((p) => p && seen(p.entity)).find(Boolean);
  return named(t.toward) || named(r && (r.player || (r.restart && r.restart.player))) || byFamily
    || bot.nearestEntity((e) => e.type === 'player' && e !== bot.entity && e.position.distanceTo(me) <= HerPlayerReach);
}

// A point `far` blocks from `from`, away from `away` (horizontally).
function awayFrom(from, away, far) {
  const dx = from.x - away.x;
  const dz = from.z - away.z;
  const n = Math.hypot(dx, dz) || 1;
  return new Vec3(from.x + (dx / n) * far, from.y, from.z + (dz / n) * far);
}

// Every physics tick while defending herself. Sized up (fight.js) every StanceHold at most: fight the closest foe;
// fall back to her player hitting whatever keeps up, eating when nothing is in reach; or get clear of what she never
// fights. Over once nothing is left for FightQuiet (a warden: once it's far or out of sight).
function fightTick() {
  const t = task;
  if (!t || t.kind !== 'fight') return;
  const me = bot.entity.position;
  const onHer = foeOnHer();
  if (onHer && t.stance !== 'getOut') t.foes.add(onHer.id);
  const foes = [...t.foes].map((id) => bot.entities[id])
    .filter((e) => e && e.isValid !== false && !headUnder(e) && reachable(t, e) && e.position.distanceTo(me) < FightSight
      && e.position.distanceTo(t.fightFrom) <= FightLeash && inSight(e))
    .sort((a, b) => a.position.distanceTo(me) - b.position.distanceTo(me));
  const p = herPlayer(t);
  if (t.stance !== 'getOut' && Date.now() - t.stanceSince >= StanceHold) {
    const around = Object.values(bot.entities).filter((e) => e !== bot.entity && e.isValid !== false
      && (t.foes.has(e.id) || (isFoe(e) && e.name !== 'creeper') || GetOutKinds.has(e.name))
      && e.position.distanceTo(me) < SizeSight && inSight(e));
    let next = sizeUp({ foes: around.map((e) => ({ name: e.name, distance: e.position.distanceTo(me) })), health: bot.health,
      armour: armourPoints(), weapon: bestWeapon(), helped: Boolean(p && p.position.distanceTo(me) < Helped), previous: t.stance });
    if (next === 'fallBack' && (t.lastStand || (!p && t.fallSince && Date.now() - t.fallSince > FallBackAlone))) {
      next = 'fight'; // nobody to fall back to, still chased: she turns and fights it out
      t.lastStand = true;
    }
    if (next === 'getOut') {
      t.threat = around.find((e) => GetOutKinds.has(e.name));
      t.threatAt = Date.now();
    }
    if (next !== t.stance) {
      t.stance = next;
      t.stanceSince = Date.now();
      t.mode = null;
      t.modeTarget = null;
      if (next === 'fallBack') {
        t.fallSince = Date.now();
        emit('retreat', { player: t.toward || '' });
      }
    }
  }
  if (t.stance === 'getOut') return getOutTick(t, p);
  if (foes.length === 0) {
    if (!t.quietSince) {
      t.quietSince = Date.now();
      t.mode = null;
      t.modeTarget = null;
      bot.pathfinder.setGoal(null);
    }
    if (Date.now() - t.quietSince > FightQuiet) endFight(t);
    return;
  }
  t.quietSince = null;
  t.foe = foes[0];
  if (t.stance === 'fight') {
    chase(t, t.foe);
    strike(t.foe, t);
    return;
  }
  // Falling back: to her player, else off away from them, again once there. Whatever keeps up gets hit.
  fightPace(true);
  if (p) setMode(t, 'fallBack', p, new goals.GoalFollow(p, 2), true);
  else if (t.mode !== 'hop' || !bot.pathfinder.isMoving()) {
    const spot = awayFrom(me, t.foe.position, 8);
    t.mode = 'hop';
    t.modeTarget = null;
    bot.pathfinder.setGoal(new goals.GoalNear(spot.x, spot.y, spot.z, 2));
  }
  if (onHer) {
    if (!eating && !(bot.heldItem && Weapons.includes(bot.heldItem.name)) && bestWeapon()) equipWeapon();
    strike(onHer);
  } else if (!eating && bot.health < 20) eatIfHungry(20).catch(() => {});
}

// Getting clear of what she never fights: beyond her player on their far side from it, else well away from it. Over
// once it's far, gone, or out of her sight for DangerUnseen (and, for an animal that hit her, once it leaves her alone).
function getOutTick(t, p) {
  const me = bot.entity.position;
  const th = t.threat;
  if (th && th.isValid !== false && inSight(th)) t.threatSeenAt = Date.now();
  if (!th || th.isValid === false || !bot.entities[th.id] || th.position.distanceTo(me) > GetOutClear
    || Date.now() - (t.threatSeenAt || t.startedAt) > DangerUnseen
    || (!GetOutKinds.has(th.name) && Date.now() - t.threatAt > GetOutQuiet)) return endFight(t);
  if (th.position.distanceTo(me) < MeleeReach + 1) t.threatAt = Date.now();
  if (Date.now() < (t.clearAt || 0)) return;
  t.clearAt = Date.now() + 1000; // her player moves: the spot moves with them
  const near = p && p.position.distanceTo(me) < DangerSight ? p : null;
  const spot = near ? awayFrom(near.position, th.position, ClearBeyond) : awayFrom(me, th.position, ClearAlone);
  t.mode = 'clear';
  t.modeTarget = th;
  fightPace(true);
  bot.pathfinder.setGoal(new goals.GoalNear(spot.x, spot.y, spot.z, 1));
}

function endFight(t) {
  if (task !== t) return;
  task = null;
  fightPace(false);
  bot.pathfinder.setGoal(null);
  approach.clear();
  if (bot.health <= RetreatHealth) eatIfHungry(20).catch(() => {});
  resumeAfterBreak(t.resume);
}

// Who hurt someone, for sure: since 1.19.4 the server says (damage_event, the shooter for an arrow). Before that,
// Mineflayer gives no source and her own hits fall back to a guess.
function exactHits() {
  const v = bot.registry && bot.registry.version;
  return Boolean(v && typeof v['>='] === 'function' && v['>=']('1.19.4'));
}

// ---- Seeking someone ---------------------------------------------------------------------------------

// Walks to a player C# names (who, and why, is decided there): straight to them when they're in sight,
// else to the spot C# remembers them at, switching to them as soon as they show up. Reports `seek`
// with `met` when she's next to them, `missing` when they can't be found. Any order replaces it.
function dimension() {
  return String(bot.game && bot.game.dimension ? bot.game.dimension : '');
}

function seek(player, spot) {
  const target = playerEntity(player);
  if (target) {
    stay();
    task = { kind: 'seek', player, tries: 0 };
    const at = target.position;
    bot.pathfinder.setGoal(new goals.GoalNear(at.x, at.y, at.z, 2));
    return;
  }
  const here = bot.entity.position;
  if (!spot || spot.dimension !== dimension() || Math.hypot(spot.x - here.x, spot.z - here.z) > MaxTrip)
    return emit('seek', { player, outcome: 'missing' });
  stay();
  task = { kind: 'seek', player, tries: 0 };
  bot.pathfinder.setGoal(new goals.GoalNear(spot.x, spot.y, spot.z, 2));
}

// She reached her goal while seeking: next to them (met), or they moved on (walk up again, a few times),
// or she's at the remembered spot and they aren't there (missing).
function seekReached() {
  const target = playerEntity(task.player);
  if (target && target.position.distanceTo(bot.entity.position) <= 4) {
    emit('seek', { player: task.player, outcome: 'met' });
    task = null;
    return;
  }
  if (target && task.tries < 5) {
    task.tries++;
    const at = target.position;
    bot.pathfinder.setGoal(new goals.GoalNear(at.x, at.y, at.z, 2));
    return;
  }
  emit('seek', { player: task.player, outcome: 'missing' });
  task = null;
}

// Where she last saw each player, as they leave her sight (walking off, or logging out next to her: they
// log back in on that spot). C# keeps the ones it cares about.
function reportSeen(entity) {
  if (!entity || entity.type !== 'player' || !entity.username || entity.username === bot.username) return;
  const p = entity.position;
  emit('seen', { player: entity.username, x: Math.floor(p.x), y: Math.floor(p.y), z: Math.floor(p.z), dimension: dimension() });
}

// ---- Looking after herself: food and armour --------------------------------------------------------

const BadFood = new Set(['rotten_flesh', 'spider_eye', 'poisonous_potato', 'pufferfish', 'suspicious_stew', 'chorus_fruit']);
let eating = false;

// Below 14 she's hungry. Hurt in a fight, she eats up to full (20): only a full stomach heals. The most filling
// thing first. While she's out finding her supper she waits for it to be cooked, unless she's starving.
async function eatIfHungry(below = 14) {
  if (eating || bot.food >= below) return;
  if (task && task.kind === 'forage' && bot.food > CookAbove) return;
  const food = bot.inventory.items().filter((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name))
    .sort((a, b) => bot.registry.foodsByName[b.name].foodPoints - bot.registry.foodsByName[a.name].foodPoints)[0];
  if (!food) return;
  eating = true;
  const before = bot.inventory.count(food.type);
  try {
    await bot.equip(food, 'hand');
    await bot.consume();
  } catch (err) {
    // Mineflayer waits 2.5 s for the « done eating » status, a meal takes 1.6 s, and a slow link (the Pi, ViaBackwards)
    // eats the rest: when the food went down, she ate.
    if (bot.inventory.count(food.type) >= before) log('eating failed:', err && err.message ? err.message : String(err));
  } finally {
    eating = false;
  }
}

function hasFood() {
  return bot.inventory.items().some((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name));
}

// Hurt, she eats to heal: Minecraft heals her only while her food bar is at 18 or more, and quickly only with
// saturation left. So below full health she eats when the bar is under 18, or under 20 with no saturation left. Not
// with a monster close (a fight's retreat eats for her, and the food in her hand would be her sword), and not while
// she digs, draws her bow, stacks a block or fishes: the food in her hand would cut it off. The tick tries again.
// With nothing to eat and a fair bit hurt, she goes to find some (startForage), as when she's hungry.
const HealEvery = 5000;
const HealSafe = 10;
const HealForage = 14;

function healTick() {
  if (eating || bot.isSleeping || bot.health <= 0 || bot.health >= 20 || bot.food >= 20) return;
  if (bot.food >= 18 && bot.foodSaturation > 0) return;
  if (bot.targetDigBlock || drawing || bot.pathfinder.isBuilding() || (task && task.casting)) return;
  if (bot.nearestEntity((e) => isFoe(e) && e.position.distanceTo(bot.entity.position) < HealSafe)) return;
  if (hasFood()) eatIfHungry(20).catch(() => {});
  else if (bot.health <= HealForage && bot.food < 18) startForage();
}

// Hungry with nothing to eat: she looks for some herself when she can (startForage); otherwise she reports it each
// time her food bar drops a notch (C# decides whether she asks), and remembers she did, so the next food she picks
// up is someone feeding her.
let hungerReported = null;
let waitingForFood = false;

function reportHunger() {
  if (bot.food >= 14 || hasFood()) {
    hungerReported = null;
    return;
  }
  if (startForage()) return;
  if (hungerReported === bot.food) return;
  hungerReported = bot.food;
  waitingForFood = true;
  emit('hungry', { food: bot.food });
}

// Food landed in her inventory after she asked: whoever stands closest gets the thanks, and she eats.
function checkFed() {
  if (!waitingForFood || !hasFood()) return;
  waitingForFood = false;
  hungerReported = null;
  const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
    && e.position.distanceTo(bot.entity.position) < 8);
  if (near) emit('fed', { by: near.username });
  eatIfHungry().catch(() => {});
}

const ArmorSlots = { helmet: 'head', chestplate: 'torso', leggings: 'legs', boots: 'feet' };
const ArmorTiers = ['leather', 'copper', 'golden', 'chainmail', 'iron', 'turtle', 'diamond', 'netherite'];

// A tier she doesn't know still beats wearing nothing: copper armour, missing here, was never put on.
function armorRank(item) {
  return item ? ArmorTiers.indexOf(item.name.split('_')[0]) + 1 : -1;
}

// « enlève ton armure »: she puts nothing on by herself until told to (« mets ton armure ») or given a piece.
let bare = false;

// Puts on the best piece she carries for each slot, when it beats what she wears, and a shield in an empty off-hand.
// How many she put on.
async function wearBestArmor() {
  let count = 0;
  for (const [piece, slot] of Object.entries(ArmorSlots)) {
    const worn = bot.inventory.slots[bot.getEquipmentDestSlot(slot)];
    const best = bot.inventory.items().filter((i) => i.name.endsWith('_' + piece))
      .sort((a, b) => armorRank(b) - armorRank(a))[0];
    if (!best || armorRank(best) <= armorRank(worn)) continue;
    try {
      await bot.equip(best, slot);
      count++;
    } catch { /* a slot she can't use; move on */ }
  }
  const shield = bot.inventory.items().find((i) => i.name === 'shield');
  if (shield && !bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')]) {
    try {
      await bot.equip(shield, 'off-hand');
      count++;
    } catch { /* keep it in her pockets */ }
  }
  return count;
}

// Picked something up: armour is a gift, put on (even after « enlève ton armure »), and whoever stands closest gets the
// thanks. Not her own things coming back after a death (`retrieve`), which she puts on without a word.
async function dressAfterPickup(gift) {
  if (gift) bare = false;
  if (bare) return;
  const count = await wearBestArmor();
  if (count === 0 || !gift || (task && task.kind === 'retrieve')) return;
  const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
    && e.position.distanceTo(bot.entity.position) < 8);
  if (near) emit('armoured', { by: near.username });
}

// « mets ton armure »: the best she carries, on. Nothing better to put on: `worn` when she already wears some.
async function dress(player) {
  bare = false;
  const count = await wearBestArmor();
  if (count > 0) return emit('dressed', { player, count });
  const shielded = (bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')] || {}).name === 'shield';
  emit('cantDress', { player, why: wornArmour().length > 0 || shielded ? 'worn' : 'none' });
}

// « enlève ton armure »: what she wears (a shield in her off-hand too) goes back in her pockets, and stays there. Each
// piece needs a free slot: with full pockets, what's left stays on (`full`).
async function undress(player) {
  const slots = wornGear();
  if (slots.length === 0) return emit('cantUndress', { player, why: 'none' });
  bare = true;
  let count = 0;
  for (const slot of slots) {
    if (bot.inventory.emptySlotCount() === 0) break;
    try {
      await bot.unequip(slot);
      count++;
    } catch (err) {
      log('undress failed:', err && err.message ? err.message : String(err));
    }
  }
  if (count === 0) {
    bare = false; // still dressed: what she picks up next may still beat it
    return emit('cantUndress', { player, why: 'full' });
  }
  emit('undressed', { player, count, full: count < slots.length });
}

// ---- Routine: her day, built on the places she was asked to remember ------------------------------------

// C# sends the farm, fishing spot, animal pen and home it found among the remembered places (`routine`). By day she
// works in turns (WorkBlock, by place), one place after the other; at dusk she goes home and stays there till morning; the night
// is autoSleepTick's. A place in another dimension, or farther than OwnTripMax, is left out; one that fails (no way
// there, nothing to do there) is left out for SpotRetry, silently. Orders always come first.
const routine = { farm: null, fish: null, ranch: null, home: null };
const WorkKinds = ['farm', 'fish', 'ranch'];
// How long a turn lasts at each: the fields get the most, the pen the least. A pen needs a few minutes now and then
// (two fed, a cull, the wool), and six-minute turns there left the fields to wait.
const WorkBlock = { farm: 540000, fish: 360000, ranch: 180000 };
const OwnTripMax = 256;
const TripTimeout = 180000;
const SpotRetry = 600000;
const DuskStart = 11500;
const DuskEnd = 12541;
const HomeNear = 6;
const failedSpots = new Map();
let lastWork = null;

function spotUsable(what) {
  const s = routine[what];
  if (!s || s.dimension !== dimension() || (failedSpots.get(what) || 0) > Date.now()) return false;
  const here = bot.entity.position;
  return Math.hypot(s.x - here.x, s.z - here.z) <= OwnTripMax;
}

function failSpot(what, why) {
  failedSpots.set(what, Date.now() + SpotRetry);
  log(`routine: ${what} left out for a while (${why})`);
}

function isRoutine(t) {
  return Boolean(t && t.own && t.what);
}

// True while the routine has her (or just took her); false leaves her to tagging along.
function routineTick() {
  const t = task;
  if (t && t.kind === 'trip' && Date.now() > t.deadline) {
    stay();
    failSpot(t.what, 'never got there');
    return false;
  }
  const time = bot.time.timeOfDay;
  if (time < DuskStart) {
    const kinds = WorkKinds.filter(spotUsable);
    if (kinds.length === 0) return endRoutine();
    if (isRoutine(t) && kinds.includes(t.what) && (t.kind === 'trip' || Date.now() < t.turnEnd)) return true;
    const from = WorkKinds.indexOf(lastWork);
    const next = [1, 2, 3].map((i) => WorkKinds[(from + i) % WorkKinds.length]).find((k) => kinds.includes(k));
    if (isRoutine(t) && t.what === next && t.kind !== 'trip') {
      t.turnEnd = Date.now() + WorkBlock[next]; // the only place: another turn of the same
      return true;
    }
    return startTrip(next);
  }
  if (time <= DuskEnd) {
    if (isRoutine(t) && t.what === 'home') return true;
    if (!spotUsable('home')) return endRoutine();
    const h = routine.home;
    if (bot.entity.position.distanceTo(new Vec3(h.x, h.y, h.z)) <= HomeNear) {
      stay();
      task = { kind: 'home', own: true, player: '', what: 'home', spot: new Vec3(h.x, h.y, h.z) };
      return true;
    }
    return startTrip('home');
  }
  // Night: nothing new. At home (or on her way), she stays; the bed is autoSleepTick's.
  if (isRoutine(t) && t.what === 'home') return true;
  return endRoutine();
}

function endRoutine() {
  if (isRoutine(task)) stay();
  return false;
}

function startTrip(what) {
  const s = routine[what];
  const spot = new Vec3(s.x, s.y, s.z);
  stay();
  const trip = { kind: 'trip', own: true, player: '', what, spot, turnEnd: Date.now() + (WorkBlock[what] || 0), deadline: Date.now() + TripTimeout };
  task = trip;
  if (WorkKinds.includes(what)) lastWork = what;
  emit('routine', { what });
  if (bot.entity.position.distanceTo(spot) <= 3) arriveOwn(trip);
  else bot.pathfinder.setGoal(new goals.GoalNear(s.x, s.y, s.z, 2));
  return true;
}

// At the place: the activity starts with the same checks as the order (crops and a chest, water and a rod, animals and
// a chest). One that can't start leaves the place out for a while.
function arriveOwn(trip) {
  if (task !== trip) return;
  bot.pathfinder.setGoal(null);
  const base = { own: true, player: '', what: trip.what, turnEnd: trip.turnEnd };
  if (trip.what === 'home') {
    task = { ...base, kind: 'home', spot: trip.spot };
    return;
  }
  const anchor = bot.entity.position.clone();
  task = null;
  if (trip.what === 'farm') {
    if (!cropsNear(anchor)) return failSpot('farm', 'no crops');
    const chest = chestNear(anchor);
    if (!chest) return failSpot('farm', 'no chest');
    task = { ...base, kind: 'farm', anchor, chest, busy: false };
    farmRound();
  } else if (trip.what === 'fish') {
    if (!fishingRod()) {
      lacking('rod');
      return failSpot('fish', 'no rod');
    }
    const water = openWater();
    if (!water) return failSpot('fish', 'no water');
    task = { ...base, kind: 'fish', water, casting: false, quiet: true };
    fishLoop(task);
  } else {
    const pen = penAt(anchor);
    if (pen.why) return failSpot('ranch', pen.why === 'none' ? 'no animals' : 'no chest');
    task = { ...base, kind: 'ranch', anchor, chest: pen.chest, busy: false, fedAt: new Map(), shornAt: new Map(), target: null };
    ranchRound();
  }
}

// ---- Tagging along with family -------------------------------------------------------------------------

// Nothing in the routine: she keeps near Papa, else Tata, when one is in sight. Over TagFar blocks away she walks up to
// TagNear, without sprinting unless they're over TagSprint off; close by she takes a few slow steps around them now
// and then. They go out of sight, and it ends, silently.
const TagFar = 8;
const TagNear = 4;
const TagSprint = 16;
let familyOrder = []; // as C# sends them: Papa, then Tata
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

function tagAlongTick() {
  let t = task;
  const who = familyInSight();
  if (!who) {
    if (t && t.kind === 'tagAlong') stay();
    return;
  }
  if (!t || t.kind !== 'tagAlong' || t.who !== who) {
    stay();
    t = { kind: 'tagAlong', own: true, player: '', who, mode: null, stepAt: Date.now() + stepPause() };
    task = t;
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

// Her walk while tagging along: sprinting only to catch up from far. stay() puts the usual one back.
function walkPace(sprint) {
  if (slowWalk && walkSprint === sprint) return;
  const movements = gentleMovements();
  movements.allowSprinting = sprint;
  bot.pathfinder.setMovements(movements);
  slowWalk = true;
  walkSprint = sprint;
}

// ---- Body language -------------------------------------------------------------------------------------

// When her gaze is free (not asleep, eating, drawing her bow or walking; nothing to do, at home, at her own farm or pen
// between rounds, tagging along, following someone, or guarding them between fights) she looks the way a person would:
// at someone for a few seconds, away for a moment, then back. Who: whoever just talked within 12 blocks (no glancing
// away while they're talking), else the one she follows or guards, else the closest player within 8. A glance goes to
// another player close by, an animal or a monster in sight, or just somewhere, now and then down or up; guarding, she
// glances away more often (she's watching for monsters). With nobody around she looks somewhere new every 4–9 s, mostly
// near where she already looks rather than spinning round. Walking, the pathfinder turns her where she goes (Mineflayer
// walks where she looks), so none of this runs then. For 10 s after a player hits her, she turns her back on them
// instead. When Papa or Tata walks up from afar, she looks at them and crouches twice, once every GreetEvery at most.
const TalkLook = 8000;
const HitSulk = 10000;
const GreetEvery = 120000;
const PersonSight = 8;
const Creatures = new Set(['hostile', 'mob', 'animal', 'passive', 'water_creature', 'ambient']);
const lastChat = new Map();
const familyAway = new Map();
const greetedAt = new Map();
let hitBy = null;
let idleAt = 0;
let greeting = false;
// Whom she's looking at (an entity id), when she next glances away, and the glance (an entity, or a yaw and a pitch)
// with when it ends.
const gaze = { person: null, awayAt: 0, away: null, backAt: 0 };

function lookFree() {
  if (bot.isSleeping || eating || drawing || greeting || swim || bot.pathfinder.isMoving()) return false;
  if (!task) return true;
  if (task.kind === 'tagAlong' || task.kind === 'home' || task.kind === 'follow') return true;
  if (task.kind === 'guard') return task.mode === 'home' && !task.foe && !task.retreatUntil;
  return Boolean(task.own && (task.kind === 'farm' || task.kind === 'ranch') && !task.busy);
}

const eyes = (entity) => entity.position.offset(0, (entity.height || 1.8) * 0.9, 0);

function idleTick() {
  if (Date.now() < idleAt || !bot.entity) return;
  idleAt = Date.now() + 250;
  greetTick();
  if (!lookFree()) return;
  const me = bot.entity.position;
  const now = Date.now();
  const hitter = hitBy && now < hitBy.until ? playerEntity(hitBy.name) : null;
  if (hitter) {
    const towards = Math.atan2(-(hitter.position.x - me.x), -(hitter.position.z - me.z));
    bot.look(towards + Math.PI, 0).catch(() => {});
    return;
  }
  const speaker = talker(me);
  const person = speaker || companion(me)
    || bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username && e.position.distanceTo(me) <= PersonSight);
  if (!person) {
    gaze.person = null;
    if (now >= gaze.backAt || !lookAtGlance()) {
      gaze.away = glance(me, null);
      gaze.backAt = now + 4000 + Math.random() * 5000;
      lookAtGlance();
    }
    return;
  }
  if (gaze.person !== person.id) {
    gaze.person = person.id;
    gaze.backAt = 0;
    gaze.awayAt = now + dwell();
  }
  if (speaker) {
    // Listening: no glancing away, and a while longer on them once they've finished.
    gaze.backAt = 0;
    gaze.awayAt = Math.max(gaze.awayAt, now + 2000);
  } else if (now >= gaze.awayAt) {
    gaze.away = glance(me, person);
    gaze.backAt = now + 700 + Math.random() * 1800;
    gaze.awayAt = gaze.backAt + dwell();
  }
  if (now < gaze.backAt && lookAtGlance()) return;
  bot.lookAt(eyes(person)).catch(() => {});
}

// How long she keeps her eyes on someone before glancing away: shorter on guard.
function dwell() {
  return task && task.kind === 'guard' ? 1500 + Math.random() * 2500 : 2500 + Math.random() * 4500;
}

// The player she follows or guards, when in sight.
function companion(me) {
  if (!task || (task.kind !== 'follow' && task.kind !== 'guard')) return null;
  const entity = playerEntity(task.player);
  return entity && entity.position.distanceTo(me) <= 16 ? entity : null;
}

// Somewhere to look for a moment: another player close by, a creature, or a direction off to one side (now and then
// down at her feet or up at the sky). Away from `person`, never back at them.
function glance(me, person) {
  const near = (e, d) => e !== person && e !== bot.entity && e.isValid !== false && e.position.distanceTo(me) <= d;
  const roll = Math.random();
  if (roll < 0.35) {
    const other = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username && near(e, 12));
    if (other) return { entity: other };
  }
  if (roll < 0.7) {
    const creature = bot.nearestEntity((e) => Creatures.has(e.type) && near(e, person ? 12 : 16));
    if (creature) return { entity: creature };
  }
  const turn = (Math.random() < 0.5 ? -1 : 1) * (0.5 + Math.random() * (person ? 1.5 : 2.2));
  const r = Math.random();
  const pitch = r < 0.2 ? -(0.5 + Math.random() * 0.5) : r < 0.3 ? 0.2 + Math.random() * 0.4 : (Math.random() - 0.5) * 0.3;
  return { yaw: bot.entity.yaw + turn, pitch };
}

// Looks where the current glance goes; false when there's none or what she glanced at is gone.
function lookAtGlance() {
  const g = gaze.away;
  if (!g) return false;
  if (g.entity) {
    if (!g.entity.isValid) return false;
    bot.lookAt(eyes(g.entity)).catch(() => {});
  } else bot.look(g.yaw, g.pitch).catch(() => {});
  return true;
}

// Whoever chatted last, within TalkLook and 12 blocks.
function talker(me) {
  let best = null;
  let bestAt = 0;
  for (const [name, at] of lastChat) {
    if (Date.now() - at > TalkLook) {
      lastChat.delete(name);
      continue;
    }
    const entity = playerEntity(name);
    if (entity && at > bestAt && entity.position.distanceTo(me) <= 12) {
      best = entity;
      bestAt = at;
    }
  }
  return best;
}

// Away is out of sight or over 10 blocks off; back within 5 is walking up.
function greetTick() {
  if (greeting || swim || bot.isSleeping || eating || (task &&['defend', 'flee', 'guard', 'patrol'].includes(task.kind))) return;
  const me = bot.entity.position;
  for (const name of familyOrder) {
    const player = Object.values(bot.players).find((p) => p.username.toLowerCase() === name);
    const entity = player && player.entity;
    const d = entity ? entity.position.distanceTo(me) : Infinity;
    if (d > 10) {
      familyAway.set(name, true);
      continue;
    }
    if (d > 5 || familyAway.get(name) === false || Date.now() - (greetedAt.get(name) || 0) < GreetEvery) continue;
    familyAway.set(name, false);
    greetedAt.set(name, Date.now());
    crouchFor(entity).catch(() => {});
    return;
  }
}

// Two crouches; sitting, she stands up and back down instead.
async function crouchFor(entity) {
  greeting = true;
  try {
    await bot.lookAt(eyes(entity)).catch(() => {});
    for (let i = 0; i < 2; i++) {
      bot.setControlState('sneak', !sitting);
      await wait(250);
      bot.setControlState('sneak', sitting);
      await wait(250);
    }
  } finally {
    greeting = false;
  }
}

// ---- Water ---------------------------------------------------------------------------------------------

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
// From the boat's centre. The server takes an interaction within reach (3) plus 3 of the boat's box; she follows a
// player to 2 of *them*, so a boat moored a block or two out keeps her 4 off it: at 3 she stood on the bank and
// never got in (checked on the local server).
const BoardReach = 5;
const BoardRetry = 2000;
const BoardTries = 3;
const BoardGiveUp = 30000;

// What her own swimming is doing: null, a dive ({ mode: 'dive', phase: down|up }) or going up for air
// ({ mode: 'surface' }). The pathfinder is left alone by the second, which only takes the controls.
let swim = null;
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
function isDragColumn(block) {
  return Boolean(block && block.name === 'bubble_column' && block.getProperties().drag === true);
}

// Whether someone's eyes are under water: hers (she needs air), or theirs (they dived), or a drowned's (out of reach).
function headUnder(entity) {
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

// Every physics tick, after the pathfinder's own (registered on spawn, so it overrides it): air first, then a dive,
// then treading water, then shaking herself off on dry land.
function waterTick() {
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
  if (under && air() <= AirLow) {
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
  if (wet && air() >= AirDiveAgain && Date.now() >= diveAgainAt && bot.health > RetreatHealth) {
    const who = diveTarget();
    if (who) return startDive(who);
  }

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
  greeting = true;
  try {
    for (let i = 0; i < 3; i++) {
      bot.setControlState('sneak', true);
      await wait(120);
      bot.setControlState('sneak', false);
      await wait(120);
    }
  } finally {
    greeting = false;
  }
  emit('shook', {});
}

// Boats: two seats, one on a chest boat (the chest takes the back one); rafts the same.
function isBoat(entity) {
  const n = (entity && entity.name) || '';
  return n.endsWith('boat') || n.endsWith('raft');
}

function freeSeat(boat) {
  return (boat.passengers || []).length < ((boat.name || '').includes('chest') ? 1 : 2);
}

// The player she's with (following, or tagging along) sits in a boat with room for her.
function boardable(target) {
  const boat = target && target.vehicle;
  return Boolean(boat && isBoat(boat) && freeSeat(boat) && (boatSkip.get(boat.id) || 0) <= Date.now());
}

// Riding: whose boat, and for which task; the ride ends with either. Physics stops while she rides (Mineflayer's), so
// no physics tick runs: rideTick is a timer.
let riding = null;
let boarding = null;
let boardAt = 0;
const boatSkip = new Map();

// Twice a second on foot or swimming: close enough to their boat, she climbs in. Three refusals (someone took the seat,
// a plugin) and she leaves that boat alone for a while.
function boardTick() {
  if (Date.now() < boardAt || !bot.entity || bot.vehicle || bot.isSleeping || eating || swim) return;
  boardAt = Date.now() + 500;
  const t = task;
  if (!t || (t.kind !== 'follow' && t.kind !== 'tagAlong')) return;
  const who = t.kind === 'follow' ? t.player : t.who;
  const target = playerEntity(who);
  if (!boardable(target)) return;
  const boat = target.vehicle;
  if (boat.position.distanceTo(bot.entity.position) > BoardReach) return;
  if (!boarding || boarding.boat !== boat || boarding.task !== t) boarding = { boat, who, task: t, tries: 0 };
  if (++boarding.tries > BoardTries) {
    boatSkip.set(boat.id, Date.now() + BoardGiveUp);
    boarding = null;
    return;
  }
  boardAt = Date.now() + BoardRetry;
  bot.setControlState('sneak', false); // crouching, a click on a boat doesn't seat you
  bot.mount(boat);
}

function onMount() {
  const b = boarding;
  boarding = null;
  if (!b || bot.vehicle !== b.boat || task !== b.task) return;
  riding = { boat: b.boat, who: b.who, task: b.task };
  swim = null;
  treading = false;
  releaseSwim();
  emit('boarded', { player: b.who });
}

// Out when they get out (or the boat goes), or when what she was doing ends: any order gets her out, as from a bed.
function rideTick() {
  if (!bot.vehicle || !riding) return;
  const target = playerEntity(riding.who);
  if (task === riding.task && target && target.vehicle === bot.vehicle) return;
  leaveBoat();
}

// Out of a boat on the water, the server puts her on top of it, and Mineflayer's physics knows no entity to stand on:
// on her side she fell into the boat, the server put her back on it, again and again (never « on the ground », so she
// couldn't even jump), and she never walked off (local server). So while she's over it at its top she stands on it on
// her side too (its top is 0.5625 above it), and hops off: jump, and run away from its centre (towards whoever she's
// with, else straight out) until she's clear of its box. Last in the physics tick, over the pathfinder's controls.
const BoatTop = 0.5625;
let offBoat = null;

function offBoatTick() {
  const o = offBoat;
  if (!o || !bot.entity) return;
  const me = bot.entity.position;
  if (Date.now() > o.until || Math.hypot(me.x - o.from.x, me.z - o.from.z) > 1.3 || bot.vehicle) {
    offBoat = null;
    for (const control of ['jump', 'forward', 'sprint']) bot.setControlState(control, false);
    bot.jumpQueued = false;
    return;
  }
  const top = o.from.y + BoatTop;
  if (me.y < top + 0.05 && me.y > top - 0.3 && bot.entity.velocity.y <= 0) {
    me.y = top;
    bot.entity.velocity.y = 0;
    bot.entity.onGround = true;
  }
  const who = task && (task.player || task.who) ? playerEntity(task.player || task.who) : null;
  const towards = who ? who.position : me.offset(me.x - o.from.x || 1, 0, me.z - o.from.z);
  bot.look(Math.atan2(-(towards.x - o.from.x), -(towards.z - o.from.z)), 0, true).catch(() => {});
  bot.setControlState('forward', true);
  bot.setControlState('sprint', true);
  bot.setControlState('jump', true);
}

// Mineflayer's own dismount sends jump, which since 1.21.2 is no longer how a passenger gets out: shift is.
function leaveBoat() {
  if (!bot.vehicle) return;
  if (!bot.supportFeature('newPlayerInputPacket')) return bot.dismount();
  bot._client.write('player_input', { inputs: { shift: true } });
  setTimeout(() => {
    try {
      bot._client.write('player_input', { inputs: {} });
    } catch {
      // Gone meanwhile.
    }
  }, 150);
}

// ---- Getting her things back -------------------------------------------------------------------------

// She dies: where, in which dimension, and whether it was by lava (her things burn). Three seconds after respawning,
// she goes back for them (`retrieve`, ahead of the routine) unless they're in another dimension, over RetrieveFar
// away, in the void or in lava. An order comes first; the walk waits until DespawnAfter (when the items vanish). At the
// spot she sweeps the items within 6 blocks for RetrieveSweep. Armour goes back on by itself (playerCollect).
const RetrieveFar = 500;
const DespawnAfter = 300000;
const RetrieveSweep = 20000;
const RetrieveRadius = 8; // what a death scatters can land 6 blocks off and roll further
let deathAt = null;
let pendingRetrieve = null;

function minY() {
  return bot.game && Number.isFinite(bot.game.minY) ? bot.game.minY : 0;
}

function lavaNear(pos) {
  const lava = bot.registry.blocksByName.lava;
  return Boolean(lava && bot.findBlocks({ matching: lava.id, point: pos, maxDistance: 2, count: 1 }).length > 0);
}

function onDeath() {
  pendingRetrieve = null;
  if (!bot.entity) return;
  const pos = bot.entity.position.clone();
  deathAt = { pos, dimension: dimension(), at: Date.now(), lava: lavaNear(pos) };
}

function armRetrieve() {
  const d = deathAt;
  deathAt = null;
  if (!d || !bot.entity) return;
  // Still carrying things: the server keeps inventories (keepInventory), nothing was dropped.
  if (bot.inventory.items().length > 0 || wornArmour().length > 0) return;
  const here = bot.entity.position;
  const why = d.dimension !== dimension() ? 'otherDimension'
    : d.pos.y < minY() + 1 ? 'void'
    : d.lava ? 'lava'
    : Math.hypot(d.pos.x - here.x, d.pos.z - here.z) > RetrieveFar ? 'tooFar'
    : null;
  if (why) return emit('retrieveLost', { why });
  pendingRetrieve = { pos: d.pos, dimension: d.dimension, until: d.at + DespawnAfter, before: itemCounts(), announced: false };
}

function startRetrieve() {
  const r = pendingRetrieve;
  if (Date.now() > r.until || r.dimension !== dimension()) {
    pendingRetrieve = null;
    return emit('retrieveLost', { why: Date.now() > r.until ? 'despawned' : 'otherDimension' });
  }
  stay();
  task = { kind: 'retrieve', own: true, player: '', pos: r.pos, sweeping: false };
  if (!r.announced) {
    r.announced = true;
    emit('retrieving', {});
  }
  bot.pathfinder.setGoal(new goals.GoalNear(r.pos.x, r.pos.y, r.pos.z, 1));
}

function retrieveTick(t) {
  if (t.sweeping || !pendingRetrieve || Date.now() <= pendingRetrieve.until) return;
  stay();
  pendingRetrieve = null;
  emit('retrieveLost', { why: 'despawned' });
}

async function sweepRetrieve(mine) {
  mine.sweeping = true;
  await pickUpDrops(mine.pos, RetrieveRadius, RetrieveSweep, mine);
  if (task !== mine) return; // an order came first: the walk is still pending
  await wait(500); // the last pickup lands
  if (task !== mine) return;
  const before = pendingRetrieve ? pendingRetrieve.before : new Map();
  pendingRetrieve = null;
  task = null;
  const items = [...itemCounts()].reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
  emit('retrieved', { count: items + wornArmour().length });
}

// Where she can pick an item up from. A player picks up what touches their box (0.6 wide, 1.8 tall) grown by a block
// sideways and half a block up and down. GoalNear(item, 1) wanted her within a block of it, counted in whole blocks,
// which an item on a fence post, on a pillar's edge or under a slab too low to stand under never allows: the
// pathfinder found no way, or kept looking. Each item gets PickUpTry, then she leaves it. A node counts within
// PickUpReach of the item sideways: her box reaches 1.3 from where she stands, and leaning against what's in the way
// (pickUp) takes her 0.2 further. At 1.2, coal at the back of the hole its ore left in a quarry face had no node at all.
const PickUpTry = 8000;
const PickUpReach = 1.5;

class GoalPickUp extends goals.Goal {
  constructor(pos) {
    super();
    this.x = pos.x;
    this.y = pos.y;
    this.z = pos.z;
  }

  heuristic(node) {
    return Math.hypot(this.x - (node.x + 0.5), this.z - (node.z + 0.5)) + Math.abs(this.y - node.y);
  }

  isEnd(node) {
    const up = this.y - node.y;
    return Math.abs(this.x - (node.x + 0.5)) <= PickUpReach && Math.abs(this.z - (node.z + 0.5)) <= PickUpReach && up >= -0.4 && up <= 2.2;
  }
}

// Picks up the dropped items within `radius` of a spot (pickUp).
async function pickUpDrops(center, radius, timeout, mine) {
  return pickUp((e) => e.position.distanceTo(center) <= radius, timeout, mine);
}

// Walks to the dropped items `choose` accepts, the closest first, until none is left, the time is up or she's no
// longer doing `mine`. Each item gets a try of PickUpTry, and another (three in all) if it has rolled more than a
// block since: aimed at while still rolling, it was left lying beside where she went.
async function pickUp(choose, timeout, mine) {
  const end = Date.now() + timeout;
  const tried = new Map(); // item id -> { at, tries }
  const retry = (e) => {
    const t = tried.get(e.id);
    return !t || (t.tries < 3 && e.position.distanceTo(t.at) > 1);
  };
  while (task === mine && Date.now() < end) {
    const item = bot.nearestEntity((e) => e.name === 'item' && retry(e) && choose(e));
    if (!item) return;
    tried.set(item.id, { at: item.position.clone(), tries: (tried.get(item.id) || { tries: 0 }).tries + 1 });
    const at = item.position;
    try {
      await Promise.race([
        bot.pathfinder.goto(new GoalPickUp(at)),
        wait(Math.min(PickUpTry, Math.max(0, end - Date.now()))).then(() => { throw new Error('too slow'); }),
      ]);
      // In reach is not picked up: a fresh drop waits half a second before anyone can take it, and she had already
      // moved on to the next one, leaving it where it fell.
      for (let i = 0; i < 15 && bot.entities[item.id]; i++) await wait(100);
      // Still there, close: in a hole the ore left in a quarry face, a block up and at its back, just past the reach
      // of where she stood. She leans in against the face, as a player does, and it comes.
      if (bot.entities[item.id] && item.position.distanceTo(bot.entity.position) < 2.5 && task === mine) {
        await bot.lookAt(item.position, true);
        bot.setControlState('forward', true);
        for (let i = 0; i < 6 && bot.entities[item.id]; i++) await wait(100);
        bot.setControlState('forward', false);
      }
    } catch (err) {
      if (task === mine) bot.pathfinder.setGoal(null); // out of reach, or too slow: the next one
      if (process.env.MC_TRACE_PATHS === 'all') log(`pickUp: ${at} out of reach: ${err && err.message}`);
    }
    if (process.env.MC_TRACE_PATHS === 'all' && bot.entities[item.id]) log(`pickUp: ${item.position} still lying (she is at ${bot.entity.position.floored()})`);
    await wait(300);
  }
}

// ---- Loot from her kills ------------------------------------------------------------------------------

// Where something she was fighting (guarding, defending) or culling died, for LootFresh: what drops there is hers to
// pick up. Nothing else is: a player's dropped items stay where they are.
const LootFresh = 30000;
const lootSpots = [];

function pruneLoot() {
  while (lootSpots.length > 0 && Date.now() - lootSpots[0].at > LootFresh) lootSpots.shift();
}

// A dropped item near one of her kills, within 6 blocks of her and within `radius` of what she guards.
function lootNear(center, radius, skip) {
  pruneLoot();
  if (lootSpots.length === 0) return null;
  const me = bot.entity.position;
  return bot.nearestEntity((e) => e.name === 'item' && !(skip && skip.has(e.id)) && e.position.distanceTo(me) <= 6
    && e.position.distanceTo(center) <= radius && lootSpots.some((s) => s.pos.distanceTo(e.position) <= 3));
}

// ---- Creeper warnings ---------------------------------------------------------------------------------

// A creeper within 4 blocks of a player (within 32 of her), behind them (over 100° off where they look): she warns them,
// once per creeper. The player she guards is guardTick's.
const CreeperBehindAngle = Math.cos((100 * Math.PI) / 180);
const warnedCreepers = new Set();
let creeperWatchAt = 0;

function creeperWatchTick() {
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

// ---- Needs: worn tools, what she lacks -------------------------------------------------------------------

// Every NeedsEvery: a kit tool at WornAt of its durability or less is reported once (`wornOut`), and again only for a
// new one (its durability went back up); one reported worn that disappears (not handed over, not lost in a death)
// `broke`. And what she lacks (`lacking`), at most once a minute each: C# decides what's said.
const NeedsEvery = 10000;
const WornAt = 0.1;
const worn = new Map();
const lackingAt = new Map();
let handedAt = 0;
let diedAt = 0;

function lacking(what) {
  if ((lackingAt.get(what) || 0) > Date.now()) return;
  lackingAt.set(what, Date.now() + 60000);
  emit('lacking', { what });
}

function needsTick() {
  if (!bot.entity || !bot.isAlive) return;
  const items = bot.inventory.items();
  const handing = (task && (task.kind === 'give' || task.kind === 'store')) || Date.now() - handedAt < 15000 || Date.now() - diedAt < 30000;
  for (const [tool, item] of Object.entries(kitTools(items))) {
    const was = worn.get(tool);
    if (!item) {
      if (was === undefined) continue;
      worn.delete(tool);
      if (!handing) emit('broke', { tool });
      continue;
    }
    const max = bot.registry.items[item.type] && bot.registry.items[item.type].maxDurability;
    if (!max) continue;
    const left = max - (item.durabilityUsed || 0);
    if (was !== undefined && left > was) worn.delete(tool); // a new one
    if (left / max > WornAt) {
      worn.delete(tool);
      continue;
    }
    if (!worn.has(tool)) emit('wornOut', { tool });
    worn.set(tool, left);
  }
  if (task && (task.kind === 'guard' || task.kind === 'patrol') && items.some((i) => i.name === 'bow') && !arrows()) lacking('arrows');
  if (bot.inventory.emptySlotCount() === 0) lacking('space');
}

// ---- Animals: « syncs occupe-toi des animaux », and the routine's pen ------------------------------------

// Where she stands (or the pen's spot), every RanchRound: she breeds a species under HerdMax (adults and babies within
// FarmRadius), shears the sheep, culls the adults over HerdMax (CullPerRound at most), picks up the drops and stores
// them in the chest within ChestRadius. Only these species, by what they eat; never a named animal, never a baby, and
// no culling at all where the server's version doesn't say which are babies. Breeding food stays on her.
const Livestock = {
  cow: (n) => n === 'wheat',
  sheep: (n) => n === 'wheat',
  pig: (n) => n === 'carrot' || n === 'potato' || n === 'beetroot',
  chicken: (n) => n.endsWith('_seeds'),
  rabbit: (n) => n === 'carrot' || n === 'dandelion',
};
const HerdMax = 8;
const RanchRound = 15000;
const FedAgain = 300000;
const ShearAgain = 300000;
const CullPerRound = 2;
const KillFor = 15000;
const MeatKept = 8;
const Meats = new Set(['beef', 'porkchop', 'chicken', 'mutton', 'rabbit',
  'cooked_beef', 'cooked_porkchop', 'cooked_chicken', 'cooked_mutton', 'cooked_rabbit']);
const isRanchGoods = (n) => Meats.has(n) || n === 'leather' || n.endsWith('_wool') || n === 'feather' || n === 'rabbit_hide';

// An entity's metadata by name (minecraft-data's metadataKeys); undefined when the version doesn't name it.
function meta(entity, key) {
  const known = bot.registry.entitiesByName[entity.name];
  const i = known && known.metadataKeys ? known.metadataKeys.indexOf(key) : -1;
  return i >= 0 && entity.metadata ? entity.metadata[i] : undefined;
}

function knowsAge(entity) {
  const known = bot.registry.entitiesByName[entity.name];
  return Boolean(known && known.metadataKeys && known.metadataKeys.includes('baby'));
}

const isBaby = (e) => meta(e, 'baby') === true;
const isNamed = (e) => Boolean(meta(e, 'custom_name'));
const isShorn = (e) => (Number(meta(e, 'wool')) & 0x10) !== 0;

// The animals around a spot, by species: how many in all, and the adults.
function herdAround(anchor) {
  const herd = new Map();
  for (const e of Object.values(bot.entities)) {
    if (!Livestock[e.name] || e.isValid === false || e.position.distanceTo(anchor) > FarmRadius) continue;
    if (!herd.has(e.name)) herd.set(e.name, { all: 0, adults: [], aged: true });
    const h = herd.get(e.name);
    h.all++;
    if (!isBaby(e)) h.adults.push(e);
    if (!knowsAge(e)) h.aged = false;
  }
  return herd;
}

// Animals and a chest around a spot: { chest }, or { why }.
function penAt(anchor) {
  if (herdAround(anchor).size === 0) return { why: 'none' };
  const chest = chestNear(anchor);
  return chest ? { chest } : { why: 'noChest' };
}

function ranch(player) {
  const anchor = bot.entity.position.clone();
  const pen = penAt(anchor);
  if (pen.why) return emit('cantRanch', { player, why: pen.why });
  stay();
  task = { kind: 'ranch', player, anchor, chest: pen.chest, busy: false, fedAt: new Map(), shornAt: new Map(), target: null };
  emit('ranching', { player });
  ranchRound();
}

async function ranchRound() {
  if (!bot || !task || task.kind !== 'ranch' || task.busy) return;
  const mine = task;
  mine.busy = true;
  try {
    const herd = herdAround(mine.anchor);
    await breed(mine, herd);
    if (task === mine) await shear(mine, herd);
    if (task === mine) await cull(mine, herd);
    pruneLoot();
    for (const spot of lootSpots.filter((s) => s.pos.distanceTo(mine.anchor) <= FarmRadius + 4)) {
      if (task !== mine) break;
      // Drops roll off the spot, and a cull's three or four stacks don't all come in 10 s (3 beef were left lying).
      await pickUpDrops(spot.pos, 4, 20000, mine);
    }
    if (task === mine) await storeRanch(mine);
    if (task === mine && bot.entity.position.distanceTo(mine.anchor) > 4)
      await bot.pathfinder.goto(new goals.GoalNear(mine.anchor.x, mine.anchor.y, mine.anchor.z, 2));
  } catch (err) {
    log('ranch round failed:', err && err.message ? err.message : String(err));
  } finally {
    mine.busy = false;
  }
}

// Two adults of a species under HerdMax, not fed in FedAgain, get its food.
async function breed(mine, herd) {
  for (const [species, h] of herd) {
    if (task !== mine) return;
    if (h.all >= HerdMax) continue;
    const ready = h.adults.filter((e) => !isNamed(e) && Date.now() - (mine.fedAt.get(e.id) || 0) > FedAgain);
    if (ready.length < 2) continue;
    if (feedCount(species) < 2) await fetchFeed(mine, species);
    if (task !== mine) return;
    if (feedCount(species) < 2) {
      lacking('breedFood');
      continue;
    }
    for (const animal of ready.slice(0, 2)) {
      if (task !== mine) return;
      mine.fedAt.set(animal.id, Date.now());
      await useOn(animal, (n) => Livestock[species](n));
    }
  }
}

function feedCount(species) {
  return bot.inventory.items().filter((i) => Livestock[species](i.name)).reduce((sum, i) => sum + i.count, 0);
}

// Short of a species' food, she takes some from the pen's chests (within ChestRadius of it), FeedTaken at most: the
// wheat a farm stored is there. Chests that had none aren't opened again for FeedLookAgain, or every round would.
const FeedTaken = 16;
const FeedLookAgain = 120000;

async function fetchFeed(mine, species) {
  if (!mine.feedLookedAt) mine.feedLookedAt = new Map(); // a copy made to resume the pen may not have it
  if (Date.now() < (mine.feedLookedAt.get(species) || 0) + FeedLookAgain) return;
  mine.feedLookedAt.set(species, Date.now());
  for (const at of chestsNear(mine.anchor, 4)) {
    let want = FeedTaken - feedCount(species);
    if (task !== mine || want <= 0) return;
    const block = bot.blockAt(at);
    if (!block || (block.name !== 'chest' && block.name !== 'barrel')) continue;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
      if (task !== mine) return;
      const box = await bot.openContainer(block);
      try {
        const there = new Map();
        for (const i of box.containerItems())
          if (Livestock[species](i.name)) there.set(i.type, (there.get(i.type) || 0) + i.count);
        for (const [type, n] of there) {
          if (want <= 0) break;
          const take = Math.min(want, n);
          await box.withdraw(type, null, take);
          want -= take;
        }
      } finally {
        box.close();
      }
    } catch (err) {
      log('feed not taken:', err && err.message ? err.message : String(err));
    }
  }
}

async function shear(mine, herd) {
  const sheep = ((herd.get('sheep') || {}).adults || [])
    .filter((e) => !isNamed(e) && !isShorn(e) && Date.now() - (mine.shornAt.get(e.id) || 0) > ShearAgain);
  if (sheep.length === 0) return;
  if (!bot.inventory.items().some((i) => i.name === 'shears')) return lacking('shears');
  for (const animal of sheep) {
    if (task !== mine) return;
    mine.shornAt.set(animal.id, Date.now());
    await useOn(animal, (n) => n === 'shears');
    lootSpots.push({ pos: animal.position.clone(), at: Date.now() }); // the wool
  }
}

// The adults over HerdMax, CullPerRound at most, never a named one.
async function cull(mine, herd) {
  let left = CullPerRound;
  for (const [, h] of herd) {
    if (!h.aged) continue; // can't tell a baby from an adult here: never
    let surplus = h.adults.length - HerdMax;
    const victims = h.adults.filter((e) => !isNamed(e));
    // The closest first: in a crowded pen the far ones are behind the others, and she spent the round on one.
    while (surplus > 0 && left > 0 && victims.length > 0 && task === mine) {
      victims.sort((a, b) => a.position.distanceTo(bot.entity.position) - b.position.distanceTo(bot.entity.position));
      if (await slaughter(mine, victims.shift())) {
        surplus--;
        left--;
      }
    }
  }
}

// The same hits as defending: chase, and hit on the weapon's cooldown, until it's dead or KillFor has passed. One she
// can't get a first hit on within ReachFor (the others in the way) is left for the next. Whether it died.
const ReachFor = 6000;

async function slaughter(mine, animal) {
  mine.target = animal;
  await equipWeapon();
  bot.pathfinder.setGoal(new goals.GoalFollow(animal, 2), true);
  const start = Date.now();
  let hit = false;
  while (task === mine && animal.isValid !== false && bot.entities[animal.id] && Date.now() < start + KillFor) {
    if (!hit && Date.now() > start + ReachFor) break;
    if (strike(animal)) hit = true;
    await wait(100);
  }
  if (task === mine) {
    bot.pathfinder.setGoal(null);
    mine.target = null;
  }
  return animal.isValid === false || !bot.entities[animal.id];
}

// Walks up to an animal and uses what `matches` on it (food, shears).
async function useOn(animal, matches) {
  const at = animal.position;
  try {
    await Promise.race([
      bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2)),
      wait(10000).then(() => { throw new Error('too slow'); }),
    ]);
    const item = bot.inventory.items().find((i) => matches(i.name));
    if (!item || !bot.entities[animal.id]) return;
    await bot.equip(item, 'hand');
    await bot.lookAt(animal.position.offset(0, (animal.height || 1) * 0.5, 0), true);
    await bot.activateEntity(animal);
  } catch (err) {
    log('animal out of reach:', err && err.message ? err.message : String(err));
    bot.pathfinder.setGoal(null);
  }
}

// Meat, leather, wool and feathers into the chest; MeatKept pieces of meat (cooked first) stay on her to eat.
async function storeRanch(mine) {
  const totals = new Map();
  for (const item of bot.inventory.items())
    if (isRanchGoods(item.name)) totals.set(item.name, (totals.get(item.name) || 0) + item.count);
  let meatLeft = MeatKept;
  const pairs = [...totals].sort(([a], [b]) => Number(b.startsWith('cooked_')) - Number(a.startsWith('cooked_'))).map(([name, n]) => {
    if (!Meats.has(name)) return [name, n];
    const kept = Math.min(meatLeft, n);
    meatLeft -= kept;
    return [name, n - kept];
  });
  await storeIn(mine, pairs);
}

function attach() {
  bot.loadPlugin(pathfinder);
  bot.loadPlugin(collectBlock);

  bot.once('spawn', () => {
    const states = (b) => new Set(Array.from({ length: b.maxStateId - b.minStateId + 1 }, (_, i) => b.minStateId + i));
    farmlandStates = states(bot.registry.blocksByName.farmland);
    waterStates = states(bot.registry.blocksByName.water);
    const statesOf = (names) => new Set(names.filter((n) => bot.registry.blocksByName[n])
      .flatMap((n) => [...states(bot.registry.blocksByName[n])]));
    burningStates = statesOf(['magma_block', 'campfire', 'soul_campfire']);
    prickly = statesOf(['sweet_berry_bush', 'cactus', 'wither_rose']);
    lavaStates = statesOf(['lava']);
    airStates = statesOf(['air', 'cave_air', 'void_air']);
    // A hair wider than a player (0.6). Mineflayer's collisions leave her side exactly on a block's face (a step she's
    // jumping up, a wall she walks along), and a 26.3 server takes that as walking into the block: it puts her back,
    // every tick, and she never climbs a one-block step. 0.0001 off the face is enough (checked on a local Paper 26.3
    // with ViaBackwards) and fits every gap a player fits.
    bot.physics.playerHalfWidth = 0.3001;
    bot.pathfinder.goto = goto; // collectblock calls it too
    bot.tool.equipForBlock = toolOrHand(bot.tool.equipForBlock.bind(bot.tool));
    const slower = Number(process.env.MC_CPU_FACTOR);
    if (slower > 1) {
      bot.pathfinder.thinkTimeout /= slower;
      bot.pathfinder.tickTimeout /= slower;
    }
    bot.pathfinder.setMovements(gentleMovements());
    // After the pathfinder's own tick (its plugin is injected later than attach() runs), so her swimming can take the
    // controls from it.
    bot.on('physicsTick', safely(waterTick));
    bot.on('physicsTick', safely(boardTick));
    // After the pathfinder too: it presses jump, this lets go of it over a field.
    bot.on('physicsTick', safely(farmlandTick));
    bot.on('physicsTick', safely(offBoatTick));
    // First, before any tick handler changes the controls: the keys this tick's move was made with.
    bot.prependListener('physicsTick', safely(inputTick));
    emit('spawned', {});
    reportPlayers();
    if (!bare) wearBestArmor().catch(() => {});
    // Not in attach() itself: Mineflayer creates bot.inventory with its plugins, after createBot returns.
    bot.inventory.on('updateSlot', scheduleInventory);
    scheduleInventory();
  });
  // Every spawn, the first and each respawn: her things to get back after a death.
  bot.on('spawn', () => {
    if (deathAt) setTimeout(safely(armRetrieve), 3000);
  });

  bot.on('physicsTick', () => {
    guardTick();
    fleeTick();
    defendTick();
  });
  bot.on('physicsTick', safely(threatTick));
  bot.on('physicsTick', safely(fightTick));
  bot.on('physicsTick', guideTick);
  bot.on('physicsTick', safely(portalTick));
  bot.on('physicsTick', safely(creeperTick));
  bot.on('physicsTick', safely(dodgeTick));
  bot.on('physicsTick', safely(backTick));
  bot.on('physicsTick', doorTick);
  bot.on('physicsTick', safely(freeTick));
  bot.on('physicsTick', safely(idleTick));
  bot.on('physicsTick', safely(autonomyTick));
  bot.on('physicsTick', safely(creeperWatchTick));
  setInterval(() => { if (bot) ranchRound().catch(() => {}); }, RanchRound);
  setInterval(() => { if (bot) safely(needsTick)(); }, NeedsEvery);
  setInterval(() => { if (bot) safely(rideTick)(); }, 500);
  bot.on('mount', safely(onMount));
  bot.on('dismount', (vehicle) => {
    if (vehicle && isBoat(vehicle)) offBoat = { from: vehicle.position.clone(), until: Date.now() + 4000 };
  });
  // Mineflayer only updates the passengers a vehicle still has: whoever got out kept pointing at it until their entity
  // was reloaded, so she stayed in a boat Papa had left, and once she got out herself she still « rode » it (her
  // physics stays off while she rides). Whoever the new list leaves out is out.
  bot._client.on('set_passengers', ({ entityId, passengers }) => {
    const vehicle = bot.entities[entityId];
    if (!vehicle) return;
    for (const e of Object.values(bot.entities)) {
      if (e.vehicle !== vehicle || passengers.includes(e.id)) continue;
      e.vehicle = null;
      if (vehicle.passengers) vehicle.passengers = vehicle.passengers.filter((p) => p !== e);
    }
    if (bot.vehicle === vehicle && bot.entity && !passengers.includes(bot.entity.id)) {
      bot.vehicle = null;
      bot.emit('dismount', vehicle);
    }
  });
  bot.on('dismount', () => { riding = null; });
  // The path she's walking, for the doors on it; closing behind her needs none.
  bot.on('path_update', (result) => setPath(result.status === 'noPath' ? [] : result.path));
  if (process.env.MC_TRACE_PATHS === '1' || process.env.MC_TRACE_PATHS === 'all')
    bot.on('path_update', (r) => {
      if (process.env.MC_TRACE_PATHS === 'all')
        log(`path ${r.status}: ${r.path.map((n) => `${n.x.toFixed(1)},${n.y},${n.z.toFixed(1)}`).join(' ')}`);
      else if (r.status !== 'success' || r.time > 200) log(`path ${r.status} in ${Math.round(r.time)} ms, ${r.visitedNodes} nodes visited`);
    });
  bot.on('goal_reached', () => setPath([]));
  bot.on('path_stop', () => setPath([]));
  setInterval(() => { if (bot) farmRound().catch(() => {}); }, FarmRound);
  bot.on('wake', wakeUp);
  bot.on('health', () => {
    eatIfHungry().catch(() => {});
    reportHunger();
    healTick();
  });
  // Her bar doesn't move while she waits to heal: the tick tries again after a dig or a fight.
  setInterval(() => { if (bot && bot.entity) safely(healTick)(); }, HealEvery);
  bot.on('heldItemChanged', scheduleInventory);
  bot.on('playerCollect', (collector, collected) => {
    if (collector !== bot.entity) return;
    // Read now: the item entity is gone once it's in her pockets.
    const item = collected && typeof collected.getDroppedItem === 'function' ? collected.getDroppedItem() : null;
    const gift = Boolean(item && Goods.armour(item.name) && item.name !== 'elytra');
    setTimeout(() => {
      dressAfterPickup(gift).catch(() => {});
      checkFed();
    }, 300);
  });

  // A monster she was fighting (or shooting) died: one less, and back to the player or the spot on the next tick. What
  // she was fighting or culling leaves a loot spot.
  bot.on('entityDead', (entity) => {
    if (!task) return;
    const fought = task.foe === entity || task.modeTarget === entity || task.target === entity;
    if (!fought || !['guard', 'patrol', 'defend', 'ranch'].includes(task.kind)) return;
    lootSpots.push({ pos: entity.position.clone(), at: Date.now() });
    if (task.kind !== 'guard' && task.kind !== 'patrol') return;
    emit('killed', { player: task.player });
    task.foe = null;
    task.mode = null;
    task.modeTarget = null;
  });

  // A player went to bed at night: she follows suit, once per night.
  bot.on('entitySleep', (entity) => {
    if (entity === bot.entity || entity.type !== 'player' || !isNight() || sleptOnDay === bot.time.day) return;
    sleep(entity.username).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
  });

  bot.on('goal_reached', () => {
    // What sets a new goal waits a beat: the pathfinder clears its goal once this event returns, so a goal (or a goto)
    // started right here would be dropped.
    const t = task;
    const later = (fn) => setTimeout(safely(fn), 0);
    if (t && t.kind === 'seek') return later(function seekGoal() { if (task === t) seekReached(); });
    if (t && t.kind === 'trip') return later(function tripGoal() { arriveOwn(t); });
    if (t && t.kind === 'retrieve' && !t.sweeping) {
      t.sweeping = true;
      return later(function retrieveGoal() {
        if (task === t) sweepRetrieve(t).catch((err) => log('retrieve failed:', err && err.message ? err.message : String(err)));
      });
    }
    if (!task || (task.kind !== 'come' && task.kind !== 'goto' && task.kind !== 'guide')) return;
    emit('arrived', { player: task.player, kind: task.kind });
    task = null;
  });

  // No way there. Coming and going give up at once; following and guarding keep trying (the player
  // may walk somewhere reachable) but say so only once per order. Gathering and sleeping handle their
  // own paths.
  bot.on('path_update', (result) => {
    climbFromTheFoot(result.path);
    if (result.status === 'timeout') timedOut = { goal: bot.pathfinder.goal, from: bot.entity.position.clone() };
    if (result.status === 'noPath' || (result.status === 'timeout' && stalledSearch(result))) noWay();
  });
  setInterval(() => { if (bot && bot.entity) safely(timeoutTick)(); }, 500);

  // The player she follows or guards went out of sight (too far, or logged out): she stops there.
  bot.on('entityGone', (entity) => {
    reportSeen(entity);
    warnedCreepers.delete(entity.id);
    if (task && task.kind === 'tagAlong' && entity.username === task.who) return stay(); // silently
    if (!task || (task.kind !== 'follow' && task.kind !== 'guard') || entity.username !== task.player) return;
    if (crossAfter(entity)) return; // through a portal: she goes after them
    emit('cantReach', { player: task.player, why: 'lost' });
    stay();
  });

  // The player she's seeking came into sight on her way to the remembered spot: she heads for them.
  bot.on('entitySpawn', (entity) => {
    if (!task || task.kind !== 'seek' || entity.type !== 'player' || entity.username !== task.player) return;
    const at = entity.position;
    bot.pathfinder.setGoal(new goals.GoalNear(at.x, at.y, at.z, 2));
  });

  bot.on('playerJoined', reportPlayers);
  bot.on('playerLeft', reportPlayers);

  bot.on('chat', (player, text) => {
    if (player === bot.username) return;
    lastChat.set(player, Date.now());
    emit('chat', { player, text, private: false });
  });
  bot.on('whisper', (player, text) => {
    if (player === bot.username) return;
    lastChat.set(player, Date.now());
    emit('chat', { player, text, private: true });
  });

  // Deaths and advancements arrive as translated system messages. Only those two families are
  // forwarded: the rest (join/leave notices, command feedback) is noise the C# side doesn't use.
  bot.on('message', (message) => {
    const key = message.translate;
    if (typeof key !== 'string') return;
    if (!key.startsWith('death.') && !key.startsWith('chat.type.advancement.')) return;
    emit('system', { key, args: (message.with ?? []).map(part) });
  });

  // Someone got hurt. Her: who did it (a player, or null for a mob, a fall, fire), which C# turns into words. One of
  // her family: a player is reported, a monster she deals with herself (Defending her family).
  bot.on('entityHurt', (entity, source) => {
    if (!entity) return;
    const exact = exactHits();
    if (entity === bot.entity) {
      // Any player, Papa included: she turns her back on them for a while (body language).
      if (exact && source && source.type === 'player' && source.username !== bot.username)
        hitBy = { name: source.username, until: Date.now() + HitSulk };
      // Anything else that hurts her is dealt with, whatever she's doing: fought back, or only got away from.
      if (exact && source && source.type !== 'player' && source !== bot.entity && source.name !== 'creeper') {
        const answer = answerTo(source.name, { hostile: isFoe(source), tamed: Boolean(meta(source, 'owneruuid')) });
        selfDefend(source, answer);
      }
      if (exact) return emit('hurt',{ by: source && source.type === 'player' && source.username !== bot.username ? source.username : null });
      // No source before 1.19.4: the closest player within reach is a fair guess, and nobody in reach means a mob,
      // a fall or fire.
      const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
        && e.position.distanceTo(bot.entity.position) < 4.5);
      return emit('hurt', { by: near ? near.username : null });
    }
    if (!exact || !source || source === bot.entity || entity.type !== 'player') return;
    if (source.type === 'player') {
      if (isFamily(entity.username) && !isFamily(source.username)) emit('familyHurt', { victim: entity.username, by: source.username });
    } else if (isFoe(source) && entity.position.distanceTo(bot.entity.position) <= DefendRadius) {
      defend(source, entity.username, false);
    }
  });

  bot.on('death', () => {
    diedAt = Date.now();
    if (task && task.kind === 'portal') stay(); // she respawns in the overworld: no crossing
    swim = null;
    drowning = false;
    wetSince = null;
    riding = null;
    boarding = null;
    onDeath();
    emit('died', {});
  });

  // Nightfall, once per night: the moment the server's clock crosses from day into night.
  let wasDay = null;
  bot.on('time', () => {
    const isDay = bot.time.isDay;
    if (wasDay === true && isDay === false) emit('night', {});
    wasDay = isDay;
    autoSleepTick();
  });

  bot.on('kicked', (reason) => emit('kicked', { reason: plainReason(reason) }));
  bot.on('error', (err) => log('error:', err && err.message ? err.message : String(err)));
  bot.on('end', (reason) => finish(reason));
}

// A kick reason arrives as a chat component (JSON text or NBT, by version): rendered to plain text
// for the owner's /debug minecraft, raw if it won't render.
function plainReason(reason) {
  let value = reason;
  if (typeof value === 'string') {
    try {
      value = JSON.parse(value);
    } catch {
      return value;
    }
  }
  try {
    const ChatMessage = require('prismarine-chat')(bot.registry);
    return ChatMessage.fromNotch(value).toString();
  } catch {
    return JSON.stringify(value);
  }
}

// Which version to speak. Mineflayer can ask the server itself (version: false), but it then refuses
// a server newer than it knows, and a new Minecraft release takes Mineflayer weeks to follow. So: the
// server's own version when it's supported, otherwise the newest one that is, which a server running
// ViaBackwards accepts (one without it refuses her with an "outdated client" kick, shown in /debug).
async function pickVersion() {
  let status;
  try {
    status = await mc.ping({ host, port });
  } catch (err) {
    log('version ping failed, letting Mineflayer ask:', err && err.message ? err.message : String(err));
    return false;
  }
  const protocol = status && status.version ? status.version.protocol : null;
  const serverName = status && status.version ? String(status.version.name) : '';
  const supported = mc.supportedVersions.map((v) => minecraftData(v)).filter(Boolean);
  const exact = supported.find((d) => d.version.version === protocol);
  if (exact) {
    emit('version', { server: serverName, using: exact.version.minecraftVersion, fallback: false });
    return exact.version.minecraftVersion;
  }
  const newest = supported.reduce((a, b) => (b.version.version > a.version.version ? b : a));
  log(`server speaks ${serverName} (protocol ${protocol}), which Mineflayer doesn't: connecting as ${newest.version.minecraftVersion}`);
  emit('version', { server: serverName, using: newest.version.minecraftVersion, fallback: true });
  return newest.version.minecraftVersion;
}

// Since 26.3 the server allows one position packet per client tick and only resets that count when
// the client sends `tick_end`, which real clients have sent after every tick since 1.21.2. Mineflayer
// never sends it, so a 26.3 server (ViaBackwards included) kicks her for « invalid player movement »
// on her second move, about 1.5 s after joining. The workaround from PrismarineJS/mineflayer#4189:
// a `tick_end` after each movement packet. A physics tick that sent no move (she stood still) gets its
// `tick_end` at the start of the next one (inputTick), so the server counts every tick as a real client's.
// Drop it once Mineflayer sends the packet itself.
const MovePackets = new Set(['position', 'position_look', 'look', 'flying']);
let tickOpen = false; // a physics tick whose tick_end hasn't gone out yet
let endTick = () => {};

function sendTickEnds(client) {
  let supported = null; // known once the version is (it can be settled by Mineflayer's own ping)
  const write = client.write.bind(client);
  endTick = () => {
    tickOpen = false;
    if (supported === null) {
      const data = minecraftData(client.version);
      supported = Boolean(data && data.protocol.play.toServer.types.packet_tick_end);
    }
    if (!supported) return;
    try {
      write('tick_end', {});
    } catch (err) {
      log('tick_end failed:', err && err.message ? err.message : String(err));
    }
  };
  client.write = (name, params) => {
    const result = write(name, params);
    if (MovePackets.has(name)) endTick();
    return result;
  };
}

// Since 1.21.2 a client tells the server which keys it holds (`player_input`) whenever they change, in the tick that
// uses them, before that tick's move. Mineflayer sends that packet only for sneaking, with every other key off. So her
// keys go out as a real client's would: the ones this tick's move was made with, which is why inputTick runs first in
// the physics tick (Mineflayer moves her, then runs the tick handlers, which press and release keys for the next tick,
// then sends the move; keys read after the handlers arrive a tick early). Not while riding (leaveBoat holds shift itself), and anything else that writes the packet (Mineflayer's sneak)
// counts as the last one sent, so the full set follows on the next tick. The same wrapper keeps her last move sent,
// for the corrections' log.
let inputsSent = '';
let lastMoveSent = null;
// Packets sent this second, by name: over PacketsLoud in a second, the busiest are logged (every 30 s at most). Paper
// kicks a client for « too many packets » near 500 a second; a normal second is under 60, and the one loop found so far
// (calmTeleports) ran at 480. The line names the next one before the kick does.
const PacketsLoud = 200;
let packetsThisSecond = {};
let packetsLoggedAt = 0;
setInterval(() => {
  const counts = Object.entries(packetsThisSecond);
  packetsThisSecond = {};
  const total = counts.reduce((sum, [, n]) => sum + n, 0);
  if (total <= PacketsLoud || Date.now() < packetsLoggedAt + 30000) return;
  packetsLoggedAt = Date.now();
  const top = counts.sort((a, b) => b[1] - a[1]).slice(0, 4).map(([name, n]) => `${name} ${n}`).join(', ');
  log(`sending ${total} packets a second (${top})`);
}, 1000);

function trackInputs(client) {
  const write = client.write.bind(client);
  client.write = (name, params) => {
    packetsThisSecond[name] = (packetsThisSecond[name] || 0) + 1;
    if (name === 'player_input') inputsSent = JSON.stringify((params && params.inputs) || {});
    if (name === 'position' || name === 'position_look') lastMoveSent = { name, x: params.x, y: params.y, z: params.z, onGround: params.onGround };
    return write(name, params);
  };
}

function inputTick() {
  if (tickOpen) endTick();
  tickOpen = true;
  if (!bot.supportFeature('newPlayerInputPacket') || bot.vehicle) return;
  const c = bot.controlState;
  const inputs = { forward: c.forward, backward: c.back, left: c.left, right: c.right, jump: c.jump, shift: c.sneak, sprint: c.sprint };
  const key = JSON.stringify(inputs);
  if (key === inputsSent) return;
  bot._client.write('player_input', { inputs });
}

// The server putting her somewhere while she walks: a move it refused. Logged at most every 30 s, with what the server
// sent, where she was and what she held just before, her last move sent, and the block ahead, since a body stuck
// against the terrain looks the same from outside whatever the server objects to. This is how the 0.0001 off a block's
// face (playerHalfWidth) was found: hundreds a minute, each one 0.01 short of where she stood.
// MC_TRACE_MOVES (the movement tests in tools/mc-pathtest; never set by the service) logs every one.
const PulledBackLogEvery = process.env.MC_TRACE_MOVES === '1' ? 0 : 30000;
let pulledBack = 0;
let pulledBackRecent = 0;
let pulledBackLoggedAt = 0;

const fmt = (v) => (v && Number.isFinite(v.x) ? `${v.x.toFixed(2)} ${v.y.toFixed(2)} ${v.z.toFixed(2)}` : '?');

function watchCorrections(client) {
  // Before Mineflayer's own handler, which moves her: her position here is still where she thought she was.
  client.prependListener('position', (packet) => {
    try {
      if (!bot || !bot.entity || !bot.pathfinder || !bot.pathfinder.isMoving()) return;
      pulledBack++;
      pulledBackRecent++;
      if (Date.now() < pulledBackLoggedAt + PulledBackLogEvery) return;
      pulledBackLoggedAt = Date.now();
      const e = bot.entity;
      const c = bot.controlState;
      const keys = ['forward', 'back', 'left', 'right', 'jump', 'sprint', 'sneak'].filter((k) => c[k]).join('+') || 'none';
      const ahead = e.position.offset(-Math.sin(e.yaw), 0, -Math.cos(e.yaw)).floored();
      const at = (dy) => (bot.blockAt(ahead.offset(0, dy, 0)) || { name: '?' }).name;
      const sent = lastMoveSent ? `${lastMoveSent.name} ${fmt(lastMoveSent)} onGround ${lastMoveSent.onGround}` : 'none';
      // Every block her box touches at her feet, as her client knows them: what the server collided with that she didn't.
      const touched = [];
      for (const dx of [-0.3, 0.3])
        for (const dz of [-0.3, 0.3]) {
          const b = bot.blockAt(e.position.offset(dx, 0, dz).floored());
          const tag = b ? `${b.position.x},${b.position.y},${b.position.z} ${b.name}${b.shapes.length ? '' : '(open)'}` : 'unloaded';
          if (!touched.includes(tag)) touched.push(tag);
        }
      log(`server teleport while walking: ${pulledBackRecent} since the last report (${pulledBack} in all).` +
        ` To ${fmt(packet)} flags ${JSON.stringify(packet.flags)} id ${packet.teleportId};` +
        ` she was at ${fmt(e.position)} vel ${fmt(e.velocity)} onGround ${e.onGround} keys ${keys};` +
        ` last move sent: ${sent}; ahead: ${at(0)} / ${at(1)}; at her feet: ${touched.join(' | ')}`);
      pulledBackRecent = 0;
    } catch (err) {
      log('correction log failed:', err && err.message ? err.message : String(err));
    }
  });
}

// 1.21.6 took sneaking out of `entity_action` (it went to `player_input`) and renumbered the rest, but Mineflayer 4.39
// still sends the old numbers: on 26.1 its « start sprinting » (3) reaches the server as « start riding jump », and
// getting out of bed (2) as « stop sprinting ». So the server never knew she sprinted while her physics ran her at
// sprint speed, and leaving a bed never reached it. The old numbers go out as the names, on versions with the new order.
// Remove it once Mineflayer sends names there itself (check physics.js and bed.js before bumping it).
const OldEntityActions = ['start_sneaking', 'stop_sneaking', 'stop_sleeping', 'start_sprinting', 'stop_sprinting',
  'start_riding_jump', 'stop_riding_jump', 'open_inventory', 'start_fall_flying'];

function fixEntityActions(client) {
  let renumbered = null; // known once the version is
  const write = client.write.bind(client);
  client.write = (name, params) => {
    if (name === 'entity_action' && params && typeof params.actionId === 'number') {
      if (renumbered === null) {
        const data = minecraftData(client.version);
        const type = data && data.protocol.play.toServer.types.packet_entity_action;
        const action = type && type[1].find((f) => f.name === 'actionId');
        renumbered = Boolean(action && Array.isArray(action.type) && action.type[1].mappings['0'] === 'stop_sleeping');
      }
      if (renumbered) {
        const action = OldEntityActions[params.actionId];
        // Sneaking goes through player_input on these versions: Mineflayer never sends it here, and nor do we.
        if (!action || action.endsWith('_sneaking')) return undefined;
        return write(name, { ...params, actionId: action });
      }
    }
    return write(name, params);
  };
}

// Lying down in a bed, the server puts her in it (a teleport), Mineflayer answers every teleport with a confirm and a move
// to the same spot, and Paper, which still has her where she stood that tick, two blocks off, takes the move for a
// sleeper wandering and puts her back: 230 of each a second until the tick ended, and once a kick for « too many
// packets » (local server). The confirm always goes; the move to the spot she was just put in goes once, and again
// only 250 ms later.
function calmTeleports(client) {
  let target = null; // where the server last put her
  let echoedAt = 0;
  client.prependListener('position', (packet) => { target = packet; });
  const write = client.write.bind(client);
  client.write = (name, params) => {
    if (name === 'position_look' && target && params.x === target.x && params.y === target.y && params.z === target.z) {
      if (Date.now() - echoedAt < 250) return undefined;
      echoedAt = Date.now();
    }
    return write(name, params);
  };
}

// The server places her (a correction, a teleport) with its own 0.3 half-width, so often flush against a block's face;
// her box is 0.3001 wide (playerHalfWidth), so it starts 0.0001 inside that block, and Mineflayer's collisions only stop
// a box that starts outside one. She walked straight into the block, was put back flush, walked in again: 364 times in
// 20 s against a dark forest step. After each placement, once Mineflayer's own handler has run, she's moved out of any face
// she overlaps by less than a hair, back to the 0.0001 clear of it that the wider box is for.
const Hair = 0.001;

function clearFaces(client) {
  // A microtask: after every listener of this packet, Mineflayer's (registered later, with its plugins) included.
  client.on('position', () => queueMicrotask(() => {
    try {
      if (!bot || !bot.entity) return;
      const p = bot.entity.position;
      const w = bot.physics.playerHalfWidth;
      const box = () => ({ x0: p.x - w, x1: p.x + w, y0: p.y, y1: p.y + 1.8, z0: p.z - w, z1: p.z + w });
      for (let x = Math.floor(p.x - w); x <= Math.floor(p.x + w); x++)
        for (let y = Math.floor(p.y); y <= Math.floor(p.y + 1.8); y++)
          for (let z = Math.floor(p.z - w); z <= Math.floor(p.z + w); z++) {
            const block = bot.blockAt(new Vec3(x, y, z));
            if (!block) continue;
            for (const [sx0, sy0, sz0, sx1, sy1, sz1] of block.shapes) {
              const b = box();
              const ox = Math.min(b.x1, x + sx1) - Math.max(b.x0, x + sx0);
              const oy = Math.min(b.y1, y + sy1) - Math.max(b.y0, y + sy0);
              const oz = Math.min(b.z1, z + sz1) - Math.max(b.z0, z + sz0);
              if (ox <= 0 || oy <= 0 || oz <= 0) continue;
              if (ox < Hair) p.x += (x + sx0 > p.x ? -1 : 1) * (ox + 1e-6);
              else if (oz < Hair) p.z += (z + sz0 > p.z ? -1 : 1) * (oz + 1e-6);
            }
          }
    } catch (err) {
      log('clearing faces failed:', err && err.message ? err.message : String(err));
    }
  }));
}

async function main() {
  const version = await pickVersion();
  if (ended) return;
  // Offline mode: no auth.
  bot = mineflayer.createBot({ host, port, username, auth: 'offline', version });
  watchRecipes(bot._client);
  sendTickEnds(bot._client);
  calmTeleports(bot._client);
  clearFaces(bot._client);
  fixEntityActions(bot._client);
  trackInputs(bot._client);
  watchCorrections(bot._client);
  attach();
}


// The goal she was walking to can't be reached (noPath, or a search with nothing more to gain): what that means for
// what she was doing.
function noWay() {
  timedOut = null;
  if (!task) return;
  if (task.mode === 'loot') return task.skipLoot.add(task.modeTarget.id); // out of reach: left where it is
  // A monster she can't get to (down a ravine, across a gap): left for a while, not her guard given up.
  if (task.mode === 'fight' && task.modeTarget && ['guard', 'patrol', 'fight'].includes(task.kind)) return setUnreachable(task, task.modeTarget);
  if (task.reported) return;
  // Following someone who dived: no path under water, and the dive (waterTick) goes after them instead.
  if (FollowsSomeone.has(task.kind) && headUnder(playerEntity(task.player))) return;
  if (['collect', 'sleep', 'harvest', 'farm', 'give', 'store', 'fish', 'smelt', 'patrol', 'forage', 'ranch', 'tagAlong', 'home', 'back']
    .includes(task.kind)) return;
  // Her own: a place she can't walk to is left out for a while; her things, given up.
  if (task.kind === 'trip') {
    const what = task.what;
    stay();
    return failSpot(what, 'no path');
  }
  if (task.kind === 'retrieve') {
    // Sweeping, it's one item out of reach, not the trip: pickUp gives each one its own time and moves on.
    if (task.sweeping) return;
    stay();
    pendingRetrieve = null;
    return emit('retrieveLost', { why: 'noPath' });
  }
  if (task.kind === 'seek') {
    emit('seek', { player: task.player, outcome: 'missing' });
    return stay();
  }
  if (task.kind === 'flee') {
    task.reported = true;
    return emit('cornered', { player: task.player });
  }
  if (task.kind === 'defend') return endDefend(task); // can't get at them: back to what she was doing
  task.reported = true;
  emit('cantReach', { player: task.player, why: 'noPath' });
  if (task.kind === 'come' || task.kind === 'goto' || task.kind === 'guide') stay();
}

// A search that ran out of time (thinkTimeout) and whose best point is where she already stands: nothing more to gain,
// so it counts as « no way ». A goal out of reach, which a fast machine on a small world ends with noPath, runs out of
// time instead on a Pi's budget (MC_CPU_FACTOR 3.5 on the local server) or in a big loaded world. A long trip's
// partial paths still go somewhere, so they go on (timeoutTick).
function stalledSearch(result) {
  const path = result.path;
  if (!path || path.length === 0 || !bot.entity) return true;
  const end = path[path.length - 1];
  return bot.entity.position.distanceTo(new Vec3(end.x, end.y, end.z)) < 1.5;
}

// And once she has walked a timed-out search's partial path, the pathfinder doesn't search again: it only does when a
// path is reset, so she stood there with her goal set, silent, for good. Twice a second: walked it and got somewhere,
// a new search from here; got nowhere, no way.
let timedOut = null; // { goal, from }: the last search for `goal` ran out of time, started from `from`
const isDynamic = (goal) => goal instanceof goals.GoalFollow || goal instanceof goals.GoalInvert;

function timeoutTick() {
  const t = timedOut;
  if (!t) return;
  const goal = bot.pathfinder.goal;
  if (!goal || goal !== t.goal) {
    timedOut = null; // reached, dropped or replaced since
    return;
  }
  if (bot.pathfinder.isMoving()) return; // still on its partial path
  timedOut = null;
  if (bot.entity.position.distanceTo(t.from) >= 2) bot.pathfinder.setGoal(goal, isDynamic(goal));
  else noWay();
}

// The pathfinder's goto, except that an unfinished search is not « arrived »: it resolved on any search that came back
// with no path, and a search's first slice (tickTimeout) often does, so she « got there » without moving, stood 2
// blocks from a cull's beef and left it, again and again (local server). Everything awaiting goto had the same lie,
// collectblock included. Resolves on goal_reached or a finished search with nothing to walk; rejects as before on no
// path, a search out of time, another goal, a stop.
function goto(goal) {
  return new Promise((resolve, reject) => {
    const fail = (name, message) => Object.assign(new Error(message), { name });
    const listeners = {
      goal_reached: () => done(),
      path_update: (r) => {
        if (r.status === 'noPath') done(fail('NoPath', 'No path to the goal!'));
        else if (r.status === 'timeout') done(fail('Timeout', 'Took to long to decide path to goal!'));
        // A finished search with nothing to walk: she's there (its start is her feet on top of what she stands on,
        // where the floored position is inside farmland, 15/16 high, and never matches). A partial one is just the
        // first slice of the search.
        else if (r.path.length === 0 && r.status === 'success') done();
      },
      goal_updated: (g) => { if (g !== goal) done(fail('GoalChanged', 'The goal was changed before it could be completed!')); },
      path_stop: () => done(fail('PathStopped', 'Path was stopped before it could be completed!')),
    };
    function done(err) {
      for (const [ev, fn] of Object.entries(listeners)) bot.removeListener(ev, fn);
      setTimeout(() => (err ? reject(err) : resolve()), 0); // the pathfinder's own cleanup first, as its goto does
    }
    for (const [ev, fn] of Object.entries(listeners)) bot.on(ev, fn);
    bot.pathfinder.setGoal(goal);
  });
}

// The pathfinder lifts a climb's first node to the top of the ladder's plate (getPositionOnTopOf). Walking into the plate
// from the front, she climbs; walking along the wall into the ladder's block from its side she wasn't pushing against
// it, the step up couldn't be simulated, and the pathfinder let go of every key and planned the same again: stuck for
// good beside the ladder. Barring the side entry would close a corridor with a ladder on its wall, so the path gets back
// the step into the ladder's foot instead: in it first, then up, facing the plate as from the front. In place: the
// pathfinder walks the very array it hands out in path_update.
const Facing = { west: [1, 0], east: [-1, 0], north: [0, 1], south: [0, -1] }; // the way into a ladder, towards its wall

function climbFromTheFoot(path) {
  for (let i = 0; i < path.length; i++) {
    const n = path[i];
    const prev = i > 0 ? path[i - 1] : bot.entity.position; // a new search starts where she stands
    const foot = bot.blockAt(new Vec3(Math.floor(n.x), Math.floor(n.y) - 1, Math.floor(n.z)));
    if (!foot || foot.name !== 'ladder' || Math.floor(prev.y) !== foot.position.y) continue;
    const dx = foot.position.x - Math.floor(prev.x);
    const dz = foot.position.z - Math.floor(prev.z);
    const into = Facing[foot.getProperties().facing];
    if (Math.abs(dx) + Math.abs(dz) !== 1 || !into || (into[0] === dx && into[1] === dz)) continue; // head-on: fine
    // The foot's centre: at the climb's own x/z, pressed to the plate, her box overlapped it and the step failed too.
    const step = new Move(foot.position.x, foot.position.y, foot.position.z, n.remainingBlocks, 0);
    step.x = foot.position.x + 0.5;
    step.z = foot.position.z + 0.5;
    path.splice(i, 0, step);
    i++;
  }
}

// The ops that are a player's order (the rest are words, questions, or C#'s own business).
const OrderOps = new Set(['come', 'follow', 'goto', 'guide', 'give', 'store', 'fish', 'patrol', 'smelt', 'guard', 'collect', 'camp', 'uncamp',
  'sleep', 'harvest', 'farm', 'ranch']);

const input = readline.createInterface({ input: process.stdin });
input.on('line', (line) => {
  let command;
  try {
    command = JSON.parse(line);
  } catch {
    log('bad command:', line);
    return;
  }
  dispatch(command);
});

// One command, from C# or an order she starts again after a fight (restartOrder).
function dispatch(command) {
  try {
    switch (command.op) {
      case 'say':
        // Minecraft's chat cap; C# keeps her lines under it, this is the backstop.
        if (bot && typeof command.text === 'string' && command.text.length > 0) bot.chat(command.text.slice(0, 256));
        break;
      case 'whisper':
        if (bot && typeof command.player === 'string' && typeof command.text === 'string' && command.text.length > 0)
          bot.whisper(command.player, command.text.slice(0, 200));
        break;
      case 'come':
        if (bot && typeof command.player === 'string') come(command.player);
        break;
      case 'follow':
        if (bot && typeof command.player === 'string') follow(command.player);
        break;
      case 'stay':
        if (bot) {
          stay();
          stayedOnDay = bot.time.day; // told to stay put: she doesn't wander off to bed tonight
          heldUntil = Date.now() + HeldFor;
        }
        break;
      case 'goto':
        if (bot && typeof command.player === 'string' && Number.isFinite(command.x) && Number.isFinite(command.z))
          goTo(command.player, command.x, Number.isFinite(command.y) ? command.y : null, command.z,
            typeof command.dimension === 'string' ? command.dimension : null);
        break;
      case 'guide':
        if (bot && typeof command.player === 'string' && Number.isFinite(command.x) && Number.isFinite(command.y) && Number.isFinite(command.z))
          guide(command.player, command.x, command.y, command.z, typeof command.dimension === 'string' ? command.dimension : null);
        break;
      case 'give':
        if (bot && typeof command.player === 'string' && typeof command.goods === 'string')
          give(command.player, command.goods).catch((err) => log('give failed:', err && err.message ? err.message : String(err)));
        break;
      case 'store':
        if (bot && typeof command.player === 'string')
          store(command.player).catch((err) => log('store failed:', err && err.message ? err.message : String(err)));
        break;
      case 'dress':
        if (bot && typeof command.player === 'string')
          dress(command.player).catch((err) => log('dress failed:', err && err.message ? err.message : String(err)));
        break;
      case 'undress':
        if (bot && typeof command.player === 'string')
          undress(command.player).catch((err) => log('undress failed:', err && err.message ? err.message : String(err)));
        break;
      case 'fish':
        if (bot && typeof command.player === 'string') fish(command.player);
        break;
      case 'patrol':
        if (bot && typeof command.player === 'string') {
          const s = command.spot;
          patrol(command.player, s && Number.isFinite(s.x) && Number.isFinite(s.y) && Number.isFinite(s.z) && typeof s.dimension === 'string' ? s : null);
        }
        break;
      case 'smelt':
        if (bot && typeof command.player === 'string' && typeof command.what === 'string')
          smelt(command.player, command.what).catch((err) => log('smelt failed:', err && err.message ? err.message : String(err)));
        break;
      case 'where':
        if (bot && typeof command.player === 'string') where(command.player);
        break;
      case 'mark':
        if (bot && typeof command.player === 'string') mark(command.player);
        break;
      case 'sit':
        if (bot) {
          sit();
          stayedOnDay = bot.time.day;
          heldUntil = Date.now() + HeldFor;
        }
        break;
      case 'jump':
        if (bot) hop(3).catch(() => {});
        break;
      case 'wave':
        if (bot && typeof command.player === 'string') wave(command.player).catch(() => {});
        break;
      case 'guard':
        if (bot && typeof command.player === 'string') guard(command.player);
        break;
      case 'camp':
        if (bot && typeof command.player === 'string')
          makeCamp(command.player, command.site
            ? { site: command.site, placed: command.placed, floor: command.floor, heights: command.heights } : null)
            .catch((err) => log('camp failed:', err && err.message ? err.message : String(err)));
        break;
      case 'uncamp':
        if (bot && typeof command.player === 'string')
          breakCamp(command.player, command.blocks, typeof command.dimension === 'string' ? command.dimension : null, command.center)
            .catch((err) => log('uncamp failed:', err && err.message ? err.message : String(err)));
        break;
      case 'collect':
        if (bot && typeof command.player === 'string' && typeof command.resource === 'string')
          collect(command.player, command.resource, Math.max(1, Math.min(64, Number(command.count) || 10)), command.since || null)
            .catch((err) => log('collect failed:', err && err.message ? err.message : String(err)));
        break;
      case 'sleep':
        if (bot && typeof command.player === 'string')
          sleep(command.player).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
        break;
      case 'harvest':
        if (bot && typeof command.player === 'string' && typeof command.crop === 'string')
          harvest(command.player, command.crop, command.since || null).catch((err) => log('harvest failed:', err && err.message ? err.message : String(err)));
        break;
      case 'farm':
        if (bot && typeof command.player === 'string') farm(command.player);
        break;
      case 'flee':
        if (bot && typeof command.player === 'string') flee(command.player);
        break;
      case 'family':
        if (Array.isArray(command.names)) {
          familyOrder = command.names.filter((n) => typeof n === 'string' && n.length > 0).map((n) => n.toLowerCase());
          family = new Set(familyOrder);
        }
        break;
      case 'ranch':
        if (bot && typeof command.player === 'string') ranch(command.player);
        break;
      case 'routine':
        for (const what of Object.keys(routine)) {
          const s = command[what];
          routine[what] = s && Number.isFinite(s.x) && Number.isFinite(s.y) && Number.isFinite(s.z) && typeof s.dimension === 'string'
            ? { x: s.x, y: s.y, z: s.z, dimension: s.dimension } : null;
        }
        failedSpots.clear(); // a place moved or added deserves another try
        break;
      case 'defend':
        if (bot && typeof command.player === 'string' && typeof command.victim === 'string') {
          const attacker = playerEntity(command.player);
          const swat = command.swat === true;
          if (attacker && defend(attacker, command.victim, swat)) emit('defending', { player: command.player, victim: command.victim, swat });
          else emit('cantDefend', { player: command.player, victim: command.victim });
        }
        break;
      case 'seek':
        if (bot && typeof command.player === 'string') {
          const s = command.spot;
          const spot = s && Number.isFinite(s.x) && Number.isFinite(s.y) && Number.isFinite(s.z) && typeof s.dimension === 'string'
            ? s : null;
          seek(command.player, spot);
        }
        break;
      case 'quit':
        // Whoever is still in sight is where she last saw them: report it before going.
        if (bot && bot.entities) for (const entity of Object.values(bot.entities)) reportSeen(entity);
        // A beat so a goodbye sent just before reaches the server ahead of the disconnect.
        if (bot) setTimeout(() => bot.quit(), 500);
        else finish('quit before connecting');
        break;
      default:
        log('unknown op:', command.op);
    }
    // An order under way lets her go after « reste » or « assis ».
    if (bot && OrderOps.has(command.op) && isOrder(task)) heldUntil = 0;
    // What a fight can interrupt and start again: the command that started the order now under way.
    if (Restartable.has(command.op) && task && task.kind === command.op && !task.command) task.command = command;
  } catch (err) {
    log('command failed:', err && err.message ? err.message : String(err));
  }
}

// The C# side went away (crash, restart): leave the server rather than idle in it unattended.
input.on('close', () => {
  try {
    if (bot) bot.quit();
  } catch {
    // Already gone.
  }
  setTimeout(() => finish('parent closed'), 1000);
});

main().catch((err) => {
  log('start failed:', err && err.message ? err.message : String(err));
  finish('start failed');
});
