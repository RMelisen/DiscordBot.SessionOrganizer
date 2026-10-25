// Every action that goes through a packet Mineflayer may build the old way, checked on the server's side: eating,
// putting armour on, storing in a chest, handing things over, smelting, sleeping and getting up, fishing, getting
// into and out of a boat. The sprint and the wake-up both reached the server as something else for weeks, with
// nothing to see from outside: here, what she says she did is never the evidence.
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn } = require('./players');

// The workshop, west of spawn: a chest, a furnace and a bed by the door, a fishing pool, a boat pool.
const W = { x: -30, z: 30 };
const at = (dx, dy, dz) => `${W.x + dx} ${-60 + dy} ${W.z + dz}`;

function buildCommands() {
  return [
    `forceload add ${W.x - 24} ${W.z - 24} ${W.x + 24} ${W.z + 24}`,
    `fill ${at(-14, -1, -14)} ${at(14, -1, 14)} grass_block`, `fill ${at(-14, 0, -14)} ${at(14, 6, 14)} air`,
    `setblock ${at(0, 0, 0)} chest[facing=south]`, `setblock ${at(2, 0, 0)} furnace[facing=south]`,
    `setblock ${at(4, 0, 0)} red_bed[part=foot,facing=east]`, `setblock ${at(5, 0, 0)} red_bed[part=head,facing=east]`,
    // Fishing: 5x5, 3 deep, open sky. Boats: 7x5, 1 deep.
    `fill ${at(-10, -3, 4)} ${at(-6, -1, 8)} water`,
    `fill ${at(2, -1, 6)} ${at(8, -1, 10)} water`,
  ];
}

const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(12)} ${detail}`);
}

async function reset(body) {
  body.send({ op: 'stay' });
  await rcon.run([`clear ${Name}`, `effect clear ${Name}`, 'difficulty peaceful', `tp ${Name} ${W.x + 0.5} -60 ${W.z + 2.5}`,
    'time set day']);
  await wait(2500);
}

async function eat(body) {
  await reset(body);
  // Peaceful keeps everyone fed: hunger only bites from easy up.
  await rcon.run(['difficulty easy', `give ${Name} bread 8`, `effect give ${Name} hunger 30 200 true`]);
  let low = 20;
  const end = Date.now() + 40000;
  let bread = 8;
  while (Date.now() < end) {
    await wait(2000);
    low = Math.min(low, await q.entityNumber(Name, 'foodLevel'));
    bread = await q.carried(Name, 'bread');
    if (bread < 8) break;
  }
  await rcon.run([`effect clear ${Name} hunger`, 'difficulty peaceful']);
  report(bread < 8, 'eat', `food went down to ${low}; bread ${8 - bread} eaten (the server's count)`);
}

async function armour(body) {
  await reset(body);
  // Dropped at her feet, so she picks them up (which is when she dresses).
  await rcon.run([`summon item ${W.x + 0.5} -59.5 ${W.z + 2.5} {Item:{id:"minecraft:iron_chestplate",count:1},PickupDelay:0s}`,
    `summon item ${W.x + 0.5} -59.5 ${W.z + 2.5} {Item:{id:"minecraft:iron_boots",count:1},PickupDelay:0s}`]);
  await wait(4000);
  const worn = await q.entityData(Name, 'equipment');
  const ok = /iron_chestplate/.test(worn) && /iron_boots/.test(worn);
  report(ok, 'armour', ok ? 'wearing the chestplate and boots (server equipment)' : `server equipment: ${worn.slice(0, 160) || 'none'}`);
}

// Armour handed to her (copper, which she once never wore, and a shield), taken off and kept, not put back on by a
// pickup of anything else, put on when asked, put on again when given a piece, handed back; and full pockets.
async function armourOrders(body, papa) {
  await reset(body);
  await rcon.run([`clear ${papa.username}`, `tp ${papa.username} ${W.x + 2.5} -60 ${W.z + 2.5}`]);
  const drop = (id) => `summon item ${W.x + 0.5} -59.5 ${W.z + 2.5} {Item:{id:"minecraft:${id}",count:1},PickupDelay:0s}`;
  // Slot by slot: the server cuts `data get … equipment` short after its first slot.
  const worn = async () => (await Promise.all(['head', 'chest', 'legs', 'feet', 'offhand']
    .map((slot) => q.entityData(Name, `equipment.${slot}`)))).join(' ');
  const wears = (snbt, ...ids) => ids.every((id) => snbt.includes(`minecraft:${id}`));
  const bareOf = (snbt) => !/_(helmet|chestplate|leggings|boots)"|minecraft:shield/.test(snbt);
  const pieces = ['copper_helmet', 'iron_leggings', 'shield'];

  let from = body.events.length;
  await rcon.run(pieces.map(drop));
  await wait(4000);
  let now = await worn();
  const thanked = body.since(from, (e) => e.ev === 'armoured' && e.by === papa.username).length > 0;
  report(wears(now, ...pieces) && thanked, 'armour gift', `wearing ${pieces.join(', ')}: ${wears(now, ...pieces)}; thanked Papa: ${thanked}`);

  body.send({ op: 'undress', player: papa.username });
  let ev = await body.next((e) => e.ev === 'undressed' || e.ev === 'cantUndress', 10000);
  await wait(1000);
  now = await worn();
  const kept = (await Promise.all(pieces.map((p) => q.carried(Name, p)))).every((n) => n === 1);
  report(ev && ev.ev === 'undressed' && ev.count === 3 && bareOf(now) && kept, 'undress',
    `${ev ? JSON.stringify(ev) : 'no answer'}; bare on the server: ${bareOf(now)}; pieces in her pockets: ${kept}`);

  await rcon.run([drop('bread')]);
  await wait(3000);
  now = await worn();
  report(bareOf(now), 'stays bare', `after picking up bread, bare on the server: ${bareOf(now)}`);

  body.send({ op: 'dress', player: papa.username });
  ev = await body.next((e) => e.ev === 'dressed' || e.ev === 'cantDress', 10000);
  await wait(1000);
  now = await worn();
  report(ev && ev.ev === 'dressed' && wears(now, ...pieces), 'dress', `${ev ? JSON.stringify(ev) : 'no answer'}; wearing them again: ${wears(now, ...pieces)}`);

  body.send({ op: 'dress', player: papa.username });
  ev = await body.next((e) => e.ev === 'dressed' || e.ev === 'cantDress', 10000);
  report(ev && ev.ev === 'cantDress' && ev.why === 'worn', 'dress again', ev ? JSON.stringify(ev) : 'no answer');

  body.send({ op: 'undress', player: papa.username });
  await body.next((e) => e.ev === 'undressed' || e.ev === 'cantUndress', 10000);
  from = body.events.length;
  await rcon.run([drop('iron_boots')]);
  await wait(4000);
  now = await worn();
  report(wears(now, ...pieces, 'iron_boots'), 'regift', `given boots after « enlève »: wearing everything again: ${wears(now, ...pieces, 'iron_boots')}`);

  body.send({ op: 'give', player: papa.username, goods: 'armour' });
  ev = await body.next((e) => e.ev === 'given' || e.ev === 'cantGive', 30000);
  await wait(3000);
  now = await worn();
  const handed = (await Promise.all([...pieces, 'iron_boots'].map((p) => q.carried(papa.username, p)))).reduce((a, b) => a + b, 0);
  const lying = await q.count(`@e[type=item,x=${W.x + 2.5},y=-60,z=${W.z + 2.5},distance=..4]`);
  report(ev && ev.ev === 'given' && bareOf(now) && handed + lying >= 4, 'give back',
    `${ev ? JSON.stringify(ev) : 'no answer'}; bare: ${bareOf(now)}; Papa carries ${handed} pieces, ${lying} lying at his feet`);
  await rcon.run([`kill @e[type=item,x=${W.x + 2.5},y=-60,z=${W.z + 2.5},distance=..6]`, `clear ${papa.username}`]);

  await rcon.run([`give ${Name} iron_chestplate`]);
  await wait(1500);
  body.send({ op: 'dress', player: papa.username });
  await body.next((e) => e.ev === 'dressed' || e.ev === 'cantDress', 10000);
  await rcon.run([`give ${Name} dirt ${36 * 64}`]);
  await wait(1500);
  body.send({ op: 'undress', player: papa.username });
  ev = await body.next((e) => e.ev === 'undressed' || e.ev === 'cantUndress', 10000);
  now = await worn();
  report(ev && ev.ev === 'cantUndress' && ev.why === 'full' && wears(now, 'iron_chestplate'), 'undress full',
    `${ev ? JSON.stringify(ev) : 'no answer'}; still wearing the chestplate: ${wears(now, 'iron_chestplate')}`);
  body.send({ op: 'dress', player: papa.username }); // no longer bare, for the runs after
  await body.next((e) => e.ev === 'dressed' || e.ev === 'cantDress', 10000);
}

async function store(body) {
  await reset(body);
  await rcon.run([`data merge block ${at(0, 0, 0)} {Items:[]}`, `give ${Name} cobblestone 32`, `give ${Name} dirt 16`,
    `give ${Name} iron_sword 1`]);
  await wait(1500);
  body.send({ op: 'store', player: 'tester' });
  const ev = await body.next((e) => e.ev === 'stored' || e.ev === 'cantStore', 30000);
  await wait(1000);
  const cobble = await q.inBlock(W.x, -60, W.z, 'cobblestone');
  const dirt = await q.inBlock(W.x, -60, W.z, 'dirt');
  const sword = await q.carried(Name, 'iron_sword');
  const ok = cobble === 32 && dirt === 16 && sword === 1;
  report(ok, 'store', `${ev ? ev.ev : 'no answer'}; chest has ${cobble} cobblestone, ${dirt} dirt; she kept ${sword} sword`);
}

async function give(body, papa) {
  await reset(body);
  await rcon.run([`clear ${papa.username}`, `tp ${papa.username} ${W.x + 3.5} -60 ${W.z + 3.5}`, `give ${Name} bread 5`,
    `give ${Name} cooked_beef 3`]);
  await wait(2000);
  body.send({ op: 'give', player: papa.username, goods: 'food' });
  const ev = await body.next((e) => e.ev === 'given' || e.ev === 'cantGive', 30000);
  await wait(3000);
  // She tosses it at the player's feet: carried, or lying by them (a player who doesn't move doesn't walk over it).
  const bread = await q.carried(papa.username, 'bread');
  const beef = await q.carried(papa.username, 'cooked_beef');
  // A tossed stack can land a few blocks off: within 4 is « at his feet ».
  const lying = await q.count(`@e[type=item,x=${W.x + 3.5},y=-60,z=${W.z + 3.5},distance=..4]`);
  report(bread + beef >= 8 || lying >= 2, 'give', `${ev ? ev.ev : 'no answer'}; Papa carries ${bread} bread and ${beef} cooked beef, ${lying} stacks lying at his feet`);
  await rcon.run([`kill @e[type=item,x=${W.x + 3.5},y=-60,z=${W.z + 3.5},distance=..4]`]);
}

async function smelt(body) {
  await reset(body);
  await rcon.run([`data merge block ${at(2, 0, 0)} {Items:[]}`, `give ${Name} raw_iron 3`, `give ${Name} coal 2`]);
  await wait(1500);
  body.send({ op: 'smelt', player: 'tester', what: 'ore' });
  const ev = await body.next((e) => e.ev === 'smelted' || e.ev === 'cantSmelt', 90000);
  await wait(1500);
  const ingots = await q.carried(Name, 'iron_ingot');
  report(ingots >= 3, 'smelt', `${ev ? `${ev.ev}${ev.why ? ` (${ev.why})` : ''}` : 'no answer in 90 s'}; she carries ${ingots} iron ingots`);
}

// Night and « sleep » (she may beat the order to it on her own: autoSleepTick, unless she was told to stay today);
// « stay » gets her up. The server keeps where a sleeper lies in `sleeping_pos` (26.x; SleepingX before).
async function sleepAndWake(body) {
  await reset(body);
  await rcon.run(['time set 18000']);
  await wait(1000);
  body.send({ op: 'sleep', player: 'tester' });
  const ev = await body.next((e) => e.ev === 'sleeping' || e.ev === 'cantSleep', 25000);
  await wait(1500);
  const [lying] = await rcon.run([`data get entity ${Name} sleeping_pos`]);
  const inBed = lying.includes('data: ');
  body.send({ op: 'stay' });
  await wait(3000);
  const [after] = await rcon.run([`data get entity ${Name} sleeping_pos`]);
  const stillInBed = after.includes('data: ');
  await rcon.run(['time set day']);
  report(inBed && !stillInBed, 'sleep+wake', `${ev ? `${ev.ev}${ev.auto ? ' (on her own)' : ''}` : 'no answer'}; in bed on the server: ${inBed}; still in bed after « stay »: ${stillInBed}`);
}

async function fish(body) {
  await reset(body);
  await rcon.run([`give ${Name} fishing_rod 1`, `tp ${Name} ${W.x - 10.5} -60 ${W.z + 2.5}`]);
  await wait(2000);
  body.send({ op: 'fish', player: 'tester' });
  const going = await body.next((e) => e.ev === 'fishing' || e.ev === 'cantFish', 20000);
  const caught = going && going.ev === 'fishing' ? await body.next((e) => e.ev === 'caught', 120000) : null;
  body.send({ op: 'stay' });
  await wait(2000);
  const [fishCount] = await Promise.all([q.carried(Name, 'cod')]);
  const anyCatch = fishCount + (await q.carried(Name, 'salmon')) + (await q.carried(Name, 'tropical_fish')) + (await q.carried(Name, 'pufferfish'));
  report(Boolean(caught), 'fish', `${going ? going.ev : 'no answer'} -> ${caught ? `caught ${caught.kind}` : 'nothing in 2 minutes'}; fish on her (server): ${anyCatch}`);
}

async function boat(body, papa) {
  await reset(body);
  await rcon.run([`kill @e[type=oak_boat]`, `summon oak_boat ${W.x + 5} -60.4 ${W.z + 8}`, `tp ${papa.username} ${W.x + 5} -60 ${W.z + 5}`]);
  await wait(2000);
  const boatEntity = papa.nearestEntity((e) => e.name === 'oak_boat');
  if (boatEntity) papa.mount(boatEntity);
  await wait(1500);
  body.send({ op: 'follow', player: papa.username });
  const asked = Date.now();
  const boarded = await body.next((e) => e.ev === 'boarded', 40000);
  const boardSecs = ((Date.now() - asked) / 1000).toFixed(1);
  await wait(1500);
  const [riding] = await rcon.run([`execute as ${Name} on vehicle if entity @s[type=oak_boat]`]);
  // Papa gets out and walks off: she should get out too.
  papa._client.write('player_input', { inputs: { shift: true } });
  await wait(600);
  papa._client.write('player_input', { inputs: {} });
  await rcon.run([`tp ${papa.username} ${W.x + 0.5} -60 ${W.z + 4.5}`]);
  await wait(6000);
  const [stillRiding] = await rcon.run([`execute as ${Name} on vehicle if entity @s[type=oak_boat]`]);
  // And she walks again (her physics stays off while she rides).
  body.send({ op: 'goto', player: 'tester', x: W.x + 0.5, y: -60, z: W.z + 2.5 });
  const walked = await body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach', 20000);
  body.send({ op: 'stay' });
  const inBoat = /passed/i.test(riding);
  const out = !/passed/i.test(stillRiding);
  const walks = Boolean(walked && walked.ev === 'arrived');
  report(inBoat && out && walks && boarded && Number(boardSecs) < 10, 'boat', `${boarded ? `boarded in ${boardSecs} s` : 'never boarded'}; in the boat on the server: ${inBoat}; out after Papa left: ${out}; walks after: ${walks}`);
}

async function actions(only = []) {
  await rcon.run(buildCommands());
  const body = startBody();
  await spawned(body);
  const papa = await standIn('Papa');
  const all = { eat, armour, wardrobe: (b) => armourOrders(b, papa), store, give: (b) => give(b, papa), smelt, sleep: sleepAndWake, fish, boat: (b) => boat(b, papa) };
  for (const [name, run] of Object.entries(all)) {
    if (only.length > 0 && !only.includes(name)) continue;
    try {
      await run(body);
    } catch (err) {
      report(false, name, `threw: ${err.message}`);
    }
  }
  await papa.leave();
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
  await body.quit();
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} actions passed`);
  return failed.length === 0;
}

module.exports = { actions, buildCommands };
