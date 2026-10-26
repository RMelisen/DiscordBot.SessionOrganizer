// SYNCS's body on the Minecraft server: a Mineflayer client the C# bot starts and drives.
//
// The body owns no policy and no words. It reports what happens as one JSON object per line on
// stdout, and does what it is told by one JSON object per line on stdin. Which line she says,
// when she joins and when she leaves are all decided in C# (MinecraftBodyService): her voice
// lives in BotResponses, behind ResponsePicker, never here. Logs go to stderr.
//
// This file connects and reads the commands. The body itself is in lib/, a module per part of her life (gather.js,
// water.js, combat.js…), with lib/wiring.js listening to the game, lib/commands.js doing what C# asks, and
// lib/compat.js holding every workaround for Mineflayer lagging behind the server's version.
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
//      {"op":"debug"} (/debug minecraft: answered with a debug event, lib/debug.js)
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
//      debug {pid,node,uptime,rss,version,pos,dimension,health,food,oxygen,task:{kind,player,mode,own,resume}|null,
//        heldFor,sitting,eating,sleeping,swim,riding,bridge:{bridging,standing},moving,goal,loop:{p50,p99,max}|null,
//        compat:{tickEnd,renumberedActions,corrections,packetsPeak,packetsPeakAt}}

import './lib/console.js';
import readline from 'node:readline';
import mineflayer from 'mineflayer';
import mc from 'minecraft-protocol';
import minecraftData from 'minecraft-data';
import { dispatch } from './lib/commands.js';
import { install as installCompat } from './lib/compat.js';
import { watchRecipes } from './lib/crafting.js';
import { watchLoop } from './lib/debug.js';
import { restartWith } from './lib/restart.js';
import { bot, emit, ended, finish, log, setBot } from './lib/state.js';
import { attach } from './lib/wiring.js';

const host = process.env.MC_HOST;
const port = Number.parseInt(process.env.MC_PORT || '25565', 10);
const username = process.env.MC_USERNAME || 'SYNCS';

if (!host) {
  log('MC_HOST is not set');
  process.exit(2);
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

async function main() {
  const version = await pickVersion();
  if (ended) return;
  // Offline mode: no auth.
  setBot(mineflayer.createBot({ host, port, username, auth: 'offline', version }));
  watchRecipes(bot._client);
  installCompat(bot._client);
  watchLoop();
  attach();
}

// An order a fight interrupted comes back through the same door as C#'s.
restartWith(dispatch);

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
