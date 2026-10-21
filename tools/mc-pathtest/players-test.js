// Her body with players around: following one who walks a route with turns, coming from afar, leading one who lags
// behind, losing one who goes too far, walking up to one by herself. Who she works for (claims, family first) is
// decided in C# (McClaim, walked by tests/ProjectSYNCS.Checks), not here.
const rcon = require('./rcon');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { standIn, req } = require('./players');

const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(14)} ${detail}`);
}

// Open ground north-east of spawn, between the other test areas.
const O = { x: 10, z: 10 };

async function scene(withPathfinder = false) {
  await rcon.run([`fill ${O.x - 4} -61 ${O.z - 4} ${O.x + 30} -61 ${O.z + 30} grass_block`,
    `fill ${O.x - 4} -60 ${O.z - 4} ${O.x + 30} -54 ${O.z + 30} air`,
    // A few things in the way of a walk: pillars, a wall with a gap, a step.
    `fill ${O.x + 6} -60 ${O.z + 2} ${O.x + 6} -59 ${O.z + 2} stone`, `fill ${O.x + 9} -60 ${O.z - 1} ${O.x + 9} -59 ${O.z - 1} stone`,
    `fill ${O.x + 14} -60 ${O.z - 4} ${O.x + 14} -58 ${O.z + 8} stone`, `fill ${O.x + 14} -60 ${O.z + 3} ${O.x + 14} -59 ${O.z + 4} air`,
    `fill ${O.x + 18} -60 ${O.z + 10} ${O.x + 24} -60 ${O.z + 16} stone`,
    'difficulty peaceful', 'time set day']);
  const body = startBody();
  await spawned(body);
  const papa = await standIn('Papa', { pathfinder: withPathfinder });
  await rcon.run([`tp ${Name} ${O.x + 0.5} -60 ${O.z + 0.5}`, `tp Papa ${O.x + 2.5} -60 ${O.z + 0.5}`]);
  body.send({ op: 'family', names: ['Papa'] });
  await wait(2500);
  return { body, papa };
}

async function end({ body, papa }, name) {
  for (const t of troubles(body)) console.log(`      ${name} log:`, t.slice(0, 200));
  await papa.leave();
  await body.quit();
}

const her = (papa) => papa.players[Name] && papa.players[Name].entity;

async function followWalk() {
  const sc = await scene(true);
  const { goals } = req('mineflayer-pathfinder');
  sc.body.send({ op: 'follow', player: 'Papa' });
  await wait(1000);
  // Papa's route: through the gap in the wall, over the step, back round.
  const route = [[O.x + 10, O.z + 3], [O.x + 17, O.z + 4], [O.x + 21, O.z + 13], [O.x + 27, O.z + 22], [O.x + 3, O.z + 24]];
  let worst = 0;
  let lost = false;
  sc.body.next((e) => e.ev === 'cantReach', 120000).then((e) => { if (e) lost = true; });
  for (const [x, z] of route) {
    sc.papa.pathfinder.setGoal(new goals.GoalNear(x, -60, z, 1));
    const until = Date.now() + 25000;
    while (Date.now() < until) {
      await wait(1000);
      const s = her(sc.papa);
      if (s) worst = Math.max(worst, s.position.distanceTo(sc.papa.entity.position));
      if (sc.papa.entity.position.distanceTo({ x: x + 0.5, y: -60, z: z + 0.5 }) < 2) break;
    }
  }
  await wait(4000);
  const s = her(sc.papa);
  const last = s ? s.position.distanceTo(sc.papa.entity.position) : NaN;
  report(!lost && worst < 12 && last < 5, 'follow a walk', `at most ${worst.toFixed(1)} blocks behind on the way, ${last.toFixed(1)} at the end; lost him: ${lost}`);
  await end(sc, 'follow');
}

async function comeFromAfar() {
  const sc = await scene();
  await rcon.run([`tp Papa ${O.x + 26.5} -60 ${O.z + 26.5}`]);
  await wait(2000);
  sc.body.send({ op: 'come', player: 'Papa' });
  const done = await sc.body.next((e) => e.ev === 'arrived' || e.ev === 'cantReach', 40000);
  const s = her(sc.papa);
  const d = s ? s.position.distanceTo(sc.papa.entity.position) : NaN;
  report(Boolean(done && done.ev === 'arrived') && d < 4, 'come', `${done ? done.ev : 'no answer'}; ${d.toFixed(1)} blocks from Papa`);
  await end(sc, 'come');
}

async function guide() {
  const sc = await scene();
  const spot = { x: O.x + 25.5, y: -60, z: O.z + 5.5 };
  sc.body.send({ op: 'guide', player: 'Papa', x: spot.x, y: spot.y, z: spot.z, dimension: 'overworld' });
  const waiting = await sc.body.next((e) => e.ev === 'waiting', 30000);
  // Papa catches up: she should go on.
  const s = her(sc.papa);
  if (s) await rcon.run([`tp Papa ${s.position.x - 1} -60 ${s.position.z}`]);
  const arrived = await sc.body.next((e) => e.ev === 'arrived', 40000);
  report(Boolean(waiting) && Boolean(arrived), 'guide', `${waiting ? 'waited for Papa' : 'never waited'}; ${arrived ? 'got there once he caught up' : 'never got there'}`);
  await end(sc, 'guide');
}

async function lost() {
  const sc = await scene();
  sc.body.send({ op: 'follow', player: 'Papa' });
  await wait(2000);
  await rcon.run([`tp Papa 0.5 -60 -300.5`]);
  const ev = await sc.body.next((e) => e.ev === 'cantReach', 15000);
  report(Boolean(ev) && ev.why === 'lost', 'lost him', ev ? `cantReach ${ev.why}` : 'no answer');
  await end(sc, 'lost');
}

async function seek() {
  const sc = await scene();
  await rcon.run([`tp Papa ${O.x + 20.5} -60 ${O.z + 20.5}`]);
  await wait(2000);
  sc.body.send({ op: 'seek', player: 'Papa', spot: null });
  const ev = await sc.body.next((e) => e.ev === 'seek', 40000);
  report(Boolean(ev) && ev.outcome === 'met', 'seek', ev ? `seek ${ev.outcome}` : 'no answer');
  await end(sc, 'seek');
}

async function playersTest(only = []) {
  const all = { follow: followWalk, come: comeFromAfar, guide, lost, seek };
  for (const [name, run] of Object.entries(all)) {
    if (only.length > 0 && !only.includes(name)) continue;
    try {
      await run();
    } catch (err) {
      report(false, name, `threw: ${err.message}`);
    }
  }
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} player checks passed`);
  return failed.length === 0;
}

module.exports = { playersTest };
