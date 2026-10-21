// A minimal RCON client for the local test server (see README.md).
//   node rcon.js "cmd 1" "cmd 2" ...            prints each command's answer
//   require('./rcon').run(['cmd', ...])         resolves to the answers, in order
const net = require('net');

const Host = process.env.MC_RCON_HOST || '127.0.0.1';
const Port = Number(process.env.MC_RCON_PORT || 25598);
const Password = process.env.MC_RCON_PASSWORD || 'local-test-only';

function run(commands) {
  return new Promise((resolve, reject) => {
    const sock = net.connect(Port, Host);
    const answers = [];
    let buf = Buffer.alloc(0);
    let id = 1;
    let next = -1; // -1: the login answer comes first
    const send = (type, body) => {
      const b = Buffer.from(body, 'utf8');
      const p = Buffer.alloc(14 + b.length);
      p.writeInt32LE(10 + b.length, 0);
      p.writeInt32LE(id++, 4);
      p.writeInt32LE(type, 8);
      b.copy(p, 12);
      sock.write(p);
    };
    sock.on('connect', () => send(3, Password));
    sock.on('data', (d) => {
      buf = Buffer.concat([buf, d]);
      while (buf.length >= 4 && buf.length >= 4 + buf.readInt32LE(0)) {
        const len = buf.readInt32LE(0);
        if (buf.readInt32LE(4) === -1) return reject(new Error('RCON password refused'));
        const body = buf.slice(12, 4 + len - 2).toString('utf8');
        buf = buf.slice(4 + len);
        if (next >= 0) answers.push(body);
        next++;
        if (next < commands.length) send(2, commands[next]);
        else sock.end();
      }
    });
    sock.on('close', () => resolve(answers));
    sock.on('error', reject);
  });
}

module.exports = { run };

if (require.main === module) {
  const commands = process.argv.slice(2);
  run(commands).then((answers) => answers.forEach((a, i) => console.log(`> ${commands[i]}\n${a}`)))
    .catch((e) => { console.error(e.message); process.exit(1); });
}
