// SYNCS's body on the Minecraft server: a Mineflayer client the C# bot starts and drives.
//
// The body owns no policy and no words. It reports what happens as one JSON object per line on
// stdout, and does what it is told by one JSON object per line on stdin. Which line she says,
// when she joins and when she leaves are all decided in C# (MinecraftBodyService): her voice
// lives in BotResponses, behind ResponsePicker, never here. Logs go to stderr.
//
// Environment (the C# side clears everything else, so the Discord token never reaches us):
//   MC_HOST, MC_PORT, MC_USERNAME
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
//      {"op":"patrol","player":"…","spot":{x,y,z,dimension}|null} · {"op":"smelt","player":"…","what":"food|ore|sand"}
//      {"op":"where","player":"…"} · {"op":"mark","player":"…"} · {"op":"sit"} · {"op":"jump"} · {"op":"wave","player":"…"}
//      {"op":"ranch","player":"…"} · {"op":"routine","farm":spot|null,"fish":spot|null,"ranch":spot|null,"home":spot|null}
//        (a spot is {x,y,z,dimension})
// Out: version {server,using,fallback} · spawned · players {names} · chat {player,text,private}
//      system {key,args:[{text,key}]} · hurt {by} · died · night · kicked {reason} · end {reason}
//      moving {player,kind: come|follow|goto|guard|guide} · arrived {player,kind} · waiting {player}
//      cantReach {player,why: notVisible|noPath|lost|tooFar|otherDimension} · killed {player}
//      creeper {player} · retreat {player}
//      collecting {player,resource,count} · collected {player,resource,count,delivered}
//      cantCollect {player,resource,why: none|noTool} · sleeping {player,auto} · cantSleep {player,why: day|noBed|failed}
//      seek {player,outcome: met|missing} · seen {player,x,y,z,dimension}
//      harvesting {player,crop,count} · harvested {player,crop,count,replanted,delivered} · cantHarvest {player,crop,why: none}
//      farming {player} · cantFarm {player,why: none|noChest}
//      fleeing {player} · safe {player} · cornered {player} · dropped {player,kind,by}
//      familyHurt {victim,by} · defending {player,victim,swat} · cantDefend {player,victim}
//      hungry {food} · fed {by}
//      given {player,goods,count,delivered} · cantGive {player,goods,why: none} · stored {player,count,full}
//      cantStore {player,why: noChest|empty} · fishing {player} · cantFish {player,why: noRod|noWater|broke}
//      caught {player,kind: fish|treasure|junk} · patrolling {player} · cantPatrol {player,why: tooFar|otherDimension}
//      smelting {player,what,count} · smelted {player,what,count,delivered}
//      cantSmelt {player,what,why: nothing|noFuel|noFurnace|busy}
//      position {player,x,y,z,dimension} · marked {player,x,y,z,dimension} · cantMark {player}
//      ranching {player} · cantRanch {player,why: none|noChest}
//      Her own (no player, no claim): routine {what: farm|fish|ranch|home} · tagAlong {player} · retrieving
//      retrieved {count} · retrieveLost {why: otherDimension|tooFar|void|lava|noPath|despawned} · creeperBehind {player}
//      wornOut {tool} · broke {tool} · lacking {what: rod|shears|breedFood|arrows|space}
//      (tool: sword|pickaxe|axe|shovel|rod|shears|bow)
//      Water: diving {player} · surfacing {player} · drowning · breathing · boarded {player} · shook

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
const mc = require('minecraft-protocol');
const minecraftData = require('minecraft-data');
const { Vec3 } = require('vec3');

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
// dirt pillar in the middle of their build is not help.
function gentleMovements() {
  const movements = new Movements(bot);
  movements.canDig = false;
  movements.scafoldingBlocks = [];
  movements.allow1by1towers = false;
  // The pathfinder sees every door as a wall (its own door option is off, flaky, and gates only): a wooden door or
  // a fence gate is a way through, whatever its state, and doorTick swings it out of the way as she comes. Not on
  // a diagonal step, where a corner catches the panel, nor one that refused to move: a wall again for a while.
  // A bubble column over magma pulls whoever swims in it to the bottom: the pathfinder takes it for air, so it's
  // made a wall.
  const getBlock = movements.getBlock.bind(movements);
  movements.getBlock = (pos, dx = 0, dy = 0, dz = 0) => {
    const b = getBlock(pos, dx, dy, dz);
    if (isDragColumn(b)) {
      b.safe = false;
      return b;
    }
    if (b && isDoor(b) && !(dx !== 0 && dz !== 0) && !stuckDoors.has(doorKey(b))) {
      b.safe = true;
      b.physical = false;
      b.replaceable = false;
      b.openable = false;
      b.height = b.position.y;
    }
    return b;
  };
  return movements;
}

// ---- Doors ---------------------------------------------------------------------------------------------

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
const DoorStuckFor = 120000;
let doorTickAt = 0;

function setPath(path) {
  currentPath = Array.isArray(path) ? path : [];
  pathIndex = 0;
}

// The next few steps on her path, from where she stands: [from, to] pairs of block positions.
function stepsAhead(here) {
  for (let i = pathIndex; i < Math.min(currentPath.length, pathIndex + 4); i++) {
    const n = currentPath[i];
    if (n.x === here.x && n.z === here.z && Math.abs(n.y - here.y) <= 1) pathIndex = i + 1;
  }
  const cells = [here, ...currentPath.slice(pathIndex, pathIndex + 3).map((n) => new Vec3(n.x, n.y, n.z))];
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
    const centre = door.pos.offset(0.5, 0, 0.5);
    if (centre.distanceTo(me) <= DoorReach + 0.5) continue; // not through yet
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
    ignored: door ? door.ignored : 0, at: Date.now(),
  });
  bot.activateBlock(block).catch(() => {});
}

// For gathering: the same walk, except that the blocks she was sent for may be broken. collectblock
// asks the movements before every break, so nothing else on the way can be.
function gatheringMovements(targets) {
  const movements = gentleMovements();
  movements.canDig = true;
  const allowed = new Set(targets.map((b) => b.position.toString()));
  const safe = movements.safeToBreak.bind(movements);
  movements.safeToBreak = (block) => allowed.has(block.position.toString()) && safe(block);
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

function follow(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'follow', player, reported: false };
  // Dynamic: the goal moves with the player.
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  emit('moving', { player, kind: 'follow' });
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
}

const wait = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

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

// Monsters she fights. Neutral ones that only turn hostile when hit (endermen, piglins) are left
// alone: starting that fight would be the opposite of protecting anyone.
const Peaceful = new Set(['enderman', 'zombified_piglin', 'piglin']);
const GuardRadius = 10;
const MeleeReach = 3.2;
const Weapons = ['netherite_sword', 'diamond_sword', 'iron_sword', 'stone_sword', 'golden_sword', 'wooden_sword',
  'netherite_axe', 'diamond_axe', 'iron_axe', 'stone_axe', 'golden_axe', 'wooden_axe'];
let attackReadyAt = 0;

function isFoe(entity) {
  return entity && entity.type === 'hostile' && !Peaceful.has(entity.name) && entity.isValid !== false;
}

// Guarding a place rather than a player: monsters within PatrolRadius of the spot.
const PatrolRadius = 16;
// Under this much health (3 hearts) she stops fighting and backs off until she has healed, or RetreatFor has passed.
const RetreatHealth = 6;
const RetreatFor = 20000;
// A creeper is never hit by hand: its blast would land on the player she guards. With a bow and arrows she shoots it
// from at least CreeperRange; without, she keeps out of its reach and lets the player deal with it.
const CreeperRange = 6;

function guard(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'guard', player, foe: null, mode: null, modeTarget: null, warned: new Set(), skipLoot: new Set(), reported: false };
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  emit('moving', { player, kind: 'guard' });
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
    // What dropped from her kills first, when it's close and inside what she guards.
    const loot = lootNear(center, radius, t.skipLoot);
    if (loot) return setMode(t, 'loot', loot, new goals.GoalNear(loot.position.x, loot.position.y, loot.position.z, 1));
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
      return;
    }
  }

  // Creepers: warned about once each, shot from range, never hit by hand.
  const creeper = bot.nearestEntity((e) => e.name === 'creeper' && e.isValid !== false
    && (e.position.distanceTo(center) < radius || e.position.distanceTo(me) < 8));
  if (creeper) {
    if (!t.warned.has(creeper.id)) {
      t.warned.add(creeper.id);
      emit('creeper', { player: t.player });
    }
    const d = creeper.position.distanceTo(me);
    if (d < CreeperRange) return setMode(t, 'flee', creeper, new goals.GoalInvert(new goals.GoalFollow(creeper, CreeperRange + 3)), true);
    if (bowReady()) {
      setMode(t, 'shoot', creeper, null);
      shoot(creeper);
      return;
    }
  }

  // Every other monster: chased and hit. Not one under water (a drowned at the bottom): she can't reach it there.
  if (t.foe && (!t.foe.isValid || t.foe.position.distanceTo(center) > radius * 1.6 || headUnder(t.foe))) t.foe = null;
  if (!t.foe) {
    t.foe = bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e)
      && (e.position.distanceTo(center) < radius || e.position.distanceTo(me) < 6));
    if (!t.foe) return goHome();
  }
  const foe = t.foe;
  if (t.mode !== 'fight' || t.modeTarget !== foe) {
    equipWeapon();
    setMode(t, 'fight', foe, new goals.GoalFollow(foe, 1), true);
  }
  if (foe.position.distanceTo(me) < MeleeReach && Date.now() >= attackReadyAt) {
    attackReadyAt = Date.now() + 650; // a sword's cooldown; close enough for anything she holds
    bot.lookAt(foe.position.offset(0, (foe.height || 1.6) * 0.8, 0), true).catch(() => {});
    bot.attack(foe);
  }
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

function isTreeLog(block) {
  for (let dy = 0; dy <= 6; dy++)
    for (let dx = -2; dx <= 2; dx++)
      for (let dz = -2; dz <= 2; dz++) {
        const near = bot.blockAt(block.position.offset(dx, dy, dz));
        if (near && near.name.endsWith('_leaves') && near.getProperties().persistent === false) return true;
      }
  return false;
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

async function collect(player, resource, count) {
  const matches = Resources[resource];
  if (!matches) return;
  const ids = bot.registry.blocksArray.filter((b) => matches(b.name)).map((b) => b.id);
  let blocks = bot.findBlocks({ matching: ids, maxDistance: GatherRadius, count: Math.min(count * 4, 256) })
    .map((p) => bot.blockAt(p)).filter(Boolean);
  if (resource === 'wood') blocks = blocks.filter(isTreeLog);
  if (blocks.length === 0) return emit('cantCollect', { player, resource, why: 'none' });
  if (!canHarvest(blocks[0])) return emit('cantCollect', { player, resource, why: 'noTool' });
  blocks = blocks.slice(0, count);
  stay();

  const before = itemCounts();
  const mine = { kind: 'collect', player };
  task = mine;
  emit('collecting', { player, resource, count: blocks.length });
  bot.collectBlock.movements = gatheringMovements(blocks);
  try {
    await bot.collectBlock.collect(blocks, { ignoreNoPath: true });
  } catch (err) {
    log('collect stopped:', err && err.message ? err.message : String(err));
  } finally {
    bot.pathfinder.setMovements(gentleMovements());
  }
  if (task !== mine) return; // told to stop in the meantime

  const { total, delivered } = await deliverGains(player, before);
  if (task === mine) task = null;
  emit('collected', { player, resource, count: total, delivered });
}

// What she gained since `before` (an itemCounts()), brought back: she walks up to the player and tosses it at
// their feet. Kept on her when the player is out of sight or the walk fails.
async function deliverGains(player, before) {
  const after = itemCounts();
  const gained = [...after].map(([type, n]) => [type, n - (before.get(type) || 0)]).filter(([, n]) => n > 0);
  const total = gained.reduce((sum, [, n]) => sum + n, 0);
  let delivered = false;
  const owner = playerEntity(player);
  if (owner && total > 0) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(owner.position.x, owner.position.y, owner.position.z, 2));
      await bot.lookAt(owner.position.offset(0, 1.6, 0), true);
      for (const [type, n] of gained) await bot.toss(type, null, n);
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
};
const HarvestRadius = 24;
const FarmRadius = 16;
const ChestRadius = 10;
const FarmRound = 15000;
// What goes in the farm's chest, and how much of each she keeps back to replant with.
const FarmProduce = new Set(['wheat', 'wheat_seeds', 'carrot', 'potato', 'poisonous_potato', 'beetroot', 'beetroot_seeds',
  'melon_slice', 'pumpkin']);
const SeedsKept = { wheat_seeds: 16, carrot: 16, potato: 16, beetroot_seeds: 16 };
// And what she keeps to eat: the carrots and potatoes kept as seeds are food too, so only these two are extra.
const FoodKept = { beetroot: 8, melon_slice: 8 };

function cropsFor(key) {
  return key === 'any' ? Object.values(Crops) : Crops[key] ? [Crops[key]] : [];
}

function isRipe(block, crop) {
  if (crop.stem) {
    return [[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dz]) => {
      const near = bot.blockAt(block.position.offset(dx, 0, dz));
      return near && near.name === crop.stem;
    });
  }
  const age = block.getProperties().age;
  return typeof age === 'number' && age >= crop.ripe;
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

// Breaks the ripe crops (collectblock picks up what drops), then puts a seed back on each patch of farmland
// it left bare. Seeds on farmland are the one thing she ever places: nothing else, nowhere else.
async function harvestBlocks(found, mine) {
  const blocks = found.map((f) => f.block);
  bot.collectBlock.movements = gatheringMovements(blocks);
  try {
    await bot.collectBlock.collect(blocks, { ignoreNoPath: true });
  } catch (err) {
    log('harvest stopped:', err && err.message ? err.message : String(err));
  } finally {
    bot.pathfinder.setMovements(gentleMovements());
  }
  let replanted = 0;
  for (const { block, crop } of found) {
    if (task !== mine) break;
    if (!crop.seed) continue;
    const soil = bot.blockAt(block.position.offset(0, -1, 0));
    const above = bot.blockAt(block.position);
    if (!soil || soil.name !== 'farmland' || !above || above.name !== 'air') continue;
    const seed = bot.inventory.items().find((i) => i.name === crop.seed);
    if (!seed) continue;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(block.position.x, block.position.y, block.position.z, 3));
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
async function harvest(player, key) {
  const found = findRipe(cropsFor(key), bot.entity.position, HarvestRadius);
  if (found.length === 0) return emit('cantHarvest', { player, crop: key, why: 'none' });
  stay();
  const before = itemCounts();
  const mine = { kind: 'harvest', player };
  task = mine;
  emit('harvesting', { player, crop: key, count: found.length });
  const replanted = await harvestBlocks(found, mine);
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
  const chestIds = bot.registry.blocksArray.filter((b) => b.name === 'chest' || b.name === 'barrel').map((b) => b.id);
  return bot.findBlocks({ matching: chestIds, maxDistance: ChestRadius, point, count: 1 })[0] || null;
}

async function farmRound() {
  if (!bot || !task || task.kind !== 'farm' || task.busy) return;
  const mine = task;
  mine.busy = true;
  try {
    const found = findRipe(Object.values(Crops), mine.anchor, FarmRadius);
    if (found.length > 0) {
      await harvestBlocks(found, mine);
      if (task === mine) await storeProduce(mine);
    }
    if (task === mine && bot.entity.position.distanceTo(mine.anchor) > 4)
      await bot.pathfinder.goto(new goals.GoalNear(mine.anchor.x, mine.anchor.y, mine.anchor.z, 2));
  } catch (err) {
    log('farm round failed:', err && err.message ? err.message : String(err));
  } finally {
    mine.busy = false;
  }
}

// Puts the produce in the farm's chest, keeping back enough seeds to replant and a little to eat. A full chest
// keeps the rest on her.
async function storeProduce(mine) {
  const totals = new Map();
  for (const item of bot.inventory.items())
    if (FarmProduce.has(item.name)) totals.set(item.name, (totals.get(item.name) || 0) + item.count);
  await storeIn(mine, [...totals].map(([name, n]) => [name, n - (SeedsKept[name] || FoodKept[name] || 0)]));
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

// « syncs vide tes poches », « donne-moi ton poisson »: she walks up to the player and tosses it at their feet.
// A farm, a fishing spot or a guarded place she was keeping, she goes back to after.
async function give(player, key) {
  const matches = Goods[key];
  if (!matches) return;
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  const worn = key === 'armour' ? wornArmour() : [];
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

// What running away or defending someone goes back to: an activity, or a follow or guard. Never her own: autonomy
// picks again by itself once she's free.
function resumeFor(previous) {
  const kept = pausedActivity(previous) || (previous && FollowsSomeone.has(previous.kind) ? previous : null);
  return kept && !kept.own ? kept : null;
}

// After running away or defending someone: back to the activity, or to the player she was following or guarding,
// if she still sees them.
function resumeAfterBreak(resume) {
  if (!resume) return;
  if (isActivity(resume)) return resumeActivity(resume);
  const target = playerEntity(resume.player);
  if (!target) return emit('cantReach', { player: resume.player, why: 'lost' });
  task = { ...resume, foe: null, mode: null, modeTarget: null };
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
}

// ---- Defending her family ----------------------------------------------------------------------------

// C# sends her family's in-game names on spawn (`family`); hits on anyone else don't concern her. A monster hitting
// one of them, she fights it herself, saying nothing. A player hitting one of them is reported (`familyHurt`) and
// C# decides what comes of it: a word, one hit back (`defend` with swat), then a fight. Either way she only goes
// when she's free: no order under way, an activity, or following or guarding that very person, and she goes back
// to it after. Busy with someone's order, she answers `cantDefend` and C# keeps to words. She never hits family or
// a creeper, and breaks off below RetreatHealth.
let family = new Set();
const DefendRadius = 16;
const DefendMobFor = 20000;
const DefendFightFor = 15000;
const SwatFor = 6000;

function isFamily(name) {
  return Boolean(name) && family.has(String(name).toLowerCase());
}

function canDefend(victim) {
  if (bot.isSleeping) return false;
  if (isFree(task) || task.kind === 'defend' || isActivity(task)) return true;
  return FollowsSomeone.has(task.kind) && String(task.player).toLowerCase() === String(victim).toLowerCase();
}

// True when she takes it on.
function defend(foe, victim, swat) {
  if (!foe || foe.isValid === false || foe.name === 'creeper' || (foe.type === 'player' && isFamily(foe.username))) return false;
  if (!canDefend(victim) || foe.position.distanceTo(bot.entity.position) > DefendRadius || headUnder(foe)) return false;
  const lasts = swat ? SwatFor : foe.type === 'player' ? DefendFightFor : DefendMobFor;
  if (task && task.kind === 'defend') {
    // Already at it: a new attacker takes over, and a fight never shrinks back to one hit.
    task.swat = task.swat && swat && task.foe === foe;
    task.foe = foe;
    task.until = Date.now() + lasts;
    return true;
  }
  const resume = resumeFor(task);
  stay();
  task = { kind: 'defend', player: '', victim, foe, swat, resume, until: Date.now() + lasts, mode: null, modeTarget: null };
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
      || Date.now() > t.until || bot.health <= RetreatHealth || headUnder(foe))
    return endDefend(t);
  setMode(t, 'fight', foe, new goals.GoalFollow(foe, 1), true);
  if (foe.position.distanceTo(me) < MeleeReach && Date.now() >= attackReadyAt) {
    attackReadyAt = Date.now() + 650;
    bot.lookAt(foe.position.offset(0, (foe.height || 1.6) * 0.8, 0), true).catch(() => {});
    bot.attack(foe);
    if (t.swat) endDefend(t);
  }
}

function endDefend(t) {
  if (task !== t) return;
  task = null;
  bot.pathfinder.setGoal(null);
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
  try {
    await bot.equip(food, 'hand');
    await bot.consume();
  } catch (err) {
    log('eating failed:', err && err.message ? err.message : String(err));
  } finally {
    eating = false;
  }
}

function hasFood() {
  return bot.inventory.items().some((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name));
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
const ArmorTiers = ['leather', 'golden', 'chainmail', 'iron', 'turtle', 'diamond', 'netherite'];

function armorRank(item) {
  return item ? ArmorTiers.indexOf(item.name.split('_')[0]) : -1;
}

// Puts on the best piece she carries for each slot, when it beats what she wears.
async function wearBestArmor() {
  for (const [piece, slot] of Object.entries(ArmorSlots)) {
    const worn = bot.inventory.slots[bot.getEquipmentDestSlot(slot)];
    const best = bot.inventory.items().filter((i) => i.name.endsWith('_' + piece))
      .sort((a, b) => armorRank(b) - armorRank(a))[0];
    if (!best || armorRank(best) <= armorRank(worn)) continue;
    try { await bot.equip(best, slot); } catch { /* a slot she can't use; move on */ }
  }
}

// ---- Routine: her day, built on the places she was asked to remember ------------------------------------

// C# sends the farm, fishing spot, animal pen and home it found among the remembered places (`routine`). By day she
// works in WorkBlock turns, one place after the other; at dusk she goes home and stays there till morning; the night
// is autoSleepTick's. A place in another dimension, or farther than OwnTripMax, is left out; one that fails (no way
// there, nothing to do there) is left out for SpotRetry, silently. Orders always come first.
const routine = { farm: null, fish: null, ranch: null, home: null };
const WorkKinds = ['farm', 'fish', 'ranch'];
const WorkBlock = 360000;
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
      t.turnEnd = Date.now() + WorkBlock; // the only place: another turn of the same
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
  const trip = { kind: 'trip', own: true, player: '', what, spot, turnEnd: Date.now() + WorkBlock, deadline: Date.now() + TripTimeout };
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
// between rounds, or tagging along) she looks at whoever just talked within 12 blocks, else at the closest player within
// 6, else somewhere now and then. For 10 s after a player hits her, she turns her back on them instead. When Papa or
// Tata walks up from afar, she looks at them and crouches twice, once every GreetEvery at most.
const TalkLook = 8000;
const HitSulk = 10000;
const GreetEvery = 120000;
const lastChat = new Map();
const familyAway = new Map();
const greetedAt = new Map();
let hitBy = null;
let idleAt = 0;
let glanceAt = 0;
let greeting = false;

function lookFree() {
  if (bot.isSleeping || eating || drawing || greeting || swim || bot.pathfinder.isMoving()) return false;
  if (!task) return true;
  if (task.kind === 'tagAlong' || task.kind === 'home') return true;
  return Boolean(task.own && (task.kind === 'farm' || task.kind === 'ranch') && !task.busy);
}

const eyes = (entity) => entity.position.offset(0, (entity.height || 1.8) * 0.9, 0);

function idleTick() {
  if (Date.now() < idleAt || !bot.entity) return;
  idleAt = Date.now() + 250;
  greetTick();
  if (!lookFree()) return;
  const me = bot.entity.position;
  const hitter = hitBy && Date.now() < hitBy.until ? playerEntity(hitBy.name) : null;
  if (hitter) {
    const towards = Math.atan2(-(hitter.position.x - me.x), -(hitter.position.z - me.z));
    bot.look(towards + Math.PI, 0).catch(() => {});
    return;
  }
  const target = talker(me) || bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username && e.position.distanceTo(me) <= 6);
  if (target) {
    bot.lookAt(eyes(target)).catch(() => {});
    return;
  }
  if (Date.now() >= glanceAt) {
    glanceAt = Date.now() + 5000 + Math.random() * 5000;
    bot.look(Math.random() * 2 * Math.PI, (Math.random() - 0.5) * 0.6).catch(() => {});
  }
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
const BoardReach = 3;
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
  await pickUpDrops(mine.pos, 6, RetrieveSweep, mine);
  if (task !== mine) return; // an order came first: the walk is still pending
  await wait(500); // the last pickup lands
  if (task !== mine) return;
  const before = pendingRetrieve ? pendingRetrieve.before : new Map();
  pendingRetrieve = null;
  task = null;
  const items = [...itemCounts()].reduce((sum, [type, n]) => sum + Math.max(0, n - (before.get(type) || 0)), 0);
  emit('retrieved', { count: items + wornArmour().length });
}

// Walks onto the dropped items within `radius` of a spot, one after the other, until none is left, the time is up or
// she's no longer doing `mine`. Each item gets one try.
async function pickUpDrops(center, radius, timeout, mine) {
  const end = Date.now() + timeout;
  const tried = new Set();
  while (task === mine && Date.now() < end) {
    const item = bot.nearestEntity((e) => e.name === 'item' && !tried.has(e.id) && e.position.distanceTo(center) <= radius);
    if (!item) return;
    tried.add(item.id);
    const at = item.position;
    try {
      await Promise.race([
        bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 1)),
        wait(Math.min(8000, Math.max(0, end - Date.now()))).then(() => { throw new Error('too slow'); }),
      ]);
    } catch {
      if (task === mine) bot.pathfinder.setGoal(null); // out of reach, or too slow: the next one
    }
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
    const creeper = bot.nearestEntity((c) => c.name === 'creeper' && c.isValid !== false && !warnedCreepers.has(c.id)
      && c.position.distanceTo(e.position) < 4);
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
      await pickUpDrops(spot.pos, 3, 10000, mine);
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
    if (bot.inventory.items().filter((i) => Livestock[species](i.name)).reduce((sum, i) => sum + i.count, 0) < 2) {
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
    while (surplus > 0 && left > 0 && victims.length > 0 && task === mine) {
      await slaughter(mine, victims.shift());
      surplus--;
      left--;
    }
  }
}

// The same hits as defending: chase, and hit on the weapon's cooldown, until it's dead or KillFor has passed.
async function slaughter(mine, animal) {
  mine.target = animal;
  await equipWeapon();
  bot.pathfinder.setGoal(new goals.GoalFollow(animal, 1), true);
  const end = Date.now() + KillFor;
  while (task === mine && animal.isValid !== false && bot.entities[animal.id] && Date.now() < end) {
    if (animal.position.distanceTo(bot.entity.position) < MeleeReach && Date.now() >= attackReadyAt) {
      attackReadyAt = Date.now() + 650;
      bot.lookAt(animal.position.offset(0, (animal.height || 1) * 0.6, 0), true).catch(() => {});
      bot.attack(animal);
    }
    await wait(100);
  }
  if (task === mine) {
    bot.pathfinder.setGoal(null);
    mine.target = null;
  }
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
    bot.pathfinder.setMovements(gentleMovements());
    // After the pathfinder's own tick (its plugin is injected later than attach() runs), so her swimming can take the
    // controls from it.
    bot.on('physicsTick', safely(waterTick));
    bot.on('physicsTick', safely(boardTick));
    emit('spawned', {});
    reportPlayers();
    wearBestArmor().catch(() => {});
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
  bot.on('physicsTick', guideTick);
  bot.on('physicsTick', doorTick);
  bot.on('physicsTick', safely(freeTick));
  bot.on('physicsTick', safely(idleTick));
  bot.on('physicsTick', safely(autonomyTick));
  bot.on('physicsTick', safely(creeperWatchTick));
  setInterval(() => { if (bot) ranchRound().catch(() => {}); }, RanchRound);
  setInterval(() => { if (bot) safely(needsTick)(); }, NeedsEvery);
  setInterval(() => { if (bot) safely(rideTick)(); }, 500);
  bot.on('mount', safely(onMount));
  bot.on('dismount', () => { riding = null; });
  // The path she's walking, for the doors on it; closing behind her needs none.
  bot.on('path_update', (result) => setPath(result.status === 'noPath' ? [] : result.path));
  bot.on('goal_reached', () => setPath([]));
  bot.on('path_stop', () => setPath([]));
  setInterval(() => { if (bot) farmRound().catch(() => {}); }, FarmRound);
  bot.on('wake', wakeUp);
  bot.on('health', () => {
    eatIfHungry().catch(() => {});
    reportHunger();
  });
  bot.on('playerCollect', (collector) => {
    if (collector !== bot.entity) return;
    setTimeout(() => {
      wearBestArmor().catch(() => {});
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
    if (!task || result.status !== 'noPath') return;
    if (task.mode === 'loot') return task.skipLoot.add(task.modeTarget.id); // out of reach: left where it is
    if (task.reported) return;
    // Following someone who dived: no path under water, and the dive (waterTick) goes after them instead.
    if (FollowsSomeone.has(task.kind) && headUnder(playerEntity(task.player))) return;
    if (['collect', 'sleep', 'harvest', 'farm', 'give', 'store', 'fish', 'smelt', 'patrol', 'forage', 'ranch', 'tagAlong', 'home']
      .includes(task.kind)) return;
    // Her own: a place she can't walk to is left out for a while; her things, given up.
    if (task.kind === 'trip') {
      const what = task.what;
      stay();
      return failSpot(what, 'no path');
    }
    if (task.kind === 'retrieve') {
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
  });

  // The player she follows or guards went out of sight (too far, or logged out): she stops there.
  bot.on('entityGone', (entity) => {
    reportSeen(entity);
    warnedCreepers.delete(entity.id);
    if (task && task.kind === 'tagAlong' && entity.username === task.who) return stay(); // silently
    if (!task || (task.kind !== 'follow' && task.kind !== 'guard') || entity.username !== task.player) return;
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
      if (exact) return emit('hurt', { by: source && source.type === 'player' && source.username !== bot.username ? source.username : null });
      // No source before 1.19.4: the closest player within reach is a fair guess, and nobody in reach means a mob,
      // a fall or fire.
      const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
        && e.position.distanceTo(bot.entity.position) < 4.5);
      return emit('hurt', { by: near ? near.username : null });
    }
    if (!exact || !source || source === bot.entity || entity.type !== 'player' || !isFamily(entity.username)) return;
    if (source.type === 'player') {
      if (!isFamily(source.username)) emit('familyHurt', { victim: entity.username, by: source.username });
    } else if (isFoe(source)) {
      defend(source, entity.username, false);
    }
  });

  bot.on('death', () => {
    diedAt = Date.now();
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
// a `tick_end` after each movement packet. Drop it once Mineflayer sends the packet itself.
const MovePackets = new Set(['position', 'position_look', 'look', 'flying']);

function sendTickEnds(client) {
  let supported = null; // known once the version is (it can be settled by Mineflayer's own ping)
  const write = client.write.bind(client);
  client.write = (name, params) => {
    const result = write(name, params);
    if (MovePackets.has(name)) {
      if (supported === null) {
        const data = minecraftData(client.version);
        supported = Boolean(data && data.protocol.play.toServer.types.packet_tick_end);
      }
      if (supported) {
        try {
          write('tick_end', {});
        } catch (err) {
          log('tick_end failed:', err && err.message ? err.message : String(err));
        }
      }
    }
    return result;
  };
}

async function main() {
  const version = await pickVersion();
  if (ended) return;
  // Offline mode: no auth.
  bot = mineflayer.createBot({ host, port, username, auth: 'offline', version });
  sendTickEnds(bot._client);
  attach();
}


// The ops that are a player's order (the rest are words, questions, or C#'s own business).
const OrderOps = new Set(['come', 'follow', 'goto', 'guide', 'give', 'store', 'fish', 'patrol', 'smelt', 'guard', 'collect',
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
      case 'collect':
        if (bot && typeof command.player === 'string' && typeof command.resource === 'string')
          collect(command.player, command.resource, Math.max(1, Math.min(64, Number(command.count) || 10)))
            .catch((err) => log('collect failed:', err && err.message ? err.message : String(err)));
        break;
      case 'sleep':
        if (bot && typeof command.player === 'string')
          sleep(command.player).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
        break;
      case 'harvest':
        if (bot && typeof command.player === 'string' && typeof command.crop === 'string')
          harvest(command.player, command.crop).catch((err) => log('harvest failed:', err && err.message ? err.message : String(err)));
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
  } catch (err) {
    log('command failed:', err && err.message ? err.message : String(err));
  }
});

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
