// Boats: climbing in behind someone, getting out, and Mineflayer's passenger list kept right.

import { eating } from './food.js';
import { playerEntity } from './orders.js';
import { bot, emit, task } from './state.js';
import { stopSwimming, swim } from './water.js';

// From the boat's centre. The server takes an interaction within reach (3) plus 3 of the boat's box; she follows a
// player to 2 of *them*, so a boat moored a block or two out keeps her 4 off it: at 3 she stood on the bank and
// never got in (checked on the local server).
const BoardReach = 5;
const BoardRetry = 2000;
const BoardTries = 3;
const BoardGiveUp = 30000;

// Boats: two seats, one on a chest boat (the chest takes the back one); rafts the same.
function isBoat(entity) {
  const n = (entity && entity.name) || '';
  return n.endsWith('boat') || n.endsWith('raft');
}

function freeSeat(boat) {
  return (boat.passengers || []).length < ((boat.name || '').includes('chest') ? 1 : 2);
}

// The player she's with (following, or tagging along) sits in a boat with room for her.
export function boardable(target) {
  const boat = target && target.vehicle;
  return Boolean(boat && isBoat(boat) && freeSeat(boat) && (boatSkip.get(boat.id) || 0) <= Date.now());
}

// Riding: whose boat, and for which task; the ride ends with either. Physics stops while she rides (Mineflayer's), so
// no physics tick runs: rideTick is a timer.
export let riding = null;
let boarding = null;
let boardAt = 0;
const boatSkip = new Map();

// Twice a second on foot or swimming: close enough to their boat, she climbs in. Three refusals (someone took the seat,
// a plugin) and she leaves that boat alone for a while.
export function boardTick() {
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

export function onMount() {
  const b = boarding;
  boarding = null;
  if (!b || bot.vehicle !== b.boat || task !== b.task) return;
  riding = { boat: b.boat, who: b.who, task: b.task };
  stopSwimming();
  emit('boarded', { player: b.who });
}

// Out of a vehicle, whichever way: no longer riding, and off a boat's top (offBoatTick).
export function onDismount(vehicle) {
  if (vehicle && isBoat(vehicle)) offBoat = { from: vehicle.position.clone(), until: Date.now() + 4000 };
  riding = null;
}

// Mineflayer only updates the passengers a vehicle still has: whoever got out kept pointing at it until their entity
// was reloaded, so she stayed in a boat Papa had left, and once she got out herself she still « rode » it (her
// physics stays off while she rides). Whoever the new list leaves out is out.
export function fixPassengers({ entityId, passengers }) {
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
}

// Dead: no boat, and none she was climbing into.
export function forgetBoat() {
  riding = null;
  boarding = null;
}

// Out when they get out (or the boat goes), or when what she was doing ends: any order gets her out, as from a bed.
export function rideTick() {
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

export function offBoatTick() {
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
export function leaveBoat() {
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
