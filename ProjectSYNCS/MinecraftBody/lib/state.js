// What every part of her body shares: the bot, the task she's on, and the two ways out (events on stdout, logs on
// stderr). `bot` and `task` are live bindings: importers read the current one, and change it through setBot and setTask.

export function emit(ev, data) {
  process.stdout.write(JSON.stringify({ ev, ...data }) + '\n');
}

export function log(...parts) {
  process.stderr.write(parts.join(' ') + '\n');
}

export let bot = null;

export function setBot(value) {
  bot = value;
}

export let ended = false;
export function finish(reason) {
  if (ended) return;
  ended = true;
  emit('end', { reason: String(reason ?? '') });
  // Let stdout drain before leaving.
  setTimeout(() => process.exit(0), 100);
}

function playerNames() {
  return Object.keys(bot.players).filter((name) => name !== bot.username);
}

export function reportPlayers() {
  emit('players', { names: playerNames() });
}

// A chat component's text and translate key, the key being what C# classifies on: it is the
// same in every language, whereas the text is whatever Mineflayer's language data renders.
export function part(component) {
  if (component === null || component === undefined) return { text: '', key: null };
  if (typeof component !== 'object') return { text: String(component), key: null };
  return { text: component.toString(), key: component.translate ?? null };
}

// What she was asked to do, for the events that report how it went. Null when she stands still.
export let task = null;

export function setTask(value) {
  task = value;
}

export const wait =(ms) => new Promise((resolve) => setTimeout(resolve, ms));

// A tick handler that can't take the process down: an exception thrown from a physics tick or a timer is uncaught.
export function safely(fn) {
  return () => {
    try {
      fn();
    } catch (err) {
      log(`${fn.name} failed:`, err && err.message ? err.message : String(err));
    }
  };
}
