// Reading the world through RCON: what the server thinks, not what she reports.
const rcon = require('./rcon');

// `data get entity <who> <path>` as a number (NaN when there's no such data).
async function entityNumber(who, path) {
  const [a] = await rcon.run([`data get entity ${who} ${path}`]);
  const m = /entity data: (-?[\d.]+)/.exec(a);
  return m ? Number(m[1]) : NaN;
}

// The raw SNBT of `data get entity <who> <path>` ('' when there's none).
async function entityData(who, path) {
  const [a] = await rcon.run([`data get entity ${who} ${path}`]);
  const i = a.indexOf('entity data: ');
  return i >= 0 ? a.slice(i + 'entity data: '.length) : '';
}

// How many of `item` (« bread », no namespace) an SNBT item list holds.
function countIn(snbt, item) {
  let n = 0;
  const re = /\{[^{}]*?count: (\d+)[^{}]*?id: "minecraft:([a-z_]+)"[^{}]*?\}|\{[^{}]*?id: "minecraft:([a-z_]+)"[^{}]*?count: (\d+)[^{}]*?\}/g;
  for (const m of snbt.matchAll(re)) {
    const id = m[2] || m[3];
    const count = Number(m[1] || m[4]);
    if (id === item) n += count;
  }
  return n;
}

// The server shortens `data get` answers (a list stops after its first element with « ... »), so a list is read one
// element at a time: `base[0]`, `base[1]`… up to `size`.
async function listItems(kind, target, base, size) {
  const answers = await rcon.run(Array.from({ length: size }, (_, i) => `data get ${kind} ${target} ${base}[${i}]`));
  return answers.filter((a) => a.includes('data: ')).map((a) => a.slice(a.indexOf('data: ') + 6)).join(' ');
}

// How many of `item` a player carries (inventory and worn).
async function carried(who, item) {
  const inventory = await listItems('entity', who, 'Inventory', 41);
  const slots = ['head', 'chest', 'legs', 'feet', 'offhand', 'mainhand'];
  const worn = (await rcon.run(slots.map((slot) => `data get entity ${who} equipment.${slot}`)))
    .filter((a) => a.includes('data: ')).map((a) => a.slice(a.indexOf('data: ') + 6)).join(' ');
  return countIn(inventory, item) + countIn(worn, item);
}

// How many of `item` a container block holds.
async function inBlock(x, y, z, item) {
  return countIn(await listItems('block', `${x} ${y} ${z}`, 'Items', 27), item);
}

// How many entities a selector matches.
async function count(selector) {
  const [a] = await rcon.run([`execute store result score #n pathtest if entity ${selector}`]);
  const m = /Count: (\d+)/.exec(a);
  return m ? Number(m[1]) : 0;
}

module.exports = { entityNumber, entityData, countIn, listItems, carried, inBlock, count };
