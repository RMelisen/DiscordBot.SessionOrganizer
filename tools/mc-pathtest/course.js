// The obstacle course: one lane per kind of terrain, built over RCON on a flat world (ground top at y -61, so
// she stands at y -60). Each lane runs east along +x from its own z; `start` and `goal` are lane-relative
// [dx, dy, dz] (dy 0 = standing on the ground). `expect` is 'arrive' unless the goal can't be reached.
//
// Lane-relative helpers: f(dx1, dy1, dz1, dx2, dy2, dz2, block) fills, b(dx, dy, dz, block) sets one block.

const X0 = 100;
const Z0 = 100;
const LaneGap = 10;
const Length = 24;

const lanes = [
  {
    name: 'flat', what: 'control: open ground',
    build() {}, start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'corridor', what: '1-wide corridor, walls both sides (brushing)',
    build({ f }) { f(3, 0, -1, 19, 2, -1, 'stone'); f(3, 0, 1, 19, 2, 1, 'stone'); },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 's-bend', what: '1-wide corridor with two right-angle turns',
    build({ f }) {
      f(3, 0, -1, 19, 2, 4, 'stone');
      f(3, 0, 0, 9, 1, 0, 'air'); f(9, 0, 0, 9, 1, 3, 'air'); f(9, 0, 3, 19, 1, 3, 'air');
    },
    start: [1, 0, 0], goal: [22, 0, 3],
  },
  {
    name: 'pillars', what: '3-wide lane with staggered pillars: weaving past corners',
    build({ f, b }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone');
      for (const [dx, dz] of [[5, -1], [5, 1], [7, 0], [9, -1], [9, 1], [11, 0], [13, -1], [13, 1], [15, 0], [17, -1], [17, 1]])
        f(dx, 0, dz, dx, 2, dz, 'stone');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'stairs-slabs', what: 'stairs up three, a platform, down by slabs and half steps',
    build({ f, b }) {
      f(3, 0, -1, 19, 3, -1, 'stone'); f(3, 0, 1, 19, 3, 1, 'stone');
      b(5, 0, 0, 'oak_stairs[facing=east]');
      b(6, 0, 0, 'stone'); b(6, 1, 0, 'oak_stairs[facing=east]');
      f(7, 0, 0, 7, 1, 0, 'stone'); b(7, 2, 0, 'oak_stairs[facing=east]');
      f(8, 0, 0, 10, 2, 0, 'stone');
      f(11, 0, 0, 11, 1, 0, 'stone'); b(11, 2, 0, 'stone_slab[type=bottom]');
      f(12, 0, 0, 12, 1, 0, 'stone');
      b(13, 0, 0, 'stone'); b(13, 1, 0, 'stone_slab[type=bottom]');
      b(14, 0, 0, 'stone');
      b(15, 0, 0, 'stone_slab[type=bottom]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'slab-climb', what: 'climbing by half blocks without a full step',
    build({ f, b }) {
      f(3, 0, -1, 19, 4, -1, 'stone'); f(3, 0, 1, 19, 4, 1, 'stone');
      b(5, 0, 0, 'stone_slab[type=bottom]');
      b(6, 0, 0, 'stone');
      b(7, 0, 0, 'stone'); b(7, 1, 0, 'stone_slab[type=bottom]');
      f(8, 0, 0, 8, 1, 0, 'stone');
      f(9, 0, 0, 12, 1, 0, 'stone'); f(9, 2, 0, 12, 2, 0, 'stone_slab[type=bottom]');
      f(13, 0, 0, 13, 1, 0, 'stone');
      b(14, 0, 0, 'stone');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'fences', what: 'a fence and a wall across the lane, gaps at opposite ends',
    build({ f }) {
      f(3, 0, -4, 19, 1, -4, 'cobblestone_wall'); f(3, 0, 4, 19, 1, 4, 'cobblestone_wall');
      f(8, 0, -3, 8, 0, 2, 'oak_fence'); f(14, 0, -2, 14, 0, 3, 'cobblestone_wall');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'decorations', what: 'carpet, snow, plates, pots, lanterns, candles, plants, a berry bush',
    build({ f, b }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone');
      f(4, 0, -1, 5, 0, 1, 'white_carpet');
      f(6, 0, -1, 6, 0, 1, 'snow[layers=1]'); f(7, 0, -1, 7, 0, 1, 'snow[layers=3]'); f(8, 0, -1, 8, 0, 1, 'snow[layers=5]');
      f(9, 0, -1, 9, 0, 1, 'oak_pressure_plate');
      b(10, 0, 0, 'flower_pot'); b(11, 0, 1, 'lantern'); b(12, 0, -1, 'candle[candles=3]');
      f(13, 0, -1, 14, 0, 1, 'short_grass'); b(13, 0, 0, 'poppy'); b(14, 0, 1, 'dead_bush');
      b(15, 0, 0, 'sea_pickle[pickles=2,waterlogged=false]'); b(16, 0, -1, 'fern'); f(17, 0, -1, 17, 0, 1, 'moss_carpet');
      b(18, 0, 1, 'sweet_berry_bush[age=3]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'trapdoors', what: 'closed trapdoors on the floor and up high, open ones on the sides and across',
    build({ f, b }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone');
      f(5, 0, -1, 5, 0, 1, 'oak_trapdoor[half=bottom,open=false]');
      f(7, 0, -1, 7, 0, 1, 'oak_trapdoor[half=top,open=false]');
      b(9, 0, -1, 'oak_trapdoor[facing=south,open=true]'); b(9, 0, 1, 'oak_trapdoor[facing=north,open=true]');
      b(11, 0, 0, 'oak_trapdoor[facing=east,open=true]');
      f(13, 1, -1, 13, 1, 1, 'oak_trapdoor[half=top,open=false]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'gaps', what: 'a 1-wide trench, then a 2-wide one with water at the bottom',
    build({ f }) {
      f(3, 0, -2, 19, 1, -2, 'stone'); f(3, 0, 2, 19, 1, 2, 'stone');
      f(8, -1, -1, 8, -3, 1, 'air');
      f(12, -1, -1, 13, -3, 1, 'air'); f(12, -3, -1, 13, -3, 1, 'water');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'tunnel', what: '2-high tunnel, part of it too low (a slab ceiling) to pass',
    build({ f }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone'); f(3, 2, -1, 19, 2, 1, 'stone');
      f(9, 1, -1, 12, 1, 0, 'stone_slab[type=top]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'thin', what: 'glass panes and iron bars across, gaps at alternate ends',
    build({ f }) {
      f(3, 0, -3, 19, 1, -3, 'stone'); f(3, 0, 3, 19, 1, 3, 'stone');
      f(6, 0, -2, 6, 1, 1, 'glass_pane'); f(10, 0, -1, 10, 1, 2, 'iron_bars'); f(14, 0, -2, 14, 1, 1, 'glass_pane');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'furniture', what: 'chests to climb, a bed, a dirt path and bare farmland',
    build({ f, b }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone');
      f(5, 0, -1, 5, 0, 1, 'chest[facing=west]');
      b(9, 0, 0, 'red_bed[part=foot,facing=east]'); b(10, 0, 0, 'red_bed[part=head,facing=east]');
      f(13, -1, -1, 14, -1, 1, 'dirt_path'); f(15, -1, -1, 16, -1, 1, 'farmland[moisture=7]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'slow', what: 'soul sand, honey, a cobweb to avoid, powder snow to avoid',
    build({ f, b }) {
      f(3, 0, -2, 19, 2, -2, 'stone'); f(3, 0, 2, 19, 2, 2, 'stone');
      f(5, -1, -1, 7, -1, 1, 'soul_sand'); f(9, -1, -1, 10, -1, 1, 'honey_block');
      f(13, 0, -1, 13, 1, 0, 'cobweb'); f(16, -1, 0, 16, -1, 1, 'powder_snow');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'door-turn', what: 'a door in a 1-wide corridor with a turn right after, then a gate and a step',
    build({ f, b }) {
      f(3, 0, -1, 19, 2, 4, 'stone');
      f(3, 0, 0, 10, 1, 0, 'air'); f(10, 0, 0, 10, 1, 3, 'air'); f(10, 0, 3, 19, 1, 3, 'air');
      b(7, 0, 0, 'oak_door[half=lower,facing=east,hinge=left]'); b(7, 1, 0, 'oak_door[half=upper,facing=east,hinge=left]');
      b(13, 0, 3, 'oak_fence_gate[facing=east]'); b(16, 0, 3, 'stone');
    },
    start: [1, 0, 0], goal: [22, 0, 3],
  },
  {
    name: 'ladder-ledge', what: 'a ladder up onto a slab-topped ledge 2.5 high, then a drop of 2.5',
    build({ f }) {
      f(3, 0, -1, 19, 4, -1, 'stone'); f(3, 0, 1, 19, 4, 1, 'stone');
      f(9, 0, 0, 12, 1, 0, 'stone'); f(9, 2, 0, 12, 2, 0, 'stone_slab[type=bottom]');
      f(8, 0, 0, 8, 2, 0, 'ladder[facing=west]');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'leaves', what: 'a hedge of leaves with one gap, an azalea in the way',
    build({ f, b }) {
      f(3, 0, -3, 19, 1, -3, 'stone'); f(3, 0, 3, 19, 1, 3, 'stone');
      f(9, 0, -2, 9, 1, 1, 'oak_leaves[persistent=true]'); b(14, 0, 0, 'azalea');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'water', what: 'a 1-deep pool, then a 2-deep one, walls on the sides',
    build({ f }) {
      f(3, 0, -3, 19, 1, -3, 'stone'); f(3, 0, 3, 19, 1, 3, 'stone');
      f(6, -1, -2, 9, -1, 2, 'water'); f(12, -2, -2, 15, -1, 2, 'water');
    },
    start: [1, 0, 0], goal: [22, 0, 0],
  },
  {
    name: 'scatter', what: 'a field of single pillars crossed on the diagonal (corners everywhere)',
    build({ f }) {
      // Deterministic: the same field every run.
      let seed = 7;
      const rand = () => ((seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648);
      for (let dx = 3; dx <= 19; dx++)
        for (let dz = -4; dz <= 4; dz++)
          if ((dx + dz) % 2 === 0 && rand() < 0.45 && !(dx <= 3 && dz <= -3)) f(dx, 0, dz, dx, 1, dz, 'stone');
    },
    start: [1, 0, -4], goal: [22, 0, 4],
  },
  {
    name: 'unreachable', what: 'the goal is on top of a 3-high pillar: she must give up, not hang',
    build({ f }) { f(12, 0, 0, 12, 2, 0, 'stone'); },
    start: [1, 0, 0], goal: [12, 3, 0], expect: 'refuse',
  },
];

// The whole course's block commands, lane by lane: ground and air reset first, then the lane's own blocks.
function commands() {
  const cmds = [];
  lanes.forEach((lane, i) => {
    const z = Z0 + i * LaneGap;
    const at = (dx, dy, dz) => `${X0 + dx} ${-60 + dy} ${z + dz}`;
    cmds.push(`fill ${at(0, -4, -5)} ${at(Length, -2, 5)} stone`);
    cmds.push(`fill ${at(0, -1, -5)} ${at(Length, -1, 5)} grass_block`);
    cmds.push(`fill ${at(0, 0, -5)} ${at(Length, 5, 5)} air`);
    lane.build({
      f: (x1, y1, z1, x2, y2, z2, block) => cmds.push(`fill ${at(x1, y1, z1)} ${at(x2, y2, z2)} ${block}`),
      b: (x, y, zz, block) => cmds.push(`setblock ${at(x, y, zz)} ${block}`),
    });
  });
  return cmds;
}

// A lane-relative point to world coordinates (block centre on x/z).
function world(i, [dx, dy, dz]) {
  return { x: X0 + dx + 0.5, y: -60 + dy, z: Z0 + i * LaneGap + dz + 0.5 };
}

const Area = { x1: X0 - 8, z1: Z0 - 8, x2: X0 + Length + 8, z2: Z0 + lanes.length * LaneGap + 8 };

module.exports = { lanes, commands, world, Area };
