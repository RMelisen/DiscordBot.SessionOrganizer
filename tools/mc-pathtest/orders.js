// Her orders, each checked on the server's side: smelting (food in the smoker, sand to glass, no fuel, nothing to
// smelt, no furnace), gathering (stone, coal, iron, sand; a quarry, and one over lava, gravel and a cave; ore without
// a pickaxe; something there isn't), fishing without a rod or water, handing over by kind, storing into a full chest,
// harvesting every crop, keeping a farm, guarding a spot, and the small ones (where, sit, mark).
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn } = require('./players');

const C = { x: -80, z: 70 }; // the yard, west of the workshop
const at = (dx, dy, dz) => `${C.x + dx} ${-60 + dy} ${C.z + dz}`;
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(18)} ${detail}`);
}

function yard() {
  return [
    `forceload add ${C.x - 40} ${C.z - 24} ${C.x + 24} ${C.z + 24}`,
    `fill ${at(-14, -3, -14)} ${at(16, -2, 14)} stone`, `fill ${at(-14, -1, -14)} ${at(16, -1, 14)} grass_block`,
    `fill ${at(-14, 0, -14)} ${at(16, 6, 14)} air`,
    // Furnaces in a row.
    `setblock ${at(0, 0, -6)} furnace[facing=south]`, `setblock ${at(2, 0, -6)} smoker[facing=south]`,
    `setblock ${at(4, 0, -6)} blast_furnace[facing=south]`,
    // A quarry face: stone with two coal and two iron in it, and sand on the ground.
    `fill ${at(9, 0, -4)} ${at(13, 3, 4)} stone`, `setblock ${at(9, 1, -2)} coal_ore`, `setblock ${at(9, 0, -1)} coal_ore`,
    `setblock ${at(9, 1, 1)} iron_ore`, `setblock ${at(9, 0, 2)} iron_ore`,
    `fill ${at(4, -1, 6)} ${at(7, -1, 8)} sand`,
    // Plots: carrots, potatoes, beetroot, with water; a melon and a pumpkin on attached stems.
    `fill ${at(-12, -1, 4)} ${at(-6, -1, 8)} farmland[moisture=7]`, `setblock ${at(-9, -1, 6)} water`,
    `fill ${at(-12, 0, 4)} ${at(-10, 0, 8)} carrots[age=7]`, `fill ${at(-8, 0, 4)} ${at(-6, 0, 5)} potatoes[age=7]`,
    `fill ${at(-8, 0, 7)} ${at(-6, 0, 8)} beetroots[age=3]`, `setblock ${at(-9, 0, 6)} air`,
    // Stems on farmland need water near, or the farmland dries to dirt and the stem pops off before the test.
    `setblock ${at(-13, -1, -6)} water`, `setblock ${at(-13, -1, -9)} water`,
    // The fruit first: an attached stem with no fruit beside it turns back into a plain one at once.
    `setblock ${at(-12, -1, -6)} farmland[moisture=7]`, `setblock ${at(-11, 0, -6)} melon`,
    `setblock ${at(-12, 0, -6)} attached_melon_stem[facing=east]`,
    `setblock ${at(-12, -1, -9)} farmland[moisture=7]`, `setblock ${at(-11, 0, -9)} pumpkin`,
    `setblock ${at(-12, 0, -9)} attached_pumpkin_stem[facing=east]`,
    // A full chest.
    `setblock ${at(0, 0, 10)} chest[facing=north]`,
    ...columns(),
  ];
}

// Columns, in the yard's south-east corner: bamboo 6 high, sugar cane 3 high by water, cacti 3 high on sand with nothing
// beside them (a cactus next to a block breaks).
const Columns = {
  bamboo: { block: 'bamboo', item: 'bamboo', at: [[12, 7], [14, 7], [12, 9]], high: 6 },
  sugarcane: { block: 'sugar_cane', item: 'sugar_cane', at: [[14, 11], [14, 12]], high: 3 },
  cactus: { block: 'cactus', item: 'cactus', at: [[10, 11], [10, 13]], high: 3 },
};

function columns() {
  const cmds = [`fill ${at(9, -1, 6)} ${at(15, -1, 14)} grass_block`, `fill ${at(9, 0, 6)} ${at(15, 8, 14)} air`,
    `fill ${at(15, -1, 11)} ${at(15, -1, 12)} water`];
  for (const { block, at: spots, high } of Object.values(Columns))
    for (const [dx, dz] of spots) {
      if (block === 'cactus') cmds.push(`setblock ${at(dx, -1, dz)} sand`);
      cmds.push(`fill ${at(dx, 0, dz)} ${at(dx, high - 1, dz)} ${block}`);
    }
  return cmds;
}

async function reset(body, x = 0.5, z = 0.5) {
  body.send({ op: 'stay' });
  // Papa parked well out of the way: a player beside the work picks up her drops (he had the sand).
  await rcon.run([`clear ${Name}`, `effect clear ${Name}`, 'difficulty peaceful', 'time set day',
    `tp ${Name} ${C.x + x} -60 ${C.z + z}`, `tp Papa ${C.x - 30.5} -60 ${C.z - 20.5}`, 'clear Papa',
    `kill @e[type=item,x=${C.x},y=-60,z=${C.z},distance=..30]`]);
  await wait(2500);
}

const ev = (body, names, ms) => body.next((e) => names.includes(e.ev), ms);
const fmt = (e) => (e ? `${e.ev}${e.why ? ` (${e.why})` : ''}${e.count !== undefined ? ` ${e.count}` : ''}` : 'no answer');

async function smeltFood(body) {
  await reset(body, 2.5, -4.5);
  await rcon.run([`data merge block ${at(2, 0, -6)} {Items:[]}`, `data merge block ${at(0, 0, -6)} {Items:[]}`,
    `give ${Name} beef 3`, `give ${Name} coal 2`]);
  await wait(1000);
  body.send({ op: 'smelt', player: 'tester', what: 'food' });
  const done = await ev(body, ['smelted', 'cantSmelt'], 90000);
  await wait(1500);
  const cooked = await q.carried(Name, 'cooked_beef');
  report(cooked === 3, 'smelt food', `${fmt(done)}; she carries ${cooked} cooked beef`);
}

async function smeltSand(body) {
  await reset(body, 0.5, -4.5);
  await rcon.run([`data merge block ${at(0, 0, -6)} {Items:[]}`, `give ${Name} sand 4`, `give ${Name} coal 2`]);
  await wait(1000);
  body.send({ op: 'smelt', player: 'tester', what: 'sand' });
  const done = await ev(body, ['smelted', 'cantSmelt'], 120000);
  await wait(1500);
  const glass = await q.carried(Name, 'glass');
  report(glass === 4, 'smelt sand', `${fmt(done)}; she carries ${glass} glass`);
}

async function smeltRefusals(body) {
  await reset(body, 0.5, -4.5);
  await rcon.run([`give ${Name} raw_iron 2`]);
  await wait(800);
  body.send({ op: 'smelt', player: 'tester', what: 'ore' });
  const noFuel = await ev(body, ['smelting', 'cantSmelt'], 15000);
  await rcon.run([`clear ${Name}`, `give ${Name} coal 2`]);
  await wait(800);
  body.send({ op: 'smelt', player: 'tester', what: 'ore' });
  const nothing = await ev(body, ['smelting', 'cantSmelt'], 15000);
  await reset(body, -30.5, 0.5); // no furnace within 16
  await rcon.run([`give ${Name} raw_iron 2`, `give ${Name} coal 2`]);
  await wait(800);
  body.send({ op: 'smelt', player: 'tester', what: 'ore' });
  const far = await ev(body, ['smelting', 'cantSmelt'], 15000);
  const ok = noFuel && noFuel.why === 'noFuel' && nothing && nothing.why === 'nothing' && far && far.why === 'noFurnace';
  report(Boolean(ok), 'smelt refusals', `no fuel: ${fmt(noFuel)}; nothing to smelt: ${fmt(nothing)}; no furnace near: ${fmt(far)}`);
}

async function collect(body, resource, count, tool, item, name) {
  await reset(body, 5.5, 0.5);
  if (tool) await rcon.run([`give ${Name} ${tool} 1`]);
  await wait(800);
  body.send({ op: 'collect', player: 'tester', resource, count });
  const started = Date.now();
  const done = await ev(body, ['collected', 'cantCollect'], 120000);
  await wait(1500);
  const got = await q.carried(Name, item);
  const lying = await q.count(`@e[type=item,x=${C.x},y=-60,z=${C.z},distance=..20]`);
  report(got >= count && lying === 0, name, `${fmt(done)} in ${((Date.now() - started) / 1000).toFixed(0)} s; she carries ${got} ${item}; ${lying} items lying`);
}

// A quarry where digging can go wrong, west of the yard: a stone face with lava inside it and gravel on part of its
// top, and stone ground before it over a cave. She starts on grass out of reach of any stone. Nothing flows, nothing
// falls, no hole opens into the cave, and she still brings the count.
async function quarryHazards(body) {
  const H = (dx, dy, dz) => at(dx - 24, dy, dz);
  await rcon.run([`fill ${H(-14, -4, 0)} ${H(4, 6, 14)} air`, `fill ${H(-14, -4, 0)} ${H(4, -2, 14)} stone`,
    `fill ${H(-14, -1, 0)} ${H(-10, -1, 14)} grass_block`, `fill ${H(-9, -1, 0)} ${H(4, -1, 14)} stone`,
    `fill ${H(-8, -3, 6)} ${H(-7, -2, 9)} air`, // the cave under the ground
    `fill ${H(-6, 0, 2)} ${H(-2, 3, 12)} stone`, `fill ${H(-6, 4, 2)} ${H(-2, 4, 6)} gravel`, `setblock ${H(-4, 1, 10)} lava`,
    `kill @e[type=item,x=${C.x - 28},y=-60,z=${C.z + 7},distance=..20]`]); // past reset's reach: a failed run's leftovers
  await reset(body, -24 - 12.5, 7.5);
  await rcon.run([`give ${Name} stone_pickaxe 1`]);
  await wait(800);
  body.send({ op: 'collect', player: 'tester', resource: 'stone', count: 40 });
  const started = Date.now();
  const done = await ev(body, ['collected', 'cantCollect'], 180000);
  await wait(1500);
  const got = await q.carried(Name, 'cobblestone');
  const is = async (dx, dy, dz, block) => /passed/i.test((await rcon.run([`execute if block ${H(dx, dy, dz)} ${block}`]))[0]);
  const broken = async (spots, block) => {
    let n = 0;
    for (const [dx, dy, dz] of spots) if (!(await is(dx, dy, dz, block))) n++;
    return n;
  };
  const around = [[-5, 1, 10], [-3, 1, 10], [-4, 0, 10], [-4, 2, 10], [-4, 1, 9], [-4, 1, 11]];
  const lavaOut = (await broken(around, 'stone')) + (await broken([[-4, 1, 10]], 'lava'));
  const gravel = [];
  for (let dx = -6; dx <= -2; dx++) for (let dz = 2; dz <= 6; dz++) gravel.push([dx, 4, dz]);
  const fell = await broken(gravel, 'gravel');
  const roof = [];
  for (let dx = -8; dx <= -7; dx++) for (let dz = 6; dz <= 9; dz++) roof.push([dx, -1, dz]);
  const holes = await broken(roof, 'stone');
  const lying = await q.count(`@e[type=item,x=${C.x - 28},y=-60,z=${C.z + 7},distance=..14]`);
  report(got >= 40 && lavaOut === 0 && fell === 0 && holes === 0 && lying === 0, 'quarry hazards',
    `${fmt(done)} in ${((Date.now() - started) / 1000).toFixed(0)} s; she carries ${got} cobblestone; lava let out ${lavaOut}, gravel fallen ${fell}, holes into the cave ${holes}, ${lying} items lying`);
}

async function collectRefusals(body) {
  await reset(body, 5.5, 0.5);
  body.send({ op: 'collect', player: 'tester', resource: 'iron', count: 2 });
  const noTool = await ev(body, ['collecting', 'cantCollect'], 15000);
  body.send({ op: 'collect', player: 'tester', resource: 'diamond', count: 2 });
  const none = await ev(body, ['collecting', 'cantCollect'], 15000);
  report(noTool && noTool.why === 'noTool' && none && none.why === 'none', 'collect refusals', `iron bare-handed: ${fmt(noTool)}; diamonds where there are none: ${fmt(none)}`);
}

async function fishRefusals(body) {
  await reset(body, 0.5, 0.5);
  body.send({ op: 'fish', player: 'tester' });
  const noRod = await ev(body, ['fishing', 'cantFish'], 10000);
  await rcon.run([`give ${Name} fishing_rod 1`, `tp ${Name} ${C.x - 30.5} -60 ${C.z + 0.5}`]);
  await wait(2000);
  body.send({ op: 'fish', player: 'tester' });
  const noWater = await ev(body, ['fishing', 'cantFish'], 10000);
  report(noRod && noRod.why === 'noRod' && noWater && noWater.why === 'noWater', 'fish refusals', `no rod: ${fmt(noRod)}; no water near: ${fmt(noWater)}`);
}

async function giveKinds(body, papa) {
  await reset(body, 0.5, 2.5);
  await rcon.run([`clear Papa`, `tp Papa ${C.x + 2.5} -60 ${C.z + 3.5}`, `give ${Name} oak_log 5`, `give ${Name} cobblestone 7`,
    `give ${Name} stone_pickaxe 1`, `give ${Name} bread 3`]);
  await wait(1500);
  const lyingNear = async (item) => {
    // A tossed tool flies further than a stack of logs: within 5 is « at his feet ».
    const [a] = await rcon.run([`execute as @e[type=item,x=${C.x + 2.5},y=-60,z=${C.z + 3.5},distance=..5] run data get entity @s Item`]);
    return (a.match(new RegExp(`minecraft:${item}`, 'g')) || []).length;
  };
  body.send({ op: 'give', player: 'Papa', goods: 'wood' });
  const wood = await ev(body, ['given', 'cantGive'], 20000);
  await wait(2500);
  const logs = (await q.carried('Papa', 'oak_log')) + (await lyingNear('oak_log'));
  const stillCobble = await q.carried(Name, 'cobblestone');
  body.send({ op: 'give', player: 'Papa', goods: 'tools' });
  const tools = await ev(body, ['given', 'cantGive'], 20000);
  await wait(2500);
  const pick = (await q.carried('Papa', 'stone_pickaxe')) + (await lyingNear('stone_pickaxe'));
  body.send({ op: 'give', player: 'Papa', goods: 'fish' });
  const fish = await ev(body, ['given', 'cantGive'], 10000);
  const ok = logs >= 1 && stillCobble === 7 && pick >= 1 && fish && fish.ev === 'cantGive';
  report(ok, 'give by kind', `wood: ${fmt(wood)}, Papa got the logs: ${logs >= 1}, her cobblestone kept: ${stillCobble}; tools: ${fmt(tools)}, pickaxe handed: ${pick >= 1}; fish she hasn't: ${fmt(fish)}`);
}

async function storeFull(body) {
  await reset(body, 0.5, 8.5);
  // 26 slots of dirt and one free: 64 cobblestone fit, the rest stays on her.
  const items = Array.from({ length: 26 }, (_, i) => `{Slot:${i}b,id:"minecraft:dirt",count:64}`).join(',');
  await rcon.run([`data merge block ${at(0, 0, 10)} {Items:[${items}]}`, `give ${Name} cobblestone 128`, `give ${Name} iron_sword 1`]);
  await wait(1000);
  body.send({ op: 'store', player: 'tester' });
  const done = await ev(body, ['stored', 'cantStore'], 30000);
  await wait(1000);
  const inChest = await q.inBlock(C.x, -60, C.z + 10, 'cobblestone');
  const onHer = await q.carried(Name, 'cobblestone');
  report(inChest === 64 && onHer === 64 && done && done.full === true, 'store, chest full', `${fmt(done)}${done && done.full ? ' (full)' : ''}; chest took ${inChest}, she kept ${onHer}`);
}

// From the yard's chest to Papa: 20 bread in it, all of it handed over (carried, or lying at his feet); an empty chest
// is « none ».
async function fetchBread(body) {
  await reset(body, 0.5, 6.5);
  await rcon.run([`clear Papa`, `tp Papa ${C.x + 3.5} -60 ${C.z + 5.5}`,
    `data merge block ${at(0, 0, 10)} {Items:[{Slot:0b,id:"minecraft:bread",count:20}]}`]);
  await wait(1500);
  body.send({ op: 'fetch', player: 'Papa', goods: 'food', count: 0 });
  const done = await ev(body, ['fetched', 'cantFetch', 'cantReach'], 30000);
  await wait(2500);
  const bread = (await q.carried('Papa', 'bread')) + (await lying('bread', 3, 5, 5));
  const left = await q.inBlock(C.x, -60, C.z + 10, 'bread');
  report(Boolean(done && done.ev === 'fetched') && bread >= 20 && left === 0, 'fetch from a chest',
    `${fmt(done)}; Papa got ${bread}/20 bread, ${left} left in the chest`);
}

async function fetchNone(body) {
  await reset(body, 0.5, 6.5);
  await rcon.run([`tp Papa ${C.x + 3.5} -60 ${C.z + 5.5}`, `data merge block ${at(0, 0, 10)} {Items:[]}`]);
  await wait(1500);
  body.send({ op: 'fetch', player: 'Papa', goods: 'iron', count: 0 });
  const done = await ev(body, ['fetched', 'cantFetch', 'cantReach'], 30000);
  report(Boolean(done && done.ev === 'cantFetch' && done.why === 'none'), 'fetch, nothing there', fmt(done));
}

// The crafting tables standing in the yard, removed as they're counted (each scenario sets its own).
async function tablesTaken() {
  const [a] = await rcon.run([`fill ${at(-16, -1, -16)} ${at(16, 4, 16)} air replace crafting_table`]);
  const m = /filled (\d+)/i.exec(a);
  return m ? Number(m[1]) : 0;
}

// At the yard's table: the pickaxe made there and handed over, nothing of hers left standing.
async function craftTable(body) {
  await reset(body, 0.5, 0.5);
  await tablesTaken();
  await rcon.run([`clear Papa`, `tp Papa ${C.x + 3.5} -60 ${C.z + 5.5}`, `setblock ${at(3, 0, 3)} crafting_table`,
    `give ${Name} iron_ingot 3`, `give ${Name} stick 2`]);
  await wait(1500);
  body.send({ op: 'craft', player: 'Papa', item: 'iron_pickaxe', count: 1 });
  const done = await ev(body, ['crafted', 'cantCraft', 'cantReach'], 60000);
  await wait(2500);
  const pickaxe = (await q.carried('Papa', 'iron_pickaxe')) + (await lying('iron_pickaxe', 3, 5, 5));
  const tables = await tablesTaken();
  report(Boolean(done && done.ev === 'crafted') && pickaxe === 1 && tables === 1, 'craft at a table',
    `${fmt(done)}; Papa got ${pickaxe} iron pickaxe; ${tables} table(s) in the yard (1: the yard's)`);
}

// No table near: sticks need none (2x2), so none is set down; logs become planks, planks sticks.
async function craftOwnTable(body) {
  await reset(body, 0.5, 0.5);
  await tablesTaken();
  await rcon.run([`clear Papa`, `tp Papa ${C.x + 3.5} -60 ${C.z + 5.5}`, `give ${Name} oak_log 4`]);
  await wait(1500);
  body.send({ op: 'craft', player: 'Papa', item: 'stick', count: 8 });
  const sticks = await ev(body, ['crafted', 'cantCraft', 'cantReach'], 60000);
  await wait(2500);
  const got = (await q.carried('Papa', 'stick')) + (await lying('stick', 3, 5, 5));
  // A ladder takes a table: hers, set down and taken back.
  body.send({ op: 'craft', player: 'Papa', item: 'ladder', count: 3 });
  const ladder = await ev(body, ['crafted', 'cantCraft', 'cantReach'], 90000);
  await wait(2500);
  const ladders = (await q.carried('Papa', 'ladder')) + (await lying('ladder', 3, 5, 5));
  const tables = await tablesTaken();
  report(Boolean(sticks && sticks.ev === 'crafted' && ladder && ladder.ev === 'crafted') && got >= 8 && ladders >= 3 && tables === 0,
    'craft with her own table', `${fmt(sticks)}, Papa got ${got}/8 sticks; ${fmt(ladder)}, ${ladders}/3 ladders; ${tables} table(s) left standing`);
}

async function craftMissing(body) {
  await reset(body, 0.5, 0.5);
  await rcon.run([`tp Papa ${C.x + 3.5} -60 ${C.z + 5.5}`]);
  await wait(1500);
  body.send({ op: 'craft', player: 'Papa', item: 'iron_pickaxe', count: 1 });
  const done = await ev(body, ['crafting', 'crafted', 'cantCraft', 'cantReach'], 20000);
  report(Boolean(done && done.ev === 'cantCraft' && done.why === 'missing' && done.missing === 'iron'), 'craft, no iron',
    `${fmt(done)}${done && done.missing ? ` (${done.missing})` : ''}`);
}

// Items lying at a spot in the yard, by item name.
const lying = (item, dx, dz, r) => q.count(`@e[type=item,nbt={Item:{id:"minecraft:${item}"}},x=${C.x + dx},y=-60,z=${C.z + dz},distance=..${r}]`);
const drop = (item, dx, dz) => `summon item ${at(dx, 0, dz)} {Item:{id:"minecraft:${item}",count:1},PickupDelay:0}`;

// `strays`: produce already lying in the field (rolled off, trampled out), picked up with the rest; and a diamond
// someone dropped 3 blocks off the field, which stays where it is (walked over, anything is picked up, by her as by
// a player: what's tested is that she doesn't go for it).
async function harvestCrop(body, crop, item, x, z, strays = null) {
  await reset(body, x, z);
  if (strays) await rcon.run([...strays.map(([dx, dz]) => drop(item, dx, dz)), drop('diamond', -9, 11)]);
  body.send({ op: 'harvest', player: 'tester', crop });
  const done = await ev(body, ['harvested', 'cantHarvest'], 90000);
  await wait(1500);
  const got = await q.carried(Name, item);
  const left = strays ? await lying(item, -9, 6, 6) : 0;
  const diamond = strays ? await lying('diamond', -9, 11, 2) : 1;
  report(Boolean(done && done.ev === 'harvested') && got > 0 && left === 0 && diamond === 1, `harvest ${crop}`,
    `${fmt(done)}${done && done.replanted !== undefined ? `, replanted ${done.replanted}` : ''}; she carries ${got} ${item}` +
    (strays ? `; ${item} left lying ${left}, the diamond ${diamond === 1 ? 'left where it was' : 'taken'}` : ''));
}

// A column crop: everything above each bottom block cut and brought along, every bottom block left to grow again, and
// no harm done (a cactus pricks).
async function harvestColumn(body, crop) {
  const { block, item, at: spots, high } = Columns[crop];
  await rcon.run(columns());
  await reset(body, 11.5, 10.5);
  const hurts = body.events.filter((e) => e.ev === 'hurt').length;
  body.send({ op: 'harvest', player: 'tester', crop });
  const done = await ev(body, ['harvested', 'cantHarvest'], 90000);
  await wait(1500);
  const got = await q.carried(Name, item);
  let bases = 0;
  let above = 0;
  for (const [dx, dz] of spots) {
    const [b] = await rcon.run([`execute if block ${at(dx, 0, dz)} minecraft:${block}`]);
    if (/passed/i.test(b)) bases++;
    const [a] = await rcon.run([`execute if block ${at(dx, 1, dz)} minecraft:${block}`]);
    if (/passed/i.test(a)) above++;
  }
  const hurt = body.events.filter((e) => e.ev === 'hurt').length - hurts;
  const expected = spots.length * (high - 1);
  // What stands above a bottom now may have grown back while she worked (bamboo does, fast): what counts is what she
  // carries, all of it, and every bottom still there.
  report(Boolean(done && done.ev === 'harvested') && bases === spots.length && got >= expected - 1 && hurt === 0,
    `harvest ${crop}`, `${fmt(done)}; she carries ${got}/${expected} ${item}; bottoms left ${bases}/${spots.length}, ` +
    `grown back above them ${above}; hurt ${hurt}`);
}

async function keepFarm(body) {
  // A small wheat plot, unripe, a chest by it: she keeps it; it ripens; the next round harvests and stores. She starts
  // with 10 wheat and the plot gives 14: 16 stay on her for the animals (FeedKept), the rest goes in the chest.
  await rcon.run([`fill ${at(-4, -1, -12)} ${at(0, -1, -10)} farmland[moisture=7]`, `setblock ${at(-2, -1, -11)} water`,
    `fill ${at(-4, 0, -12)} ${at(0, 0, -10)} wheat[age=3]`, `setblock ${at(-2, 0, -11)} air`,
    `setblock ${at(2, 0, -11)} chest[facing=west]`, `data merge block ${at(2, 0, -11)} {Items:[]}`]);
  await reset(body, 1.5, -9.5);
  await rcon.run([`give ${Name} wheat_seeds 20`, `give ${Name} wheat 10`]);
  body.send({ op: 'farm', player: 'tester' });
  const going = await ev(body, ['farming', 'cantFarm'], 10000);
  await wait(3000);
  // Carrots someone left lying on the field: they come in with the round.
  await rcon.run([`fill ${at(-4, 0, -12)} ${at(0, 0, -10)} wheat[age=7] replace wheat`, drop('carrot', -4, -12), drop('carrot', 0, -10)]);
  await wait(40000);
  const wheat = await q.inBlock(C.x + 2, -60, C.z - 11, 'wheat');
  const carrots = (await q.inBlock(C.x + 2, -60, C.z - 11, 'carrot')) + (await q.carried(Name, 'carrot'));
  const carrotsLying = await lying('carrot', -2, -11, 5);
  const seedsOn = await q.carried(Name, 'wheat_seeds');
  const wheatOn = await q.carried(Name, 'wheat');
  body.send({ op: 'stay' });
  report(Boolean(going && going.ev === 'farming') && wheat > 0 && wheatOn === 16 && seedsOn <= 16 + 8 && carrots === 2 && carrotsLying === 0, 'keep a farm',
    `${fmt(going)}; chest has ${wheat} wheat, she keeps ${wheatOn} (16); seeds on her ${seedsOn} (keeps 16); carrots picked up ${carrots}/2, left lying ${carrotsLying}`);
}

async function patrol(body) {
  await reset(body, 0.5, 0.5);
  await rcon.run([`give ${Name} iron_sword 1`, 'difficulty easy']);
  body.send({ op: 'patrol', player: 'tester', spot: null });
  const going = await ev(body, ['patrolling', 'cantPatrol'], 10000);
  await rcon.run([`summon zombie ${C.x + 8} -60 ${C.z + 6} {PersistenceRequired:1b,Tags:["patrol"]}`]);
  let left = 1;
  const t0 = Date.now();
  while (left && Date.now() - t0 < 30000) { await wait(1000); left = await q.count('@e[type=zombie,tag=patrol]'); }
  await wait(6000);
  const x = await q.entityNumber(Name, 'Pos[0]');
  const z = await q.entityNumber(Name, 'Pos[2]');
  const back = Math.hypot(x - (C.x + 0.5), z - (C.z + 0.5));
  await rcon.run(['difficulty peaceful', 'kill @e[type=zombie,tag=patrol]']);
  body.send({ op: 'stay' });
  report(Boolean(going) && left === 0 && back < 4, 'patrol a spot', `${fmt(going)}; zombie ${left === 0 ? 'dead' : 'alive'}; back ${back.toFixed(1)} from her spot`);
}

async function small(body, papa) {
  await reset(body, 3.5, 3.5);
  await rcon.run([`tp Papa ${C.x + 5.5} -60 ${C.z + 5.5}`]);
  await wait(1000);
  body.send({ op: 'where', player: 'tester' });
  const pos = await ev(body, ['position'], 5000);
  const x = await q.entityNumber(Name, 'Pos[0]');
  const z = await q.entityNumber(Name, 'Pos[2]');
  const whereOk = pos && pos.x === Math.floor(x) && pos.z === Math.floor(z);
  body.send({ op: 'mark', player: papa.username });
  const marked = await ev(body, ['marked', 'cantMark'], 5000);
  body.send({ op: 'sit' });
  await wait(1500);
  // Crouched, on the server: an inline predicate on her sneaking flag.
  const [crouched] = await rcon.run([`execute as ${Name} if predicate {type:"minecraft:entity_properties",entity:"this",predicate:{flags:{is_sneaking:true}}}`]);
  const sitting = /passed/i.test(crouched);
  body.send({ op: 'stay' });
  report(Boolean(whereOk) && Boolean(marked && marked.ev === 'marked') && sitting, 'where, mark, sit', `where: ${pos ? `${pos.x} ${pos.z}` : 'none'} (server ${Math.floor(x)} ${Math.floor(z)}); mark Papa's spot: ${fmt(marked)}; crouched on the server: ${sitting}`);
}

async function orders(only = []) {
  await rcon.run(yard());
  const body = startBody();
  await spawned(body);
  const papa = await standIn('Papa');
  const all = {
    smeltFood, smeltSand, smeltRefusals,
    collectRefusals, // before the ores are mined
    stone: (b) => collect(b, 'stone', 6, 'stone_pickaxe', 'cobblestone', 'collect stone'),
    coal: (b) => collect(b, 'coal', 2, 'stone_pickaxe', 'coal', 'collect coal'),
    iron: (b) => collect(b, 'iron', 2, 'stone_pickaxe', 'raw_iron', 'collect iron'),
    sand: (b) => collect(b, 'sand', 4, null, 'sand', 'collect sand'),
    // After the ores: a quarry into the face, 40 of its 180 stone, from where she stands as much as she can.
    quarry: (b) => collect(b, 'stone', 40, 'stone_pickaxe', 'cobblestone', 'quarry stone'),
    quarryHazards,
    fishRefusals, give: (b) => giveKinds(b, papa), storeFull, fetch: fetchBread, fetchNone, craftTable, craftOwnTable, craftMissing,
    carrots: (b) => harvestCrop(b, 'carrot', 'carrot', -9.5, 3.5, [[-12, 4], [-10, 8], [-12, 8]]),
    potatoes: (b) => harvestCrop(b, 'potato', 'potato', -7.5, 3.5),
    beetroot: (b) => harvestCrop(b, 'beetroot', 'beetroot', -7.5, 9.5),
    melon: (b) => harvestCrop(b, 'melon', 'melon_slice', -10.5, -5.5),
    pumpkin: (b) => harvestCrop(b, 'pumpkin', 'pumpkin', -10.5, -8.5),
    bamboo: (b) => harvestColumn(b, 'bamboo'),
    sugarcane: (b) => harvestColumn(b, 'sugarcane'),
    cactus: (b) => harvestColumn(b, 'cactus'),
    farm: keepFarm, patrol, small: (b) => small(b, papa),
  };
  for (const [name, run] of Object.entries(all)) {
    if (only.length > 0 && !only.includes(name)) continue;
    try {
      await run(body);
    } catch (err) {
      report(false, name, `threw: ${err.message}`);
    }
  }
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
  await papa.leave();
  await body.quit();
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} orders passed`);
  return failed.length === 0;
}

module.exports = { orders };
