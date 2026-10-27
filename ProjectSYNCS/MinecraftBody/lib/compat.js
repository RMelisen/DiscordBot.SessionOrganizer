// Every workaround for Mineflayer and its libraries not keeping up with the server's version, in one place. Before
// bumping mineflayer, minecraft-protocol, minecraft-data or prismarine-physics, go through this list: each entry says
// what it fixes and when it can go.
//
// Here:
//   - Collisions with the game's own tolerance (the AABB patch)                 prismarine-physics compares exactly
//   - A hair wider than a player (playerHalfWidth 0.3001, onSpawn)                 a 26.3 server refuses her at every step
//   - Out of a face she was put flush against (clearFaces)                         follows from the two above
//   - tick_end after every move (sendTickEnds)                                     PrismarineJS/mineflayer#4189, 26.3
//   - Her keys as a real client sends them (inputTick)                             Mineflayer sends player_input for sneaking only
//   - entity_action by name (fixEntityActions)                                     Mineflayer 4.39 sends 1.21.5's numbers
//   - Teleports answered once (calmTeleports)                                      Paper's packet limiter kicked her in bed
// Diagnostics riding the same packet hooks: packets a second over PacketsLoud, and every server correction while she
// walks (watchCorrections). status() reports all of it to /debug minecraft.
//
// Elsewhere, because each is part of what it fixes (check them too):
//   - pathing.js: goto (the pathfinder resolving on a search with no path), timeoutTick (a timed-out search never
//     searched again), climbFromTheFoot (a ladder met from the side)
//   - boats.js: fixPassengers (Mineflayer keeps passengers who got out), leaveBoat (shift, not jump, since 1.21.2)
//   - crafting.js: bookCraft (bot.craft's clicks refused on 26.1)
//   - gather.js: deliverGains (toss by count lost items on 26.1); farm.js: isRipe (states as strings on 26.1)
//   - emergency.js: potions by registry id; combat.js: exactHits (damage_event since 1.19.4)

import minecraftData from 'minecraft-data';
import AABB from 'prismarine-physics/lib/aabb.js';
import vec3 from 'vec3';
import { bot, log } from './state.js';

const { Vec3 } = vec3;

// The client's packet hooks, in this order: each one wraps the write of the one before.
export function install(client) {
  sendTickEnds(client);
  calmTeleports(client);
  clearFaces(client);
  fixEntityActions(client);
  trackInputs(client);
  watchCorrections(client);
}

// Once she's in the world. A hair wider than a player (0.6). Mineflayer's collisions leave her side exactly on a block's
// face (a step she's jumping up, a wall she walks along), and a 26.3 server takes that as walking into the block: it
// puts her back, every tick, and she never climbs a one-block step. 0.0001 off the face is enough (checked on a local
// Paper 26.3 with ViaBackwards) and fits every gap a player fits.
export function onSpawn() {
  bot.physics.playerHalfWidth = 0.3001;
}

// For /debug minecraft: which workarounds the server's version needs, and what the diagnostics counted.
export function status() {
  return {
    tickEnd: tickEndSupported,
    renumberedActions,
    corrections: pulledBack,
    packetsPeak,
    packetsPeakAt,
  };
}

// Collisions with the game's own tolerance: a box less than 1e-7 into a block still touches its face, and on the other
// two axes overlaps by less than that don't count (vanilla's VoxelShape epsilon). Mineflayer compares exactly, so a box
// rounded a hair past a face (a step after being put flush against it: -127.99999999999999 for -128) no longer finds
// the block in front of it and walks straight in, and the server puts her back, again and again.
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

// Since 26.3 the server allows one position packet per client tick and only resets that count when
// the client sends `tick_end`, which real clients have sent after every tick since 1.21.2. Mineflayer
// never sends it, so a 26.3 server (ViaBackwards included) kicks her for Â« invalid player movement Â»
// on her second move, about 1.5 s after joining. The workaround from PrismarineJS/mineflayer#4189:
// a `tick_end` after each movement packet. A physics tick that sent no move (she stood still) gets its
// `tick_end` at the start of the next one (inputTick), so the server counts every tick as a real client's.
// Drop it once Mineflayer sends the packet itself.
const MovePackets = new Set(['position', 'position_look', 'look', 'flying']);
let tickOpen = false; // a physics tick whose tick_end hasn't gone out yet
let endTick = () => {};
let tickEndSupported = null; // known once the version is (it can be settled by Mineflayer's own ping)

function sendTickEnds(client) {
  const write = client.write.bind(client);
  endTick = () => {
    tickOpen = false;
    if (tickEndSupported === null) {
      const data = minecraftData(client.version);
      tickEndSupported = Boolean(data && data.protocol.play.toServer.types.packet_tick_end);
    }
    if (!tickEndSupported) return;
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
// kicks a client for Â« too many packets Â» near 500 a second; a normal second is under 60, and the one loop found so far
// (calmTeleports) ran at 480. The line names the next one before the kick does.
const PacketsLoud = 200;
let packetsThisSecond = {};
let packetsLoggedAt = 0;
// The busiest second so far, for /debug minecraft.
let packetsPeak = 0;
let packetsPeakAt = null;

function countPackets() {
  const counts = Object.entries(packetsThisSecond);
  packetsThisSecond = {};
  const total = counts.reduce((sum, [, n]) => sum + n, 0);
  if (total > packetsPeak) {
    packetsPeak = total;
    packetsPeakAt = Date.now();
  }
  if (total <= PacketsLoud || Date.now() < packetsLoggedAt + 30000) return;
  packetsLoggedAt = Date.now();
  const top = counts.sort((a, b) => b[1] - a[1]).slice(0, 4).map(([name, n]) => `${name} ${n}`).join(', ');
  log(`sending ${total} packets a second (${top})`);
}

function trackInputs(client) {
  setInterval(countPackets, 1000);
  const write = client.write.bind(client);
  client.write = (name, params) => {
    packetsThisSecond[name] = (packetsThisSecond[name] || 0) + 1;
    if (name === 'player_input') inputsSent = JSON.stringify((params && params.inputs) || {});
    if (name === 'position' || name === 'position_look') lastMoveSent = { name, x: params.x, y: params.y, z: params.z, onGround: params.onGround };
    return write(name, params);
  };
}

export function inputTick() {
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
// still sends the old numbers: on 26.1 its Â« start sprinting Â» (3) reaches the server as Â« start riding jump Â», and
// getting out of bed (2) as Â« stop sprinting Â». So the server never knew she sprinted while her physics ran her at
// sprint speed, and leaving a bed never reached it. The old numbers go out as the names, on versions with the new order.
// Remove it once Mineflayer sends names there itself (check physics.js and bed.js before bumping it).
const OldEntityActions = ['start_sneaking', 'stop_sneaking', 'stop_sleeping', 'start_sprinting', 'stop_sprinting',
  'start_riding_jump', 'stop_riding_jump', 'open_inventory', 'start_fall_flying'];

let renumberedActions = null; // known once the version is

function fixEntityActions(client) {
  const write = client.write.bind(client);
  client.write = (name, params) => {
    if (name === 'entity_action' && params && typeof params.actionId === 'number') {
      if (renumberedActions === null) {
        const data = minecraftData(client.version);
        const type = data && data.protocol.play.toServer.types.packet_entity_action;
        const action = type && type[1].find((f) => f.name === 'actionId');
        renumberedActions = Boolean(action && Array.isArray(action.type) && action.type[1].mappings['0'] === 'stop_sleeping');
      }
      if (renumberedActions) {
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
// sleeper wandering and puts her back: 230 of each a second until the tick ended, and once a kick for Â« too many
// packets Â» (local server). The confirm always goes; the move to the spot she was just put in goes once, and again
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
