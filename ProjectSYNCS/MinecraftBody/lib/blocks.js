// Block state ids she checks every step (farmland, water, bubble columns, what burns, what pricks, lava, air, fire).
// Ids, not blocks: the pathfinder asks for every step it weighs, and a whole block per step slowed the search past
// its limit. Filled once the version is known (fillStates, on the first spawn).

export let farmlandStates = new Set();
export let waterStates = new Set();
// Bubble columns (lifting over soul sand, dragging down over magma), and what else fills a column with water.
let bubbleStates = new Set();
export let liftStates = new Set();
export let dragStates = new Set();
export let wetStates = new Set();
// Floors that burn whoever stands on them: magma, a lit campfire. Not forbidden: a way across may need one.
export let burningStates = new Set();
// What hurts to brush against, and the cost of a step beside it.
export let prickly = new Set();
// Lava, and what a fall goes straight through (air): fightMovements keeps her off the lip of a drop or a lava pool.
export let lavaStates = new Set();
export let airStates = new Set();
export let fireStates = new Set();

export function fillStates(registry) {
  const states = (b) => new Set(Array.from({ length: b.maxStateId - b.minStateId + 1 }, (_, i) => b.minStateId + i));
  const statesOf = (names) => new Set(names.filter((n) => registry.blocksByName[n])
    .flatMap((n) => [...states(registry.blocksByName[n])]));
  farmlandStates = states(registry.blocksByName.farmland);
  waterStates = states(registry.blocksByName.water);
  const bubble = registry.blocksByName.bubble_column;
  if (bubble) {
    // drag=true is its first state (prismarine-physics reads it the same way: metadata 0 pulls down).
    bubbleStates = states(bubble);
    dragStates = new Set([bubble.minStateId]);
    liftStates = new Set([...bubbleStates].filter((id) => !dragStates.has(id)));
  }
  wetStates = new Set([...waterStates, ...bubbleStates, ...statesOf(['kelp', 'kelp_plant', 'seagrass', 'tall_seagrass'])]);
  burningStates = statesOf(['magma_block', 'campfire', 'soul_campfire']);
  prickly = statesOf(['sweet_berry_bush', 'cactus', 'wither_rose']);
  lavaStates = statesOf(['lava']);
  airStates = statesOf(['air', 'cave_air', 'void_air']);
  fireStates = statesOf(['fire', 'soul_fire']);
}
