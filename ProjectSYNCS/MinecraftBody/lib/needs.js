// Needs: worn-out tools and what she lacks, reported to the service.

import { kitTools } from './inventory.js';
import { arrows } from './shooting.js';
import { bot, emit, task } from './state.js';

// Every NeedsEvery: a kit tool at WornAt of its durability or less is reported once (`wornOut`), and again only for a
// new one (its durability went back up); one reported worn that disappears (not handed over, not lost in a death)
// `broke`. And what she lacks (`lacking`), at most once a minute each: C# decides what's said.
export const NeedsEvery = 10000;
const WornAt = 0.1;
const worn = new Map();
const lackingAt = new Map();
let handedAt = 0;

export function setHandedAt(value) {
  handedAt = value;
}
let diedAt = 0;

export function setDiedAt(value) {
  diedAt = value;
}

export function lacking(what) {
  if ((lackingAt.get(what) || 0) > Date.now()) return;
  lackingAt.set(what, Date.now() + 60000);
  emit('lacking', { what });
}

export function needsTick() {
  if (!bot.entity || !bot.isAlive) return;
  const items = bot.inventory.items();
  const handing = (task && (task.kind === 'give' || task.kind === 'store')) || Date.now() - handedAt < 15000 || Date.now() - diedAt < 30000;
  for (const [tool, item] of Object.entries(kitTools(items))) {
    const was = worn.get(tool);
    if (!item) {
      if (was === undefined) continue;
      worn.delete(tool);
      if (!handing) emit('broke', { tool });
      continue;
    }
    const max = bot.registry.items[item.type] && bot.registry.items[item.type].maxDurability;
    if (!max) continue;
    const left = max - (item.durabilityUsed || 0);
    if (was !== undefined && left > was) worn.delete(tool); // a new one
    if (left / max > WornAt) {
      worn.delete(tool);
      continue;
    }
    if (!worn.has(tool)) emit('wornOut', { tool });
    worn.set(tool, left);
  }
  if (task && (task.kind === 'guard' || task.kind === 'patrol') && items.some((i) => i.name === 'bow') && !arrows()) lacking('arrows');
  if (bot.inventory.emptySlotCount() === 0) lacking('space');
}
