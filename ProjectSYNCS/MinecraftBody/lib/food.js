// Eating when hungry, eating to heal, and asking for food when there's none.

import { Reserved } from './emergency.js';
import { CookAbove, startForage } from './forage.js';
import { isFoe } from './guard.js';
import { shieldDown } from './ranged.js';
import { drawing } from './shooting.js';
import { bot, emit, log, task } from './state.js';

const BadFood = new Set(['rotten_flesh', 'spider_eye', 'poisonous_potato', 'pufferfish', 'suspicious_stew', 'chorus_fruit']);
export let eating = false;

export function setEating(value) {
  eating = value;
}

// Below 14 she's hungry. Hurt in a fight, she eats up to full (20): only a full stomach heals. The most filling
// thing first. While she's out finding her supper she waits for it to be cooked, unless she's starving.
export async function eatIfHungry(below = 14) {
  if (eating || bot.food >= below) return;
  if (task && task.kind === 'forage' && bot.food > CookAbove) return;
  const food = bot.inventory.items().filter((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name) && !Reserved.has(i.name))
    .sort((a, b) => bot.registry.foodsByName[b.name].foodPoints - bot.registry.foodsByName[a.name].foodPoints)[0];
  if (!food) return;
  eating = true;
  const before = bot.inventory.count(food.type);
  try {
    shieldDown();
    await bot.equip(food, 'hand');
    await bot.consume();
  } catch (err) {
    // Mineflayer waits 2.5 s for the « done eating » status, a meal takes 1.6 s, and a slow link (the Pi, ViaBackwards)
    // eats the rest: when the food went down, she ate.
    if (bot.inventory.count(food.type) >= before) log('eating failed:', err && err.message ? err.message : String(err));
  } finally {
    eating = false;
  }
}

export function hasFood() {
  return bot.inventory.items().some((i) => bot.registry.foodsByName[i.name] && !BadFood.has(i.name) && !Reserved.has(i.name));
}

// Hurt, she eats to heal: Minecraft heals her only while her food bar is at 18 or more, and quickly only with
// saturation left. So below full health she eats when the bar is under 18, or under 20 with no saturation left. Not
// with a monster close (a fight's retreat eats for her, and the food in her hand would be her sword), and not while
// she digs, draws her bow, stacks a block or fishes: the food in her hand would cut it off. The tick tries again.
// With nothing to eat and a fair bit hurt, she goes to find some (startForage), as when she's hungry.
export const HealEvery = 5000;
const HealSafe = 10;
const HealForage = 14;

export function healTick() {
  if (eating || bot.isSleeping || bot.health <= 0 || bot.health >= 20 || bot.food >= 20) return;
  if (bot.food >= 18 && bot.foodSaturation > 0) return;
  if (bot.targetDigBlock || drawing || bot.pathfinder.isBuilding() || (task && task.casting)) return;
  if (bot.nearestEntity((e) => isFoe(e) && e.position.distanceTo(bot.entity.position) < HealSafe)) return;
  if (hasFood()) eatIfHungry(20).catch(() => {});
  else if (bot.health <= HealForage && bot.food < 18) startForage();
}

// Hungry with nothing to eat: she looks for some herself when she can (startForage); otherwise she reports it each
// time her food bar drops a notch (C# decides whether she asks), and remembers she did, so the next food she picks
// up is someone feeding her.
let hungerReported = null;
let waitingForFood = false;

export function setWaitingForFood(value) {
  waitingForFood = value;
}

export function reportHunger() {
  if (bot.food >= 14 || hasFood()) {
    hungerReported = null;
    return;
  }
  if (startForage()) return;
  if (hungerReported === bot.food) return;
  hungerReported = bot.food;
  waitingForFood = true;
  emit('hungry', { food: bot.food });
}

// Food landed in her inventory after she asked: whoever stands closest gets the thanks, and she eats.
export function checkFed() {
  if (!waitingForFood || !hasFood()) return;
  waitingForFood = false;
  hungerReported = null;
  const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
    && e.position.distanceTo(bot.entity.position) < 8);
  if (near) emit('fed', { by: near.username });
  eatIfHungry().catch(() => {});
}
