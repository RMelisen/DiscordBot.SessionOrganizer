// Her day, built on the places she was asked to remember: the fields, fishing, the pen twice a day, home at dusk.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { chestNear, cropsNear, farmRound } from './farm.js';
import { fishingRod, fishLoop, openWater } from './fishing.js';
import { lacking } from './needs.js';
import { stay } from './orders.js';
import { penAt, ranchRound } from './ranch.js';
import { dimension } from './seek.js';
import { bot, emit, log, setTask, task } from './state.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// C# sends the farm, fishing spot, animal pen and home it found among the remembered places (`routine`). By day she
// works at the farm and the fishing spot in turns (WorkBlock, by place), and visits the pen twice: in the morning and
// before dusk (PenSlots); at dusk she goes home and stays there till morning; the night is autoSleepTick's. A place in
// another dimension, or farther than OwnTripMax, is left out; one that fails (no way there, nothing to do there) is left
// out for SpotRetry, silently. Orders always come first.
export const routine = { farm: null, fish: null, ranch: null, home: null };
const WorkKinds = ['farm', 'fish'];
// How long a turn lasts at each: the fields get the most. A visit to the pen (two fed per kind each round, a cull, the
// wool) needs a couple of minutes.
const WorkBlock = { farm: 540000, fish: 360000, ranch: 120000 };
// The pen's two visits, by the time of day they may start in: as soon as she's up, and late enough in the afternoon to
// fit a visit and the walk before dusk. Twice a day breeds twice (an adult can again after 5 minutes, and yesterday's
// babies are grown: they take a day) and shears twice (the wool grows back as the sheep graze).
const PenSlots = { morning: [0, 3000], evening: [8000, 11500] };
const penDone = { morning: -1, evening: -1 }; // the day (bot.time.day) each visit was last made
const OwnTripMax = 256;
const TripTimeout = 180000;
const SpotRetry = 600000;
const DuskStart = 11500;
const DuskEnd = 12541;
const HomeNear = 6;
export const failedSpots = new Map();
let lastWork = null;

function spotUsable(what) {
  const s = routine[what];
  if (!s || s.dimension !== dimension() || (failedSpots.get(what) || 0) > Date.now()) return false;
  const here = bot.entity.position;
  return Math.hypot(s.x - here.x, s.z - here.z) <= OwnTripMax;
}

export function failSpot(what, why) {
  failedSpots.set(what, Date.now() + SpotRetry);
  log(`routine: ${what} left out for a while (${why})`);
}

function isRoutine(t) {
  return Boolean(t && t.own && t.what);
}

// True while the routine has her (or just took her); false leaves her to tagging along.
export function routineTick() {
  const t = task;
  if (t && t.kind === 'trip' && Date.now() > t.deadline) {
    stay();
    failSpot(t.what, 'never got there');
    return false;
  }
  const time = bot.time.timeOfDay;
  if (time < DuskStart) {
    // A visit to the pen runs to its end, whatever the clock says, and counts once it has: one an order cut short is
    // made again if its hours aren't over.
    if (isRoutine(t) && t.what === 'ranch') {
      if (t.kind === 'trip' || Date.now() < t.turnEnd) return true;
      penDone[t.slot] = t.day;
    }
    const slot = Object.keys(PenSlots).find((s) => time >= PenSlots[s][0] && time < PenSlots[s][1]);
    if (slot && penDone[slot] !== bot.time.day && spotUsable('ranch')) return startTrip('ranch', slot);
    const kinds = WorkKinds.filter(spotUsable);
    if (kinds.length === 0) return endRoutine();
    if (isRoutine(t) && kinds.includes(t.what) && (t.kind === 'trip' || Date.now() < t.turnEnd)) return true;
    const from = WorkKinds.indexOf(lastWork);
    const next = WorkKinds.map((_, i) => WorkKinds[(from + 1 + i) % WorkKinds.length]).find((k) => kinds.includes(k));
    if (isRoutine(t) && t.what === next && t.kind !== 'trip') {
      t.turnEnd = Date.now() + WorkBlock[next]; // the only place: another turn of the same
      return true;
    }
    return startTrip(next);
  }
  if (time <= DuskEnd) {
    if (isRoutine(t) && t.what === 'home') return true;
    if (!spotUsable('home')) return endRoutine();
    const h = routine.home;
    if (bot.entity.position.distanceTo(new Vec3(h.x, h.y, h.z)) <= HomeNear) {
      stay();
      setTask({ kind: 'home', own: true, player: '', what: 'home', spot: new Vec3(h.x, h.y, h.z) });
      return true;
    }
    return startTrip('home');
  }
  // Night: nothing new. At home (or on her way), she stays; the bed is autoSleepTick's.
  if (isRoutine(t) && t.what === 'home') return true;
  return endRoutine();
}

function endRoutine() {
  if (isRoutine(task)) stay();
  return false;
}

// `slot`: which of the pen's visits this is (PenSlots), counted on its day once it's over.
function startTrip(what, slot = null) {
  const s = routine[what];
  const spot = new Vec3(s.x, s.y, s.z);
  stay();
  const trip = { kind: 'trip', own: true, player: '', what, spot, turnEnd: Date.now() + (WorkBlock[what] || 0), deadline: Date.now() + TripTimeout,
    slot, day: bot.time.day };
  setTask(trip);
  if (WorkKinds.includes(what)) lastWork = what;
  emit('routine', { what });
  if (bot.entity.position.distanceTo(spot) <= 3) arriveOwn(trip);
  else bot.pathfinder.setGoal(new goals.GoalNear(s.x, s.y, s.z, 2));
  return true;
}

// At the place: the activity starts with the same checks as the order (crops and a chest, water and a rod, animals and
// a chest). One that can't start leaves the place out for a while.
export function arriveOwn(trip) {
  if (task !== trip) return;
  bot.pathfinder.setGoal(null);
  // A visit to the pen is short: its minutes count from her arrival, not from setting off.
  const turnEnd = trip.what === 'ranch' ? Date.now() + WorkBlock.ranch : trip.turnEnd;
  const base = { own: true, player: '', what: trip.what, turnEnd, slot: trip.slot, day: trip.day };
  if (trip.what === 'home') {
    setTask({ ...base, kind: 'home', spot: trip.spot });
    return;
  }
  const anchor = bot.entity.position.clone();
  setTask(null);
  if (trip.what === 'farm') {
    if (!cropsNear(anchor)) return failSpot('farm', 'no crops');
    const chest = chestNear(anchor);
    if (!chest) return failSpot('farm', 'no chest');
    setTask({ ...base, kind: 'farm', anchor, chest, busy: false });
    farmRound();
  } else if (trip.what === 'fish') {
    if (!fishingRod()) {
      lacking('rod');
      return failSpot('fish', 'no rod');
    }
    const water = openWater();
    if (!water) return failSpot('fish', 'no water');
    setTask({ ...base, kind: 'fish', water, casting: false, quiet: true });
    fishLoop(task);
  } else {
    const pen = penAt(anchor);
    if (pen.why) return failSpot('ranch', pen.why === 'none' ? 'no animals' : 'no chest');
    setTask({ ...base, kind: 'ranch', anchor, chest: pen.chest, busy: false, fedAt: new Map(), shornAt: new Map(), target: null });
    ranchRound();
  }
}
