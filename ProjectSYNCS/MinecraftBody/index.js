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
//      {"op":"harvest","player":"…","crop":"any|wheat|carrot|potato|beetroot|melon|pumpkin"} · {"op":"farm","player":"…"}
// Out: version {server,using,fallback} · spawned · players {names} · chat {player,text,private}
//      system {key,args:[{text,key}]} · hurt {by} · died · night · kicked {reason} · end {reason}
//      moving {player,kind: come|follow|goto|guard} · arrived {player,kind}
//      cantReach {player,why: notVisible|noPath|lost|tooFar} · killed {player}
//      collecting {player,resource,count} · collected {player,resource,count,delivered}
//      cantCollect {player,resource,why: none|noTool} · sleeping {player} · cantSleep {player,why: day|noBed|failed}
//      seek {player,outcome: met|missing} · seen {player,x,y,z,dimension}
//      harvesting {player,crop,count} · harvested {player,crop,count,replanted,delivered} · cantHarvest {player,crop,why: none}
//      farming {player} · cantFarm {player,why: none|noChest}

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
  return movements;
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
// walks partial paths towards the goal and plans again as the world loads.
// Every order checks it can be done before stay() drops the current one: an order that fails at once
// (out of sight, too far, nothing to gather) leaves her doing what she was doing.
function goTo(player, x, y, z) {
  const here = bot.entity.position;
  if (Math.hypot(x - here.x, z - here.z) > MaxTrip) return emit('cantReach', { player, why: 'tooFar' });
  stay();
  task = { kind: 'goto', player, reported: false };
  bot.pathfinder.setGoal(y === null ? new goals.GoalNearXZ(x, z, 2) : new goals.GoalNear(x, y, z, 1));
  emit('moving', { player, kind: 'goto' });
}

function stay() {
  if (task && (task.kind === 'collect' || task.kind === 'harvest' || task.kind === 'farm'))
    bot.collectBlock.cancelTask().catch(() => {});
  task = null;
  farmAfterSleep = null;
  bot.pathfinder.setGoal(null);
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

function guard(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantReach', { player, why: 'notVisible' });
  stay();
  task = { kind: 'guard', player, foe: null, reported: false };
  bot.pathfinder.setGoal(new goals.GoalFollow(target, 2), true);
  emit('moving', { player, kind: 'guard' });
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

// Every physics tick while guarding: pick the closest monster near the player (or near her), chase
// it, hit it when in reach, then go back to following.
function guardTick() {
  if (!task || task.kind !== 'guard') return;
  const owner = playerEntity(task.player);
  if (!owner) {
    emit('cantReach', { player: task.player, why: 'lost' });
    stay();
    return;
  }
  if (task.foe && (!task.foe.isValid || task.foe.position.distanceTo(owner.position) > GuardRadius * 1.6)) {
    task.foe = null;
    bot.pathfinder.setGoal(new goals.GoalFollow(owner, 2), true);
  }
  if (!task.foe) {
    const foe = bot.nearestEntity((e) => isFoe(e)
      && (e.position.distanceTo(owner.position) < GuardRadius || e.position.distanceTo(bot.entity.position) < 6));
    if (!foe) return;
    task.foe = foe;
    equipWeapon();
    bot.pathfinder.setGoal(new goals.GoalFollow(foe, 1), true);
  }
  const foe = task.foe;
  if (foe.position.distanceTo(bot.entity.position) < MeleeReach && Date.now() >= attackReadyAt) {
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

  const after = itemCounts();
  const gained = [...after].map(([type, n]) => [type, n - (before.get(type) || 0)]).filter(([, n]) => n > 0);
  const total = gained.reduce((sum, [, n]) => sum + n, 0);

  // Bring it back: walk up to the player and toss everything she gathered at their feet.
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
  if (task === mine) task = null;
  emit('collected', { player, resource, count: total, delivered });
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
  if (task === mine) task = null;
  emit('harvested', { player, crop: key, count: total, replanted, delivered });
}

// « syncs occupe-toi de la ferme »: she stays on this spot and, every round, harvests what ripened within
// FarmRadius, replants, and stores the produce in the chest by the field. A farm needs crops and a chest:
// without one she says so instead of filling her pockets all evening.
function farm(player) {
  const anchor = bot.entity.position.clone();
  const cropIds = bot.registry.blocksArray.filter((b) => Object.values(Crops).some((c) => c.block === b.name)).map((b) => b.id);
  if (bot.findBlocks({ matching: cropIds, maxDistance: FarmRadius, count: 1 }).length === 0)
    return emit('cantFarm', { player, why: 'none' });
  const chestIds = bot.registry.blocksArray.filter((b) => b.name === 'chest' || b.name === 'barrel').map((b) => b.id);
  const chest = bot.findBlocks({ matching: chestIds, maxDistance: ChestRadius, count: 1 })[0];
  if (!chest) return emit('cantFarm', { player, why: 'noChest' });
  stay();
  task = { kind: 'farm', player, anchor, chest, busy: false };
  emit('farming', { player });
  farmRound();
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

// Puts the produce in the farm's chest, keeping back enough seeds to replant. A full chest keeps the rest on her.
async function storeProduce(mine) {
  const block = bot.blockAt(mine.chest);
  if (!block || (block.name !== 'chest' && block.name !== 'barrel')) return;
  const totals = new Map();
  for (const item of bot.inventory.items())
    if (FarmProduce.has(item.name)) totals.set(item.name, (totals.get(item.name) || 0) + item.count);
  const toStore = [...totals].map(([name, n]) => [name, n - (SeedsKept[name] || 0)]).filter(([, n]) => n > 0);
  if (toStore.length === 0) return;
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

// ---- Sleeping ----------------------------------------------------------------------------------------

// The night only passes when everyone on the server sleeps (by default), her included: when a player
// goes to bed she goes too, once per night, or says why she can't.
let sleptOnDay = null;
// A farm she was keeping when she went to bed: she goes back to it when she wakes. Any order clears it (stay()).
let farmAfterSleep = null;

function isNight() {
  const t = bot.time.timeOfDay;
  return (t >= 12542 && t <= 23459) || bot.thunderState > 0;
}

async function sleep(player) {
  if (bot.isSleeping) return;
  if (!isNight()) return emit('cantSleep', { player, why: 'day' });
  sleptOnDay = bot.time.day;
  const bedIds = bot.registry.blocksArray.filter((b) => b.name.endsWith('_bed')).map((b) => b.id);
  const beds = bot.findBlocks({ matching: bedIds, maxDistance: GatherRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  if (beds.length === 0) return emit('cantSleep', { player, why: 'noBed' });
  const keptFarm = task && task.kind === 'farm' ? task : farmAfterSleep;
  stay();
  farmAfterSleep = keptFarm;
  const mine = { kind: 'sleep', player };
  task = mine;
  for (const at of beds) {
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 1));
      await bot.sleep(bot.blockAt(at));
      if (task === mine) task = null;
      return emit('sleeping', { player });
    } catch (err) {
      log('bed refused:', err && err.message ? err.message : String(err)); // occupied, too far, monsters near
      if (task !== mine) return;
    }
  }
  task = null;
  resumeFarm();
  emit('cantSleep', { player, why: 'failed' });
}

// Back to the farm she was keeping, after a night's sleep or a bed that refused her.
function resumeFarm() {
  if (!farmAfterSleep || task) return;
  task = farmAfterSleep;
  task.busy = false;
  farmAfterSleep = null;
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

async function eatIfHungry() {
  if (eating || bot.food >= 14) return;
  const food = bot.inventory.items().find((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name));
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

function attach() {
  bot.loadPlugin(pathfinder);
  bot.loadPlugin(collectBlock);

  bot.once('spawn', () => {
    bot.pathfinder.setMovements(gentleMovements());
    emit('spawned', {});
    reportPlayers();
    wearBestArmor().catch(() => {});
  });

  bot.on('physicsTick', guardTick);
  setInterval(() => { if (bot) farmRound().catch(() => {}); }, FarmRound);
  bot.on('wake', resumeFarm);
  bot.on('health', () => { eatIfHungry().catch(() => {}); });
  bot.on('playerCollect', (collector) => {
    if (collector === bot.entity) setTimeout(() => { wearBestArmor().catch(() => {}); }, 300);
  });

  bot.on('entityDead', (entity) => {
    if (!task || task.kind !== 'guard' || task.foe !== entity) return;
    emit('killed', { player: task.player });
    task.foe = null;
    const owner = playerEntity(task.player);
    if (owner) bot.pathfinder.setGoal(new goals.GoalFollow(owner, 2), true);
  });

  // A player went to bed at night: she follows suit, once per night.
  bot.on('entitySleep', (entity) => {
    if (entity === bot.entity || entity.type !== 'player' || !isNight() || sleptOnDay === bot.time.day) return;
    sleep(entity.username).catch((err) => log('sleep failed:', err && err.message ? err.message : String(err)));
  });

  bot.on('goal_reached', () => {
    if (task && task.kind === 'seek') return seekReached();
    if (!task || (task.kind !== 'come' && task.kind !== 'goto')) return;
    emit('arrived', { player: task.player, kind: task.kind });
    task = null;
  });

  // No way there. Coming and going give up at once; following and guarding keep trying (the player
  // may walk somewhere reachable) but say so only once per order. Gathering and sleeping handle their
  // own paths.
  bot.on('path_update', (result) => {
    if (!task || result.status !== 'noPath' || task.reported) return;
    if (task.kind === 'collect' || task.kind === 'sleep' || task.kind === 'harvest' || task.kind === 'farm') return;
    if (task.kind === 'seek') {
      emit('seek', { player: task.player, outcome: 'missing' });
      return stay();
    }
    task.reported = true;
    emit('cantReach', { player: task.player, why: 'noPath' });
    if (task.kind === 'come' || task.kind === 'goto') stay();
  });

  // The player she follows or guards went out of sight (too far, or logged out): she stops there.
  bot.on('entityGone', (entity) => {
    reportSeen(entity);
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
    emit('chat', { player, text, private: false });
  });
  bot.on('whisper', (player, text) => {
    if (player === bot.username) return;
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
        if (bot) stay();
        break;
      case 'goto':
        if (bot && typeof command.player === 'string' && Number.isFinite(command.x) && Number.isFinite(command.z))
          goTo(command.player, command.x, Number.isFinite(command.y) ? command.y : null, command.z);
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
