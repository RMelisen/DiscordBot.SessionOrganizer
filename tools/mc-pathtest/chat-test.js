// Her chat in game (7.25.0, « she answers chat the way she does on Discord »), end to end: MinecraftBodyService itself
// (tools/mc-chattest: the real pools and rules, Discord offline) drives her body on the local server, stand-in players
// talk to her, and her answers are read from the game chat and traced back to the pool they came from.
//   node chat-test.js            (the local server running; builds and starts tools/mc-chattest itself)
const { spawn, execFileSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');
const rcon = require('./rcon');
const { wait } = require('./body');
const { standIn } = require('./players');

const repo = path.resolve(__dirname, '../..');
const outDir = fs.mkdtempSync(path.join(os.tmpdir(), 'mc-chattest-'));
const results = [];
function report(ok, name, detail) {
  results.push({ ok, name });
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name.padEnd(26)} ${detail}`);
}

// ---- Which pool a line came from -------------------------------------------------------------------------

// Lines are compared on their words only: the game version of a Discord line loses emotes, faces and stickers
// (McChat.Game, the sticker markers « ⟪bored⟫ » included), and placeholders ({0}, {1:…}) match anything.
const words = (s) => s.replace(/⟪[^⟫]*⟫/g, ' ').replace(/<a?:\w+:\d+>/g, ' ').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/\{[^}]*\}/g, ' \u0001 ')
  .replace(/[^a-z0-9\u0001]+/g, ' ').trim();
let patterns = [];
function loadPools() {
  const pools = JSON.parse(fs.readFileSync(path.join(outDir, 'pools.json'), 'utf8'));
  patterns = [];
  for (const [name, lines] of Object.entries(pools))
    for (const line of lines) {
      const w = words(line);
      if (w.replace(/\u0001/g, '').trim().length < 4) continue; // « {0} ! » matches everything
      const re = new RegExp(`^${w.split('\u0001').map((p) => p.trim().replace(/[.*+?^${}()|[\]\\]/g, '\\$&')).join('.*?')}$`);
      patterns.push({ name, re });
    }
}
function poolsOf(text) {
  const w = words(text);
  return [...new Set(patterns.filter((p) => p.re.test(w)).map((p) => p.name))];
}

// Plain game chat: what McChat.Game promises.
function clean(text) {
  if (text.length > 256) return 'over 256 characters';
  if (/[{}]/.test(text)) return 'a stray brace';
  if (/<a?:|<@|<#|<t:/.test(text)) return 'Discord markup';
  if (/\*\*|`|✨/.test(text)) return 'bold, code or ✨';
  return null;
}

// ---- Her lines, as players see them --------------------------------------------------------------------

const heard = []; // { at, to, text, whisper }
const said = []; // what the players said, for heard.json
function listen(bot) {
  bot.on('chat', (username, message) => { if (username === 'SYNCS') heard.push({ at: Date.now(), to: null, text: message, by: bot.username }); });
  bot.on('whisper', (username, message) => { if (username === 'SYNCS') heard.push({ at: Date.now(), to: bot.username, text: message, whisper: true }); });
}
// Her public lines (seen once, by any listener) and whispers to `who`, since `since`.
function linesSince(since, who) {
  const seen = new Set();
  return heard.filter((h) => h.at >= since && (h.whisper ? h.to === who : true)).filter((h) => {
    const key = `${h.whisper ? 'w' : 'p'}|${h.text}|${Math.round(h.at / 500)}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

async function say(player, text) {
  const t = Date.now();
  said.push({ at: t, by: player.username, text });
  player.chat(text);
  return t;
}

// What she said after `since` within `ms`, the first line (or null).
async function answer(since, ms = 7000, who = null) {
  const end = Date.now() + ms;
  while (Date.now() < end) {
    const got = linesSince(since, who);
    if (got.length > 0) return got[0];
    await wait(200);
  }
  return null;
}

const describe = (line) => (line ? `« ${line.text} » [${poolsOf(line.text).join(', ') || 'no pool found'}]` : 'silence');
const fromCalled = (line) => line && poolsOf(line.text).some((p) => p.startsWith('McCalledLines'));
const fromAny = (line, prefixes) => line && poolsOf(line.text).some((p) => prefixes.some((x) => p.startsWith(x)));

// ---- The scenarios -----------------------------------------------------------------------------------

async function scenarios(p) {
  const { papa, tata, steve, alex, max } = p;
  let t;
  let a;

  // A question with her name: an answer, not « oui ? ».
  t = await say(steve, 'syncs ça va ?');
  a = await answer(t);
  report(Boolean(a) && !fromCalled(a), 'ça va ? (a stranger)', describe(a));
  await wait(2000);

  // A bare call: « oui ? »; the next message, without her name, is heard.
  t = await say(alex, 'syncs');
  a = await answer(t);
  report(fromCalled(a), 'bare call', describe(a));
  await wait(1500);
  t = await say(alex, 'tu fais quoi ?');
  a = await answer(t);
  report(Boolean(a) && !fromCalled(a), 'follow-up without her name', describe(a));
  await wait(2000);

  // An intent, from a stranger and from Papa (his own twin pools).
  t = await say(max, 'syncs je t\'aime');
  a = await answer(t);
  report(Boolean(a) && !fromAny(a, ['Owner']), 'je t\'aime (a stranger)', describe(a));
  await wait(2000);
  t = await say(papa, 'syncs je t\'aime');
  a = await answer(t);
  report(fromAny(a, ['Owner']), 'je t\'aime (Papa)', describe(a));
  await wait(2000);

  // Mean, from a stranger (a comeback) and from Papa (hurt, never a roast).
  t = await say(tata, 'syncs ça va ?');
  a = await answer(t);
  report(Boolean(a) && !fromCalled(a), 'ça va ? (Tata)', describe(a));
  await wait(2000);
  t = await say(papa, 'syncs t\'es nulle');
  a = await answer(t);
  report(fromAny(a, ['Owner']) && !fromAny(a, ['Comebacks']), 'insult (Papa)', describe(a));
  await wait(2000);

  // A favour she has no order for.
  t = await say(max, 'syncs tu peux me faire un gâteau ?');
  a = await answer(t);
  report(fromAny(a, ['McCantDo']), 'a favour she can\'t do', describe(a));
  await wait(2000);

  // « Inabot ».
  t = await say(steve, 'syncs t\'es inabot');
  a = await answer(t);
  report(fromAny(a, ['MistakenIdentity']), 'Inabot', describe(a));
  await wait(2000);

  // A whisper without her name: for her, answered by whisper.
  t = Date.now();
  alex.whisper('SYNCS', 'salut toi');
  a = await answer(t, 7000, 'Alex');
  report(Boolean(a && a.whisper), 'whisper without her name', `${describe(a)}${a && a.whisper ? ' (whispered back)' : ''}`);
  await wait(2000);

  // Talked about, not to: silence, or a remark about it; never an answer pool, never « oui ? ».
  t = await say(max, 'je joue avec syncs ce soir');
  a = await answer(t, 6000);
  report(!a || fromAny(a, ['TalkedAbout', 'Backbite']), 'talked about, not to', describe(a));
  await wait(2000);

  // An order is still an order.
  t = await say(alex, 'syncs viens');
  a = await answer(t, 9000);
  report(Boolean(a) && fromAny(a, ['McComeAck']), 'an order (come)', describe(a));
  await wait(3000);
  steve.chat('syncs stop');
  await wait(6000); // her ack, typed, lands well before the next scenario

  // After an answer, without her name, only what is plainly for her answers (a specific intent): « ok » or a plain
  // question may be for someone else, and get silence.
  t = await say(max, 'syncs comment ça va ?');
  a = await answer(t);
  await wait(2000);
  t = await say(max, 'ok');
  const ok = await answer(t, 5000);
  t = await say(max, 'on va miner ?');
  const what = await answer(t, 5000);
  report(Boolean(a) && !ok && !what, 'without her name, after', `opener: ${describe(a)}; « ok »: ${describe(ok)}; « on va miner ? »: ${describe(what)}`);
  await wait(2000);
  // The window is 30 s from her answer: a message she let go (for someone else) doesn't close it.
  t = await say(steve, 'syncs ça va bien ?');
  a = await answer(t);
  await wait(2000);
  steve.chat('mdr t\'as vu ça Alex');
  await wait(3000);
  t = await say(steve, 'je t\'aime');
  const love = await answer(t);
  report(Boolean(a) && Boolean(love), 'window outlasts a let-go', `opener: ${describe(a)}; « je t'aime » after a line for Alex: ${describe(love)}`);
  await wait(2000);

  // A burst of hellos: the first stranger gets words, Papa always does, the other stranger only a wave. The burst gate
  // is per kind, across players, for 30 s: well clear of any hello before it.
  await wait(32000);
  t = Date.now();
  steve.chat('salut syncs');
  await wait(400);
  papa.chat('salut syncs');
  await wait(400);
  alex.chat('salut syncs');
  await wait(9000);
  const burst = linesSince(t);
  const papaWords = burst.some((l) => fromAny(l, ['McWaveLinesOwner']));
  report(papaWords && burst.length === 2, 'a burst of hellos', `${burst.length} lines: ${burst.map(describe).join(' | ')}`);
  await wait(2000);

  // The streak, with someone she hasn't talked with yet: five answers in a row; the 3rd, nothing read in it (a
  // comeback), is a nudge, the 5th ends it, then silence. Long sentences: six words or fewer after her name is a call.
  const streak = [];
  for (const text of ['syncs la lune est carrée et le soleil est un peu violet', 'syncs les patates volent au dessus de la grande montagne',
    'syncs le ciel est vert depuis que les cochons font du vélo', 'syncs les vaches nagent dans la rivière avec les petits poissons',
    'syncs les poules chantent faux tous les matins dans la cour', 'syncs les cochons dansent la valse sous la pluie de la nuit']) {
    t = await say(p.zoe, text);
    streak.push(await answer(t, 6000));
    await wait(1500);
  }
  const names = streak.map((l) => (l ? (poolsOf(l.text)[0] || '?') : '-'));
  const nudged = streak.slice(0, 4).some((l) => fromAny(l, ['StreakNudge']));
  const ended = streak.slice(0, 5).some((l) => fromAny(l, ['StreakEnd']));
  report(nudged && ended && !streak[5], 'the conversation streak', names.join(' > '));

  // Every line she said: plain game chat.
  const dirty = heard.map((h) => [h.text, clean(h.text)]).filter(([, why]) => why);
  report(dirty.length === 0, 'every line is game chat', dirty.length === 0 ? `${new Set(heard.map((h) => h.text)).size} lines checked` : dirty.map(([text, why]) => `${why}: « ${text} »`).join(' | '));
}

(async () => {
  console.log('building tools/mc-chattest…');
  execFileSync('dotnet', ['build', path.join(repo, 'tools/mc-chattest'), '-v', 'q', '-nologo'], { stdio: 'ignore' });
  await rcon.run(['difficulty peaceful', 'time set day', 'tp @a 0 -60 0', 'gamerule spawn_mobs false', 'gamerule spawn_monsters false']);
  const players = {};
  for (const name of ['Papa', 'Tata', 'Steve', 'Alex', 'Max', 'Zoe']) {
    players[name.toLowerCase()] = await standIn(name);
    listen(players[name.toLowerCase()]);
  }
  await rcon.run(['tp Papa 1.5 -60 0.5', 'tp Tata 2.5 -60 1.5', 'tp Steve -1.5 -60 0.5', 'tp Alex 0.5 -60 2.5', 'tp Max 0.5 -60 -1.5', 'tp Zoe -0.5 -60 -1.5']);
  const host = spawn('dotnet', ['run', '--no-build', '--project', path.join(repo, 'tools/mc-chattest'), '--', outDir], { cwd: repo });
  const hostLog = [];
  host.stdout.on('data', (d) => String(d).split('\n').filter(Boolean).forEach((l) => hostLog.push(l)));
  host.stderr.on('data', (d) => String(d).split('\n').filter(Boolean).forEach((l) => hostLog.push(l)));
  // In game once Papa sees her.
  const end = Date.now() + 60000;
  while (!players.papa.players.SYNCS && Date.now() < end) await wait(500);
  if (!players.papa.players.SYNCS) {
    console.log('she never came in. Host log:\n' + hostLog.slice(-20).join('\n'));
    process.exit(1);
  }
  loadPools();
  await wait(8000); // her arrival line and her first look around
  heard.length = 0;
  await rcon.run(['tp SYNCS 0.5 -60 0.5']);
  await wait(2000);
  try {
    await scenarios(players);
  } catch (err) {
    report(false, 'scenarios', `threw: ${err.stack}`);
  }
  host.stdin.write('quit\n');
  await wait(5000);
  host.kill();
  for (const p of Object.values(players)) await p.leave();
  fs.writeFileSync(path.join(outDir, 'host.log'), hostLog.join('\n'));
  fs.writeFileSync(path.join(outDir, 'heard.json'), JSON.stringify([...heard, ...said].sort((x, y) => x.at - y.at), null, 1));
  console.log(`      host log: ${path.join(outDir, 'host.log')}`);
  const warnings = hostLog.filter((l) => /fail|warn|error|exception/i.test(l));
  for (const w of warnings.slice(0, 8)) console.log('      host:', w.slice(0, 220));
  const failed = results.filter((r) => !r.ok);
  console.log(`\n${results.length - failed.length}/${results.length} chat checks passed`);
  process.exit(failed.length === 0 ? 0 : 1);
})().catch((e) => { console.error(e); process.exit(1); });
