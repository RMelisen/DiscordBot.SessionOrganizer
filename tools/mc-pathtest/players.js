// Stand-in players: plain Mineflayer clients (from the body's own node_modules) with the two protocol fixes a
// 26.3 server needs from any Mineflayer client (a tick_end after each move, entity actions by name), so they can
// stand, walk, ride, hit and carry things where a test needs a player.
const path = require('path');
const { BodyDir, wait } = require('./body');

const req = (m) => require(path.join(BodyDir, 'node_modules', m));
const OldEntityActions = ['start_sneaking', 'stop_sneaking', 'stop_sleeping', 'start_sprinting', 'stop_sprinting',
  'start_riding_jump', 'stop_riding_jump', 'open_inventory', 'start_fall_flying'];
const MovePackets = new Set(['position', 'position_look', 'look', 'flying']);

// Resolves once the player is in the world. `opts.pathfinder` loads the pathfinder so it can walk.
function standIn(name, opts = {}) {
  const mineflayer = req('mineflayer');
  const bot = mineflayer.createBot({ host: process.env.MC_HOST || '127.0.0.1', port: Number(process.env.MC_PORT || 25599),
    username: name, auth: 'offline', version: '26.1' });
  const raw = bot._client.write.bind(bot._client);
  bot._client.write = (n, p) => {
    if (n === 'entity_action' && p && typeof p.actionId === 'number') {
      const a = OldEntityActions[p.actionId];
      if (!a || a.endsWith('_sneaking')) return undefined;
      p = { ...p, actionId: a };
    }
    const r = raw(n, p);
    if (MovePackets.has(n)) raw('tick_end', {});
    return r;
  };
  if (opts.pathfinder) {
    const { pathfinder, Movements } = req('mineflayer-pathfinder');
    bot.loadPlugin(pathfinder);
    bot.once('spawn', () => {
      bot.physics.playerHalfWidth = 0.3001; // the 26.3 server refuses a box flush against a block face
      const m = new Movements(bot);
      m.canDig = false;
      m.allow1by1towers = false;
      m.scafoldingBlocks = [];
      bot.pathfinder.setMovements(m);
    });
  }
  bot.on('error', () => {});
  bot.on('kicked', (r) => console.log(`  (${name} kicked: ${JSON.stringify(r).slice(0, 120)})`));
  bot.leave = async () => { try { bot.quit(); } catch { /* gone */ } await wait(500); };
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error(`${name} never spawned`)), 20000);
    bot.once('spawn', async () => { clearTimeout(timer); await wait(1000); resolve(bot); });
  });
}

module.exports = { standIn, req };
