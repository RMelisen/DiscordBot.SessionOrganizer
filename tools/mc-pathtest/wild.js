// The wild: a generated world instead of the flat test world (see README, « The wild »). Papa, a stand-in with a
// pathfinder, walks routes through real biomes while she follows; orders where they really happen (a forest's
// trees, a river's fish, a mountain's stone); the Nether's ground and foes, and a portal walked through in front
// of her. With `--lag ms jitter`, her body (and only hers) talks to the server through lag.js.
//   node wild.js [follow|orders|nether|portal ...] [--lag 120 60] [-v]
const rcon = require('./rcon');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn, req } = require('./players');
const lag = require('./lag');

const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(22)} ${detail}`);
}

const args = process.argv.slice(2).filter((a) => a !== '-v');
const lagAt = args.indexOf('--lag');
const lagMs = lagAt >= 0 ? [Number(args[lagAt + 1]) || 120, Number(args[lagAt + 2]) || 60] : null;
const only = (lagAt >= 0 ? args.slice(0, lagAt).concat(args.slice(lagAt + 3)) : args);

// ---- Places -------------------------------------------------------------------------------------

// Where a biome or a structure is, by the server's own `locate` (x, z), or null.
async function locate(what, dimension = 'overworld') {
  const kind = what.startsWith('structure:') ? 'structure' : 'biome';
  const id = what.replace(/^structure:/, '');
  const [a] = await rcon.run([`execute in minecraft:${dimension} run locate ${kind} minecraft:${id}`]);
  const m = /\[(-?\d+), [^,]+, (-?\d+)\]/.exec(a);
  return m ? { x: Number(m[1]), z: Number(m[2]) } : null;
}

const Under = { the_nether: 'under 100 ' };
// Whether Papa can plan a 12-block walk in some direction from where he stands: spreadplayers under the Nether's roof
// can put him in a closed cave pocket, where there's nowhere to go for either of them.
function roomy(papa) {
  const { goals } = req('mineflayer-pathfinder');
  const p = papa.entity.position;
  return [[12, 0], [-12, 0], [0, 12], [0, -12]].some(([dx, dz]) => {
    const r = papa.pathfinder.getPathTo(papa.pathfinder.movements, new goals.GoalNearXZ(p.x + dx, p.z + dz, 2), 1500);
    return r.status === 'success';
  });
}
// Papa on the ground near (x, z): spreadplayers puts him on the top block, which in a forest can be the canopy, so
// a spot on leaves (or in water, for a walk) is tried again a little further on. Then she's sent to him.
async function placeTogether(papa, at, dimension = 'overworld', { water = false } = {}) {
  const spread = dimension === 'the_nether' ? 120 : 40; // the Nether's caves run wide
  for (let i = 0; i < 8; i++) {
    const x = at.x + (i === 0 ? 0 : Math.round((Math.random() - 0.5) * spread));
    const z = at.z + (i === 0 ? 0 : Math.round((Math.random() - 0.5) * spread));
    await rcon.run([`execute in minecraft:${dimension} run spreadplayers ${x} ${z} 0 6 ${Under[dimension] || ''}false Papa`]);
    await wait(2500);
    const below = papa.blockAt(papa.entity.position.offset(0, -0.5, 0));
    const feet = papa.blockAt(papa.entity.position);
    // In the Nether, nothing down by the lava sea (31): a ledge over it has no way off, for him or for her.
    const ledge = dimension === 'the_nether' && papa.entity.position.y < 40;
    const bad = !below || ledge || /leaves|lava|magma|fire|campfire/.test(below.name) || (!water && feet && feet.name === 'water')
      || (dimension === 'the_nether' && !roomy(papa));
    if (!bad) break;
  }
  await rcon.run([`tp ${Name} Papa`]);
  await wait(3000);
}

// ---- Papa's walks ------------------------------------------------------------------------------

// Papa walks `legs` legs of about `len` blocks in random directions, by his own pathfinder (no digging, no
// building: only where a player walks). A leg with no way is given up after `legMs`. Meanwhile her gap to him is
// sampled; `stuck` counts the times she stood still for 8 s while more than 5 blocks behind.
async function walk(sc, legs, len, legMs = 30000) {
  const { goals } = req('mineflayer-pathfinder');
  const { papa } = sc;
  const s = { travelled: 0, worst: 0, stuck: 0, legsDone: 0 };
  let last = papa.entity.position.clone();
  let still = { at: Date.now(), pos: null };
  const sample = setInterval(() => {
    const her = papa.players[Name] && papa.players[Name].entity;
    if (!her) return;
    const gap = her.position.distanceTo(papa.entity.position);
    s.worst = Math.max(s.worst, gap);
    if (!still.pos || her.position.distanceTo(still.pos) > 1) still = { at: Date.now(), pos: her.position.clone() };
    else if (gap > 5 && Date.now() - still.at > 8000) { s.stuck++; still = { at: Date.now(), pos: her.position.clone() }; }
  }, 500);
  for (let leg = 0; leg < legs; leg++) {
    const a = Math.random() * 2 * Math.PI;
    const p = papa.entity.position;
    const goal = new goals.GoalNearXZ(Math.round(p.x + Math.cos(a) * len), Math.round(p.z + Math.sin(a) * len), 2);
    let timer;
    try {
      await Promise.race([papa.pathfinder.goto(goal), new Promise((_, no) => { timer = setTimeout(() => no(new Error('slow')), legMs); })]);
      s.legsDone++;
    } catch (err) {
      papa.pathfinder.setGoal(null);
      if (process.argv.includes('-v')) console.log(`      (Papa's leg from ${p.floored()} failed: ${err.message})`);
    }
    clearTimeout(timer);
    s.travelled += papa.entity.position.distanceTo(last);
    last = papa.entity.position.clone();
  }
  await wait(8000); // she catches up
  clearInterval(sample);
  const her = papa.players[Name] && papa.players[Name].entity;
  s.final = her ? her.position.distanceTo(papa.entity.position) : NaN;
  return s;
}

// ---- A run ---------------------------------------------------------------------------------------

async function scene({ difficulty = 'peaceful', papaMode = 'survival' } = {}) {
  await rcon.run(['scoreboard objectives add pathtest dummy', `difficulty ${difficulty}`, 'time set day', 'weather clear',
    `gamerule spawn_monsters ${difficulty !== 'peaceful'}`, `gamemode ${papaMode} Papa`]);
  const proxy = lagMs ? await lag.start({ ms: lagMs[0], jitter: lagMs[1] }) : null;
  const body = startBody(proxy ? { MC_PORT: String(proxy.port) } : {});
  await spawned(body);
  const papa = await standIn('Papa', { pathfinder: true });
  await rcon.run([`gamemode ${papaMode} Papa`, `gamemode survival ${Name}`, `effect give ${Name} minecraft:saturation 600 1 true`,
    `effect give Papa minecraft:saturation 600 1 true`]);
  body.send({ op: 'family', names: ['Papa'] });
  const sc = { body, papa, proxy, deaths: 0, hurts: 0, lost: [] };
  const watch = (ev) => {
    if (ev.ev === 'died') sc.deaths++;
    if (ev.ev === 'hurt') sc.hurts++;
    if (ev.ev === 'cantReach') sc.lost.push(ev.why);
  };
  sc.from = body.events.length;
  sc.watch = setInterval(() => { for (const ev of body.events.slice(sc.from)) watch(ev); sc.from = body.events.length; }, 250);
  return sc;
}

async function end(sc, name) {
  clearInterval(sc.watch);
  for (const t of troubles(sc.body)) console.log(`      ${name} log:`, t.slice(0, 200));
  await sc.papa.leave();
  await sc.body.quit();
  if (sc.proxy) await sc.proxy.close();
}

// ---- Following through the biomes ------------------------------------------------------------------

const Biomes = process.env.WILD_BIOMES ? process.env.WILD_BIOMES.split(',') : ['meadow', 'forest', 'taiga', 'river', 'structure:village_plains', 'dark_forest', 'mangrove_swamp', 'birch_forest',
  'snowy_slopes', 'beach'];

async function follow() {
  const sc = await scene();
  for (const biome of Biomes) {
    const at = await locate(biome);
    if (!at) { console.log(`      (no ${biome} found)`); continue; }
    try {
      await placeTogether(sc.papa, at);
      sc.body.send({ op: 'follow', player: 'Papa' });
      await wait(1000);
      const corrections = sc.body.corrections;
      const { deaths, hurts } = sc;
      sc.lost = [];
      const s = await walk(sc, 4, 22);
      sc.body.send({ op: 'stay' });
      const fixes = sc.body.corrections - corrections;
      const ok = s.travelled >= 30 && s.final < 6 && s.stuck === 0 && sc.lost.length === 0 && sc.deaths === deaths && fixes === 0;
      report(ok, `follow: ${biome.replace('structure:', '')}`,
        `Papa walked ${s.travelled.toFixed(0)} blocks (${s.legsDone}/4 legs); she was at most ${s.worst.toFixed(1)} behind, ` +
        `${s.final.toFixed(1)} at the end; stuck ${s.stuck}×, lost ${JSON.stringify(sc.lost)}, ` +
        `${sc.hurts - hurts} hurts, ${sc.deaths - deaths} deaths, ${fixes} corrections`);
    } catch (err) {
      report(false, `follow: ${biome}`, `threw: ${err.message}`);
    }
    await wait(1500);
  }
  await end(sc, 'follow');
}

// The village: following Papa among the houses and fields, and nothing trampled on the way.
async function village() {
  const sc = await scene();
  const at = await locate('structure:village_plains');
  await placeTogether(sc.papa, at);
  const mcData = req('minecraft-data')(sc.papa.version);
  // Papa keeps off the fields (a player's jump tramples too): whatever is trampled is hers. Counted around one point.
  sc.papa.pathfinder.movements.blocksToAvoid.add(mcData.blocksByName.farmland.id);
  const center = sc.papa.entity.position.clone();
  const near = (ids) => sc.papa.findBlocks({ point: center, matching: ids, maxDistance: 48, count: 4000 }).length;
  const fields = () => near([mcData.blocksByName.farmland.id]);
  const crops = () => near(['wheat', 'carrots', 'potatoes', 'beetroots'].map((n) => mcData.blocksByName[n].id));
  const before = { fields: fields(), crops: crops() };
  sc.body.send({ op: 'follow', player: 'Papa' });
  await wait(1000);
  const corrections = sc.body.corrections;
  const s = await walk(sc, 6, 16);
  sc.body.send({ op: 'stay' });
  await rcon.run([`tp Papa ${center.x} ${center.y} ${center.z}`]); // back where the counts were taken
  await wait(3000);
  const after = { fields: fields(), crops: crops() };
  // Papa walks the fields too (a player can trample): only what she alone could have done is hers, so this reports both.
  report(s.final < 6 && s.stuck === 0 && sc.body.corrections === corrections && after.fields === before.fields,
    'follow: village', `Papa walked ${s.travelled.toFixed(0)} blocks; at most ${s.worst.toFixed(1)} behind, ${s.final.toFixed(1)} at the end; ` +
    `stuck ${s.stuck}×, ${sc.body.corrections - corrections} corrections; farmland ${before.fields} → ${after.fields}, crops ${before.crops} → ${after.crops}`);
  await end(sc, 'village');
}

// ---- Orders in the wild -----------------------------------------------------------------------

async function orderInto(sc, place, give, op, done, ms, name, detail) {
  const at = await locate(place);
  if (!at) return report(false, name, `no ${place}`);
  await placeTogether(sc.papa, at, 'overworld', { water: op.op === 'fish' });
  await rcon.run([`clear ${Name}`, ...give.map((g) => `give ${Name} ${g}`)]);
  await wait(2500);
  sc.body.send(op);
  // -v: where she is every 10 s, as Papa sees her (an order that never ends shows where it hangs).
  const trace = process.argv.includes('-v') ? setInterval(() => {
    const her = sc.papa.players[Name] && sc.papa.players[Name].entity;
    console.log(`      (${name}: she is at ${her ? her.position.floored() : '?'})`);
  }, 10000) : null;
  const ev = await sc.body.next((e) => done.includes(e.ev) || e.ev.startsWith('cant'), ms);
  if (trace) clearInterval(trace);
  report(Boolean(ev) && !ev.ev.startsWith('cant') && (ev.count === undefined || ev.count > 0), name, ev ? detail(ev) : `nothing in ${ms / 1000} s`);
  sc.body.send({ op: 'stay' });
  await wait(2000);
}

async function orders() {
  const sc = await scene();
  const pick = process.env.WILD_ORDERS ? process.env.WILD_ORDERS.split(',') : null; // wood,stone,coal,fish
  const orderIn = (place, give, op, done, ms, name, detail) =>
    (!pick || pick.some((p) => name.startsWith(p)) ? orderInto(sc, place, give, op, done, ms, name, detail) : null);
  await orderIn('forest', ['iron_axe'], { op: 'collect', player: 'Papa', resource: 'wood', count: 12 }, ['collected'], 180000,
    'wood in a forest', (e) => `${e.ev} ${e.count ?? ''} ${e.why ?? ''} ${e.delivered === undefined ? '' : `delivered ${e.delivered}`}`);
  await orderIn('meadow', ['iron_pickaxe'], { op: 'collect', player: 'Papa', resource: 'stone', count: 12 }, ['collected'], 180000,
    'stone on a mountain', (e) => `${e.ev} ${e.count ?? ''} ${e.why ?? ''} ${e.delivered === undefined ? '' : `delivered ${e.delivered}`}`);
  await orderIn('jagged_peaks', ['iron_pickaxe'], { op: 'collect', player: 'Papa', resource: 'coal', count: 4 }, ['collected'], 180000,
    'coal on a peak', (e) => `${e.ev} ${e.count ?? ''} ${e.why ?? ''} ${e.delivered === undefined ? '' : `delivered ${e.delivered}`}`);
  await orderIn('river', ['fishing_rod'], { op: 'fish', player: 'Papa' }, ['caught'], 120000,
    'fish in a river', (e) => `${e.ev} ${e.kind ?? e.why ?? ''}`);
  await end(sc, 'orders');
}

// Coming from afar across a dark forest (huge trees, roots, a canopy she can't see through).
async function come() {
  const sc = await scene();
  const at = await locate('dark_forest');
  await placeTogether(sc.papa, at);
  await rcon.run([`execute at Papa run spreadplayers ~45 ~10 0 6 false Papa`]);
  await wait(3000);
  sc.body.send({ op: 'come', player: 'Papa' });
  const ev = await sc.body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach', 90000);
  const her = sc.papa.players[Name] && sc.papa.players[Name].entity;
  const d = her ? her.position.distanceTo(sc.papa.entity.position) : NaN;
  report(Boolean(ev && ev.ev === 'arrived') && d < 5, 'come across a dark forest', `${ev ? `${ev.ev} ${ev.why ?? ''}` : 'no answer'}; ${d.toFixed(1)} blocks from Papa`);
  await end(sc, 'come');
}

// ---- The Nether ----------------------------------------------------------------------------------

const NetherBiomes = process.env.WILD_NETHER ? process.env.WILD_NETHER.split(',') : ['nether_wastes', 'crimson_forest', 'warped_forest', 'soul_sand_valley', 'basalt_deltas'];

// Following through each Nether biome, monsters on (Papa in creative so he lives; she has a sword and armour).
async function nether() {
  const sc = await scene({ difficulty: 'normal', papaMode: 'creative' });
  for (const biome of NetherBiomes) {
    const at = await locate(biome, 'the_nether');
    if (!at) { console.log(`      (no ${biome} found)`); continue; }
    try {
      await rcon.run([`clear ${Name}`, `give ${Name} iron_sword`, `give ${Name} iron_chestplate`, `give ${Name} golden_helmet`,
        `give ${Name} cooked_beef 16`, `effect clear ${Name}`]);
      await placeTogether(sc.papa, at, 'the_nether');
      sc.body.send({ op: 'follow', player: 'Papa' });
      await wait(1000);
      const corrections = sc.body.corrections;
      const { deaths, hurts } = sc;
      sc.lost = [];
      const s = await walk(sc, 4, 18);
      sc.body.send({ op: 'stay' });
      const fixes = sc.body.corrections - corrections;
      report(s.travelled >= 25 && s.final < 6 && s.stuck === 0 && sc.lost.length === 0 && sc.deaths === deaths && fixes === 0,
        `nether: ${biome}`, `Papa walked ${s.travelled.toFixed(0)} blocks (${s.legsDone}/4 legs); at most ${s.worst.toFixed(1)} behind, ` +
        `${s.final.toFixed(1)} at the end; stuck ${s.stuck}×, lost ${JSON.stringify(sc.lost)}, ${sc.hurts - hurts} hurts, ` +
        `${sc.deaths - deaths} deaths, ${fixes} corrections`);
      if (sc.deaths > deaths) await rcon.run([`tp ${Name} Papa`]);
    } catch (err) {
      report(false, `nether: ${biome}`, `threw: ${err.message}`);
    }
    await wait(1500);
  }
  await end(sc, 'nether');
}

// A portal: Papa walks through one in front of her while she follows. Does she go through after him, and come
// back through the Nether's side when he does?
async function portal() {
  const sc = await scene();
  const at = await locate('meadow');
  await placeTogether(sc.papa, at);
  const p = sc.papa.entity.position.floored();
  // A standing portal 4 blocks east of Papa, facing him (x axis frame along z): obsidian, then fire inside lights it.
  const x = p.x + 4;
  const y = p.y;
  await rcon.run([`fill ${x - 2} ${y} ${p.z - 3} ${x + 2} ${y + 5} ${p.z + 3} air`, `fill ${x - 2} ${y - 1} ${p.z - 3} ${x + 2} ${y - 1} ${p.z + 3} stone`,
    `fill ${x} ${y - 1} ${p.z - 2} ${x} ${y + 3} ${p.z + 1} obsidian`, `fill ${x} ${y} ${p.z - 1} ${x} ${y + 2} ${p.z} air`,
    `setblock ${x} ${y} ${p.z - 1} fire`]);
  await wait(1500);
  const [lit] = await rcon.run([`execute if block ${x} ${y + 1} ${p.z - 1} minecraft:nether_portal`]);
  if (!/passed/i.test(lit)) {
    report(false, 'portal', `the portal didn't light (${lit.trim()})`);
    return end(sc, 'portal');
  }
  sc.body.send({ op: 'follow', player: 'Papa' });
  await wait(2000);
  const { goals } = req('mineflayer-pathfinder');
  // Papa walks into the portal and stands in it until it takes him.
  sc.papa.pathfinder.setGoal(new goals.GoalBlock(x, y, p.z - 1));
  const crossed = await new Promise((resolve) => {
    const timer = setTimeout(() => resolve(false), 30000);
    sc.papa.once('respawn', () => { clearTimeout(timer); resolve(true); });
  });
  await wait(15000); // her turn
  sc.body.send({ op: 'where', player: 'Papa' });
  const pos = await sc.body.next((e) => e.ev === 'position', 4000);
  report(crossed && pos && /nether/.test(pos.dimension), 'portal: she follows him through',
    `Papa ${crossed ? 'went through' : 'never went through'}; she is in ${pos ? pos.dimension : '?'}; lost ${JSON.stringify(sc.lost)}`);
  await end(sc, 'portal');
}

(async () => {
  const all = { follow, village, orders, come, nether, portal };
  const [level] = await rcon.run(['locate biome minecraft:plains']);
  if (!/nearest/i.test(level)) {
    console.log('This needs the server on a generated world (level-type=minecraft:normal): see README, « The wild ».');
    process.exit(1);
  }
  for (const [name, run] of Object.entries(all)) {
    if (only.length > 0 && !only.includes(name)) continue;
    try {
      await run();
    } catch (err) {
      report(false, name, `threw: ${err.stack}`);
    }
  }
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} wild checks passed${lagMs ? ` (her lag: ${lagMs[0]} ms + up to ${lagMs[1]})` : ''}`);
  process.exit(failed.length === 0 ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
