// Feeding herself with nothing to eat: crops nearby or fishing, then cooking the catch.

import pathfinderPackage from 'mineflayer-pathfinder';
import { fuelValue, SmeltBlocks, SmeltInputs, SmeltRadius } from './cooking.js';
import { Crops, findRipe, harvestBlocks, HarvestRadius } from './farm.js';
import { fishingRod, fishLoop, openWater } from './fishing.js';
import { eatIfHungry, eating, hasFood, reportHunger, setWaitingForFood } from './food.js';
import { isActivity, isFree, resumeActivity, stay } from './orders.js';
import { bot, log, setTask, task, wait } from './state.js';

const { goals } = pathfinderPackage;

// Hungry with nothing on her, and free (or at an activity, which this pauses and goes back to): before asking
// anyone she looks for food herself, and says nothing about it (the owner's call). Sometimes she picks a few ripe
// crops nearby (replanted, like any harvest), sometimes she fishes, whichever is around, then the other if the
// first gave nothing. With time to spare (not starving) and a free furnace or smoker close, she cooks it first.
// Nothing found: she asks, as before, and doesn't look again for a while.
const EdibleCrops = [Crops.carrot, Crops.potato, Crops.beetroot, Crops.melon];
const ForagePlants = 3;
const ForageFishTime = 120000;
const ForageRetry = 300000;
// Her food bar: at this or below she eats what she has, raw, rather than wait by a furnace.
export const CookAbove = 6;
const CookMax = 8;
let forageAt = 0;

// True when she sets off (or already has): reportHunger then doesn't ask.
export function startForage() {
  if (task && task.kind === 'forage') return true;
  if (eating || bot.isSleeping || Date.now() < forageAt) return false;
  if (task && !isFree(task) && !isActivity(task)) return false; // someone's order
  if (task && (task.kind === 'fish' || task.kind === 'retrieve')) return false; // fishing feeds her; her things won't wait
  const sources = [];
  if (findRipe(EdibleCrops, bot.entity.position, HarvestRadius).length > 0) sources.push('crops');
  if (fishingRod() && openWater()) sources.push('fish');
  if (sources.length === 0) return false;
  if (Math.random() < 0.5) sources.reverse();
  forage(sources).catch((err) => log('forage failed:', err && err.message ? err.message : String(err)));
  return true;
}

async function forage(sources) {
  const resume = isActivity(task) ? task : null; // her own trip or tagging along just ends: she picks again after
  stay();
  setWaitingForFood(false); // what she picks up now isn't someone feeding her
  const mine = { kind: 'forage', player: '', resume, forage: true, casting: false, window: null };
  setTask(mine);
  for (const source of sources) {
    if (source === 'crops') {
      const here = bot.entity.position;
      const found = findRipe(EdibleCrops, here, HarvestRadius)
        .sort((a, b) => a.block.position.distanceTo(here) - b.block.position.distanceTo(here))
        .slice(0, ForagePlants);
      if (found.length > 0) await harvestBlocks(found, mine);
    } else {
      mine.water = openWater();
      mine.until = Date.now() + ForageFishTime;
      if (mine.water && fishingRod()) await fishLoop(mine);
    }
    if (task !== mine) return; // an order came: it's hers now
    if (hasFood()) break;
  }
  if (hasFood() && bot.food > CookAbove) await cookForHerself(mine);
  if (task !== mine) return;
  setTask(null);
  if (!hasFood()) forageAt = Date.now() + ForageRetry;
  resumeActivity(resume); // before eating: free for a moment, bedtime would take her and the activity would be lost
  await eatIfHungry();
  reportHunger(); // still nothing: now she asks
}

// The raw food she carries (meat, fish, potatoes), up to CookMax of one kind, into the closest free furnace or smoker
// with her own fuel. Free means empty: nothing in it at all. She waits, and takes back what's left if it runs long.
async function cookForHerself(mine) {
  const items = bot.inventory.items();
  const raw = items.find((i) => SmeltInputs.food(i.name) && bot.registry.foodsByName[i.name]);
  const fuel = items.filter((i) => fuelValue(i.name) > 0 && !SmeltInputs.food(i.name))
    .sort((a, b) => fuelValue(b.name) - fuelValue(a.name))[0];
  if (!raw || !fuel) return;
  const count = Math.min(raw.count, CookMax, Math.floor(fuelValue(fuel.name) * fuel.count));
  if (count < 1) return;
  const ids = bot.registry.blocksArray.filter((b) => SmeltBlocks.food.includes(b.name)).map((b) => b.id);
  const furnaces = bot.findBlocks({ matching: ids, maxDistance: SmeltRadius, count: 8 })
    .sort((a, b) => a.distanceTo(bot.entity.position) - b.distanceTo(bot.entity.position));
  for (const at of furnaces) {
    let box;
    try {
      await bot.pathfinder.goto(new goals.GoalNear(at.x, at.y, at.z, 2));
      if (task !== mine) return;
      box = await bot.openFurnace(bot.blockAt(at));
    } catch (err) {
      if (task !== mine) return;
      log('furnace refused:', err && err.message ? err.message : String(err));
      continue;
    }
    if (box.inputItem() || box.outputItem() || box.fuelItem()) {
      box.close();
      continue;
    }
    mine.window = box;
    try {
      await box.putInput(raw.type, null, count);
      await box.putFuel(fuel.type, null, Math.ceil(count / fuelValue(fuel.name)));
      const deadline = Date.now() + count * 12000 + 15000;
      while (task === mine && box.inputItem() && Date.now() < deadline) {
        if (box.outputItem()) await box.takeOutput();
        await wait(1000);
      }
      if (task === mine) {
        if (box.outputItem()) await box.takeOutput();
        if (box.inputItem()) await box.takeInput();
        if (box.fuelItem()) await box.takeFuel(); // an empty bucket, or fuel left over
      }
    } catch (err) {
      log('cooking stopped:', err && err.message ? err.message : String(err));
    }
    if (task !== mine) return; // stay() closed the window
    box.close();
    mine.window = null;
    return;
  }
}
