// stdout is the event channel and carries nothing else. The protocol libraries print their own errors and warnings
// through console.*, so every console method goes to stderr (the log). index.js imports this first, before anything
// that loads them.

for (const method of ['log', 'info', 'warn', 'error', 'debug', 'trace']) {
  console[method] = (...parts) => process.stderr.write(parts.map(String).join(' ') + '\n');
}
