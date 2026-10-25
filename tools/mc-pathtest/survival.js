// Her survival, at night on normal difficulty, with stand-ins for Papa and a stranger: guarding against zombies and a
// skeleton, a creeper shot from range, an enderman left alone, backing off when badly hurt, running from a player who
// hits her, defending Papa from one, noticing who hurt Papa, and coming up from deep water. Each runs in its own
// body session, so nothing one leaves behind (a held order, a grudge) colours the next.
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn } = require('./players');

const S = { x: 60, z: -40 }; // the arena: open flat ground, far from the other tests
const sat = (dx, dy, dz) => `${S.x + dx} ${-60 + dy} ${S.z + dz}`;
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(16)} ${detail}`);
}

const near = (r) => `x=${S.x},y=-60,z=${S.z},distance=..${r}`;

async function arena() {
  await rcon.run([`forceload add ${S.x - 32} ${S.z - 32} ${S.x + 32} ${S.z + 32}`,
    `fill ${sat(-24, -1, -24)} ${sat(24, -1, 24)} grass_block`, `fill ${sat(-24, 0, -24)} ${sat(24, 8, 24)} air`,
    `kill @e[type=!player,${near(40)}]`,
    // Deep water for the drowning test, in a corner.
    `fill ${sat(16, -8, 16)} ${sat(22, -1, 22)} water`]);
  await wait(1000);
  await rcon.run([`kill @e[type=item,${near(40)}]`]);
}

// A fresh body, Papa (and a stranger when asked) by her side, the arena reset; night and normal difficulty.
async function scene({ stranger = false, family = true, kit = true } = {}) {
  await arena();
  const body = startBody();
  await spawned(body);
  const papa = await standIn('Papa');
  const other = stranger ? await standIn('Stranger') : null;
  await rcon.run([`clear ${Name}`, `effect clear ${Name}`, `tp ${Name} ${S.x + 0.5} -60 ${S.z + 0.5}`,
    // Full health whatever the last scene left: she logs in where she quit, and a fall from where a cleared ledge stood
    // cost her a third of it.
    `effect give ${Name} instant_health 1 5 true`,
    `tp Papa ${S.x + 2.5} -60 ${S.z + 0.5}`, ...(other ? [`tp Stranger ${S.x - 4.5} -60 ${S.z + 0.5}`] : []),
    // Resistance III, not V: an immune Papa takes no damage, so nobody (her included) sees him hurt.
    `effect give Papa resistance 600 2 true`, `effect give Papa regeneration 600 2 true`, 'gamerule spawn_mobs false',
    'gamerule spawn_monsters false',
    ...(kit ? [`give ${Name} iron_sword 1`, `give ${Name} bow 1`, `give ${Name} arrow 32`, `give ${Name} cooked_beef 16`,
      `give ${Name} iron_chestplate 1`] : []),
    'gamemode survival Papa', 'difficulty normal', 'time set 18000']);
  if (family) body.send({ op: 'family', names: ['Papa'] });
  await wait(2500);
  return { body, papa, other };
}

async function end({ body, papa, other }, name) {
  await rcon.run(['difficulty peaceful', 'time set day', `kill @e[type=!player,type=!item,${near(40)}]`]);
  for (const t of troubles(body)) console.log(`      ${name} log:`, t.slice(0, 200));
  await papa.leave();
  if (other) await other.leave();
  await body.quit();
}

async function guardZombies() {
  const sc = await scene();
  sc.body.send({ op: 'guard', player: 'Papa' });
  await sc.body.next((e) => e.ev === 'moving', 5000);
  await rcon.run([0, 1, 2].map((i) => `summon zombie ${S.x + 8 + i} -60 ${S.z - 4 + 3 * i} {PersistenceRequired:1b,Tags:["test"]}`));
  const started = Date.now();
  let left = 3;
  while (Date.now() < started + 45000 && left > 0) {
    await wait(2000);
    left = await q.count(`@e[type=zombie,tag=test]`);
  }
  const health = await q.entityNumber(Name, 'Health');
  const kills = sc.body.events.filter((e) => e.ev === 'killed').length;
  report(left === 0 && health > 0, 'guard: zombies', `${3 - left}/3 dead in ${((Date.now() - started) / 1000).toFixed(0)} s (${kills} reported as hers); her health ${health}`);
  await end(sc, 'zombies');
}

async function guardSkeleton() {
  const sc = await scene();
  sc.body.send({ op: 'guard', player: 'Papa' });
  await wait(1000);
  await rcon.run([`summon skeleton ${S.x + 9} -60 ${S.z + 6} {PersistenceRequired:1b}`]);
  const started = Date.now();
  let left = 1;
  while (Date.now() < started + 40000 && left > 0) {
    await wait(2000);
    left = await q.count(`@e[type=skeleton,${near(30)}]`);
  }
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && health > 0, 'guard: skeleton', `${left === 0 ? 'dead' : 'still standing'} after ${((Date.now() - started) / 1000).toFixed(0)} s; her health ${health}`);
  await end(sc, 'skeleton');
}

async function creeper() {
  const sc = await scene();
  sc.body.send({ op: 'guard', player: 'Papa' });
  await wait(1000);
  // 15 from Papa: about as far as she can see one coming over flat ground (CreeperSight 16).
  await rcon.run([`summon creeper ${S.x + 17.5} -60 ${S.z + 0.5} {PersistenceRequired:1b}`]);
  const started = Date.now();
  let left = 1;
  while (Date.now() < started + 40000 && left > 0) {
    await wait(1000);
    left = await q.count(`@e[type=creeper,${near(30)}]`);
  }
  // Shot dead it leaves her a kill; blown up, it leaves a crater.
  const [crater] = await rcon.run([`execute store result score #c pathtest run fill ${sat(-12, -2, -12)} ${sat(20, -1, 12)} grass_block replace air`]);
  const holes = Number((/(\d+)/.exec(crater) || [0, 0])[1]);
  const warned = sc.body.events.some((e) => e.ev === 'creeper');
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && holes === 0 && health > 0, 'creeper', `${left === 0 ? (holes === 0 ? 'shot dead' : `blew up (${holes} blocks of crater)`) : 'still around'}; warned: ${warned}; her health ${health}`);
  await end(sc, 'creeper');
}

// A creeper walks up to her while she keeps a farm (busy, not guarding): she deals with it as when guarding (shot from
// range with her bow, or kept out of its reach), and goes back to the farm after. Papa is out of the way, so the
// creeper is hers alone.
async function creeperBusy() {
  const sc = await scene();
  await rcon.run([`fill ${sat(-4, -1, -4)} ${sat(0, -1, 0)} farmland[moisture=7]`, `setblock ${sat(-2, -1, -2)} water`,
    `fill ${sat(-4, 0, -4)} ${sat(0, 0, 0)} wheat[age=2]`, `setblock ${sat(-2, 0, -2)} air`, `setblock ${sat(2, 0, -2)} chest[facing=west]`,
    `tp ${Name} ${S.x + 1.5} -60 ${S.z + 0.5}`, `tp Papa ${S.x - 20.5} -60 ${S.z - 20.5}`]);
  await wait(1500);
  sc.body.send({ op: 'farm', player: 'Papa' });
  const going = await sc.body.next((e) => e.ev === 'farming' || e.ev === 'cantFarm', 8000);
  await wait(2000);
  await rcon.run([`summon creeper ${S.x + 10.5} -60 ${S.z + 0.5} {PersistenceRequired:1b}`]);
  const started = Date.now();
  const trace = process.argv.includes('-v') ? setInterval(() => {
    sc.body.send({ op: 'where', player: 'Papa' });
    sc.body.next((e) => e.ev === 'position', 2000).then((p) => p && console.log(`      t+${((Date.now() - started) / 1000).toFixed(0)} s: she is at ${p.x - S.x} ${p.z - S.z} from the arena`));
  }, 3000) : null;
  let left = 1;
  while (Date.now() < started + 40000 && left > 0) {
    await wait(1000);
    left = await q.count(`@e[type=creeper,${near(30)}]`);
  }
  const [crater] = await rcon.run([`execute store result score #c pathtest run fill ${sat(-12, -2, -12)} ${sat(20, -1, 12)} grass_block replace air`]);
  const holes = Number((/(\d+)/.exec(crater) || [0, 0])[1]);
  const health = await q.entityNumber(Name, 'Health');
  await wait(20000); // back to the farm: its next round (every 15 s) walks her back to it
  if (trace) clearInterval(trace);
  sc.body.send({ op: 'where', player: 'Papa' });
  const pos = await sc.body.next((e) => e.ev === 'position', 4000);
  const x = pos ? pos.x : NaN;
  const z = pos ? pos.z : NaN;
  const back = Math.hypot(x - (S.x - 2), z - (S.z - 2)) < 8;
  report(Boolean(going && going.ev === 'farming') && left === 0 && holes === 0 && health >= 14 && back, 'creeper while busy',
    `${going ? going.ev : 'no answer'}; ${left === 0 ? (holes === 0 ? 'shot dead' : `blew up (${holes} blocks of crater)`) : 'still around'}; ` +
    `her health ${health}; ${back ? 'back at the farm' : `not back at the farm (at ${x.toFixed(0)} ${z.toFixed(0)})`}`);
  await end(sc, 'creeper while busy');
}

// Monsters she can't see, shut in chambers under the arena's floor (the flat world's dirt, on its bedrock): a creeper
// 6 blocks off and a zombie 6 the other way, with no AI so they stay put. Guarding Papa (`guard`) or free (her own
// creeper watch), she leaves both alone: not an arrow into the ground, not a swing at the floor, and no warning.
// Before, she shot at the creeper through the grass.
async function buried(guarding) {
  const sc = await scene();
  await rcon.run([`fill ${sat(5, -3, -1)} ${sat(7, -2, 1)} air`, `fill ${sat(-7, -3, -1)} ${sat(-5, -2, 1)} air`,
    `summon creeper ${S.x + 6.5} -63 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["buried"]}`,
    `summon zombie ${S.x - 5.5} -63 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["buried"]}`]);
  if (guarding) {
    sc.body.send({ op: 'guard', player: 'Papa' });
    await sc.body.next((e) => e.ev === 'moving', 5000);
  }
  const from = sc.body.events.length;
  await wait(15000);
  const arrows = await q.carried(Name, 'arrow');
  const flying = await q.count(`@e[type=arrow,${near(30)}]`);
  const creeperHealth = await q.entityNumber('@e[type=creeper,tag=buried,limit=1]', 'Health');
  const zombieHealth = await q.entityNumber('@e[type=zombie,tag=buried,limit=1]', 'Health');
  const warned = sc.body.since(from, (e) => e.ev === 'creeper').length;
  report(arrows === 32 && flying === 0 && creeperHealth === 20 && zombieHealth === 20 && warned === 0,
    guarding ? 'buried, guarding' : 'buried, free',
    `arrows on her ${arrows}/32, in the world ${flying}; creeper health ${creeperHealth}, zombie ${zombieHealth} (20 untouched); ` +
    `creeper warnings ${warned}`);
  await rcon.run([`kill @e[tag=buried]`, `fill ${sat(-7, -3, -1)} ${sat(7, -2, 1)} dirt`]);
  await end(sc, 'buried');
}

async function enderman() {
  const sc = await scene();
  sc.body.send({ op: 'guard', player: 'Papa' });
  await wait(1000);
  await rcon.run([`summon enderman ${S.x + 5} -60 ${S.z + 3} {PersistenceRequired:1b,Tags:["test"]}`]);
  await wait(15000);
  const health = await q.entityNumber('@e[type=enderman,tag=test,limit=1]', 'Health');
  const alive = await q.count('@e[type=enderman,tag=test]');
  report(alive === 1 && health === 40, 'enderman', `alive: ${alive === 1}; its health ${health}/40 after 15 s of her guarding next to it`);
  await end(sc, 'enderman');
}

async function retreat() {
  const sc = await scene();
  sc.body.send({ op: 'guard', player: 'Papa' });
  await wait(1000);
  await rcon.run([`summon zombie ${S.x + 6} -60 ${S.z + 0.5} {PersistenceRequired:1b}`, `summon zombie ${S.x + 7} -60 ${S.z + 2.5} {PersistenceRequired:1b}`]);
  await wait(1500);
  await rcon.run([`damage ${Name} 15 minecraft:generic`]);
  const ev = await sc.body.next((e) => e.ev === 'retreat', 10000);
  await wait(12000);
  const health = await q.entityNumber(Name, 'Health');
  report(Boolean(ev) && health > 0, 'retreat', `${ev ? 'backed off' : 'never backed off'} when hurt; alive after: ${health > 0} (health ${health})`);
  await end(sc, 'retreat');
}

async function fleePlayer() {
  const sc = await scene({ stranger: true });
  const before = await q.entityData(Name, 'Pos');
  sc.body.send({ op: 'flee', player: 'Stranger' });
  const ev = await sc.body.next((e) => e.ev === 'fleeing' || e.ev === 'safe' || e.ev === 'cornered', 5000);
  await wait(10000);
  const syncs = sc.papa.players[Name] && sc.papa.players[Name].entity;
  const them = sc.papa.players.Stranger && sc.papa.players.Stranger.entity;
  const distance = syncs && them ? syncs.position.distanceTo(them.position) : NaN;
  report(Boolean(ev) && distance >= 12, 'flee a player', `${ev ? ev.ev : 'no answer'}; ${Number.isNaN(distance) ? 'out of Papa\'s sight' : `${distance.toFixed(0)} blocks from them`} 10 s later (from ${before.slice(0, 40)})`);
  await end(sc, 'flee');
}

async function defendPapa() {
  const sc = await scene({ stranger: true });
  const syncs = () => sc.other.players[Name] && sc.other.players[Name].entity;
  // The stranger hits Papa (from beside him: a hit from further than reach is refused): she should notice
  // (familyHurt), and fight back when told to (C# sends defend).
  await rcon.run([`tp Stranger ${S.x + 4} -60 ${S.z + 0.5} 90 0`]);
  await wait(1500);
  const papaEntity = sc.other.players.Papa && sc.other.players.Papa.entity;
  if (papaEntity) {
    await sc.other.lookAt(papaEntity.position.offset(0, 1.6, 0), true);
    sc.other.attack(papaEntity);
  }
  const hurt = await sc.body.next((e) => e.ev === 'familyHurt', 5000);
  const before = await q.entityNumber('Stranger', 'Health');
  let died = false;
  sc.other.once('death', () => { died = true; });
  sc.body.send({ op: 'defend', player: 'Stranger', victim: 'Papa', swat: false });
  const going = await sc.body.next((e) => e.ev === 'defending' || e.ev === 'cantDefend', 5000);
  await wait(6000);
  // A fight is to the end: dead and respawned, he's back at full health.
  const after = died ? 0 : await q.entityNumber('Stranger', 'Health');
  report(Boolean(hurt) && hurt.by === 'Stranger', 'family hurt', hurt ? `noticed: ${hurt.by} hurt ${hurt.victim}` : `never noticed (Papa visible to the stranger: ${Boolean(papaEntity)}, her: ${Boolean(syncs())})`);
  report(Boolean(going) && after < before, 'defend Papa', `${going ? going.ev : 'no answer'}; the stranger's health ${before} -> ${died ? 'dead' : after}`);
  await end(sc, 'defend');
}

// A monster bites someone near her who isn't family: she goes for it. The bite is the server's own (/damage … by the
// zombie: the same damage_event a real bite sends), so it lands on the stranger and nobody else; the zombie has no AI
// and stays put, so the fight is hers to start.
async function bite(victim, at) {
  await rcon.run([`summon zombie ${at} {NoAI:1b,PersistenceRequired:1b,Tags:["bite"]}`, `effect give ${victim} resistance 600 2 true`]);
  await wait(1000);
  await rcon.run([`damage ${victim} 2 minecraft:mob_attack by @e[type=zombie,tag=bite,limit=1]`]);
}
const biterLeft = () => q.count(`@e[type=zombie,tag=bite,${near(40)}]`);

async function defendStranger() {
  const sc = await scene({ stranger: true });
  await rcon.run([`tp Stranger ${S.x - 6.5} -60 ${S.z + 0.5}`]);
  await bite('Stranger', sat(-8, 0, 0));
  let left = 1;
  for (const until = Date.now() + 20000; Date.now() < until && left > 0; await wait(1000)) left = await biterLeft();
  report(left === 0, 'defend a stranger', left === 0 ? 'killed the zombie that bit the stranger' : 'left the zombie alone');
  await end(sc, 'defend a stranger');
}

// The same bite while she's harvesting a field for Papa: she leaves the field for the fight, then goes back, finishes
// it and hands everything over, what she cut before the fight included. `noBite`: the harvest for Papa alone, handed
// over with seeds left from the replanting (a stack partly used: the case a one-by-one toss broke).
async function defendMidHarvest(noBite = false) {
  const sc = await scene({ stranger: true });
  await rcon.run([`fill ${sat(4, -1, -2)} ${sat(8, -1, 2)} farmland[moisture=7]`, `fill ${sat(4, 0, -2)} ${sat(8, 0, 2)} wheat[age=7]`,
    `setblock ${sat(6, -1, 0)} water`, `setblock ${sat(6, 0, 0)} air`, `tp Stranger ${S.x - 1.5} -60 ${S.z + 5.5}`]);
  await wait(1500);
  sc.body.send({ op: 'harvest', player: 'Papa', crop: 'wheat' });
  const started = await sc.body.next((e) => e.ev === 'harvesting' || e.ev === 'cantHarvest', 8000);
  await wait(3000);
  if (!noBite) await bite('Stranger', sat(-3, 0, 5)); // within her reach (16) wherever she is in the field
  else await rcon.run([`summon zombie ${sat(-30, 0, 30)} {NoAI:1b,PersistenceRequired:1b,Tags:["bite"]}`, `kill @e[tag=bite]`]);
  let left = 1;
  for (const until = Date.now() + 20000; Date.now() < until && left > 0; await wait(1000)) left = await biterLeft();
  const done = await sc.body.next((e) => e.ev === 'harvested', 90000);
  await wait(4000); // thrown items wait 2 s before anyone can pick them up
  // Handed over: none left on her, and Papa has it, in his pockets or at his feet (she throws from 2 blocks off, and a
  // stand-in doesn't step forward to pick it up as a player does).
  const herWheat = await q.carried(Name, 'wheat');
  const papaWheat = await q.carried('Papa', 'wheat')
    + await q.count('@e[type=item,nbt={Item:{id:"minecraft:wheat"}},' + `x=${S.x + 2.5},y=-60,z=${S.z + 0.5},distance=..4]`);
  let stalks = 0;
  for (let dx = 4; dx <= 8; dx++)
    for (let dz = -2; dz <= 2; dz++) {
      const [a] = await rcon.run([`execute if block ${sat(dx, 0, dz)} minecraft:wheat[age=7]`]);
      if (/passed/i.test(a)) stalks++;
    }
  report(Boolean(started && started.ev === 'harvesting') && (noBite || left === 0) && Boolean(done) && done.count > 0 && done.delivered
    && stalks === 0 && herWheat === 0 && papaWheat > 0,
    noBite ? 'harvest for Papa' : 'defend mid-harvest', `${started ? started.ev : 'no start'}; ${noBite ? 'no bite' : `zombie ${left === 0 ? 'killed' : 'left alone'}`}; ` +
    `${done ? `harvested ${done.count}, delivered ${done.delivered}` : 'never finished the harvest'}; ripe stalks left ${stalks}; wheat left on her ${herWheat}, with Papa ${papaWheat > 0 ? 'yes' : 'no'}`);
  await end(sc, 'defend mid-harvest');
}

async function drowning() {
  const sc = await scene({ kit: false });
  await rcon.run([`tp ${Name} ${S.x + 19.5} -67.5 ${S.z + 19.5}`]);
  const started = Date.now();
  await wait(20000);
  const y = await q.entityNumber(Name, 'Pos[1]');
  const air = await q.entityNumber(Name, 'Air');
  const health = await q.entityNumber(Name, 'Health');
  report(health >= 18 && y > -62, 'deep water', `20 s after dropping her at the bottom of 7 blocks of water: y ${y.toFixed(1)}, air ${air}, health ${health}`);
  void started;
  await end(sc, 'drowning');
}

// A fight on the lip of a ravine with lava at the bottom: she guards Papa on a ledge 7 high, and zombies with a
// knockback stick (the push without a sword's damage) come at her from the land side, so a hit taken at the lip throws her
// in. She must never go over.
async function ravine() {
  const sc = await scene();
  const top = -53; // where feet stand on the ledge (its blocks up to y -54)
  // Out of the way first: built round her, she suffocated in the stone before the tp and started the fight half dead.
  await rcon.run([`tp ${Name} ${S.x + 15.5} -60 ${S.z + 0.5}`, `tp Papa ${S.x + 15.5} -60 ${S.z + 3.5}`, `fill ${sat(-8, 0, -8)} ${sat(1, 6, 8)} stone`, `fill ${sat(2, 0, -8)} ${sat(4, 0, 8)} lava`,
    `fill ${sat(5, 0, -8)} ${sat(6, 6, 8)} stone`,
    `tp ${Name} ${S.x + 1.5} ${top} ${S.z + 0.5}`, `tp Papa ${S.x - 1.5} ${top} ${S.z + 4.5}`]);
  await wait(1500);
  sc.body.send({ op: 'guard', player: 'Papa' });
  await sc.body.next((e) => e.ev === 'moving', 5000);
  const sword = '{mainhand:{id:"minecraft:stick",count:1,components:{"minecraft:enchantments":{"minecraft:knockback":2}}}}';
  await rcon.run([-1, 1].map((dz) => `summon zombie ${S.x - 4.5} ${top} ${S.z + 0.5 + dz} {PersistenceRequired:1b,Tags:["test"],equipment:${sword}}`));
  const started = Date.now();
  let lowest = top;
  let lowestAt = '';
  let left = 2;
  while (Date.now() < started + 40000 && left > 0) {
    await wait(500);
    const y = await q.entityNumber(Name, 'Pos[1]');
    if (Number.isFinite(y) && y < lowest) { lowest = y; lowestAt = ` at t+${((Date.now() - started) / 1000).toFixed(1)} s, ${(await q.entityData(Name, 'Pos')).replace(/d/g, '')}`; }
    left = await q.count(`@e[type=zombie,tag=test,x=${S.x - 12},y=${top - 1},z=${S.z - 12},dx=24,dy=6,dz=24]`); // on the ledge: one knocked off it is out of the fight, and she mustn't jump after it
  }
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && health > 0 && lowest > top - 0.5, 'ravine', `${2 - left}/2 dead or off the ledge in ${((Date.now() - started) / 1000).toFixed(0)} s; ` +
    `lowest she stood ${lowest.toFixed(1)}${lowestAt} (the ledge is ${top}); her health ${health}`);
  await end(sc, 'ravine');
}

// A zombie right beside Papa: a sword's sweep would land on him too, so she hits it from a jump (a critical hit never
// sweeps). Papa has no resistance here, so any hit on him shows.
async function beside() {
  const sc = await scene();
  await rcon.run(['effect clear Papa', `summon zombie ${S.x + 3.5} -60 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["test"]}`]);
  let papaHurt = 0;
  sc.papa.on('entityHurt', (e) => { if (e === sc.papa.entity) papaHurt++; });
  sc.body.send({ op: 'guard', player: 'Papa' });
  const started = Date.now();
  let left = 1;
  for (const until = Date.now() + 20000; Date.now() < until && left > 0; await wait(500)) left = await q.count('@e[type=zombie,tag=test]');
  report(papaHurt === 0 && left === 0, 'beside Papa', `Papa hit ${papaHurt} times; zombie ${left === 0
    ? `killed beside him in ${((Date.now() - started) / 1000).toFixed(0)} s` : 'still standing after 20 s'}`);
  await end(sc, 'beside');
}

// ---- Defending herself ----------------------------------------------------------------------------------------

// Where she is, by her own report (Papa asks), or null.
async function whereIs(sc) {
  sc.body.send({ op: 'where', player: 'Papa' });
  const p = await sc.body.next((e) => e.ev === 'position', 4000);
  return p ? { x: p.x, z: p.z } : null;
}

// Waits up to `ms` for no entity to match `selector`; how many are left.
async function until(selector, ms) {
  let left = await q.count(selector);
  for (const end = Date.now() + ms; Date.now() < end && left > 0; await wait(1000)) left = await q.count(selector);
  return left;
}

// The farm of creeperBusy, her keeping it for Papa, who's far off and in creative (monsters leave him alone: whatever
// comes is hers).
async function farmScene() {
  const sc = await scene();
  await rcon.run([`fill ${sat(-4, -1, -4)} ${sat(0, -1, 0)} farmland[moisture=7]`, `setblock ${sat(-2, -1, -2)} water`,
    `fill ${sat(-4, 0, -4)} ${sat(0, 0, 0)} wheat[age=2]`, `setblock ${sat(-2, 0, -2)} air`, `setblock ${sat(2, 0, -2)} chest[facing=west]`,
    `tp ${Name} ${S.x + 1.5} -60 ${S.z + 0.5}`, `tp Papa ${S.x - 20.5} -60 ${S.z - 20.5}`, 'gamemode creative Papa']);
  await wait(1500);
  sc.body.send({ op: 'farm', player: 'Papa' });
  sc.going = await sc.body.next((e) => e.ev === 'farming' || e.ev === 'cantFarm', 8000);
  await wait(2000);
  return sc;
}
const nearFarm = (p) => Boolean(p) && Math.hypot(p.x - (S.x - 2), p.z - (S.z - 2)) < 8;

// A zombie walks up to her at her farm: she kills it, then goes back to the farm.
async function farmZombie() {
  const sc = await farmScene();
  await rcon.run([`summon zombie ${S.x + 7.5} -60 ${S.z + 0.5} {PersistenceRequired:1b,Tags:["test"]}`]);
  const left = await until('@e[type=zombie,tag=test]', 30000);
  const health = await q.entityNumber(Name, 'Health');
  await wait(25000); // the farm's next round (every 15 s) walks her back to it
  const p = await whereIs(sc);
  report(sc.going && sc.going.ev === 'farming' && left === 0 && health > 0 && nearFarm(p), 'zombie at the farm',
    `${left === 0 ? 'killed it' : 'left it alone'}; her health ${health}; ${nearFarm(p) ? 'back at the farm' : `not back (at ${p ? `${p.x - S.x} ${p.z - S.z}` : '?'})`}`);
  await end(sc, 'zombie at the farm');
}

// Following Papa (in creative: the zombie is after her alone), a zombie comes from behind: she kills it and follows on.
async function followZombie() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa']);
  sc.body.send({ op: 'follow', player: 'Papa' });
  await sc.body.next((e) => e.ev === 'moving', 5000);
  await rcon.run([`summon zombie ${S.x - 4.5} -60 ${S.z + 0.5} {PersistenceRequired:1b,Tags:["test"]}`]);
  const left = await until('@e[type=zombie,tag=test]', 30000);
  await wait(10000);
  const syncs = sc.papa.players[Name] && sc.papa.players[Name].entity;
  const d = syncs && sc.papa.entity ? syncs.position.distanceTo(sc.papa.entity.position) : NaN;
  report(left === 0 && d <= 4, 'zombie while following', `${left === 0 ? 'killed it' : 'left it alone'}; ${d.toFixed(1)} blocks from Papa after`);
  await end(sc, 'zombie while following');
}

// Waiting at a smoker for Papa's beef, a zombie comes for her: her life before the errand. She fights, and the smelt
// is dropped, the body saying so with nobody to blame (`by` empty).
async function furnaceZombie() {
  const sc = await scene({ kit: false });
  await rcon.run([`setblock ${sat(2, 0, 0)} smoker[facing=west]`, `give ${Name} iron_sword 1`, `give ${Name} beef 3`, `give ${Name} coal 2`,
    'gamemode creative Papa']);
  await wait(1000);
  sc.body.send({ op: 'smelt', player: 'Papa', what: 'food' });
  const smelting = await sc.body.next((e) => e.ev === 'smelting' || e.ev === 'cantSmelt', 15000);
  const from = sc.body.events.length;
  await rcon.run([`summon zombie ${S.x - 3.5} -60 ${S.z + 0.5} {PersistenceRequired:1b,Tags:["test"]}`]);
  const left = await until('@e[type=zombie,tag=test]', 30000);
  const dropped = sc.body.since(from, (e) => e.ev === 'dropped');
  report(Boolean(smelting && smelting.ev === 'smelting') && left === 0 && dropped.length === 1 && dropped[0].by === '' && dropped[0].player === 'Papa',
    'zombie at the smoker', `${smelting ? smelting.ev : 'no answer'}; ${left === 0 ? 'killed it' : 'left it alone'}; dropped: ${JSON.stringify(dropped)}`);
  await end(sc, 'zombie at the smoker');
}

// A skeleton standing off on a pillar 10 blocks away (no AI: it isn't coming) while she farms: no reason to leave the
// field. She stays at the farm and the skeleton is left alone.
async function farmSkeleton() {
  const sc = await farmScene();
  await rcon.run([`fill ${sat(10, 0, 0)} ${sat(10, 2, 0)} stone`,
    `summon skeleton ${S.x + 10.5} -57 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["test"]}`]);
  let away = 0;
  for (const end = Date.now() + 30000; Date.now() < end; await wait(2000)) if (!nearFarm(await whereIs(sc))) away++;
  const health = await q.entityNumber('@e[type=skeleton,tag=test,limit=1]', 'Health');
  report(away === 0 && health === 20, 'skeleton off the farm', `left the farm ${away} times in 30 s; skeleton health ${health}/20`);
  await rcon.run([`fill ${sat(10, 0, 0)} ${sat(10, 2, 0)} air`]);
  await end(sc, 'skeleton off the farm');
}

// Hurt by an iron golem: a fight she loses, so she gets away from it and never hits it.
async function golem() {
  const sc = await scene();
  await rcon.run([`summon iron_golem ${S.x + 2.5} -60 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["test"]}`]);
  await wait(1000);
  await rcon.run([`damage ${Name} 2 minecraft:mob_attack by @e[type=iron_golem,tag=test,limit=1]`]);
  await wait(8000);
  const health = await q.entityNumber('@e[type=iron_golem,tag=test,limit=1]', 'Health');
  report(health === 100, 'iron golem', `its health ${health}/100 after it hit her`);
  await end(sc, 'iron golem');
}

async function survival(only = []) {
  const all = { zombies: guardZombies, skeleton: guardSkeleton, creeper, creeperbusy: creeperBusy, buried: () => buried(true), buriedfree: () => buried(false), enderman, retreat, flee: fleePlayer, defend: defendPapa, stranger: defendStranger, midharvest: defendMidHarvest, harvestpapa: () => defendMidHarvest(true), drowning, ravine, beside, farmzombie: farmZombie, followzombie: followZombie, furnace: furnaceZombie, farmskeleton: farmSkeleton, golem };
  for (const [name, run] of Object.entries(all)) {
    if (only.length > 0 && !only.includes(name)) continue;
    try {
      await run();
    } catch (err) {
      report(false, name, `threw: ${err.message}`);
      await rcon.run(['difficulty peaceful', 'time set day']);
    }
  }
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} survival checks passed`);
  return failed.length === 0;
}

module.exports = { survival };
