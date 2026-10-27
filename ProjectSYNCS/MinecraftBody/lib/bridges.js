// « No way there » with a gap of a block or two in the way (a ravine's narrow end, a ditch, a broken bridge): the walk
// is planned once more with blocks she may place, BridgeMost at most, of what she'd least miss (BridgeItems, never a
// log), and every one comes down once she's across. Only for a walk a player asked for (come, follow, go to, guide,
// guard) or her walk up to family, only from what she carries, never into or over lava. Everywhere else the rule
// stands: she places nothing (movements.js).

import { lavaStates } from './blocks.js';
import { gentleMovements } from './movements.js';
import { isDynamic } from './pathing.js';
import { bot, log, safely, task } from './state.js';

const BridgeKinds = new Set(['come', 'follow', 'goto', 'guide', 'guard', 'seek']);
// The block items she bridges with, the first she has first. Their blocks are what she takes down (dirt may have
// greened over in between).
const BridgeItems = ['dirt', 'coarse_dirt', 'cobblestone', 'cobbled_deepslate', 'netherrack'];
const BridgeBlocks = new Set([...BridgeItems, 'grass_block']);
const BridgeMost = 2;
// A block placed costs as much as ten steps: a way round, when the second search finds one, wins.
const BridgeCost = 10;
// One more try for the same walk only this long after the last; and a try that placed nothing gives the plain walk
// back after BridgeIdle.
const BridgeAgain = 30000;
const BridgeIdle = 20000;
// Off her blocks by this much sideways (from a block's centre) she's across, and takes them down from where she stands.
const BridgeClear = 1.5;
const BridgeReach = 4.5;

let bridging = false; // the walk is planned with bridgingMovements
let bridgingSince = 0;
let bridgedTask = null; // the walk that may bridge: no other (her own trips, a fight) ever does
const placed = []; // where she set a block, until it's down
let placeBlock = null; // bot.placeBlock, while hers notes where each block goes
let takingDown = false;
let bridgeTickAt = 0;

function bridgingMovements() {
  const movements = gentleMovements();
  movements.scafoldingBlocks = BridgeItems.map((n) => bot.registry.itemsByName[n]).filter(Boolean).map((i) => i.id);
  movements.placeCost = BridgeCost;
  // The pathfinder plans with as many blocks as she carries: BridgeMost, whatever she has.
  const count = movements.countScaffoldingItems.bind(movements);
  movements.countScaffoldingItems = () => Math.min(count(), BridgeMost);
  // Never into lava, nor where she'd stand over it: past 100, the pathfinder drops the step.
  movements.exclusionAreasPlace.push((block) => (block && block.position && [0, -1].some((dy) =>
    lavaStates.has(bot.world.getBlockStateId(block.position.offset(0, dy, 0)))) ? 1000 : 0));
  return movements;
}

const hasBridgeItems = () => bot.inventory.items().some((i) => BridgeItems.includes(i.name));

// From noWay: true when she tries again with a bridge, and the walk isn't given up yet.
export function tryBridge(t) {
  if (!BridgeKinds.has(t.kind) || bridging || Date.now() < (t.bridgeAt || 0) || !hasBridgeItems()) return false;
  const goal = bot.pathfinder.goal;
  if (!goal) return false;
  t.bridgeAt = Date.now() + BridgeAgain;
  // Not inside the pathfinder's own path_update, where noWay is called from: it sets the path it found after its
  // listeners return, and a search asked for in one never runs.
  setTimeout(safely(function bridgeWalk() {
    if (task !== t || bot.pathfinder.goal !== goal) return;
    startBridging(t);
    bot.pathfinder.setGoal(goal, isDynamic(goal));
  }), 0);
  return true;
}

function startBridging(t) {
  bridging = true;
  bridgingSince = Date.now();
  bridgedTask = t;
  bot.pathfinder.setMovements(bridgingMovements());
  placeBlock = bot.placeBlock;
  bot.placeBlock = (ref, face, ...rest) => {
    // Noted before it's tried: Mineflayer gives up on a block whose update comes late, which the server set all the
    // same (only what stands is taken down).
    const at = ref.position.plus(face);
    if (!placed.some((p) => p.equals(at))) placed.push(at);
    return placeBlock.call(bot, ref, face, ...rest);
  };
}

// The plain walk back: once across, or when the walk ends (stay()).
export function stopBridging() {
  if (!bridging) return;
  bridging = false;
  bridgedTask = null;
  bot.placeBlock = placeBlock;
  placeBlock = null;
  bot.pathfinder.setMovements(gentleMovements());
}

// For /debug minecraft: planning with blocks, and how many of hers still stand.
export function bridgeStatus() {
  return { bridging, standing: placed.length };
}

const isBridgeBlock = (block) => Boolean(block && BridgeBlocks.has(block.name));

// A player standing on the block: it stays until they step off.
const standsOn = (p) => Object.values(bot.entities).some((e) => e.type === 'player' && e !== bot.entity
  && Math.floor(e.position.y) === p.y + 1 && Math.abs(e.position.x - (p.x + 0.5)) < 0.8 && Math.abs(e.position.z - (p.z + 0.5)) < 0.8);

// A few times a second: across her blocks, she takes them down.
export function bridgeTick() {
  if (Date.now() < bridgeTickAt || takingDown || !bot.entity) return;
  bridgeTickAt = Date.now() + 250;
  // Arrived, or another task (an order, a fight, her own trip): the plain walk back, her blocks still to take down.
  if (bridging && (task !== bridgedTask || (placed.length === 0 && Date.now() > bridgingSince + BridgeIdle))) stopBridging();
  if (placed.length === 0 || bot.pathfinder.isBuilding() || !bot.entity.onGround) return;
  // What the server refused, or someone broke, is not hers to take down.
  for (let i = placed.length - 1; i >= 0; i--) if (!isBridgeBlock(bot.blockAt(placed[i]))) placed.splice(i, 1);
  if (placed.length === 0) return;
  const me = bot.entity.position;
  if (placed.some((p) => Math.hypot(me.x - (p.x + 0.5), me.z - (p.z + 0.5)) < BridgeClear && me.y - p.y < 3)) return; // still on it
  stopBridging();
  takeDown().catch((err) => log('bridge: taking down failed:', err && err.message ? err.message : String(err)));
}

// Her walk waits while she breaks her blocks from where she stands, and goes on after, if it's still hers. A block
// out of reach, or one someone still stands on, is left with a log line.
async function takeDown() {
  takingDown = true;
  const t = task;
  const goal = bot.pathfinder.goal;
  if (goal) bot.pathfinder.setGoal(null);
  try {
    for (const p of [...placed]) {
      const block = bot.blockAt(p);
      if (!isBridgeBlock(block)) continue;
      const eyes = bot.entity.position.offset(0, bot.entity.eyeHeight || 1.62, 0);
      if (standsOn(p) || eyes.distanceTo(p.offset(0.5, 0.5, 0.5)) > BridgeReach) {
        log(`bridge: left ${block.name} at ${p} (${standsOn(p) ? 'someone on it' : 'out of reach'})`);
        continue;
      }
      await bot.tool.equipForBlock(block);
      await bot.dig(block, true);
    }
  } finally {
    placed.length = 0;
    takingDown = false;
    if (goal && t && task === t) bot.pathfinder.setGoal(goal, isDynamic(goal));
  }
}
