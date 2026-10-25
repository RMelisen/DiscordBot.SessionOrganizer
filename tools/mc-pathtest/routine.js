// Her routine's turns (WorkBlock): a farm and an animal pen sent as her remembered places, the clock stopped at
// morning, nobody giving orders. She starts at the farm, stays about 9 minutes, goes to the pen for about 3, and comes
// back to the farm. Real time: about 13 minutes.
const rcon = require('./rcon');
const { startBody, spawned, wait, troubles, Name } = require('./body');
const { penCommands } = require('./safety');

const P = { x: -40, z: -40 }; // safety.js's pen
const F = { x: -40, z: -12 }; // a wheat plot north of it, 28 blocks off
const fat = (dx, dy, dz) => `${F.x + dx} ${-60 + dy} ${F.z + dz}`;
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(16)} ${detail}`);
}

async function routine() {
  // The pen with a gate on the farm's side: the other tests put her inside, the routine walks her in.
  await rcon.run(['kill @e[type=cow]', ...penCommands(), `setblock ${P.x} -60 ${P.z + 5} oak_fence_gate[facing=south]`,
    `forceload add ${F.x - 10} ${F.z - 10} ${F.x + 10} ${F.z + 10}`,
    `fill ${fat(-6, -1, -6)} ${fat(6, -1, 6)} grass_block`, `fill ${fat(-6, 0, -6)} ${fat(6, 4, 6)} air`,
    `fill ${fat(-2, -1, -2)} ${fat(2, -1, 2)} farmland[moisture=7]`, `setblock ${fat(0, -1, 0)} water`,
    `fill ${fat(-2, 0, -2)} ${fat(2, 0, 2)} wheat[age=2]`, `setblock ${fat(0, 0, 0)} air`,
    `setblock ${fat(4, 0, 0)} chest[facing=west]`, 'gamerule advance_time false', 'time set 1000']);
  await wait(1500);
  await rcon.run([...[0, 1, 2, 3].map((i) => `summon cow ${P.x - 3 + i * 2} -60 ${P.z + 2}`), 'kill @e[type=item]']);
  const body = startBody();
  await spawned(body);
  await rcon.run([`clear ${Name}`, `give ${Name} wheat 8`, `tp ${Name} ${F.x + 3.5} -60 ${F.z + 3.5}`]);
  await wait(2000);
  body.send({ op: 'where', player: 'tester' });
  const pos = await body.next((e) => e.ev === 'position', 3000);
  const dimension = pos ? pos.dimension : 'minecraft:overworld';
  const t0 = Date.now();
  body.send({ op: 'routine', farm: { x: F.x + 3, y: -60, z: F.z + 3, dimension },
    ranch: { x: P.x + 1, y: -60, z: P.z + 1, dimension }, fish: null, home: null });
  const turns = [];
  const on = (ev) => ev.ev === 'routine' && turns.push({ what: ev.what, at: (Date.now() - t0) / 1000 });
  for (let i = 0; i < 3; i++) {
    const ev = await body.next((e) => e.ev === 'routine', 12 * 60000);
    if (!ev) break;
    on(ev);
    console.log(`      t+${turns[turns.length - 1].at.toFixed(0)} s: off to the ${ev.what}`);
  }
  await body.quit();
  const [a, b, c] = turns;
  const farmTurn = a && b ? b.at - a.at : NaN;
  const penTurn = b && c ? c.at - b.at : NaN;
  const order = turns.map((t) => t.what).join(' -> ');
  // A turn ends at the first autonomy tick past its end, and a round under way (the farm's every 15 s) runs on a bit.
  report(order === 'farm -> ranch -> farm', 'turn order', order || 'never started');
  report(farmTurn >= 530 && farmTurn <= 600, 'farm turn', `${farmTurn.toFixed(0)} s (9 minutes: 540)`);
  report(penTurn >= 170 && penTurn <= 240, 'pen turn', `${penTurn.toFixed(0)} s (3 minutes: 180)`);
  for (const t of troubles(body)) console.log('      log:', t.slice(0, 200));
  await rcon.run(['kill @e[type=cow]', `fill ${fat(-6, 0, -6)} ${fat(6, 4, 6)} air`, `fill ${fat(-6, -1, -6)} ${fat(6, -1, 6)} grass_block`]);
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} routine checks passed`);
  return failed.length === 0;
}

module.exports = { routine };
