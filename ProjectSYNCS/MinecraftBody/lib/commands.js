// The commands from C# (one JSON object per line on stdin, read by index.js), each checked and handed to what does it.
// An order interrupted by a fight comes back through here too (restart.js).

import { dress, undress } from './armour.js';
import { breakCamp, makeCamp } from './camp.js';
import { holdForFight } from './combat.js';
import { smelt } from './cooking.js';
import { craftFor } from './craftorder.js';
import { snapshot } from './debug.js';
import { defend } from './defend.js';
import { fetchFromChests } from './fetch.js';
import { setFamilyNames } from './family.js';
import { farm, harvest } from './farm.js';
import { fish } from './fishing.js';
import { flee } from './flee.js';
import { collect } from './gather.js';
import { hop, mark, sit, wave, where } from './gestures.js';
import { guard, patrol } from './guard.js';
import { give, store } from './inventory.js';
import { come, follow, goTo, guide, holdHere, isOrder, letGo, OrderOps, playerEntity, skipGrace, stay } from './orders.js';
import { ranch } from './ranch.js';
import { Restartable } from './resume.js';
import { failedSpots, routine } from './routine.js';
import { reportSeen, seek } from './seek.js';
import { sleep } from './sleep.js';
import { bot, emit, finish, log, task } from './state.js';

// One command, from C# or an order she starts again after a fight (restart.js).
export function dispatch(command) {
  try {
    if (holdForFight(command)) return;
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
          holdHere();
        }
        break;
      case 'free':
        // Back to her own life at once: the order (or activity) ends, a « reste » hold too, and no grace before autonomy.
        if (bot) {
          stay();
          letGo();
          skipGrace();
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
      case 'fetch':
        if (bot && typeof command.player === 'string' && typeof command.goods === 'string')
          fetchFromChests(command.player, command.goods, Math.max(0, Math.min(64, Number(command.count) || 0)))
            .catch((err) => log('fetch failed:', err && err.message ? err.message : String(err)));
        break;
      case 'craft':
        // Started again after a fight (resume.js): her pockets at the start (`since`) and the table she had set down.
        if (bot && typeof command.player === 'string' && typeof command.item === 'string')
          craftFor(command.player, command.item, Math.max(1, Math.min(64, Number(command.count) || 1)),
            command.since ? { before: command.since, table: command.table || null } : null)
            .catch((err) => log('craft failed:', err && err.message ? err.message : String(err)));
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
      case 'debug':
        emit('debug', snapshot());
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
          holdHere();
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
        if (Array.isArray(command.names)) setFamilyNames(command.names);
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
    if (bot && OrderOps.has(command.op) && isOrder(task)) letGo();
    // What a fight can interrupt and start again: the command that started the order now under way.
    if (Restartable.has(command.op) && task && task.kind === command.op && !task.command) task.command = command;
  } catch (err) {
    log('command failed:', err && err.message ? err.message : String(err));
  }
}
