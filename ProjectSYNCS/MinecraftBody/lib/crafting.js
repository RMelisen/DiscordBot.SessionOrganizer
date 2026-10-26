// Crafting through her recipe book (the server fills the grid), with bot.craft as the fallback.

import { held, isLog, isPlanks } from './camp.js';
import { bot, log, task, wait } from './state.js';

// One craft, checked: the item must show up in her pockets. Through her recipe book when the recipe is in it
// (bookCraft): the server fills the grid itself and one shift-click takes the result. Mineflayer's bot.craft places
// every ingredient by clicks the server checks one by one, and on 26.1 it failed again and again (the first table craft
// of a mixed recipe always: no bed, no campfire, no pickaxe, so the furnace was dug by hand and lost; with a count it
// made one and put her inventory out of step with the server's), whatever the delays. It stays the way for a recipe
// not in her book yet. Each craft waits for its item, then CraftGap; one that made nothing is tried again once her
// inventory has settled (CraftSettle).
const CraftTries = 3;
const CraftGap = 300;
const CraftSettle = 1500;

// Her recipe book: the display ids of the crafting recipes she knows, by the item they make. The server sends them at
// login and as recipes unlock (as she gets their ingredients), so this listens from the start (main).
const recipeBook = new Map();

export function watchRecipes(client) {
  client.on('recipe_book_add', (packet) => {
    if (packet.replaceAll) recipeBook.clear();
    for (const entry of packet.entries || []) {
      const display = entry.recipe && entry.recipe.display;
      const id = display && display.data && display.data.result && display.data.result.data && display.data.result.data.itemId;
      if (id == null || !/^crafting/.test(String(display.type))) continue;
      if (!recipeBook.has(id)) recipeBook.set(id, []);
      if (!recipeBook.get(id).includes(entry.recipe.displayId)) recipeBook.get(id).push(entry.recipe.displayId);
    }
  });
  client.on('recipe_book_remove', (packet) => {
    for (const ids of recipeBook.values())
      for (const gone of packet.recipeIds || []) if (ids.includes(gone)) ids.splice(ids.indexOf(gone), 1);
  });
}

async function bookCraft(item, table) {
  const window = table ? await bot.openBlock(table) : bot.inventory;
  try {
    for (const displayId of recipeBook.get(item.id) || []) {
      bot._client.write('craft_recipe_request', { windowId: window.id, recipeId: displayId, makeAll: false });
      for (let i = 0; i < 15 && !(window.slots[0] && window.slots[0].type === item.id); i++) await wait(100);
      if (window.slots[0] && window.slots[0].type === item.id) {
        await bot.clickWindow(0, 0, 1); // the result, shift-clicked into her pockets
        return;
      }
    }
  } finally {
    bot.closeWindow(window); // what's left in the grid goes back to her pockets
  }
}

export async function craftOne(name, table) {
  const item = bot.registry.itemsByName[name];
  if (!item) return false;
  for (let attempt = 0; attempt < CraftTries; attempt++) {
    const before = held((n) => n === name);
    // The book first: the server knows its own tags (a campfire from charcoal and acacia logs), minecraft-data's
    // recipe list didn't, and said there was no recipe.
    const inBook = (recipeBook.get(item.id) || []).length > 0;
    const recipe = inBook ? null : bot.recipesFor(item.id, null, 1, table || null)[0];
    if (!inBook && !recipe) {
      log(`camp: no recipe for ${name} with ${bot.inventory.items().map((i) => `${i.name}x${i.count}`).join(' ')}`);
      return false;
    }
    try {
      if (inBook) await bookCraft(item, table);
      else await bot.craft(recipe, 1, table || undefined);
    } catch (err) {
      log(`camp: crafting ${name} failed:`, err && err.message ? err.message : String(err));
    }
    for (let i = 0; i < 20; i++) {
      if (held((n) => n === name) > before) {
        await wait(CraftGap);
        return true;
      }
      await wait(100);
    }
    log(`camp: crafting ${name} made nothing, ${attempt + 1 < CraftTries ? 'again' : 'giving up'}`);
    await wait(CraftSettle);
  }
  return false;
}

// Planks from her logs, the kind she has most of, until she has `want` (keeping `keepLogs` logs).
export async function craftPlanks(want, keepLogs, mine) {
  while (task === mine && held(isPlanks) < want) {
    const logs = bot.inventory.items().filter((i) => isLog(i.name)).sort((a, b) => b.count - a.count);
    if (logs.length === 0 || held(isLog) <= keepLogs) return;
    if (!(await craftOne(logs[0].name.replace(/_log$/, '_planks')))) return;
  }
}

// One wooden thing (`suffix`: '_trapdoor', '_slab', '_fence'), `per` planks of one kind a craft: of the camp's wood when
// there's enough of it, else of the kind she has most of, planks made for it from its logs when short (never the logs
// she keeps: `keep`). With logs of several kinds her planks come out mixed, and a recipe takes one kind only: tied to
// one wood, the camp lost a fence post with planks to spare.
export async function craftWooden(suffix, per, prefer, table, keep) {
  const planksOf = (k) => held((n) => n === `${k}_planks`);
  const kinds = [...new Set(bot.inventory.items().filter((i) => isPlanks(i.name) || isLog(i.name))
    .map((i) => i.name.replace(/_(planks|log)$/, '')))];
  const order = [prefer, ...kinds.filter((k) => k !== prefer).sort((a, b) => planksOf(b) - planksOf(a))];
  for (const k of order) {
    while (planksOf(k) < per && held((n) => n === `${k}_log`) > 0 && held(isLog) > keep() && (await craftOne(`${k}_planks`)));
    if (planksOf(k) >= per && (await craftOne(`${k}${suffix}`, table))) return true;
  }
  return false;
}
