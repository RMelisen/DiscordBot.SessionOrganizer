// Her real body (ProjectSYNCS/MinecraftBody) as a child process, driven the way MinecraftBodyService drives it: JSON
// ops on stdin, JSON events on stdout, logs on stderr.
const { spawn } = require('child_process');
const path = require('path');

const BodyDir = path.resolve(__dirname, '../../ProjectSYNCS/MinecraftBody');
const Name = 'SYNCS';
const wait = (ms) => new Promise((r) => setTimeout(r, ms));
const t0 = Date.now();
const stamp = () => ((Date.now() - t0) / 1000).toFixed(1).padStart(6);
const verbose = process.argv.includes('-v');

function startBody(extraEnv = {}) {
  const child = spawn(process.execPath, [path.join(BodyDir, 'index.js')], {
    cwd: BodyDir,
    env: { PATH: process.env.PATH, SYSTEMROOT: process.env.SYSTEMROOT, TEMP: process.env.TEMP, TMP: process.env.TMP,
      MC_HOST: process.env.MC_HOST || '127.0.0.1', MC_PORT: process.env.MC_PORT || '25599', MC_USERNAME: Name,
      MC_TRACE_MOVES: '1',
      // The Pi's budget (MC_CPU_FACTOR, see README) and the search log, when the run asks for them.
      ...(process.env.MC_CPU_FACTOR ? { MC_CPU_FACTOR: process.env.MC_CPU_FACTOR } : {}),
      ...(process.env.MC_TRACE_PATHS ? { MC_TRACE_PATHS: process.env.MC_TRACE_PATHS } : {}),
      ...extraEnv },
  });
  const listeners = new Set();
  const body = {
    events: [],
    corrections: 0,
    logs: [],
    exited: false,
    send: (op) => { if (!body.exited) child.stdin.write(JSON.stringify(op) + '\n'); },
    // The next event matching `test`, or null after `ms`.
    next(test, ms) {
      return new Promise((resolve) => {
        const timer = setTimeout(() => { listeners.delete(on); resolve(null); }, ms);
        const on = (ev) => { if (test(ev)) { clearTimeout(timer); listeners.delete(on); resolve(ev); } };
        listeners.add(on);
      });
    },
    // Every event since `since` (an index into body.events) matching `test`.
    since(index, test) { return body.events.slice(index).filter(test); },
    async quit() {
      body.send({ op: 'quit' });
      await Promise.race([new Promise((r) => child.on('exit', r)), wait(4000)]);
      child.kill();
    },
  };
  child.on('exit', () => { body.exited = true; });
  let out = '';
  child.stdout.on('data', (d) => {
    out += d;
    let i;
    while ((i = out.indexOf('\n')) >= 0) {
      const line = out.slice(0, i);
      out = out.slice(i + 1);
      let ev;
      try { ev = JSON.parse(line); } catch { continue; }
      if (verbose && !['inventory', 'players', 'hunger', 'seen'].includes(ev.ev)) console.log(stamp(), 'EV ', line.slice(0, 200));
      // Kicked or cut off mid-run: everything after it fails for that reason, so it's always said.
      if (!verbose && (ev.ev === 'kicked' || (ev.ev === 'end' && ev.reason !== 'disconnect.quitting')))
        console.log(`      (her body: ${line.slice(0, 200)})`);
      body.events.push(ev);
      for (const on of [...listeners]) on(ev);
    }
  });
  child.stderr.on('data', (d) => String(d).split('\n').filter(Boolean).forEach((l) => {
    if (l.startsWith('server teleport while walking')) body.corrections++;
    else if (!l.includes('MaxListeners') && !l.includes('trace-warnings')) body.logs.push(l);
    if (verbose) console.log(stamp(), 'LOG', l.slice(0, 300));
  }));
  return body;
}

async function spawned(body) {
  if (!(await body.next((e) => e.ev === 'spawned', 20000))) throw new Error('she never spawned (is the server up?)');
  await wait(1500);
}

async function where(body) {
  body.send({ op: 'where', player: 'tester' });
  const ev = await body.next((e) => e.ev === 'position', 3000);
  return ev ? `${ev.x} ${ev.y} ${ev.z}` : '?';
}

// The log lines that look like trouble (a throw, a failed action), for a report.
function troubles(body) {
  return body.logs.filter((l) => /failed|stopped|error|Error|TypeError|at /.test(l));
}

module.exports = { startBody, spawned, where, wait, stamp, troubles, Name, BodyDir };
