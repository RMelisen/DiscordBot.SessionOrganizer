// What could hurt the group's world or belongings, checked on the server: keeping a herd (she kills animals),
// and what's left of the scenery around what she gathers and harvests.
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, troubles, Name } = require('./body');

const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(14)} ${detail}`);
}

// The pen: 11x11 of fence with a chest in a corner, south-west of spawn.
const P = { x: -40, z: -40 };
const pat = (dx, dy, dz) => `${P.x + dx} ${-60 + dy} ${P.z + dz}`;

function penCommands() {
  return [
    `forceload add ${P.x - 20} ${P.z - 20} ${P.x + 20} ${P.z + 20}`,
    `fill ${pat(-10, -1, -10)} ${pat(10, -1, 10)} grass_block`, `fill ${pat(-10, 0, -10)} ${pat(10, 5, 10)} air`,
    // Four sides (« hollow » on a one-high box fills it all: the first pens were solid fence).
    `fill ${pat(-5, 0, -5)} ${pat(5, 0, -5)} oak_fence`, `fill ${pat(-5, 0, 5)} ${pat(5, 0, 5)} oak_fence`,
    `fill ${pat(-5, 0, -5)} ${pat(-5, 0, 5)} oak_fence`, `fill ${pat(5, 0, -5)} ${pat(5, 0, 5)} oak_fence`,
    // The chest well inside: by the fence, cows jumped onto it and over (a chest is 0.875 high).
    `setblock ${pat(-1, 0, -1)} chest[facing=east]`,
  ];
}

const inPen = `x=${P.x - 5},y=-61,z=${P.z - 5},dx=10,dy=4,dz=10`;

async function ranch() {
  // Killing the last run's animals drops their loot: cleared once it has landed.
  // Every animal of these kinds, wherever an earlier run's left it: escaped ones would count as leaving.
  await rcon.run(['kill @e[type=cow]', 'kill @e[type=sheep]', 'kill @e[type=pig]', `kill @e[type=!player,${inPen}]`, ...penCommands()]);
  await wait(1500);
  await rcon.run([`kill @e[type=item,${inPen}]`]);
  const summons = [];
  for (let i = 0; i < 10; i++) summons.push(`summon cow ${P.x - 3 + (i % 5)} -60 ${P.z - 2 + Math.floor(i / 5)}`);
  summons.push(`summon cow ${P.x + 2} -60 ${P.z + 2} {Age:-24000}`, `summon cow ${P.x + 3} -60 ${P.z + 2} {Age:-24000}`,
    `summon cow ${P.x} -60 ${P.z + 3} {CustomName:"Marguerite"}`,
    `summon sheep ${P.x - 2} -60 ${P.z + 3}`, `summon sheep ${P.x - 1} -60 ${P.z + 3}`,
    `summon pig ${P.x + 3} -60 ${P.z - 3} {CustomName:"Hamlet"}`);
  await rcon.run(summons);
  const body = startBody();
  await spawned(body);
  await rcon.run([`clear ${Name}`, `give ${Name} wheat 24`, `give ${Name} iron_sword 1`, `tp ${Name} ${P.x + 0.5} -60 ${P.z + 0.5}`,
    `data merge block ${pat(-1, 0, -1)} {Items:[]}`]);
  await wait(3000);
  const counts = async () => ({
    cows: await q.count(`@e[type=cow,${inPen}]`),
    marguerite: await q.count(`@e[type=cow,name=Marguerite]`),
    sheep: await q.count(`@e[type=sheep,${inPen}]`),
    hamlet: await q.count(`@e[type=pig,name=Hamlet]`),
  });
  const before = await counts();
  // Babies by age, on the server: Age below 0.
  const babies = async () => {
    const [a] = await rcon.run([`execute store result score #b pathtest if entity @e[type=cow,${inPen},nbt=!{Age:0}]`]);
    return Number((/Count: (\d+)/.exec(a) || [0, 0])[1]);
  };
  const babiesBefore = await babies();
  body.send({ op: 'ranch', player: 'tester' });
  const started = await body.next((e) => e.ev === 'ranching' || e.ev === 'cantRanch', 15000);
  // Three rounds (RanchRound 15 s), and time for the last one's pickup and walk to the chest: a « stay » mid-pickup
  // leaves its drops lying, which is the order's doing, not hers.
  await wait(75000);
  body.send({ op: 'stay' });
  await wait(2000);
  const after = await counts();
  const babiesAfter = await babies();
  // She keeps up to 8 meat for herself (MeatKept): what a cull drops is in the chest or on her.
  const leather = await q.inBlock(P.x - 1, -60, P.z - 1, 'leather') + await q.carried(Name, 'leather');
  const beef = await q.inBlock(P.x - 1, -60, P.z - 1, 'beef') + await q.carried(Name, 'beef');
  const lying = await q.count(`@e[type=item,${inPen}]`);
  const wheatUsed = 24 - await q.carried(Name, 'wheat');
  if (lying > 0) {
    const [where] = await rcon.run([`execute as @e[type=item,${inPen}] run data get entity @s Pos`]);
    console.log('      lying at', where.replace(/has the following entity data: /g, ''));
  }
  const culled = before.cows - after.cows;
  await body.quit();
  const okNamed = after.marguerite === 1 && after.hamlet === 1;
  const okBabies = babiesAfter >= babiesBefore; // new calves are fine; none killed
  const okCull = culled >= 1 && culled <= 4 && after.cows >= 8;
  // Hers is the feeding: a lamb also needs the two fed sheep to find each other in time.
  const okBred = after.sheep > before.sheep || wheatUsed >= 2;
  report(Boolean(started) && started.ev === 'ranching', 'ranch start', started ? started.ev : 'no answer');
  report(okNamed, 'named animals', `Marguerite ${after.marguerite ? 'alive' : 'KILLED'}, Hamlet ${after.hamlet ? 'alive' : 'KILLED'}`);
  report(okBabies, 'babies spared', `calves ${babiesBefore} before, ${babiesAfter} after (Age != 0 on the server)`);
  report(okCull, 'culling', `cows ${before.cows} -> ${after.cows} (${culled} culled; 2 a round at most, never under 8)`);
  report(okBred, 'breeding', `sheep ${before.sheep} -> ${after.sheep}; ${wheatUsed} wheat fed`);
  report(leather + beef > 0 && lying === 0, 'drops kept', `chest and her pockets: ${beef} beef, ${leather} leather; ${lying} items left lying in the pen`);
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
}

// Breeding food from the pen's chest: four cows (under HerdMax, so two get fed), no wheat on her, 32 in the chest.
// She takes some (FeedTaken at most) and feeds two, and never says she lacks it. With the chest empty she says so.
async function ranchFeed(stocked) {
  await rcon.run(['kill @e[type=cow]', 'kill @e[type=sheep]', 'kill @e[type=pig]', `kill @e[type=!player,${inPen}]`, ...penCommands()]);
  await wait(1500);
  await rcon.run([`kill @e[type=item,${inPen}]`,
    ...[0, 1, 2, 3].map((i) => `summon cow ${P.x - 3 + i * 2} -60 ${P.z + 2}`)]);
  const body = startBody();
  await spawned(body);
  await rcon.run([`clear ${Name}`, `give ${Name} iron_sword 1`, `tp ${Name} ${P.x + 0.5} -60 ${P.z + 0.5}`,
    `data merge block ${pat(-1, 0, -1)} {Items:[${stocked ? '{Slot:0b,id:"minecraft:wheat",count:32}' : ''}]}`]);
  await wait(3000);
  const cowsBefore = await q.count(`@e[type=cow,${inPen}]`);
  body.send({ op: 'ranch', player: 'tester' });
  const started = await body.next((e) => e.ev === 'ranching' || e.ev === 'cantRanch', 15000);
  await wait(25000);
  body.send({ op: 'stay' });
  await wait(1500);
  const inChest = await q.inBlock(P.x - 1, -60, P.z - 1, 'wheat');
  const onHer = await q.carried(Name, 'wheat');
  const fed = 32 - inChest - onHer;
  const lacked = body.events.some((e) => e.ev === 'lacking' && e.what === 'breedFood');
  const calves = await q.count(`@e[type=cow,${inPen}]`) - cowsBefore;
  await body.quit();
  if (stocked) {
    report(Boolean(started) && started.ev === 'ranching' && inChest < 32 && 32 - inChest <= 16 && fed >= 2 && !lacked, 'feed from chest',
      `${started ? started.ev : 'no answer'}; chest wheat 32 -> ${inChest} (took ${32 - inChest}, 16 at most); on her ${onHer}; ` +
      `fed ${fed}; calves ${calves}; said she lacked food: ${lacked}`);
  } else {
    report(Boolean(started) && started.ev === 'ranching' && lacked && onHer === 0, 'empty chest', `${started ? started.ev : 'no answer'}; ` +
      `said she lacked food: ${lacked}; wheat on her ${onHer}`);
  }
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
}

// Scenery around a cut grove (run.js gather builds it): every block that isn't a log or leaves is still there.
const GroveScenery = [
  ['69 -60 45', 'oak_fence'], ['69 -60 46', 'oak_fence'], ['69 -60 47', 'oak_fence'], ['70 -60 45', 'oak_fence'],
  ['75 -59 42', 'stone_slab'], ['75 -59 43', 'stone_slab'], ['77 -60 47', 'flower_pot'], ['73 -60 53', 'lantern'],
  ['75 -60 53', 'oak_fence'],
];

async function scenery(spots, what) {
  const missing = [];
  for (const [pos, block] of spots) {
    const [a] = await rcon.run([`execute if block ${pos} minecraft:${block}`]);
    if (!/passed/i.test(a)) missing.push(`${block} at ${pos}`);
  }
  report(missing.length === 0, what, missing.length === 0 ? `all ${spots.length} still there` : `gone: ${missing.join(', ')}`);
  return missing.length === 0;
}

async function safety() {
  await ranch();
  await ranchFeed(true);
  await ranchFeed(false);
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} safety checks passed`);
  return failed.length === 0;
}

module.exports = { safety, scenery, GroveScenery, penCommands };
