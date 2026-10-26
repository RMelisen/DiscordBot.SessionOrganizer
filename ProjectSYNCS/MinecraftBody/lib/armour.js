// Armour: the best she carries worn, put on and taken off when asked.

import { wornArmour, wornGear } from './inventory.js';
import { bot, emit, log, task } from './state.js';

const ArmorSlots = { helmet: 'head', chestplate: 'torso', leggings: 'legs', boots: 'feet' };
const ArmorTiers = ['leather', 'copper', 'golden', 'chainmail', 'iron', 'turtle', 'diamond', 'netherite'];

// A tier she doesn't know still beats wearing nothing: copper armour, missing here, was never put on.
function armorRank(item) {
  return item ? ArmorTiers.indexOf(item.name.split('_')[0]) + 1 : -1;
}

// « enlève ton armure »: she puts nothing on by herself until told to (« mets ton armure ») or given a piece.
export let bare = false;

// Puts on the best piece she carries for each slot, when it beats what she wears, and a shield in an empty off-hand.
// How many she put on.
export async function wearBestArmor() {
  let count = 0;
  for (const [piece, slot] of Object.entries(ArmorSlots)) {
    const worn = bot.inventory.slots[bot.getEquipmentDestSlot(slot)];
    const best = bot.inventory.items().filter((i) => i.name.endsWith('_' + piece))
      .sort((a, b) => armorRank(b) - armorRank(a))[0];
    if (!best || armorRank(best) <= armorRank(worn)) continue;
    try {
      await bot.equip(best, slot);
      count++;
    } catch { /* a slot she can't use; move on */ }
  }
  const shield = bot.inventory.items().find((i) => i.name === 'shield');
  if (shield && !bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')]) {
    try {
      await bot.equip(shield, 'off-hand');
      count++;
    } catch { /* keep it in her pockets */ }
  }
  return count;
}

// Picked something up: armour is a gift, put on (even after « enlève ton armure »), and whoever stands closest gets the
// thanks. Not her own things coming back after a death (`retrieve`), which she puts on without a word.
export async function dressAfterPickup(gift) {
  if (gift) bare = false;
  if (bare) return;
  const count = await wearBestArmor();
  if (count === 0 || !gift || (task && task.kind === 'retrieve')) return;
  const near = bot.nearestEntity((e) => e.type === 'player' && e.username !== bot.username
    && e.position.distanceTo(bot.entity.position) < 8);
  if (near) emit('armoured', { by: near.username });
}

// « mets ton armure »: the best she carries, on. Nothing better to put on: `worn` when she already wears some.
export async function dress(player) {
  bare = false;
  const count = await wearBestArmor();
  if (count > 0) return emit('dressed', { player, count });
  const shielded = (bot.inventory.slots[bot.getEquipmentDestSlot('off-hand')] || {}).name === 'shield';
  emit('cantDress', { player, why: wornArmour().length > 0 || shielded ? 'worn' : 'none' });
}

// « enlève ton armure »: what she wears (a shield in her off-hand too) goes back in her pockets, and stays there. Each
// piece needs a free slot: with full pockets, what's left stays on (`full`).
export async function undress(player) {
  const slots = wornGear();
  if (slots.length === 0) return emit('cantUndress', { player, why: 'none' });
  bare = true;
  let count = 0;
  for (const slot of slots) {
    if (bot.inventory.emptySlotCount() === 0) break;
    try {
      await bot.unequip(slot);
      count++;
    } catch (err) {
      log('undress failed:', err && err.message ? err.message : String(err));
    }
  }
  if (count === 0) {
    bare = false; // still dressed: what she picks up next may still beat it
    return emit('cantUndress', { player, why: 'full' });
  }
  emit('undressed', { player, count, full: count < slots.length });
}
