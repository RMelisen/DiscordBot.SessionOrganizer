// The camp (« monte le camp »): built from what she's given, checked cell by cell on the server against the layout,
// then taken down (only its own blocks; the trodden floor stays, nothing else touched, everything back on her). On the
// flat world, in its own corner, over short grass, with a poppy on a floor cell of the nearest site (it stays), and
// Papa standing beside her where that site's fire pit would be (as he does when he asks): she builds round him.
//   node camp.js [interrupt|nobed|straw|holes|charcoal|mixed|uneven] [-v]
// `interrupt`: a creeper walks up while she builds, then a zombie bites a stranger by the site: she deals with both
//   and finishes the same camp. `nobed`: no wool, the camp goes up without a bed. `straw`: no wool but 27 wheat, a straw
//   bed (a yellow bed to her 26.1 eyes). `holes`: a camp taken up again at a given site with a floor cell dug out (she
//   fills it), a table in its list that a creeper blew away (she makes another), and someone's diamond lying by the camp
//   (not hers: it stays). `charcoal`: no coal, none to mine: charcoal from the camp's furnace. `mixed`: oak, birch
//   and spruce logs, ten each: every wooden part still made. `uneven`: the site centred on her has a post's ground a
//   block higher and a floor cell's a block lower: she builds there anyway, the post on its bump, the cell trodden in
//   its dip.
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn } = require('./players');
const { Vec3 } = require('../../ProjectSYNCS/MinecraftBody/node_modules/vec3');

const K = { x: -40, z: -80 }; // a clear corner of the flat world
const at = (dx, dy, dz) => `${K.x + dx} ${-60 + dy} ${K.z + dz}`;
const mode = ['interrupt', 'nobed', 'straw', 'holes', 'charcoal', 'mixed', 'uneven'].find((m) => process.argv.includes(m)) || 'build';
const Woods = ['oak', 'birch', 'spruce'];
const wooden = (suffix) => (n) => Woods.some((w) => n === `${w}${suffix}`);
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(16)} ${detail}`);
}

// The body's CampLayout (lib/camp.js), the back to the north: F fire, d trapdoor, L log, S slab, T table, K furnace,
// C chest, B bed, l torch post, p/D floor, . left alone.
const Layout = [
  '.l.......',
  '...LSS.Bl',
  '..pDpp.B.',
  '.LpDdppD.',
  '.SpdFdpT.',
  '.SDpdpDK.',
  '..pDppDC.',
  'l..pDp...',
  '....p..l.',
];
const cells = (marks) => {
  const out = [];
  Layout.forEach((row, z) => [...row].forEach((m, x) => { if (marks.includes(m)) out.push([x, z]); }));
  return out;
};
const Floor = cells('pD');
const Ways = ['4,6', '4,7', '4,8']; // from the fire to the way in: always path
const Poppy = { dx: 1, dz: 1 };
// `uneven`: on the site centred on her (corner at -4, -4), the post at (1, 0) a block up, the floor cell (2, 3) a block down.
const Uneven = { '1,0': 1, '2,3': -1 };

let watcher = null;
const blockAt = (x, y, z) => watcher.blockAt(new Vec3(x, y, z));
const props = (b) => (b ? b.getProperties() : {});

// Every cell of a camp with its north-west corner at `o`, as the watcher sees it. `bed`: the bed's name, or null for
// none. The wrong cells, and how many floor cells are path.
function checkCamp(o, bed, { anyFloor = false, lift = {} } = {}) {
  const wrong = [];
  // `lift`: cells whose ground the test moved, by how much ("x,z": dy).
  const look = (x, z, y = 0) => blockAt(o.x + x, o.y + y + (lift[`${x},${z}`] || 0), o.z + z);
  const want = (x, z, ok, what, y = 0) => {
    const b = look(x, z, y);
    if (!b || !ok(b)) wrong.push(`(${x},${z}${y ? `,${y}` : ''}) ${b ? `${b.name}${JSON.stringify(props(b))}` : '?'} not ${what}`);
  };
  let paths = 0;
  let dirt = 0;
  Layout.forEach((row, z) => [...row].forEach((m, x) => {
    switch (m) {
      case 'F': want(x, z, (b) => b.name === 'campfire' && props(b).lit === true, 'a lit campfire'); break;
      case 'd': want(x, z, (b) => wooden('_trapdoor')(b.name) && props(b).half === 'bottom' && props(b).open === false, 'a flat trapdoor'); break;
      case 'L': want(x, z, (b) => wooden('_log')(b.name) && props(b).axis === (z === 1 ? 'x' : 'z'), `a log along ${z === 1 ? 'x' : 'z'}`); break;
      case 'S': want(x, z, (b) => wooden('_slab')(b.name) && props(b).type === 'bottom', 'a bottom slab'); break;
      case 'T': want(x, z, (b) => b.name === 'crafting_table', 'the table'); break;
      case 'K': want(x, z, (b) => b.name === 'furnace', 'the furnace'); break;
      case 'C': want(x, z, (b) => b.name === 'chest', 'the chest'); break;
      case 'B':
        if (bed) want(x, z, (b) => b.name === bed && props(b).part === (z === 1 ? 'head' : 'foot') && props(b).facing === 'north', `${bed} ${z === 1 ? 'head' : 'foot'} north`);
        else want(x, z, (b) => !b.name.endsWith('_bed'), 'no bed');
        break;
      case 'l':
        want(x, z, (b) => wooden('_fence')(b.name), 'a fence');
        want(x, z, (b) => b.name === 'torch', 'a torch', 1);
        break;
      case 'p': case 'D': {
        const g = look(x, z, -1);
        if (g && g.name === 'dirt_path') paths++;
        if (g && g.name === 'dirt') dirt++;
        // Plain dirt grows back to grass within minutes (the game's doing), so grass passes here; the counts below
        // say she trod it.
        want(x, z, (b) => ['dirt_path', 'dirt', 'grass_block'].includes(b.name), 'floor', -1);
        if (!anyFloor && Ways.includes(`${x},${z}`)) want(x, z, (b) => b.name === 'dirt_path', 'path', -1);
        break;
      }
      default: { // left as it was
        want(x, z, (b) => b.name === 'grass_block', 'grass', -1);
        const poppy = o.x + x === K.x + Poppy.dx && o.z + z === K.z + Poppy.dz;
        for (const y of [0, 1]) want(x, z, (b) => ['air', 'short_grass'].includes(b.name) || (poppy && y === 0 && b.name === 'poppy'), 'untouched', y);
      }
    }
  }));
  if (!anyFloor && (paths < 10 || paths > 20)) wrong.push(`${paths} path cells, not 10 to 20`);
  if (paths + dirt < Floor.length / 2) wrong.push(`only ${paths} path and ${dirt} dirt cells: the floor wasn't trodden`);
  return { wrong, paths };
}

// Holes in the ground: nothing she broke may be missing (a path is ground too; `uneven`'s dip is the test's).
function groundTouched() {
  let holes = 0;
  for (let dx = -12; dx <= 12; dx++)
    for (let dz = -12; dz <= 12; dz++) {
      if (mode === 'uneven' && dx === -2 && dz === -1) continue;
      const b = blockAt(K.x + dx, -61, K.z + dz);
      if (!b || b.boundingBox !== 'block') holes++;
    }
  return holes;
}

(async () => {
  await rcon.run([`forceload add ${K.x - 16} ${K.z - 16} ${K.x + 16} ${K.z + 16}`, `fill ${at(-12, -1, -12)} ${at(12, -1, 12)} grass_block`,
    `fill ${at(-12, -2, -12)} ${at(12, -2, 12)} dirt`, `fill ${at(-12, 0, -12)} ${at(12, 8, 12)} air`, `fill ${at(-12, 0, -12)} ${at(12, 0, 12)} short_grass`,
    `kill @e[type=item,x=${K.x},y=-60,z=${K.z},distance=..30]`, 'scoreboard objectives add pathtest dummy', 'difficulty peaceful', 'time set day']);
  if (mode !== 'holes') await rcon.run([`setblock ${at(Poppy.dx, 0, Poppy.dz)} poppy`]);
  if (mode === 'uneven') await rcon.run([`setblock ${at(-3, 0, -4)} grass_block`, `setblock ${at(-3, 1, -4)} short_grass`,
    `setblock ${at(-2, 0, -1)} air`, `setblock ${at(-2, -1, -1)} air`]);
  const body = startBody();
  await spawned(body);
  watcher = await standIn('Watcher');
  await rcon.run(['gamemode spectator Watcher', `tp Watcher ${K.x} -50 ${K.z}`]);
  // Papa beside her, on the nearest site's pit (its cell (4, 5)); holes takes up a site given, so he stays out of it.
  const papa = mode === 'holes' ? null : await standIn('Papa');
  // In `uneven`, on that site's corner (0, 0), left alone: the site is the one under test there.
  const papaAt = mode === 'uneven' ? [K.x - 3.5, K.z - 3.5] : [K.x + 0.5, K.z + 1.5];
  if (papa) await rcon.run([`tp Papa ${papaAt[0]} -60 ${papaAt[1]}`, 'gamemode survival Papa', 'effect give Papa resistance 900 4 true']);
  const bedGoods = { nobed: [], straw: [`give ${Name} wheat 27`] }[mode] || [`give ${Name} white_wool 3`];
  const logs = mode === 'mixed' ? Woods.map((w) => `give ${Name} ${w}_log 10`) : [`give ${Name} oak_log 30`];
  const coal = mode === 'charcoal' ? [] : [`give ${Name} coal 2`];
  await rcon.run([`clear ${Name}`, `advancement revoke ${Name} everything`, `recipe take ${Name} *`, `tp ${Name} ${K.x + 0.5} -60 ${K.z + 0.5}`, ...logs,
    `give ${Name} cobblestone 8`, ...coal, ...bedGoods]);
  await wait(3000);
  const bed = mode === 'nobed' ? null : mode === 'straw' ? 'yellow_bed' : 'white_bed';
  let stranger = null;
  if (mode === 'interrupt') {
    stranger = await standIn('Stranger');
    await rcon.run([`give ${Name} bow`, `give ${Name} arrow 32`, `give ${Name} iron_sword`, 'difficulty normal', 'gamerule spawn_monsters false',
      `tp Stranger ${K.x - 7.5} -60 ${K.z + 8.5}`, `effect give Stranger resistance 600 2 true`, 'gamemode survival Stranger']);
  }
  // `holes`: taken up again at a site of our choosing, every floor cell plain dirt, one of them dug out.
  const given = mode === 'holes' ? { x: K.x - 4, y: -60, z: K.z - 4 } : null;
  // Dug as she digs (`destroy`: its dirt drops in the hole), the way an interruption leaves it; someone's diamond just
  // outside the camp, three blocks from the hole.
  if (given) await rcon.run([`setblock ${given.x + 2} -60 ${given.z + 2} air`, `setblock ${given.x + 2} -61 ${given.z + 2} air destroy`,
    `summon item ${given.x - 0.5} -60 ${given.z + 2.5} {Item:{id:"minecraft:diamond",count:1},PickupDelay:0s,Tags:["theirs"]}`]);
  // The table in her list, gone (a creeper): it must be made again, not counted as there.
  const gone = given ? [{ x: given.x + 7, y: -60, z: given.z + 4, name: 'crafting_table' }] : [];
  const started = Date.now();
  body.send(given ? { op: 'camp', player: 'Papa', site: given, placed: gone, floor: Floor.map(([x, z]) => [x, z, 'dirt']) }
    : { op: 'camp', player: 'Papa' });
  const going = given ? { ev: 'resumed' } : await body.next((e) => e.ev === 'camping' || e.ev === 'cantCamp', 15000);
  const dangers = mode === 'interrupt' ? (async () => {
    await wait(20000);
    await rcon.run([`summon creeper ${at(15, 0, -3)} {PersistenceRequired:1b}`]); // nearer her than the stranger: it comes for her
    await wait(25000);
    await rcon.run([`summon zombie ${at(-9, 0, 9)} {NoAI:1b,PersistenceRequired:1b,Tags:["bite"]}`]);
    await wait(1000);
    await rcon.run([`damage Stranger 2 minecraft:mob_attack by @e[type=zombie,tag=bite,limit=1]`]);
  })() : null;
  const done = going && going.ev === 'cantCamp' ? going : await body.next((e) => e.ev === 'camped' || e.ev === 'cantCamp', Number(process.env.CAMP_WAIT) || 900000);
  if (dangers) await dangers;
  const secs = ((Date.now() - started) / 1000).toFixed(0);
  if (done && done.blocks) console.log(`      placed: ${done.blocks.map((b) => b.name).join(' ')}`);
  await wait(1500);
  if (process.argv.includes('-v')) {
    const inv = body.events.filter((e) => e.ev === 'inventory').pop();
    if (inv) console.log(`      on her: ${inv.pockets.map((i) => `${i.name}x${i.count}`).join(' ')}`);
  }
  const o = done && done.ev === 'camped' ? { x: done.x - 4, y: done.y, z: done.z - 4 } : null;
  const seen = o ? checkCamp(o, bed, { anyFloor: mode === 'holes', lift: mode === 'uneven' ? Uneven : {} }) : { wrong: ['no camp'], paths: 0 };
  if (mode === 'uneven') report(Boolean(o) && o.x === K.x - 4 && o.z === K.z - 4, 'on the bumps', o ? `camp's corner at ${o.x - K.x} ${o.z - K.z} from hers (-4 -4 wanted)` : 'no camp');
  const flags = done ? `${done.ev}${done.why ? ` (${done.why} ${JSON.stringify(done.missing)})` : ''}, complete ${done.complete}, bed ${done.bed}` : 'never finished';
  report(Boolean(done && done.ev === 'camped' && done.complete === true && done.bed === Boolean(bed)) && seen.wrong.length === 0, 'build',
    `${going ? going.ev : 'no start'} → ${flags} in ${secs} s; ${seen.paths} path cells; ${seen.wrong.length ? `wrong: ${seen.wrong.slice(0, 8).join('; ')}` : 'every cell right'}`);
  if (mode === 'straw' && o) {
    const [r] = await rcon.run([`execute if block ${o.x + 7} ${o.y} ${o.z + 1} minecraft:straw_bed`]);
    report(/passed/i.test(r || ''), 'straw bed', `the server says: ${r}`);
  }
  if (mode === 'holes' && o) {
    const g = blockAt(o.x + 2, o.y - 1, o.z + 2);
    const filled = Boolean(g) && ['dirt', 'grass_block'].includes(g.name);
    report(filled && groundTouched() === 0, 'holes filled', `the dug cell: ${g ? g.name : '?'}, holes in the ground ${groundTouched()}`);
    const theirs = await q.count('@e[type=item,tag=theirs]');
    report(theirs === 1, 'not hers', `the diamond by the camp ${theirs === 1 ? 'still lying' : 'picked up'}`);
  }
  if (mode !== 'holes') {
    const [poppy] = await rcon.run([`execute if block ${at(Poppy.dx, 0, Poppy.dz)} minecraft:poppy`]);
    report(/passed/i.test(poppy || '') && groundTouched() === 0, 'untouched', `poppy ${/passed/i.test(poppy || '') ? 'standing' : 'gone'}, holes in the ground ${groundTouched()}`);
  }
  if (mode === 'interrupt') {
    const creepers = await q.count(`@e[type=creeper,x=${K.x},y=-60,z=${K.z},distance=..40]`);
    const zombies = await q.count(`@e[type=zombie,tag=bite]`);
    report(creepers === 0 && zombies === 0, 'dealt with', `creeper ${creepers === 0 ? 'gone' : 'still there'}, the biting zombie ${zombies === 0 ? 'killed' : 'left alone'}`);
    await rcon.run(['difficulty peaceful']);
  }

  if (o) {
    if (papa) await rcon.run([`tp Papa ${K.x + 20} -60 ${K.z + 20}`]); // a player picks up what drops by him
    const downAt = Date.now();
    body.send({ op: 'uncamp', player: 'Papa', blocks: done.blocks, dimension: done.dimension, center: { x: done.x, y: done.y, z: done.z } });
    const down = await body.next((e) => e.ev === 'uncamped' || e.ev === 'cantUncamp', 600000);
    const downSecs = ((Date.now() - downAt) / 1000).toFixed(0);
    await wait(1500);
    const standing = done.blocks.filter((b) => { const now = blockAt(b.x, b.y, b.z); return now && now.name === b.name; }).map((b) => b.name);
    // The floor stays: every path still a path; plain dirt may have grown back to grass (the game's doing: grass spreads
    // onto dirt beside it within minutes), never anything else.
    const lift = mode === 'uneven' ? Uneven : {};
    const ground = Floor.map(([x, z]) => blockAt(o.x + x, o.y - 1 + (lift[`${x},${z}`] || 0), o.z + z)).map((g) => (g ? g.name : '?'));
    const regrown = ground.filter((n) => n === 'grass_block').length;
    const floorKept = ground.filter((n) => n === 'dirt_path').length === seen.paths && ground.every((n) => ['dirt_path', 'dirt', 'grass_block'].includes(n));
    // A campfire broken without silk touch drops two charcoal, not itself (vanilla).
    const any = (suffix) => Woods.map((w) => `${w}${suffix}`);
    const kit = [['chest', 1], ['crafting_table', 1], ['furnace', 1], ['charcoal', 2], [any('_trapdoor'), 4], [any('_slab'), 4], [any('_log'), 2],
      // Her pockets as the server keeps them: the straw bed she sees as a yellow one is a straw_bed there.
      [any('_fence'), 4], ['torch', 1], ...(bed ? [[mode === 'straw' ? 'straw_bed' : bed, 1]] : [])];
    for (const k of kit) {
      let n = 0;
      for (const name of [].concat(k[0])) n += await q.carried(Name, name);
      k.push(n);
    }
    const short = kit.filter(([, n, c]) => c < n);
    const [lying] = await rcon.run([`execute store result score #n pathtest if entity @e[type=item,x=${o.x + 4},y=-60,z=${o.z + 4},distance=..14]`]);
    report(Boolean(down && down.ev === 'uncamped' && down.left === 0) && standing.length === 0 && floorKept && groundTouched() === 0 && short.length === 0,
      'take down', `${down ? `${down.ev} ${down.count ?? ''}${down.left ? `, ${down.left} left` : ''}` : 'never finished'} in ${downSecs} s; ` +
      `still standing: ${standing.join(', ') || 'nothing'}; floor kept ${floorKept} (${regrown} dirt grown back); holes ${groundTouched()}; ` +
      `short on her: ${short.map(([n, w, c]) => `${n} ${c}/${w}`).join(', ') || 'nothing'}; items lying ${(/Count: (\d+)/.exec(lying) || [0, '?'])[1]}`);
  }
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
  await watcher.leave();
  if (papa) await papa.leave();
  if (stranger) await stranger.leave();
  await body.quit();
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} camp checks passed`);
  process.exit(failed.length === 0 ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
