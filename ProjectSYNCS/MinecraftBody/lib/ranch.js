// The animals (« occupe-toi des animaux »): fed, bred, sheared, culled over the herd's size, drops in a chest.

import pathfinderPackage from 'mineflayer-pathfinder';
import { chestNear, chestsNear, FarmRadius, storeIn } from './farm.js';
import { lacking } from './needs.js';
import { stay } from './orders.js';
import { lootSpots, pickUpDrops, pruneLoot } from './pickup.js';
import { equipWeapon } from './shooting.js';
import { bot, emit, log, setTask, task, wait } from './state.js';
import { strike } from './strike.js';

const { goals } = pathfinderPackage;

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
export const RanchRound = 15000;
const FedAgain = 300000;
const ShearAgain = 300000;
const CullPerRound = 2;
const KillFor = 15000;
const MeatKept = 8;
const Meats = new Set(['beef', 'porkchop', 'chicken', 'mutton', 'rabbit',
  'cooked_beef', 'cooked_porkchop', 'cooked_chicken', 'cooked_mutton', 'cooked_rabbit']);
const isRanchGoods = (n) => Meats.has(n) || n === 'leather' || n.endsWith('_wool') || n === 'feather' || n === 'rabbit_hide';

// An entity's metadata by name (minecraft-data's metadataKeys); undefined when the version doesn't name it.
export function meta(entity, key) {
  const known = bot.registry.entitiesByName[entity.name];
  const i = known && known.metadataKeys ? known.metadataKeys.indexOf(key) : -1;
  return i >= 0 && entity.metadata ? entity.metadata[i] : undefined;
}

function knowsAge(entity) {
  const known = bot.registry.entitiesByName[entity.name];
  return Boolean(known && known.metadataKeys && known.metadataKeys.includes('baby'));
}

const isBaby = (e) => meta(e, 'baby') === true;
export const isNamed = (e) => Boolean(meta(e, 'custom_name'));
export const isShorn = (e) => (Number(meta(e, 'wool')) & 0x10) !== 0;

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
export function penAt(anchor) {
  if (herdAround(anchor).size === 0) return { why: 'none' };
  const chest = chestNear(anchor);
  return chest ? { chest } : { why: 'noChest' };
}

export function ranch(player) {
  const anchor = bot.entity.position.clone();
  const pen = penAt(anchor);
  if (pen.why) return emit('cantRanch', { player, why: pen.why });
  stay();
  setTask({ kind: 'ranch', player, anchor, chest: pen.chest, busy: false, fedAt: new Map(), shornAt: new Map(), target: null });
  emit('ranching', { player });
  ranchRound();
}

export async function ranchRound() {
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
      // Drops roll off the spot, and a cull's three or four stacks don't all come in 10 s (3 beef were left lying).
      await pickUpDrops(spot.pos, 4, 20000, mine);
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
    if (feedCount(species) < 2) await fetchFeed(mine, species);
    if (task !== mine) return;
    if (feedCount(species) < 2) {
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

function feedCount(species) {
  return bot.inventory.items().filter((i) => Livestock[species](i.name)).reduce((sum, i) => sum + i.count, 0);
}

// Short of a species' food, she takes some from the pen's chests (within ChestRadius of it), FeedTaken at most: the
// wheat a farm stored is there. Chests that had none aren't opened again for FeedLookAgain, or every round would.
const FeedTaken = 16;
const FeedLookAgain = 120000;

async function fetchFeed(mine, species) {
  if (!mine.feedLookedAt) mine.feedLookedAt = new Map(); // a copy made to resume the pen may not have it
  if (Date.now() < (mine.feedLookedAt.get(species) || 0) + FeedLookAgain) return;
  mine.feedLookedAt.set(species, Date.now());
  for (const at of chestsNear(mine.anchor, 4)) {
    let want = FeedTaken - feedCount(species);
    if (task !== mine || want <= 0) return;
    const block = bot.blockAt(at);
    if (!block || (block.name !== 'chest' && block.name !== 'barrel')) continue;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
      if (task !== mine) return;
      const box = await bot.openContainer(block);
      try {
        const there = new Map();
        for (const i of box.containerItems())
          if (Livestock[species](i.name)) there.set(i.type, (there.get(i.type) || 0) + i.count);
        for (const [type, n] of there) {
          if (want <= 0) break;
          const take = Math.min(want, n);
          await box.withdraw(type, null, take);
          want -= take;
        }
      } finally {
        box.close();
      }
    } catch (err) {
      log('feed not taken:', err && err.message ? err.message : String(err));
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
    // The closest first: in a crowded pen the far ones are behind the others, and she spent the round on one.
    while (surplus > 0 && left > 0 && victims.length > 0 && task === mine) {
      victims.sort((a, b) => a.position.distanceTo(bot.entity.position) - b.position.distanceTo(bot.entity.position));
      if (await slaughter(mine, victims.shift())) {
        surplus--;
        left--;
      }
    }
  }
}

// The same hits as defending: chase, and hit on the weapon's cooldown, until it's dead or KillFor has passed. One she
// can't get a first hit on within ReachFor (the others in the way) is left for the next. Whether it died.
const ReachFor = 6000;

async function slaughter(mine, animal) {
  mine.target = animal;
  await equipWeapon();
  bot.pathfinder.setGoal(new goals.GoalFollow(animal, 2), true);
  const start = Date.now();
  let hit = false;
  while (task === mine && animal.isValid !== false && bot.entities[animal.id] && Date.now() < start + KillFor) {
    if (!hit && Date.now() > start + ReachFor) break;
    if (strike(animal)) hit = true;
    await wait(100);
  }
  if (task === mine) {
    bot.pathfinder.setGoal(null);
    mine.target = null;
  }
  return animal.isValid === false || !bot.entities[animal.id];
}

// Walks up to an animal and uses what `matches` on it (food, shears).
export async function useOn(animal, matches) {
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
