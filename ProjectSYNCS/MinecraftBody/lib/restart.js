// An order started again as if C# had sent it once more: after a fight, a run or a walk back (resume.js), or an order
// held during a fight (combat.js). commands.js's dispatch is handed over by index.js: imported here, it would make the
// modules that restart orders depend on every module an order can reach.

let dispatchCommand = () => {};

export function restartWith(dispatch) {
  dispatchCommand = dispatch;
}

export function redo(command) {
  dispatchCommand(command);
}
