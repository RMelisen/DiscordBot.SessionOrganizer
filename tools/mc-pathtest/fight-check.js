// The pure half of her fights (ProjectSYNCS/MinecraftBody/lib/fight.js): sizing a fight up, weapon cooldowns, who she may
// hit, whether she's surrounded. No server needed:
//   node --test fight-check.js
const test = require('node:test');
const assert = require('node:assert/strict');
const fight = require('../../ProjectSYNCS/MinecraftBody/lib/fight.js'); // an ES module (Node 22.12 or later)

const zombies = (n, at) => Array.from({ length: n }, () => ({ name: 'zombie', distance: at }));
const me = (over = {}) => ({ health: 20, armour: 0, weapon: null, helped: false, previous: null, ...over });

test('one zombie, bare-handed at full health: she fights', () => {
  assert.equal(fight.sizeUp({ foes: zombies(1, 2), ...me() }), 'fight');
});

test('three zombies, bare-handed: she falls back', () => {
  assert.equal(fight.sizeUp({ foes: zombies(3, 3), ...me() }), 'fallBack');
});

test('three zombies with an iron sword and iron armour: she fights', () => {
  assert.equal(fight.sizeUp({ foes: zombies(3, 3), ...me({ weapon: 'iron_sword', armour: 15 }) }), 'fight');
});

test('three zombies, bare-handed, beside her player: she fights', () => {
  assert.equal(fight.sizeUp({ foes: zombies(3, 3), ...me({ helped: true }) }), 'fight');
});

test('a warden at 14: she gets out, whatever she carries', () => {
  const foes = [{ name: 'warden', distance: 14 }];
  assert.equal(fight.sizeUp({ foes, ...me({ weapon: 'netherite_sword', armour: 20 }) }), 'getOut');
});

test('at 3 hearts she falls back, even against one zombie with a diamond kit', () => {
  assert.equal(fight.sizeUp({ foes: zombies(1, 2), ...me({ health: 6, weapon: 'diamond_sword', armour: 20 }) }), 'fallBack');
});

test('between the two bars, she keeps what she was doing', () => {
  // Bare-handed at full health her strength is 2: two zombies close (score 2) is a fight, and 1.75 sits between 0.75 × 2
  // and 2. A zombie at 4 weighs 1, one at 8 weighs 0.7 (linear from 1 at 4 to 0.4 at 12): 1 + 0.7 = 1.7.
  const foes = [{ name: 'zombie', distance: 4 }, { name: 'zombie', distance: 8 }];
  assert.equal(fight.sizeUp({ foes, ...me({ previous: 'fallBack' }) }), 'fallBack');
  assert.equal(fight.sizeUp({ foes, ...me({ previous: 'fight' }) }), 'fight');
});

test('a zombie further than 12 adds nothing', () => {
  const near = zombies(2, 2);
  for (const previous of [null, 'fight', 'fallBack']) {
    assert.equal(fight.sizeUp({ foes: [...near, { name: 'zombie', distance: 13 }], ...me({ previous, health: 14 }) }),
      fight.sizeUp({ foes: near, ...me({ previous, health: 14 }) }));
  }
  assert.equal(fight.foeWeight('zombie', 13), 0);
});

test('weapon cooldowns', () => {
  assert.equal(fight.cooldownFor('iron_sword'), 625);
  assert.equal(fight.cooldownFor('stone_axe'), 1250);
  assert.equal(fight.cooldownFor('iron_axe'), 1111);
  assert.equal(fight.cooldownFor('diamond_axe'), 1000);
  assert.equal(fight.cooldownFor('trident'), 909);
  assert.equal(fight.cooldownFor('mace'), 1667);
  assert.equal(fight.cooldownFor(null), 250);
  assert.equal(fight.cooldownFor('stick'), 250);
});

test('who she fights back and who she only gets away from', () => {
  assert.equal(fight.answerTo('zombie', { hostile: true, tamed: false }), 'fight');
  assert.equal(fight.answerTo('enderman', { hostile: false, tamed: false }), 'fight');
  assert.equal(fight.answerTo('wolf', { hostile: false, tamed: true }), 'getOut');
  assert.equal(fight.answerTo('iron_golem', { hostile: false, tamed: false }), 'getOut');
  assert.equal(fight.answerTo('warden', { hostile: true, tamed: false }), 'getOut');
  assert.equal(fight.answerTo('bee', { hostile: false, tamed: false }), 'getOut');
});

test('surrounded: three or more, spread over more than a quarter circle', () => {
  assert.equal(fight.surrounded([0, 2.1, 4.2]), true);
  assert.equal(fight.surrounded([0, 0.3, 0.6]), false);
  assert.equal(fight.surrounded([0, Math.PI]), false);
  assert.equal(fight.surrounded([6.1, 0.1, 0.3]), false);
});

test('the open side is the middle of the widest gap', () => {
  const side = fight.openSide([0, Math.PI / 2, Math.PI]);
  const tau = 2 * Math.PI;
  assert.ok(Math.abs(((side % tau) + tau) % tau - 3 * Math.PI / 2) < 1e-9, `got ${side}`);
});

test('a sword sweep reaches a player beside the target: its box grown by 1, a quarter up and down', () => {
  assert.equal(fight.sweepReaches({ dx: 1.5, dy: 0, dz: 1.5 }), true); // diagonal, 2.1 off
  assert.equal(fight.sweepReaches({ dx: 0, dy: 1, dz: 0.5 }), true); // a block higher
  assert.equal(fight.sweepReaches({ dx: 1.7, dy: 0, dz: 0 }), false);
  assert.equal(fight.sweepReaches({ dx: 0, dy: 2.3, dz: 0 }), false);
  assert.equal(fight.sweepReaches({ dx: 0, dy: -2.1, dz: 0 }), false);
});
