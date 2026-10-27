// The one way every fight hits (strike): her weapon's cooldown, never by an edge or through a sweep onto a player;
// her fight walk, chasing, and the foes she can't get to.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { cooldownFor, sweepReaches } from './fight.js';
import { airStates, lavaStates } from './blocks.js';
import { bestWeapon } from './combat.js';
import { eating } from './food.js';
import { attackReadyAt, isFoe, MeleeReach, setAttackReadyAt, setMode } from './guard.js';
import { gentleMovements } from './movements.js';
import { shieldDown } from './ranged.js';
import { minY } from './retrieve.js';
import { equipWeapon, inSight } from './shooting.js';
import { bot } from './state.js';
import { headUnder } from './water.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// A hit taken with a drop or lava behind her throws her in (knockback). Before swinging she steps off such a lip for
// EdgeStep, towards what she fights or along the edge.
const EdgeStep = 1000;
// A foe this far from where it stood when the fight started has run: let go, or she's drawn deep into a cave.
export const FightLeash = 12;
// A sword's sweep lands on whoever stands within a block of what it hits (sweepReaches). With a player that close she
// hits on the way down from a jump: a critical hit, which never sweeps (the game sweeps only from the ground). Not
// hitting at all left a zombie on Papa until it killed him.

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

// A player (not her, not the one she hits) the sweep would catch (fight.js: its box grown by 1, a quarter up and down).
function playerBeside(foe) {
  return Object.values(bot.players).some((p) => p.entity && p.entity !== bot.entity && p.entity !== foe
    && sweepReaches({ dx: p.entity.position.x - foe.position.x, dy: p.entity.position.y - foe.position.y,
      dz: p.entity.position.z - foe.position.z }));
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

// The fight walk in use, if it's the one: a flag went stale when something else (gather's cleanup, after a fight had
// started) set its own movements, and her fight went on with parkour and 4-block drops.
let fightMoves = null;

export function fightPace(on) {
  const inUse = fightMoves !== null && bot.pathfinder.movements === fightMoves;
  if (on && !inUse) {
    fightMoves = fightMovements();
    bot.pathfinder.setMovements(fightMoves);
  } else if (!on && inUse) {
    fightMoves = null;
    bot.pathfinder.setMovements(gentleMovements());
  }
}

// A foe with no way to it is left alone this long (noWay), then tried again.
export const UnreachableFor = 10000;

export function reachable(t, foe) {
  return !(t.unreachable && t.unreachable.get(foe.id) > Date.now());
}

export function setUnreachable(t, foe) {
  if (foe.position.distanceTo(bot.entity.position) < MeleeReach) return; // in reach: hit it where it stands
  if (!t.unreachable) t.unreachable = new Map();
  t.unreachable.set(foe.id, Date.now() + UnreachableFor);
  if (t.foe === foe) t.foe = null;
  t.mode = null;
  t.modeTarget = null;
}

// The closest monster (not a creeper) in reach of her and in sight: the one hitting her, or about to. Never one set
// aside as unreachable: in reach, it can be hit whatever the pathfinder found.
export function foeOnHer() {
  const me = bot.entity.position;
  return bot.nearestEntity((e) => isFoe(e) && e.name !== 'creeper' && !headUnder(e)
    && e.position.distanceTo(me) < MeleeReach + 0.5 && inSight(e));
}

// After `foe`, weapon in hand, with her fight walk. Not while she steps off a lip (strike).
export function chase(t, foe) {
  if (Date.now() < (t.edgeUntil || 0)) return;
  // Her best weapon in hand, whatever she held last: after an emergency meal she fought on with the bottle.
  const best = bestWeapon();
  if (!eating && best && !(bot.heldItem && bot.heldItem.name === best)) equipWeapon();
  fightPace(true);
  setMode(t, 'fight', foe, new goals.GoalFollow(foe, 1), true);
}

// One hit on `foe` when she can: in reach and in sight, her weapon ready (its own cooldown), and no hazard behind her
// (then she steps off the lip first, when there's a task `t` to move for). With a player beside it and a sword in hand,
// from a jump (sweepReaches). True when she swung.
export function strike(foe, t = null) {
  const me = bot.entity.position;
  if (!foe || foe.isValid === false) return false;
  // Off the lip as soon as it's close, not only when she swings: its hits come on its own time, while her weapon
  // recharges (one threw her off the far end of a ledge).
  if (t && foe.position.distanceTo(me) < MeleeReach + 1 && Date.now() >= (t.edgeUntil || 0)) {
    const step = edgeBehind(foe);
    if (step) {
      t.edgeUntil = Date.now() + EdgeStep;
      t.mode = 'edge';
      t.modeTarget = foe;
      t.modeSince = Date.now();
      bot.pathfinder.setGoal(new goals.GoalNear(step.x, step.y, step.z, 0));
      return false;
    }
  }
  if (foe.position.distanceTo(me) >= MeleeReach || Date.now() < attackReadyAt || !inSight(foe)) return false;
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
  shieldDown(); // no hit while it's up
  setAttackReadyAt(Date.now() + cooldownFor(bot.heldItem ? bot.heldItem.name : null));
  bot.lookAt(foe.position.offset(0, (foe.height || 1.6) * 0.8, 0), true).catch(() => {});
  bot.attack(foe);
  return true;
}
