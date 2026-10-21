// How many arrows she needs for a creeper that can't move, at a few distances (her aim, tuned here).
const rcon = require('./rcon');
const q = require('./query');
const { startBody, spawned, wait, Name } = require('./body');
const { standIn } = require('./players');
(async () => {
  const S = { x: 60, z: -40 };
  const body = startBody();
  await spawned(body);
  const papa = await standIn('Papa');
  for (const d of [8, 12, 15]) {
    await rcon.run([`kill @e[type=creeper]`, `kill @e[type=arrow]`, `clear ${Name}`, `give ${Name} bow 1`, `give ${Name} arrow 64`,
      `tp ${Name} ${S.x + 0.5} -60 ${S.z + 0.5} -90 0`, `tp Papa ${S.x - 1.5} -60 ${S.z + 0.5}`, 'effect give Papa resistance 600 4 true',
      'difficulty normal', 'time set day', 'gamerule spawn_mobs false']);
    await wait(1500);
    body.send({ op: 'guard', player: 'Papa' });
    await wait(500);
    await rcon.run([`summon creeper ${S.x + 0.5 + d} -60 ${S.z + 0.5} {NoAI:1b,PersistenceRequired:1b,Tags:["acc"]}`]);
    const t0 = Date.now();
    let alive = 1;
    while (alive && Date.now() - t0 < 30000) { await wait(500); alive = await q.count('@e[type=creeper,tag=acc]'); }
    const used = 64 - await q.carried(Name, 'arrow');
    console.log(`distance ${d}: ${alive ? 'still alive' : 'dead'} after ${((Date.now() - t0) / 1000).toFixed(1)} s, ${used} arrows`);
    body.send({ op: 'stay' });
    await wait(1000);
  }
  await rcon.run(['difficulty peaceful']);
  await papa.leave();
  await body.quit();
  process.exit(0);
})();
