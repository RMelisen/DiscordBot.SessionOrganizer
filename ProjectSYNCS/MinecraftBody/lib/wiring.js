// Every event she listens to, wired once the bot exists (attach). The physics tick handlers run in the order they
// are registered here, and that order matters: keep it.

import pathfinderPackage from 'mineflayer-pathfinder';
import collectBlockPackage from 'mineflayer-collectblock';
import prismarineChat from 'prismarine-chat';
import { answerTo } from './fight.js';
import { bare, dressAfterPickup, wearBestArmor } from './armour.js';
import { fillStates } from './blocks.js';
import { boardTick, fixPassengers, forgetBoat, offBoatTick, onDismount, onMount, rideTick } from './boats.js';
import { bridgeTick } from './bridges.js';
import { exactHits, fightTick, selfDefend, threatTick } from './combat.js';
import { inputTick, onSpawn as compatOnSpawn } from './compat.js';
import { creeperTick, creeperWatchTick, dodgeTick, warnedCreepers } from './creepers.js';
import { defend, DefendRadius, defendTick } from './defend.js';
import { isFamily } from './family.js';
import { doorTick, setPath } from './doors.js';
import { emergencyTick } from './emergency.js';
import { farmRound, FarmRound } from './farm.js';
import { fleeTick } from './flee.js';
import { checkFed, eatIfHungry, HealEvery, healTick, reportHunger } from './food.js';
import { toolOrHand } from './gather.js';
import { HitSulk, idleTick, lastChat, setHitBy } from './gaze.js';
import { guardTick, isFoe } from './guard.js';
import { hazardTick } from './hazards.js';
import { Goods, scheduleInventory } from './inventory.js';
import { farmlandTick, gentleMovements } from './movements.js';
import { NeedsEvery, needsTick, setDiedAt } from './needs.js';
import { autonomyTick, freeTick, guideTick, stay } from './orders.js';
import { goto, onPathUpdate, timeoutTick } from './pathing.js';
import { lootSpots } from './pickup.js';
import { crossAfter, portalTick } from './portals.js';
import { meta, ranchRound, RanchRound } from './ranch.js';
import { fireballTick, shieldBroken, shieldTick } from './ranged.js';
import { backTick } from './resume.js';
import { armRetrieve, deathAt, onDeath, sweepRetrieve } from './retrieve.js';
import { arriveOwn } from './routine.js';
import { reportSeen, seekReached } from './seek.js';
import { autoSleepTick, isNight, sleep, sleptOnDay, wakeUp } from './sleep.js';
import { bot, emit, finish, log, part, reportPlayers, safely, setTask, task } from './state.js';
import { forgetSwim, waterTick } from './water.js';

const { pathfinder, goals } = pathfinderPackage;
const { plugin: collectBlock } = collectBlockPackage;

export function attach() {
  // Her physics tick handlers alone are past Node's 10 listeners an event, which it warns about as a leak (in the logs
  // /debug minecraft shows).
  bot.setMaxListeners(50);
  bot.loadPlugin(pathfinder);
  bot.loadPlugin(collectBlock);

  bot.once('spawn', () => {
    fillStates(bot.registry);
    compatOnSpawn();
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
    bot.on('physicsTick', safely(hazardTick)); // after her swimming: out of lava comes first
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

  bot.on('physicsTick', safely(guardTick));
  bot.on('physicsTick', safely(fleeTick));
  bot.on('physicsTick', safely(defendTick));
  bot.on('physicsTick', safely(threatTick));
  bot.on('physicsTick', safely(fightTick));
  bot.on('physicsTick', safely(shieldTick));
  bot.on('physicsTick', safely(fireballTick));
  bot.on('physicsTick', safely(emergencyTick));
  // Her shield broken by an axe: no use for ShieldDisabled. Her totem popping.
  bot._client.on('entity_status', (p) => safely(() => {
    if (bot.entity && p.entityId === bot.entity.id && p.entityStatus === 30) shieldBroken();
    // Her totem popped: she lived through a blow that should have killed her (C# says so).
    if (bot.entity && p.entityId === bot.entity.id && p.entityStatus === 35) emit('totem', {});
  })());
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
  bot.on('physicsTick', safely(bridgeTick));
  setInterval(() => { if (bot) ranchRound().catch(() => {}); }, RanchRound);
  setInterval(() => { if (bot) safely(needsTick)(); }, NeedsEvery);
  setInterval(() => { if (bot) safely(rideTick)(); }, 500);
  bot.on('mount', safely(onMount));
  bot.on('dismount', onDismount);
  bot._client.on('set_passengers', fixPassengers);
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
    setTask(null);
  });

  // A ladder met from the side, a search out of time, and no way there (pathing.js).
  bot.on('path_update', onPathUpdate);
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
        setHitBy({ name: source.username, until: Date.now() + HitSulk });
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
    setDiedAt(Date.now());
    if (task && task.kind === 'portal') stay(); // she respawns in the overworld: no crossing
    forgetSwim();
    forgetBoat();
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
    const ChatMessage = prismarineChat(bot.registry);
    return ChatMessage.fromNotch(value).toString();
  } catch {
    return JSON.stringify(value);
  }
}
