// The half of her fights that needs no bot: sizing a fight up (fight it, fall back to her player, or get out), how long
// her weapon takes between two hits, who she may hit back, and whether she's surrounded. Plain data and functions,
// checked by tools/mc-pathtest/fight-check.js; index.js owns everything that moves her.

// Never fought, only got away from: the warden (two hits kill her, and a fight only angers it more) and the creaking
// (nothing hurts it while its heart stands).
const GetOutKinds = new Set(['warden', 'creaking']);
// Left alone until they hurt her: hitting one first starts the fight she's supposed to prevent.
const NeutralFoes = new Set(['enderman', 'piglin', 'zombified_piglin']);
// Shot back with her bow when she has one; closed in on behind her shield when she doesn't.
const RangedKinds = new Set(['skeleton', 'stray', 'bogged', 'pillager', 'blaze', 'ghast', 'witch', 'phantom', 'breeze']);

// How much each kind counts against her, in zombies. Starting values, tuned on the local server.
const KindWeights = {
  silverfish: 0.5, endermite: 0.5,
  zombie: 1, husk: 1, drowned: 1, zombie_villager: 1, skeleton: 1, stray: 1, bogged: 1, slime: 1, magma_cube: 1, phantom: 1,
  spider: 1.2,
  cave_spider: 1.5, pillager: 1.5, vex: 1.5, shulker: 1.5, piglin: 1.5, zombified_piglin: 1.5,
  witch: 2, blaze: 2, ghast: 2, breeze: 2, guardian: 2,
  enderman: 2.5,
  vindicator: 3, evoker: 3, hoglin: 3, zoglin: 3,
  piglin_brute: 4,
  ravager: 6, elder_guardian: 6,
};
const OtherWeight = 1.5;
// A foe counts fully up to Close, less and less up to Far (0.4 of it there), not at all beyond.
const Close = 4;
const Far = 12;
const FarShare = 0.4;
// A get-out kind this close is reason enough.
const GetOutReach = 16;
// At or under this (3 hearts) she always falls back.
const LowHealth = 6;
// From falling back, she fights again only once the odds are this much better than even: no flipping every tick.
const BackToFight = 0.75;
const Helped = 1.5;

function foeWeight(name, distance) {
  if (GetOutKinds.has(name)) return distance <= GetOutReach ? Infinity : 0;
  if (distance > Far) return 0;
  const share = distance <= Close ? 1 : 1 - (1 - FarShare) * (distance - Close) / (Far - Close);
  return (KindWeights[name] ?? OtherWeight) * share;
}

// What her weapon's tier is worth, a bare hand being 1.
const TierFactors = { wooden: 1.2, golden: 1.2, copper: 1.5, stone: 1.5, iron: 2, diamond: 2.5, netherite: 3 };

function weaponFactor(weapon) {
  if (!weapon) return 1;
  if (weapon === 'trident') return 2;
  if (weapon === 'mace') return 2.5;
  const m = /^([a-z]+)_(sword|axe)$/.exec(weapon);
  return (m && TierFactors[m[1]]) || 1;
}

// How many zombies she can take on: two at full health, bare-handed and unarmoured.
function strength({ health, armour, weapon, helped }) {
  return 2 * (health / 20) * (1 + armour / 20) * weaponFactor(weapon) * (helped ? Helped : 1);
}

function sizeUp({ foes, health, armour, weapon, helped, previous }) {
  const score = foes.reduce((sum, f) => sum + foeWeight(f.name, f.distance), 0);
  if (score === Infinity) return 'getOut';
  if (health <= LowHealth) return 'fallBack';
  const can = strength({ health, armour, weapon, helped });
  if (previous === 'fallBack') return score <= BackToFight * can ? 'fight' : 'fallBack';
  return score <= can ? 'fight' : 'fallBack';
}

// Between two full-strength hits, by what she holds (the game's attack speeds). A bare hand or anything else: 250.
const AxeCooldowns = { wooden: 1250, stone: 1250, copper: 1250, iron: 1111, golden: 1000, diamond: 1000, netherite: 1000 };

function cooldownFor(item) {
  if (!item) return 250;
  if (item.endsWith('_sword')) return 625;
  if (item === 'trident') return 909;
  if (item === 'mace') return 1667;
  const axe = /^([a-z]+)_axe$/.exec(item);
  return (axe && AxeCooldowns[axe[1]]) || 250;
}

// Hurt by something that isn't a player: fight it back, or only get away from it. A tamed animal is someone's (Papa's
// wolf attacks her when he hits her), an iron golem is a fight she loses, and the animals that only hit back (bees,
// goats, llamas, polar bears, wolves) are no monsters.
function answerTo(name, { hostile, tamed }) {
  if (tamed || GetOutKinds.has(name)) return 'getOut';
  return hostile || NeutralFoes.has(name) ? 'fight' : 'getOut';
}

const Tau = 2 * Math.PI;
const norm = (a) => ((a % Tau) + Tau) % Tau;

// The gaps between the directions (radians, as seen from her), the widest first: [start, size].
function widestGap(angles) {
  const sorted = angles.map(norm).sort((a, b) => a - b);
  let best = [sorted[sorted.length - 1], Tau - sorted[sorted.length - 1] + sorted[0]];
  for (let i = 1; i < sorted.length; i++) {
    const size = sorted[i] - sorted[i - 1];
    if (size > best[1]) best = [sorted[i - 1], size];
  }
  return best;
}

// Three or more, and the narrowest arc holding them all is wider than a quarter circle.
function surrounded(angles) {
  return angles.length >= 3 && Tau - widestGap(angles)[1] > Math.PI / 2;
}

// The direction with no foe: the middle of the widest gap between them.
function openSide(angles) {
  const [start, size] = widestGap(angles);
  return norm(start + size / 2);
}

module.exports = {
  GetOutKinds, NeutralFoes, RangedKinds, foeWeight, strength, sizeUp, cooldownFor, answerTo, surrounded, openSide,
};
