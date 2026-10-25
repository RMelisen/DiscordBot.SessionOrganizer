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

// ---- Sizing a fight up: falling back, getting out -----------------------------------------------------------

// Where an entity is, by the server ([x, y, z]), or null.
async function pos(who) {
  const m = /\[(-?[\d.]+)d, (-?[\d.]+)d, (-?[\d.]+)d\]/.exec(await q.entityData(who, 'Pos'));
  return m ? [Number(m[1]), Number(m[2]), Number(m[3])] : null;
}
async function apart(a, b) {
  const [p, r] = [await pos(a), await pos(b)];
  return p && r ? Math.hypot(p[0] - r[0], p[1] - r[1], p[2] - r[2]) : NaN;
}

// Five zombies on her, bare-handed: she can't win that alone, so she falls back to Papa (10 blocks off), not away
// into the dark.
async function horde() {
  const sc = await scene({ kit: false });
  await rcon.run([`tp Papa ${S.x - 9.5} -60 ${S.z + 0.5}`]);
  await wait(1000);
  await rcon.run([0, 1, 2, 3, 4].map((i) => `summon zombie ${S.x + 3.5} -60 ${S.z - 1.5 + i} {PersistenceRequired:1b,Tags:["test"]}`));
  let best = Infinity;
  for (const end = Date.now() + 10000; Date.now() < end && best > 4; await wait(500)) best = Math.min(best, await apart(Name, 'Papa'));
  const from = sc.body.events.length;
  await rcon.run(['kill @e[type=zombie,tag=test]']);
  await wait(500);
  const health = await q.entityNumber(Name, 'Health');
  const retreat = sc.body.events.some((e) => e.ev === 'retreat');
  void from;
  report(best <= 4 && health > 0, 'horde', `closest she came to Papa ${best.toFixed(1)} blocks in 10 s; said she backed off: ${retreat}; her health ${health}`);
  await end(sc, 'horde');
}

// Hurt (4 hearts), a wooden sword, nobody around: she backs off first, and a zombie that keeps up with her (a fast one)
// gets
// fought after FallBackAlone, rather than run from for ever.
async function alone() {
  const sc = await scene({ kit: false });
  await rcon.run([`tp Papa ${S.x - 60.5} -60 ${S.z + 0.5}`, 'gamemode creative Papa', `give ${Name} wooden_sword 1`,
    `give ${Name} cooked_beef 4`, `damage ${Name} 12 minecraft:generic`]);
  await wait(1000);
  // Fast (Speed II): it keeps up with her however she backs off, so she has to turn and fight it.
  await rcon.run([`summon zombie ${S.x + 3.5} -60 ${S.z + 0.5} {PersistenceRequired:1b,Tags:["test"]}`, 'effect give @e[type=zombie,tag=test] speed 120 1 true']);
  const left = await until('@e[type=zombie,tag=test]', 60000);
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && health > 0, 'alone, hurt', `${left === 0 ? 'killed it' : 'still chased after 60 s'}; her health ${health}`);
  await end(sc, 'alone, hurt');
}

// Following Papa when a warden stands 8 blocks off (no AI: it stays put): she gets clear of it, staying by Papa on his
// far side, never hits it, and still follows him when he moves.
async function warden() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa']);
  sc.body.send({ op: 'follow', player: 'Papa' });
  await sc.body.next((e) => e.ev === 'moving', 5000);
  await rcon.run([`summon warden ${S.x + 10.5} -60 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["test"]}`]);
  await wait(10000);
  const fromWarden = await apart(Name, '@e[type=warden,tag=test,limit=1]');
  const fromPapa = await apart(Name, 'Papa');
  await rcon.run([`tp Papa ${S.x - 7.5} -60 ${S.z + 0.5}`]);
  await wait(8000);
  const after = await apart(Name, 'Papa');
  const health = await q.entityNumber('@e[type=warden,tag=test,limit=1]', 'Health');
  report(fromWarden >= 12 && fromPapa <= 6 && after <= 6 && health === 500, 'warden',
    `${fromWarden.toFixed(1)} from the warden and ${fromPapa.toFixed(1)} from Papa after 10 s; ${after.toFixed(1)} from Papa once he moved; warden health ${health}/500`);
  await end(sc, 'warden');
}

// ---- Orders during a fight -----------------------------------------------------------------------------------

// Three tough zombies (no AI, 200 health: the fight lasts) right by her. « pêche » mid-fight waits for the fight;
// « suis-moi » brings her to Papa at once, fighting as she goes, and replaces the waiting « pêche »: once the zombies
// are gone, she follows him.
async function ordered() {
  const sc = await scene();
  const tough = '{NoAI:1b,PersistenceRequired:1b,Tags:["test"],Health:200f,attributes:[{id:"minecraft:max_health",base:200.0}]}';
  await rcon.run([`fill ${sat(6, -1, -3)} ${sat(8, -1, -1)} water`, `give ${Name} fishing_rod 1`, 'gamemode creative Papa',
    `tp Papa ${S.x - 9.5} -60 ${S.z + 0.5}`,
    ...[-1, 0, 1].map((dz) => `summon zombie ${S.x + 1.7} -60 ${S.z + 0.5 + dz} ${tough}`)]);
  await wait(2500);
  const from = sc.body.events.length;
  sc.body.send({ op: 'fish', player: 'Papa' });
  await wait(4000);
  const early = sc.body.since(from, (e) => ['moving', 'fishing', 'cantFish'].includes(e.ev));
  sc.body.send({ op: 'follow', player: 'Papa' });
  let best = Infinity;
  for (const end = Date.now() + 6000; Date.now() < end && best > 4; await wait(500)) best = Math.min(best, await apart(Name, 'Papa'));
  const before = sc.body.events.length;
  await rcon.run(['kill @e[type=zombie,tag=test]']);
  const then = await sc.body.next((e) => ['moving', 'fishing'].includes(e.ev), 6000);
  const fished = sc.body.since(before, (e) => e.ev === 'fishing').length;
  const waited = early.length === 0 ? 'the fishing order waited' : `answered ${early.map((e) => e.ev).join(', ')}`;
  report(early.length === 0 && best <= 4 && Boolean(then) && then.ev === 'moving' && then.kind === 'follow' && fished === 0, 'orders mid-fight',
    `while fighting: ${waited}; « suis-moi » brought her within ${best.toFixed(1)} of Papa; ` +
    `after: ${then ? `${then.ev} ${then.kind || ''}` : 'nothing'}${fished ? ', and fished' : ''}`);
  await end(sc, 'orders mid-fight');
}

// ---- Ranged foes, the shield, fireballs ---------------------------------------------------------------------------

// Guarding Papa with a sword and a shield but no bow, three skeletons 10 blocks off: she closes in behind her shield and
// kills them, taking little from their arrows.
async function skeletonShield() {
  const sc = await scene({ kit: false });
  await rcon.run([`give ${Name} iron_sword 1`, `give ${Name} iron_chestplate 1`, `item replace entity ${Name} weapon.offhand with shield`,
    'gamemode creative Papa']);
  await wait(1500);
  sc.body.send({ op: 'guard', player: 'Papa' });
  await sc.body.next((e) => e.ev === 'moving', 5000);
  // Three, spread out: one is too easy to tell whether the shield did anything.
  await rcon.run([[10.5, -5.5], [11.5, 0.5], [10.5, 6.5]].map(([dx, dz]) => `summon skeleton ${S.x + dx} -60 ${S.z + dz} {PersistenceRequired:1b,Tags:["test"]}`));
  const started = Date.now();
  const left = await until('@e[type=skeleton,tag=test]', 40000);
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && health >= 12, 'skeleton, shield', `${left === 0 ? `dead in ${((Date.now() - started) / 1000).toFixed(0)} s` : 'still standing after 40 s'}; her health ${health}`);
  await end(sc, 'skeleton, shield');
}

// A skeleton in a boat 14 blocks off (it can shoot, not walk: off a platform it jumped down to her) shoots her (Papa in
// creative: she's its only target). She has a bow: it's shot from where she stands.
async function skeletonBow() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa',
    `summon oak_boat ${S.x + 14.5} -60 ${S.z + 0.5} {Passengers:[{id:"minecraft:skeleton",PersistenceRequired:1b,Tags:["test"]}]}`]);
  await wait(1500);
  // Its arrow, by the server (/damage … by the skeleton): a boat skeleton doesn't always shoot.
  await rcon.run([`damage ${Name} 2 minecraft:arrow by @e[type=skeleton,tag=test,limit=1]`]);
  const started = Date.now();
  const left = await until('@e[type=skeleton,tag=test]', 40000);
  const arrows = await q.carried(Name, 'arrow');
  const health = await q.entityNumber(Name, 'Health');
  report(left === 0 && arrows < 32 && health > 0, 'skeleton, bow', `${left === 0 ? `dead in ${((Date.now() - started) / 1000).toFixed(0)} s` : 'still standing after 40 s'}; ` +
    `arrows left ${arrows}/32; her health ${health}`);
  await end(sc, 'skeleton, bow');
}

// A ghast's fireball flying at her: she hits it back, and its blast never reaches her.
async function fireball() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `tp Papa ${S.x - 12.5} -60 ${S.z + 0.5}`]);
  await wait(1000);
  await rcon.run([`summon fireball ${S.x + 9.5} -58.5 ${S.z + 0.5} {Motion:[-0.6d,0.0d,0.0d],acceleration_power:0.05d,ExplosionPower:1b,Tags:["test"]}`]);
  await wait(5000);
  const health = await q.entityNumber(Name, 'Health');
  report(health >= 18, 'fireball', `her health ${health} 5 s after a fireball came at her`);
  await end(sc, 'fireball');
}

// ---- Not surrounded -------------------------------------------------------------------------------------------

// The narrowest arc (degrees) holding every direction from `from` to `points`, as seen from above.
function span(from, points) {
  const a = points.map((p) => Math.atan2(p[2] - from[2], p[0] - from[0])).map((x) => (x + 2 * Math.PI) % (2 * Math.PI)).sort((x, y) => x - y);
  let gap = 2 * Math.PI - a[a.length - 1] + a[0];
  for (let i = 1; i < a.length; i++) gap = Math.max(gap, a[i] - a[i - 1]);
  return ((2 * Math.PI - gap) * 180) / Math.PI;
}

// Four zombies pressed round her, north, east, south and west, 1.6 blocks out (all in her reach: from a looser ring
// chasing the one that hit her took her out of it by itself; no AI: they stay spread, where live ones
// bunch up by themselves; 40 health, so none goes down in the first seconds; no knockback, so her hits don't break the
// ring). The east one hits her: she gets out of the ring, so they're all on one side of her (a span under 120°). Papa is
// far off, in creative.
async function surround() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `tp Papa ${S.x - 20.5} -60 ${S.z + 0.5}`]);
  await wait(1000);
  const ring = '{NoAI:1b,PersistenceRequired:1b,Tags:["test"],Health:40f,attributes:[{id:"minecraft:max_health",base:40.0},'
    + '{id:"minecraft:knockback_resistance",base:1.0}]}';
  await rcon.run([[1.6, 0], [-1.6, 0], [0, 1.6], [0, -1.6]].map(([dx, dz]) => `summon zombie ${S.x + 0.5 + dx} -60 ${S.z + 0.5 + dz} ${ring}`));
  await wait(500);
  await rcon.run([`damage ${Name} 2 minecraft:mob_attack by @e[type=zombie,tag=test,x=${S.x + 1.5},y=-60,z=${S.z},dx=1,dy=1,dz=1,limit=1]`]);
  let narrowest = 360;
  for (const end = Date.now() + 4000; Date.now() < end && narrowest >= 120; await wait(250)) {
    const me = await pos(Name);
    // Every zombie at once: rcon gives the answers of an execute as … run one after the other.
    const all = (await rcon.run(['execute as @e[type=zombie,tag=test] run data get entity @s Pos']))[0];
    const points = [...all.matchAll(/\[(-?[\d.]+)d, (-?[\d.]+)d, (-?[\d.]+)d\]/g)].map((m) => [Number(m[1]), Number(m[2]), Number(m[3])]);
    if (me && points.length >= 3) narrowest = Math.min(narrowest, span(me, points));
  }
  // Standing zombies (no AI) she's stepped away from are no threat, and she may leave them be: only getting out counts.
  const health = await q.entityNumber(Name, 'Health');
  report(narrowest < 120 && health > 0, 'surrounded', `narrowest span of the zombies round her in 4 s: ${narrowest.toFixed(0)}°; her health ${health}`);
  await end(sc, 'surrounded');
}

// ---- Emergencies: golden apples, potions, the totem ------------------------------------------------------------

// Down to 3.5 hearts with a zombie coming and two golden apples on her: she eats one.
async function goldenApple() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `give ${Name} golden_apple 2`, `damage ${Name} 13 minecraft:generic`,
    `summon zombie ${S.x + 6.5} -60 ${S.z + 0.5} {PersistenceRequired:1b,Tags:["test"]}`]);
  let apples = 2;
  for (const end = Date.now() + 10000; Date.now() < end && apples > 1; await wait(500)) apples = await q.carried(Name, 'golden_apple');
  const health = await q.entityNumber(Name, 'Health');
  report(apples === 1 && health > 0, 'golden apple', `golden apples left ${apples}/2; her health ${health}`);
  await end(sc, 'golden apple');
}

// A totem and a shield: down to 3.5 hearts, the totem goes in her off-hand; a blow that would kill her pops it (she
// lives, and says so: `totem`); healed past 7 hearts, the shield goes back.
async function totem() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `item replace entity ${Name} weapon.offhand with shield`, `give ${Name} totem_of_undying 1`]);
  await wait(1500);
  await rcon.run([`damage ${Name} 13 minecraft:generic`]);
  let offhand = '';
  for (const end = Date.now() + 3000; Date.now() < end && !offhand.includes('totem'); await wait(250)) offhand = await q.entityData(Name, 'equipment.offhand');
  const held = offhand.includes('totem_of_undying');
  const from = sc.body.events.length;
  await rcon.run([`damage ${Name} 30 minecraft:generic`]);
  await wait(1500);
  const alive = (await q.entityNumber(Name, 'Health')) > 0 && !sc.body.since(from, (e) => e.ev === 'died').length;
  const popped = sc.body.since(from, (e) => e.ev === 'totem').length;
  await rcon.run([`effect give ${Name} regeneration 30 4 true`]);
  let back = '';
  for (const end = Date.now() + 20000; Date.now() < end && !back.includes('shield'); await wait(500)) back = await q.entityData(Name, 'equipment.offhand');
  report(held && alive && popped === 1 && back.includes('shield'), 'totem', `totem in hand at 3.5 hearts: ${held}; alive after a deadly blow: ${alive}; ` +
    `said so: ${popped === 1}; shield back once healed: ${back.includes('shield')}`);
  await end(sc, 'totem');
}

// Putting her things away keeps her emergencies: golden apples, a totem, a healing potion stay on her; the dirt goes.
async function keepKit() {
  const sc = await scene({ kit: false });
  await rcon.run([`setblock ${sat(2, 0, 0)} chest[facing=west]`, `give ${Name} golden_apple 1`, `give ${Name} totem_of_undying 1`,
    `give ${Name} potion[potion_contents={potion:"minecraft:healing"}] 1`, `give ${Name} dirt 10`]);
  await wait(1500);
  sc.body.send({ op: 'store', player: 'Papa' });
  const done = await sc.body.next((e) => e.ev === 'stored' || e.ev === 'cantStore', 20000);
  await wait(1000);
  const [x, y, z] = [S.x + 2, -60, S.z];
  const chest = { dirt: await q.inBlock(x, y, z, 'dirt'), golden_apple: await q.inBlock(x, y, z, 'golden_apple'),
    totem_of_undying: await q.inBlock(x, y, z, 'totem_of_undying'), potion: await q.inBlock(x, y, z, 'potion') };
  report(Boolean(done) && done.ev === 'stored' && chest.dirt === 10 && chest.golden_apple + chest.totem_of_undying + chest.potion === 0,
    'keeps her emergencies', `${done ? done.ev : 'no answer'}; in the chest: ${JSON.stringify(chest)}`);
  await end(sc, 'keeps her emergencies');
}

// ---- Fire and lava ----------------------------------------------------------------------------------------------

// Set on fire (a fire block at her feet) with a little pool 5 blocks off: she makes for the water and is out within
// 8 s. Papa is far off, in creative.
async function burning() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `tp Papa ${S.x - 20.5} -60 ${S.z + 0.5}`, `fill ${sat(5, -1, -1)} ${sat(6, -1, 0)} water`]);
  await wait(1500);
  await rcon.run([`setblock ${sat(0, 0, 0)} fire`]);
  await wait(1000);
  const lit = await q.entityNumber(Name, 'Fire');
  let fire = lit;
  for (const end = Date.now() + 8000; Date.now() < end && fire > 0; await wait(250)) fire = await q.entityNumber(Name, 'Fire');
  const health = await q.entityNumber(Name, 'Health');
  report(lit > 0 && fire <= 0 && health > 0, 'on fire', `${lit > 0 ? `burning (${lit} ticks)` : 'never caught fire'}; ` +
    `${fire <= 0 ? 'put out within 8 s' : `still burning after 8 s (${fire})`}; her health ${health}`);
  await rcon.run([`setblock ${sat(0, 0, 0)} air`]);
  await end(sc, 'on fire');
}

// Dropped into the edge of a lava pool 1 deep: she gets out onto the ground within 5 s, alive.
async function lava() {
  const sc = await scene();
  await rcon.run(['gamemode creative Papa', `tp Papa ${S.x - 20.5} -60 ${S.z + 0.5}`]);
  await wait(1500);
  await rcon.run([`fill ${sat(3, -1, -1)} ${sat(5, -1, 1)} lava`, `tp ${Name} ${S.x + 3.5} -61 ${S.z + 0.5}`]);
  let inLava = true;
  for (const end = Date.now() + 5000; Date.now() < end && inLava; await wait(250)) {
    const p = await pos(Name);
    if (!p) continue;
    const [a] = await rcon.run([`execute if block ${Math.floor(p[0])} ${Math.floor(p[1])} ${Math.floor(p[2])} minecraft:lava`]);
    inLava = /passed/i.test(a);
  }
  const health = await q.entityNumber(Name, 'Health');
  report(!inLava && health > 0, 'in lava', `${inLava ? 'still in the lava after 5 s' : 'out of the lava'}; her health ${health}`);
  await rcon.run([`fill ${sat(3, -1, -1)} ${sat(5, -1, 1)} grass_block`]);
  await end(sc, 'in lava');
}

async function survival(only = []) {
  const all = { zombies: guardZombies, skeleton: guardSkeleton, creeper, creeperbusy: creeperBusy, buried: () => buried(true), buriedfree: () => buried(false), enderman, retreat, flee: fleePlayer, defend: defendPapa, stranger: defendStranger, midharvest: defendMidHarvest, harvestpapa: () => defendMidHarvest(true), drowning, ravine, beside, farmzombie: farmZombie, followzombie: followZombie, furnace: furnaceZombie, farmskeleton: farmSkeleton, golem, horde, alone, warden, ordered, skeletonshield: skeletonShield, skeletonbow: skeletonBow, fireball, surround, gapple: goldenApple, totem, keepkit: keepKit, burning, lava };
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
