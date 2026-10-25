// Runs her real body (ProjectSYNCS/MinecraftBody) against the local test server and reports what happened.
//   node run.js build                  build the course, the crop field and the item arena (once per world)
//   node run.js lanes [name ...]       walk the course's lanes (all, or the ones named)
//   node run.js crops                  cross a ripe wheat field back and forth, count what survived
//   node run.js items                  die among decorations with things scattered around, see what she gets back
//   node run.js gather                 cut a grove whose logs fall among fences, holes and low overhangs
//   node run.js harvest                harvest and replant a small plot: nothing trampled, nothing left lying
//   node run.js actions [name ...]     every packet-level action, checked on the server's side (actions.js)
//   node run.js safety                 what could hurt the world: keeping a herd (safety.js)
//   node run.js survival [name ...]    night fights, creepers, endermen, being hit, deep water (survival.js)
//   node run.js trip                   250 blocks, far past the loaded chunks: re-planning as she goes
//   node run.js players [name ...]     with players around: follow a walk and through the hard lanes, come, guide, lose one, seek, gaze (players-test.js)
//   node run.js orders [name ...]      smelting, gathering, fishing, giving, storing, harvesting, farm, patrol (orders.js)
//   node run.js routine                her day's turns: 9 minutes at a farm, 3 at a pen, back (routine.js, ~13 min)
// See README.md for the server.
const rcon = require('./rcon');
const course = require('./course');
const { startBody, spawned, where, wait, Name } = require('./body');
const { actions } = require('./actions');
const { safety, scenery, GroveScenery } = require('./safety');
const { survival } = require('./survival');
const { playersTest } = require('./players-test');
const { orders } = require('./orders');
const { routine } = require('./routine');
const { standIn } = require('./players');
const { carried } = require('./query');

// ---- Building ----------------------------------------------------------------------------------------

const Crops = { x1: 40, z1: -40, x2: 50, z2: -30 };
const Arena = { x: 46, z: 46 }; // where she dies, in the item arena
const Grove = [[70, 46], [74, 42], [78, 48], [74, 52]]; // tree trunks, 4 logs each
const Plot = { x1: 0, z1: -62, x2: 4, z2: -58 }; // a 5x5 wheat plot, far enough from the big field (harvest reaches 24)

async function build() {
  const a = course.Area;
  const cmds = [
    // Game rules are snake_case since 26.x.
    'whitelist off', 'gamerule keep_inventory false', 'difficulty peaceful', 'time set day', 'gamerule advance_time false',
    'weather clear', 'gamerule advance_weather false', 'gamerule respawn_radius 0', 'gamerule spawn_mobs false',
    'gamerule spawn_monsters false', 'setworldspawn 0 -60 0',
    'scoreboard objectives add pathtest dummy',
    `forceload add ${a.x1} ${a.z1} ${a.x2} ${a.z2}`, `forceload add 0 -48 64 64`, `forceload add 64 32 96 64`, `forceload add -16 -80 16 -48`,
  ];
  // The crop field: ripe wheat on wet farmland, one water block in the middle, open ground all around.
  const { x1, z1, x2, z2 } = Crops;
  cmds.push(`fill ${x1 - 6} -61 ${z1 - 6} ${x2 + 6} -61 ${z2 + 6} grass_block`, `fill ${x1 - 6} -60 ${z1 - 6} ${x2 + 6} -55 ${z2 + 6} air`,
    `fill ${x1} -61 ${z1} ${x2} -61 ${z2} farmland[moisture=7]`, `fill ${x1} -60 ${z1} ${x2} -60 ${z2} wheat[age=7]`,
    `setblock ${(x1 + x2) / 2} -61 ${(z1 + z2) / 2} water`, `setblock ${(x1 + x2) / 2} -60 ${(z1 + z2) / 2} air`);
  // The item arena: decorations, half blocks and awkward spots around where she dies.
  const ax = Arena.x;
  const az = Arena.z;
  const at = (dx, dy, dz) => `${ax + dx} ${-60 + dy} ${az + dz}`;
  cmds.push(`fill ${at(-9, -1, -9)} ${at(9, -1, 9)} grass_block`, `fill ${at(-9, 0, -9)} ${at(9, 5, 9)} air`,
    `fill ${at(2, 0, -1)} ${at(3, 0, 1)} stone_slab[type=bottom]`,
    `fill ${at(-3, 0, 2)} ${at(1, 0, 2)} oak_fence`,
    `setblock ${at(-2, 0, -2)} flower_pot`, `setblock ${at(-1, 0, -3)} lantern`, `setblock ${at(0, 0, -3)} candle[candles=4]`,
    `setblock ${at(3, 0, -3)} chest[facing=west]`, `fill ${at(-4, 0, -1)} ${at(-3, 0, 1)} white_carpet`,
    `fill ${at(1, 0, -5)} ${at(4, 0, -4)} short_grass`, `setblock ${at(-4, -1, -4)} air`,
    `setblock ${at(4, 1, 3)} stone_slab[type=top]`, `fill ${at(5, 0, 2)} ${at(5, 1, 4)} stone`,
    `fill ${at(-5, 0, 4)} ${at(-5, 1, 5)} stone`, `fill ${at(-5, 0, 5)} ${at(-3, 1, 5)} stone`,
    `fill ${at(0, 0, 4)} ${at(0, 2, 4)} stone`, `setblock ${at(-1, 0, 5)} oak_fence`);
  cmds.push(...groveCommands());
  const answers = await rcon.run([...cmds, ...course.commands()]);
  const failed = answers.map((a2, i) => [a2, [...cmds, ...course.commands()][i]])
    .filter(([a2]) => /Unknown|Incorrect|Expected|not loaded|Invalid|error/i.test(a2));
  for (const [a2, c] of failed) console.log('FAILED', c, '->', a2);
  console.log(`built ${course.lanes.length} lanes, the crop field and the item arena (${answers.length - failed.length}/${answers.length} commands ok)`);
}

// ---- Lanes -------------------------------------------------------------------------------------------

async function lanes(names) {
  const body = startBody();
  await spawned(body);
  const results = [];
  for (const [i, lane] of course.lanes.entries()) {
    if (names.length > 0 && !names.includes(lane.name)) continue;
    const s = course.world(i, lane.start);
    const g = course.world(i, lane.goal);
    await rcon.run([`tp ${Name} ${s.x} ${s.y} ${s.z} -90 0`]);
    await wait(1500);
    const before = body.corrections;
    const firstEvent = body.events.length;
    const started = Date.now();
    body.send({ op: 'goto', player: 'tester', x: g.x, y: g.y, z: g.z });
    const end = await body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach' || e.ev === 'stopped', lane.timeout || 30000);
    const secs = ((Date.now() - started) / 1000).toFixed(1);
    const outcome = !end ? 'timeout' : end.ev === 'arrived' ? 'arrive' : `refuse:${end.why || end.ev}`;
    body.send({ op: 'stay' });
    const pos = await where(body);
    const corrections = body.corrections - before;
    const hurt = body.since(firstEvent, (e) => e.ev === 'hurt').length;
    const expected = lane.expect || 'arrive';
    const ok = outcome.split(':')[0] === expected && corrections === 0 && hurt === 0;
    results.push({ lane: lane.name, ok, outcome, secs, corrections, pos });
    console.log(`${ok ? 'PASS' : 'FAIL'}  ${lane.name.padEnd(13)} ${outcome.padEnd(16)} ${secs.padStart(5)} s  corrections ${String(corrections).padStart(3)}  hurt ${hurt}  ended at ${pos}   (${lane.what})`);
  }
  await body.quit();
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} lanes passed`);
  return failed.length === 0;
}

// ---- A long trip ---------------------------------------------------------------------------------------

// 250 blocks over open ground, far past the chunks loaded around her: searches run out of time and re-plan as she
// goes (timeoutTick), and must not give up while she still gets somewhere.
async function trip() {
  const body = startBody();
  await spawned(body);
  await rcon.run([`tp ${Name} 0.5 -60 -70.5`]);
  await wait(3000);
  const started = Date.now();
  body.send({ op: 'goto', player: 'tester', x: 250.5, y: null, z: -70.5 });
  const end = await body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach', 150000);
  const secs = ((Date.now() - started) / 1000).toFixed(0);
  const pos = await where(body);
  await body.quit();
  const ok = Boolean(end && end.ev === 'arrived');
  console.log(`${ok ? 'PASS' : 'FAIL'}  long trip: ${end ? end.ev + (end.why ? ` (${end.why})` : '') : 'no answer in 150 s'} after ${secs} s, ended at ${pos}`);
  return ok;
}

// ---- Crops -------------------------------------------------------------------------------------------

async function countWheat() {
  const { x1, z1, x2, z2 } = Crops;
  // Replacing ripe wheat with itself reports nothing, so: to age 6 (which reports how many) and straight back.
  const [a] = await rcon.run([`fill ${x1} -60 ${z1} ${x2} -60 ${z2} wheat[age=6] replace wheat[age=7]`,
    `fill ${x1} -60 ${z1} ${x2} -60 ${z2} wheat[age=7] replace wheat[age=6]`]);
  const m = /(\d+)/.exec(a);
  return m ? Number(m[1]) : 0;
}

async function crops() {
  const { x1, z1, x2, z2 } = Crops;
  await rcon.run([`fill ${x1} -61 ${z1} ${x2} -61 ${z2} farmland[moisture=7]`,
    `fill ${x1} -60 ${z1} ${x2} -60 ${z2} wheat[age=7]`, `setblock ${(x1 + x2) / 2} -61 ${(z1 + z2) / 2} water`,
    `setblock ${(x1 + x2) / 2} -60 ${(z1 + z2) / 2} air`]);
  const total = await countWheat();
  const body = startBody();
  await spawned(body);
  const zc = (z1 + z2) / 2 + 0.5;
  const xc = (x1 + x2) / 2 + 0.5;
  // Across and back, along the middle and along the edges, then on the diagonal.
  const legs = [
    [[x1 - 3.5, zc], [x2 + 3.5, zc]], [[x2 + 3.5, zc], [x1 - 3.5, zc]],
    [[x1 - 3.5, z1 + 1.5], [x2 + 3.5, z1 + 1.5]], [[xc, z1 - 3.5], [xc, z2 + 3.5]],
    [[x1 - 3.5, z1 - 3.5], [x2 + 3.5, z2 + 3.5]], [[x2 + 3.5, z1 - 3.5], [x1 - 3.5, z2 + 3.5]],
  ];
  let lostTotal = 0;
  for (const [[sx, sz], [gx, gz]] of legs) {
    const before = await countWheat();
    await rcon.run([`tp ${Name} ${sx} -60 ${sz}`]);
    await wait(1500);
    const started = Date.now();
    body.send({ op: 'goto', player: 'tester', x: gx, y: -60, z: gz });
    const end = await body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach', 40000);
    await wait(1000);
    const lost = before - (await countWheat());
    lostTotal += lost;
    console.log(`${lost === 0 && end && end.ev === 'arrived' ? 'PASS' : 'FAIL'}  ${`(${sx},${sz}) -> (${gx},${gz})`.padEnd(28)} ${end ? end.ev : 'timeout'} in ${((Date.now() - started) / 1000).toFixed(1)} s, wheat lost ${lost}`);
  }
  await body.quit();
  console.log(`\nwheat: ${total} at the start, ${lostTotal} lost`);
  return lostTotal === 0;
}

// ---- Items -------------------------------------------------------------------------------------------

// Awkward spots around where she dies (arena-relative block centres: block -4 is centred at -3.5), each with one item,
// and whether she can reach it. An item summoned inside a block is pushed out of it and slides off somewhere else.
const ItemSpots = [
  { what: 'on a bottom slab', at: [2.5, 0.6, 0.5] },
  { what: 'behind a fence line', at: [-1.5, 0.1, 3.5] },
  { what: 'next to a flower pot', at: [-2.5, 0.1, -1.5] },
  { what: 'in front of a lantern and candles', at: [-0.5, 0.1, -1.5] },
  { what: 'on top of a chest', at: [3.5, 1.0, -2.5] },
  { what: 'on a carpet', at: [-3.5, 0.1, 0.5] },
  { what: 'in tall grass', at: [2.5, 0.1, -4.5] },
  { what: 'in a 1-deep hole', at: [-3.5, -0.9, -3.5] },
  { what: 'under a slab too low to stand under', at: [4.5, 0.1, 3.5] },
  { what: 'in a walled corner', at: [-3.5, 0.1, 4.5] },
  { what: 'on a fence post (reachable from beside it)', at: [-0.5, 1.6, 5.5] },
  { what: 'on a 3-high pillar (out of reach)', at: [0.5, 3.1, 4.5], unreachable: true },
];

async function items() {
  const ax = Arena.x;
  const az = Arena.z;
  await rcon.run([`kill @e[type=item,x=${ax},y=-60,z=${az},distance=..12]`, `clear ${Name}`]);
  const body = startBody();
  await spawned(body);
  await rcon.run([`clear ${Name}`, `give ${Name} cobblestone 64`, `give ${Name} oak_log 32`, `give ${Name} bread 16`,
    `tp ${Name} ${ax + 0.5} -60 ${az + 0.5}`]);
  await wait(2000);
  await rcon.run([`kill ${Name}`]);
  await wait(300);
  await rcon.run(ItemSpots.map((s) => `summon item ${ax + s.at[0]} ${-60 + s.at[1]} ${az + s.at[2]} {Item:{id:"minecraft:apple",count:1},PickupDelay:0s,Motion:[0.0,0.0,0.0]}`));
  const started = Date.now();
  const going = await body.next((e) => e.ev === 'retrieving' || e.ev === 'retrieveLost', 30000);
  const done = await body.next((e) => e.ev === 'retrieved' || e.ev === 'retrieveLost', 120000);
  const secs = ((Date.now() - started) / 1000).toFixed(1);
  // What's left, wherever it ended up (an item pushed out of a block slides away): each item still in the arena is
  // put down to the spot it's closest to, and anything not by an unreachable spot is a miss.
  const [listing] = await rcon.run([`execute as @e[type=item,x=${ax},y=-60,z=${az},distance=..12] run data get entity @s Pos`]);
  const remaining = [...listing.matchAll(/\[(-?[\d.]+)d, (-?[\d.]+)d, (-?[\d.]+)d\]/g)].map((m) => m.slice(1).map(Number));
  const near = (p, s) => Math.hypot(p[0] - (ax + s.at[0]), p[2] - (az + s.at[2])) <= 1.2 && Math.abs(p[1] - (-60 + s.at[1])) <= 1.2;
  const left = ItemSpots.map((s) => ({ ...s, left: remaining.filter((p) => near(p, s)).length }));
  const stray = remaining.filter((p) => !ItemSpots.some((s) => near(p, s)));
  const pos = await where(body);
  await body.quit();
  console.log(`retrieve: ${going ? going.ev : 'never started'} -> ${done ? `${done.ev} ${JSON.stringify(done)}` : 'timeout (stuck?)'} after ${secs} s, she ended at ${pos}`);
  let ok = Boolean(done && done.ev === 'retrieved');
  for (const s of left) {
    const good = s.unreachable ? true : s.left === 0;
    ok = ok && good;
    console.log(`${good ? 'PASS' : 'FAIL'}  ${s.what.padEnd(44)} ${s.left === 0 ? 'picked up' : 'still there'}${s.unreachable ? ' (out of reach: either is fine, as long as she moved on)' : ''}`);
  }
  for (const p of stray) console.log(`FAIL  an item she left at ${p.map((v) => v.toFixed(1)).join(' ')}`);
  return ok && stray.length === 0;
}

// ---- Gathering ---------------------------------------------------------------------------------------

// The grove: natural trees (she only cuts logs with natural leaves near them), with the awkward spots where their logs
// fall: a fence ring, a slab too low to stand under, a hole, pots, grass and a lantern.
function groveCommands() {
  const cmds = ['fill 64 -61 36 84 -61 58 grass_block', 'fill 64 -60 36 84 -50 58 air'];
  for (const [x, z] of Grove) {
    cmds.push(`fill ${x - 1} -57 ${z - 1} ${x + 1} -56 ${z + 1} oak_leaves[persistent=false]`,
      `fill ${x} -60 ${z} ${x} -57 ${z} oak_log`);
  }
  cmds.push('fill 69 -60 45 69 -60 47 oak_fence', 'setblock 70 -60 45 oak_fence', 'setblock 75 -59 42 stone_slab[type=top]',
    'setblock 75 -59 43 stone_slab[type=top]', 'setblock 79 -61 48 air', 'setblock 77 -60 47 flower_pot',
    'fill 73 -60 51 75 -60 51 short_grass', 'setblock 73 -60 53 lantern', 'setblock 75 -60 53 oak_fence');
  return cmds;
}

// How many blocks of `block` in a region: swapped to `other` (which reports how many) and straight back.
async function countBlocks(r, y, block, other) {
  const [a] = await rcon.run([`fill ${r.x1} ${y} ${r.z1} ${r.x2} ${y} ${r.z2} ${other} replace ${block}`,
    `fill ${r.x1} ${y} ${r.z1} ${r.x2} ${y} ${r.z2} ${block} replace ${other}`]);
  return Number((/(\d+)/.exec(a) || [0, 0])[1]);
}

async function harvestTest() {
  const { x1, z1, x2, z2 } = Plot;
  await rcon.run([`fill ${x1 - 4} -61 ${z1 - 4} ${x2 + 4} -61 ${z2 + 4} grass_block`, `fill ${x1 - 4} -60 ${z1 - 4} ${x2 + 4} -55 ${z2 + 4} air`,
    `fill ${x1} -61 ${z1} ${x2} -61 ${z2} farmland[moisture=7]`, `fill ${x1} -60 ${z1} ${x2} -60 ${z2} wheat[age=7]`,
    `setblock ${x1 + 2} -61 ${z1 + 2} water`, `setblock ${x1 + 2} -60 ${z1 + 2} air`,
    `kill @e[type=item,x=${x1 + 2},y=-60,z=${z1 + 2},distance=..10]`, `clear ${Name}`]);
  const body = startBody();
  await spawned(body);
  // An axe in her hand from some earlier order: crops are broken by hand all the same. A watcher notes what she holds
  // each time a stalk breaks.
  await rcon.run([`clear ${Name}`, `tp ${Name} ${x1 - 2.5} -60 ${z1 + 2.5}`, `item replace entity ${Name} weapon.mainhand with iron_axe`]);
  const watcher = await standIn('Watcher');
  await rcon.run([`tp Watcher ${x1 + 2.5} -57 ${z2 + 3.5}`, 'gamemode spectator Watcher']);
  const heldAtBreak = [];
  watcher.on('blockUpdate', (old, now) => {
    if (!old || old.name !== 'wheat' || (now && now.name === 'wheat')) return;
    const her = watcher.players[Name] && watcher.players[Name].entity;
    heldAtBreak.push(her && her.heldItem ? her.heldItem.name : 'hand');
  });
  await wait(5000); // a long teleport: the plot's chunks have to reach her first
  const started = Date.now();
  body.send({ op: 'harvest', player: 'tester', crop: 'wheat' });
  const done = await body.next((e) => e.ev === 'harvested' || e.ev === 'cantHarvest', 240000);
  const secs = ((Date.now() - started) / 1000).toFixed(1);
  await wait(1500);
  const ripe = await countBlocks(Plot, -60, 'wheat[age=7]', 'wheat[age=6]');
  const planted = await countBlocks(Plot, -60, 'wheat[age=0]', 'wheat[age=1]');
  const trampled = await countBlocks(Plot, -61, 'dirt', 'coarse_dirt');
  const [items] = await rcon.run([`execute store result score #plot pathtest if entity @e[type=item,x=${x1 + 2},y=-60,z=${z1 + 2},distance=..8]`]);
  const lying = Number((/Count: (\d+)/.exec(items) || [0, 0])[1]);
  if (lying > 0) {
    const [where2] = await rcon.run([`execute as @e[type=item,x=${x1 + 2},y=-60,z=${z1 + 2},distance=..8] run data get entity @s Pos`]);
    console.log('      left lying at', where2.replace(/has the following entity data: /g, ''));
  }
  const axeKept = await carried(Name, 'iron_axe');
  await watcher.leave();
  await body.quit();
  // Seeds in hand is the replanting after the stalk broke; anything that cuts is not a hand.
  const tooled = heldAtBreak.filter((h) => /_(axe|pickaxe|shovel|hoe|sword)$/.test(h));
  const ok = Boolean(done && done.ev === 'harvested') && ripe === 0 && trampled === 0 && lying === 0 && planted >= 20
    && heldAtBreak.length > 0 && tooled.length === 0 && axeKept === 1;
  console.log(`${ok ? 'PASS' : 'FAIL'}  harvest: ${done ? JSON.stringify(done) : 'never finished (stuck?)'} in ${secs} s; ripe left ${ripe}, replanted ${planted}/24, trampled ${trampled}, items lying ${lying}; ` +
    `${heldAtBreak.length} stalks broken, ${tooled.length} with a tool (${[...new Set(heldAtBreak)].join(', ')}), axe kept ${axeKept}`);
  for (const l of body.logs) if (/stopped|failed/.test(l)) console.log('      ', l);
  return ok;
}

async function logsLeft() {
  const [a] = await rcon.run(['execute store result score #logs pathtest if entity @e[type=item,x=74,y=-60,z=47,distance=..12,nbt={Item:{id:"minecraft:oak_log"}}]']);
  return Number((/Count: (\d+)/.exec(a) || [0, 0])[1]);
}

async function gatherTest() {
  await rcon.run(['kill @e[type=item,x=74,y=-60,z=47,distance=..14]', ...groveCommands(), `clear ${Name}`]);
  const body = startBody();
  await spawned(body);
  await rcon.run([`clear ${Name}`, `tp ${Name} 72.5 -60 38.5`]);
  await wait(2000);
  const started = Date.now();
  body.send({ op: 'collect', player: 'tester', resource: 'wood', count: 16 });
  const going = await body.next((e) => e.ev === 'collecting' || e.ev === 'cantCollect', 10000);
  const done = await body.next((e) => e.ev === 'collected', 180000);
  const secs = ((Date.now() - started) / 1000).toFixed(1);
  const left = await logsLeft();
  const pos = await where(body);
  await body.quit();
  const intact = await scenery(GroveScenery, 'grove scenery');
  const ok = Boolean(done) && done.count >= 16 && left === 0 && intact;
  console.log(`${ok ? 'PASS' : 'FAIL'}  gather: ${going ? going.ev : 'no answer'} -> ${done ? `collected ${done.count}` : 'never finished (stuck?)'} in ${secs} s, logs left on the ground ${left}, she ended at ${pos}`);
  for (const l of body.logs) if (/stopped|failed/.test(l)) console.log('      ', l);
  return ok;
}

// ---- Main --------------------------------------------------------------------------------------------

(async () => {
  const [what, ...rest] = process.argv.slice(2).filter((a) => a !== '-v');
  let ok = true;
  if (what === 'build') await build();
  else if (what === 'lanes') ok = await lanes(rest);
  else if (what === 'crops') ok = await crops();
  else if (what === 'items') ok = await items();
  else if (what === 'gather') ok = await gatherTest();
  else if (what === 'harvest') ok = await harvestTest();
  else if (what === 'actions') ok = await actions(rest);
  else if (what === 'safety') ok = await safety();
  else if (what === 'survival') ok = await survival(rest);
  else if (what === 'trip') ok = await trip();
  else if (what === 'players') ok = await playersTest(rest);
  else if (what === 'orders') ok = await orders(rest);
  else if (what === 'routine') ok = await routine();
  else if (what === 'all') {
    ok = (await lanes([])) && ok;
    ok = (await crops()) && ok;
    ok = (await items()) && ok;
    ok = (await gatherTest()) && ok;
    ok = (await harvestTest()) && ok;
    ok = (await actions([])) && ok;
    ok = (await safety()) && ok;
    ok = (await survival([])) && ok;
    ok = (await trip()) && ok;
    ok = (await playersTest([])) && ok;
    ok = (await orders([])) && ok;
  } else {
    console.log('usage: node run.js build | lanes [name ...] | crops | items | gather | harvest | actions [name ...] | safety | survival [name ...] | trip | players [name ...] | orders [name ...] | routine | all   [-v]');
    process.exit(2);
  }
  process.exit(ok ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
