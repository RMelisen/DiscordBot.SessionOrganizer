// Small things answered at once: where she is, a marked spot, sitting, hopping, waving.

import { playerEntity, stay } from './orders.js';
import { dimension } from './seek.js';
import { bot, emit, wait } from './state.js';

export let sitting = false;

export function setSitting(value) {
  sitting = value;
}

export function where(player) {
  const p = (bot.vehicle || bot.entity).position; // riding, her own position isn't updated: the boat's is
  emit('position', { player, x: Math.floor(p.x), y: Math.floor(p.y), z: Math.floor(p.z), dimension: dimension() });
}

// Where the player stands, for a place they want remembered.
export function mark(player) {
  const target = playerEntity(player);
  if (!target) return emit('cantMark', { player });
  const p = target.position;
  emit('marked', { player, x: Math.floor(p.x), y: Math.floor(p.y), z: Math.floor(p.z), dimension: dimension() });
}

export function sit() {
  stay();
  sitting = true;
  bot.setControlState('sneak', true);
}

export async function hop(times) {
  for (let i = 0; i < times; i++) {
    bot.setControlState('jump', true);
    await wait(250);
    bot.setControlState('jump', false);
    await wait(350);
  }
}

// The players' hello: look at them, wave, and crouch up and down.
export async function wave(player) {
  const target = playerEntity(player);
  if (target) await bot.lookAt(target.position.offset(0, 1.6, 0)).catch(() => {});
  for (let i = 0; i < 3; i++) {
    bot.swingArm('right');
    bot.setControlState('sneak', true);
    await wait(200);
    bot.setControlState('sneak', sitting);
    await wait(200);
  }
}
