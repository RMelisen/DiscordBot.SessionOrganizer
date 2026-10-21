// A TCP relay that delays everything between her and the server, as the road from the Pi to the group's server does.
// Each direction is held back by `ms` plus up to `jitter` ms, in order (TCP never reorders, so neither does this):
// a chunk never leaves before the one ahead of it.
//   const proxy = await lag.start({ ms: 80, jitter: 40 });  ...  MC_PORT=proxy.port for her body  ...  proxy.close()
//   node lag.js 80 40         (alone: listens on 25600 until stopped)
const net = require('net');

// One queue per direction, drained in order by a single timer: a timer per chunk let two chunks due in the same
// millisecond go out swapped, and a swapped compressed stream is garbage (« incorrect header check »).
function relay(from, to, ms, jitter) {
  const queue = []; // { at, chunk } in arrival order, `at` never decreasing
  let last = 0;
  let timer = null;
  let ended = false;
  const drain = () => {
    timer = null;
    while (queue.length > 0 && queue[0].at <= Date.now()) {
      const { chunk } = queue.shift();
      if (!to.destroyed) to.write(chunk);
    }
    if (queue.length > 0) timer = setTimeout(drain, Math.max(0, queue[0].at - Date.now()));
    else if (ended) to.end();
  };
  from.on('data', (chunk) => {
    last = Math.max(last, Date.now() + ms + Math.random() * jitter);
    queue.push({ at: last, chunk });
    if (!timer) timer = setTimeout(drain, Math.max(0, last - Date.now()));
  });
  from.on('end', () => { ended = true; if (!timer) drain(); });
  from.on('error', () => to.destroy());
}

function start({ ms = 80, jitter = 40, port = 25600, target = 25599, host = '127.0.0.1' } = {}) {
  const sockets = new Set();
  const server = net.createServer((client) => {
    const upstream = net.connect(target, host);
    client.setNoDelay(true);
    upstream.setNoDelay(true);
    sockets.add(client);
    sockets.add(upstream);
    relay(client, upstream, ms, jitter);
    relay(upstream, client, ms, jitter);
    const drop = () => { client.destroy(); upstream.destroy(); sockets.delete(client); sockets.delete(upstream); };
    client.on('close', drop);
    upstream.on('close', drop);
  });
  return new Promise((resolve) => server.listen(port, '127.0.0.1', () => resolve({
    port,
    close: () => new Promise((r) => { for (const s of sockets) s.destroy(); server.close(() => r()); }),
  })));
}

module.exports = { start };

if (require.main === module) {
  const [ms, jitter] = process.argv.slice(2).map(Number);
  start({ ms: ms || 80, jitter: jitter || 40 }).then((p) => console.log(`lag proxy on ${p.port}: ${ms || 80} ms + up to ${jitter || 40}`));
}
