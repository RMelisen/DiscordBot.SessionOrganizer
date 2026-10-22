// The camp (« monte le camp »): built from what she's given, checked block by block on the server, then taken down
// (only its own blocks, the ground left as it was, everything back on her). On the flat world, in its own corner.
//   node camp.js [-v]
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn } = require('./players');

const K = { x: -40, z: -80 }; // a clear corner of the flat world
const at = (dx, dy, dz) => `${K.x + dx} ${-60 + dy} ${K.z + dz}`;
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(16)} ${detail}`);
}

// What stands around the corner, by block name, as a spectator watching from above reads it (a `fill … replace` with
// the same block counts nothing: nothing changed).
let watcher = null;
function blocksIn(names) {
  const counts = {};
  for (const name of names) {
    const id = watcher.registry.blocksByName[name].id;
    counts[name] = watcher.findBlocks({ matching: id, maxDistance: 20, count: 2000 })
      .filter((p) => Math.abs(p.x - K.x) <= 12 && Math.abs(p.z - K.z) <= 12 && p.y >= -60 && p.y <= -54).length;
  }
  return counts;
}

// Holes in the ground: grass under a block turns to dirt (the game's doing), but nothing she broke may be missing.
function groundTouched() {
  const { Vec3 } = require('../../ProjectSYNCS/MinecraftBody/node_modules/vec3');
  let holes = 0;
  for (let dx = -12; dx <= 12; dx++)
    for (let dz = -12; dz <= 12; dz++) {
      const b = watcher.blockAt(new Vec3(K.x + dx, -61, K.z + dz));
      if (!b || b.boundingBox !== 'block') holes++;
    }
  return holes;
}

(async () => {
  await rcon.run([`forceload add ${K.x - 16} ${K.z - 16} ${K.x + 16} ${K.z + 16}`, `fill ${at(-12, -1, -12)} ${at(12, -1, 12)} grass_block`,
    `fill ${at(-12, 0, -12)} ${at(12, 8, 12)} air`, `kill @e[type=item,x=${K.x},y=-60,z=${K.z},distance=..30]`,
    'scoreboard objectives add pathtest dummy', 'difficulty peaceful', 'time set day']);
  const body = startBody();
  await spawned(body);
  watcher = await standIn('Watcher');
  await rcon.run(['gamemode spectator Watcher', `tp Watcher ${K.x} -50 ${K.z}`]);
  await rcon.run([`clear ${Name}`, `tp ${Name} ${K.x + 0.5} -60 ${K.z + 0.5}`, `give ${Name} oak_log 30`, `give ${Name} cobblestone 8`,
    `give ${Name} coal 2`, `give ${Name} white_wool 3`]);
  await wait(3000);
  const started = Date.now();
  body.send({ op: 'camp', player: 'Papa' });
  const going = await body.next((e) => e.ev === 'camping' || e.ev === 'cantCamp', 15000);
  const done = await body.next((e) => e.ev === 'camped' || e.ev === 'cantCamp', 600000);
  const secs = ((Date.now() - started) / 1000).toFixed(0);
  if (done && done.blocks) console.log(`      placed: ${done.blocks.map((b) => b.name).join(' ')}`);
  await wait(1500);
  const names = ['oak_planks', 'white_bed', 'chest', 'crafting_table', 'furnace', 'oak_door', 'campfire', 'torch'];
  const stood = blocksIn(names);
  if (process.argv.includes('-v')) {
    const inv = body.events.filter((e) => e.ev === 'inventory').pop();
    if (inv) console.log(`      on her: ${inv.pockets.map((i) => `${i.name}x${i.count}`).join(' ')}`);
    console.log(`      server: ${(await q.listItems('entity', Name, 'Inventory', 41)).replace(/components: \{[^}]*\}/g, '').replace(/\{Slot: \d+b, id: "minecraft:([a-z_]+)", count: (\d+)[^}]*\}/g, '$1x$2')}`);
  }
  const want = { oak_planks: 71, white_bed: 2, chest: 1, crafting_table: 1, furnace: 1, oak_door: 2, campfire: 1, torch: 3 };
  const short = names.filter((n) => stood[n] !== want[n]);
  report(Boolean(done && done.ev === 'camped' && done.complete) && short.length === 0, 'build',
    `${going ? going.ev : 'no start'} → ${done ? `${done.ev}${done.why ? ` (${done.why} ${JSON.stringify(done.missing)})` : ''}${done.complete === false ? ' (incomplete)' : ''}` : 'never finished'} in ${secs} s; ` +
    `${names.map((n) => `${n} ${stood[n]}/${want[n]}`).join(', ')}`);
  // She's outside, not walled in.
  const x = await q.entityNumber(Name, 'Pos[0]');
  const z = await q.entityNumber(Name, 'Pos[2]');
  report(Boolean(done && done.ev === 'camped'), 'out of the hut', `she stands at ${(x - K.x).toFixed(1)} ${(z - K.z).toFixed(1)} from the corner`);

  if (done && done.ev === 'camped') {
    const downAt = Date.now();
    body.send({ op: 'uncamp', player: 'Papa', blocks: done.blocks, dimension: done.dimension, center: { x: done.x, y: done.y, z: done.z } });
    const down = await body.next((e) => e.ev === 'uncamped' || e.ev === 'cantUncamp', 600000);
    const downSecs = ((Date.now() - downAt) / 1000).toFixed(0);
    await wait(1500);
    const left = blocksIn(names);
    const leftOver = names.filter((n) => left[n] > 0);
    const touched = groundTouched();
    const planks = await q.carried(Name, 'oak_planks');
    const kit = ['white_bed', 'chest', 'crafting_table', 'furnace', 'oak_door', 'torch'].map((n) => [n, 0]);
    for (const k of kit) k[1] = await q.carried(Name, k[0]);
    const [lying] = await rcon.run([`execute store result score #n pathtest if entity @e[type=item,x=${K.x},y=-60,z=${K.z},distance=..14]`]);
    report(Boolean(down && down.ev === 'uncamped') && leftOver.length === 0 && touched === 0 && planks >= 65 && kit.every(([, n]) => n > 0),
      'take down', `${down ? `${down.ev} ${down.count ?? ''}${down.left ? `, ${down.left} left` : ''}` : 'never finished'} in ${downSecs} s; still standing: ${leftOver.join(', ') || 'nothing'}; ` +
      `holes in the ground ${touched}; on her: planks ${planks}, ${kit.map(([n, c]) => `${n} ${c}`).join(', ')}; items lying ${(/Count: (\d+)/.exec(lying) || [0, '?'])[1]}`);
  }
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
  await watcher.leave();
  await body.quit();
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} camp checks passed`);
  process.exit(failed.length === 0 ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
