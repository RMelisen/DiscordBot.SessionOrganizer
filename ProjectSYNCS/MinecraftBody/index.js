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
// In:  {"op":"say","text":"…"} · {"op":"quit"}
// Out: spawned · players {names} · chat {player,text} · system {key,args:[{text,key}]}
//      hurt {by} · died · night · kicked {reason} · end {reason}

'use strict';

const readline = require('node:readline');
const mineflayer = require('mineflayer');

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

// version: false lets Mineflayer ask the server which version it speaks. Offline mode: no auth.
const bot = mineflayer.createBot({ host, port, username, auth: 'offline', version: false });

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

bot.once('spawn', () => {
  emit('spawned', {});
  reportPlayers();
});

bot.on('playerJoined', reportPlayers);
bot.on('playerLeft', reportPlayers);

bot.on('chat', (player, text) => {
  if (player === bot.username) return;
  emit('chat', { player, text });
});

// Deaths and advancements arrive as translated system messages. Only those two families are
// forwarded: the rest (join/leave notices, command feedback) is noise the C# side doesn't use.
bot.on('message', (message) => {
  const key = message.translate;
  if (typeof key !== 'string') return;
  if (!key.startsWith('death.') && !key.startsWith('chat.type.advancement.')) return;
  emit('system', { key, args: (message.with ?? []).map(part) });
});

bot.on('entityHurt', (entity) => {
  if (entity !== bot.entity) return;
  // Mineflayer doesn't say who hit; the closest player within reach is a fair guess, and no
  // player in reach means a mob, fall or fire, which C# ignores.
  const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
    && e.position.distanceTo(bot.entity.position) < 4.5);
  emit('hurt', { by: near ? near.username : null });
});

bot.on('death', () => emit('died', {}));

// Nightfall, once per night: the moment the server's clock crosses from day into night.
let wasDay = null;
bot.on('time', () => {
  const isDay = bot.time.isDay;
  if (wasDay === true && isDay === false) emit('night', {});
  wasDay = isDay;
});

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

bot.on('kicked', (reason) => emit('kicked', { reason: plainReason(reason) }));
bot.on('error', (err) => log('error:', err && err.message ? err.message : String(err)));
bot.on('end', (reason) => finish(reason));

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
        if (typeof command.text === 'string' && command.text.length > 0) bot.chat(command.text.slice(0, 256));
        break;
      case 'quit':
        // A beat so a goodbye sent just before reaches the server ahead of the disconnect.
        setTimeout(() => bot.quit(), 500);
        break;
      default:
        log('unknown op:', command.op);
    }
  } catch (err) {
    log('command failed:', err && err.message ? err.message : String(err));
  }
});

// The C# side went away (crash, restart): leave the server rather than idle in it unattended.
input.on('close', () => {
  try {
    bot.quit();
  } catch {
    // Already gone.
  }
  setTimeout(() => finish('parent closed'), 1000);
});
