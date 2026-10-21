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

async function survival(only = []) {
  const all = { zombies: guardZombies, skeleton: guardSkeleton, creeper, enderman, retreat, flee: fleePlayer, defend: defendPapa, stranger: defendStranger, midharvest: defendMidHarvest, harvestpapa: () => defendMidHarvest(true), drowning };
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
