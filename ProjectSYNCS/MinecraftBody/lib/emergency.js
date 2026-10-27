// Emergencies: the totem in her off-hand, healing potions, golden apples.

import { bare } from './armour.js';
import { eating, setEating } from './food.js';
import { isFoe, MeleeReach } from './guard.js';
import { shieldDown } from './ranged.js';
import { drawing } from './shooting.js';
import { bot, log } from './state.js';

// Golden apples are for when she's close to dying, never a meal (eatIfHungry, hasFood leave them be).
export const Reserved = new Set(['golden_apple', 'enchanted_golden_apple']);
// At or under this (4 hearts): a totem in her off-hand, then a healing potion, else a golden apple.
const EmergencyHealth = 8;
// Over this (7 hearts), her shield goes back in place of the totem (or of the totem that popped).
const TotemBack = 14;
const RemedyEvery = 3000;
// Potions by their id in the game's potion registry (as her 26.1 client gets them; checked on the local server:
// healing 24, poison 28, fire resistance 11). Only these are drunk: a potion is never tried blind.
export const HealingPotions = new Set([24, 25, 31, 32, 33]); // healing, strong healing, regeneration (normal, long, strong)
export const FireResistPotions = new Set([11, 12]);
let remedyAt = 0;
let swapping = false;

function potionId(item) {
  const c = item && item.components && item.components.find((x) => x.type === 'potion_contents');
  return c && c.data ? c.data.potionId : null;
}

export const isPotion = (item, ids) => item.name === 'potion' && ids.has(potionId(item));

// Every physics tick: close to dying, a totem goes in her off-hand (the shield to her pockets), and with nothing in
// reach (her hand busy with it, the shield comes down), she drinks a healing potion, else eats a golden apple
// (enchanted first), once every RemedyEvery. Healed past TotemBack, the shield goes back. Not asleep, eating already, or drawing her bow.
export function emergencyTick() {
  if (!bot.entity || bot.isSleeping || eating || drawing || swapping || bot.health <= 0) return;
  const items = bot.inventory.items();
  const offhand = bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')];
  if (bot.health <= EmergencyHealth) {
    const totem = items.find((i) => i.name === 'totem_of_undying');
    if (totem && !(offhand && offhand.name === 'totem_of_undying')) return void swapOffhand(totem);
    if (Date.now() < remedyAt) return;
    if (bot.nearestEntity((e) => isFoe(e) && e.position.distanceTo(bot.entity.position) < MeleeReach)) return;
    const remedy = items.find((i) => isPotion(i, HealingPotions)) || items.find((i) => i.name === 'enchanted_golden_apple')
      || items.find((i) => i.name === 'golden_apple');
    if (remedy) {
      remedyAt = Date.now() + RemedyEvery;
      consumeNow(remedy).catch(() => {});
    }
    return;
  }
  // The totem still there, or gone (it popped): the shield back, unless she was told to take her things off (bare).
  if (bot.health > TotemBack && !bare && (!offhand || offhand.name === 'totem_of_undying')) {
    const shield = items.find((i) => i.name === 'shield');
    if (shield) swapOffhand(shield);
  }
}

async function swapOffhand(item) {
  swapping = true;
  shieldDown();
  try {
    await bot.equip(item, 'off-hand');
  } catch (err) {
    log('off-hand swap failed:', err && err.message ? err.message : String(err));
  } finally {
    swapping = false;
  }
}

// Eats or drinks `item` now, whatever her hunger (a golden apple or a potion always goes down).
export async function consumeNow(item) {
  setEating(true);
  shieldDown();
  const before = bot.inventory.count(item.type);
  try {
    await bot.equip(item, 'hand');
    await bot.consume();
  } catch (err) {
    if (bot.inventory.count(item.type) >= before) log('remedy failed:', err && err.message ? err.message : String(err));
  } finally {
    setEating(false);
  }
}
