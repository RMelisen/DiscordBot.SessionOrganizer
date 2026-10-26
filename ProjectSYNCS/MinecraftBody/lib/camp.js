// The camp (« monte le camp », « démonte le camp »): the layout, the site, what she needs and gathers, placing it, the
// floor, and taking it down.

import pathfinderPackage from 'mineflayer-pathfinder';
import vec3 from 'vec3';
import { craftOne, craftPlanks, craftWooden } from './crafting.js';
import { FieldSweep } from './farm.js';
import { gather, gatherCandidates, gatherRounds } from './gather.js';
import { MaxTrip, stay } from './orders.js';
import { pickUp } from './pickup.js';
import { isNamed, isShorn, useOn } from './ranch.js';
import { dimension } from './seek.js';
import { bot, emit, log, setTask, task, wait } from './state.js';
import { fellTrees, treesNear } from './trees.js';

const { goals } = pathfinderPackage;
const { Vec3 } = vec3;

// « monte le camp » (Papa or Tata: C# checks): an open camp round a campfire, as CampLayout draws it: the fire in a pit
// of four trapdoors, two benches (a log on its side and two slabs) on two of its sides, the workstation (table, furnace,
// chest) on the third, a bed in a back corner, four torches on fence posts, and a floor of path and plain dirt trodden
// round it all. Nothing square on purpose (the owner's design, docs/superpowers/specs/2026-10-22-camp-design.md). All of
// it crafted, one craft at a time, from what she carries; what's missing she gathers: wood, stone (with a wooden pickaxe
// she makes), coal (else charcoal from logs, in the camp's own furnace). Wool only from shearing, never a sheep killed.
// Only on her own cells, never over anything; « démonte le camp » breaks only what she placed, still what she placed:
// the list goes to C#, which keeps it across restarts and sends it back. The floor is never in it, and stays. She keeps
// the tools she made (a wooden axe, so taking the camp down doesn't take minutes; a wooden shovel, for the path).
const CampSearch = 12; // how far from her a site may be
const CampTrapdoors = 4;
const CampSlabs = 4;
const CampBenchLogs = 2;
const CampFences = 4;
const CampTorches = 4;

// The camp from above, the back to the north: x east, z south from its north-west corner at her feet' level. F the
// campfire, d a trapdoor of its pit, L a bench's log, S its slabs, T the crafting table, K the furnace, C the chest, B
// the bed (its head to the back), l a torch on a fence post, p and D the floor (path or plain dirt, drawn anew for each
// camp: campFloor), . left as it is, not even checked: the camp sits into uneven ground.
const CampLayout = [
  '.l.......',
  '...LSS.Bl',
  '..pDpp.B.',
  '.LpDdppD.',
  '.SpdFdpT.',
  '.SDpdpDK.',
  '..pDppDC.',
  'l..pDp...',
  '....p..l.',
];

function campCells(marks) {
  const cells = [];
  CampLayout.forEach((row, z) => [...row].forEach((m, x) => { if (marks.includes(m)) cells.push([x, z]); }));
  return cells;
}

const At = (o, dx, dy, dz) => o.offset(dx, dy, dz);
const CampFire = [4, 0, 4];
const CampTable = [7, 0, 4];
const CampFurnace = [7, 0, 5];
const CampChest = [7, 0, 6];
const CampBed = [7, 0, 2]; // the foot: a bed is laid towards where she looks, so from CampBedStand its head is at (7, 1)
const CampBedStand = [7, 0, 3];
const CampWork = [6, 0, 5]; // by the workstation, where she crafts from; each of its blocks is set from its west, to face the fire
const CampWays = ['4,6', '4,7', '4,8']; // from the fire to the way in: always path
const CampPathShare = 0.6; // of the other floor cells
// What a full camp places, the floor and the bed aside (a torch post is two blocks).
const CampBlocks = campCells('FdLSTKC').length + 2 * campCells('l').length;

// What the game lets a block be placed over (its own « replaceable »): grass, leaf litter, bushes, a snow layer. Flowers,
// mushrooms and saplings are not, and a site with one is passed over: they may be someone's.
const CampOver = new Set(['air', 'cave_air', 'short_grass', 'tall_grass', 'fern', 'large_fern', 'dead_bush', 'leaf_litter', 'bush',
  'short_dry_grass', 'tall_dry_grass', 'vine', 'snow', 'glow_lichen']);

function campReplaceable(block) {
  return Boolean(block) && CampOver.has(block.name);
}

// Where a cell's ground is, from the site's level: 0, a block up or a block down (that order), with `high` blocks of
// room above it (0: the ground alone); null: none. Firm full ground only.
function campHeight(o, x, z, high) {
  for (const h of [0, 1, -1]) {
    const ground = bot.blockAt(At(o, x, h - 1, z));
    if (!ground || ground.boundingBox !== 'block' || /leaves|farmland|ice|_log$/.test(ground.name)) continue;
    let room = true;
    for (let dy = 0; dy < high && room; dy++) room = campReplaceable(bot.blockAt(At(o, x, h + dy, z)));
    if (room) return h;
  }
  return null;
}

// A site: one level, with room above, under the fire, its pit, the benches, the workstation and the bed; the torch
// posts and the floor follow the ground, a block up or down (campHeights), and the floor has no deeper drop: she stood
// in a two-deep hole in it to reach something, and never got out. The cells left alone aren't looked at. All of it at
// one level, a 9x9 with its corner posts, was rare outside a flat world: at a forest's edge, none in reach.
function campFits(o, standing = []) {
  if (standing.some(([x, z]) => /[FdLSTKCBl]/.test((CampLayout[z - o.z] || '')[x - o.x] || ''))) return false;
  if (!campCells('FdLSTKCB').every(([x, z]) => campHeight(o, x, z, 2) === 0)) return false;
  if (!campCells('l').every(([x, z]) => campHeight(o, x, z, 3) !== null)) return false;
  return campCells('pD').every(([x, z]) => campHeight(o, x, z, 0) !== null);
}

// Each post's and floor cell's ground, by "x,z", as found when the site is chosen (kept on the task: a fence set down
// would read as ground a block up). A floor cell with none (a flower on it, a drop) is left as it is: null.
function campHeights(o) {
  const heights = {};
  for (const [x, z] of campCells('l')) heights[`${x},${z}`] = campHeight(o, x, z, 3);
  for (const [x, z] of campCells('pD')) heights[`${x},${z}`] = campHeight(o, x, z, 1);
  return heights;
}

// The closest site within CampSearch, its fire nearest her, with nobody standing where a block goes: centred on her,
// its fire came under the feet of the one who asked, standing beside her, and the server refused it (a block isn't set
// in a player); null when there's none.
function campSite() {
  const me = bot.entity.position.floored();
  const standing = Object.values(bot.players).map((p) => p.entity).filter((e) => e && e !== bot.entity)
    .map((e) => [Math.floor(e.position.x), Math.floor(e.position.z)]);
  const sites = [];
  for (let dx = -CampSearch; dx <= CampSearch; dx++)
    for (let dz = -CampSearch; dz <= CampSearch; dz++)
      for (const dy of [0, -1, 1, -2, 2]) sites.push(me.offset(dx - CampFire[0], dy, dz - CampFire[2]));
  sites.sort((a, b) => At(a, ...CampFire).distanceTo(me) - At(b, ...CampFire).distanceTo(me));
  return sites.find((o) => campFits(o, standing)) || null;
}

// The floor's pattern: which cells are path and which plain dirt, about CampPathShare path, the way in always. Drawn
// once per camp and kept on the task, so a camp taken up again goes on with the same one.
function campFloor() {
  const cells = campCells('pD');
  const free = cells.filter(([x, z]) => !CampWays.includes(`${x},${z}`));
  for (let i = free.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [free[i], free[j]] = [free[j], free[i]];
  }
  const paths = new Set(free.slice(0, Math.round(free.length * CampPathShare)).map(([x, z]) => `${x},${z}`));
  return cells.map(([x, z]) => [x, z, CampWays.includes(`${x},${z}`) || paths.has(`${x},${z}`) ? 'path' : 'dirt']);
}

export const held = (match) => bot.inventory.items().filter((i) => match(i.name)).reduce((n, i) => n + i.count, 0);
export const isPlanks = (n) => n.endsWith('_planks');
export const isLog = (n) => n.endsWith('_log') && !n.startsWith('stripped_');
const isCoal = (n) => n === 'coal' || n === 'charcoal';
const hasItem = (match) => held(match) > 0;

// The colour she has three wool of, if any.
function woolColour() {
  const by = new Map();
  for (const i of bot.inventory.items()) if (i.name.endsWith('_wool')) by.set(i.name, (by.get(i.name) || 0) + i.count);
  const best = [...by].sort((a, b) => b[1] - a[1])[0];
  return best && best[1] >= 3 ? best[0].replace(/_wool$/, '') : null;
}

const campSheep = () => bot.nearestEntity((e) => e.name === 'sheep' && !isNamed(e) && !isShorn(e)
  && e.position.distanceTo(bot.entity.position) < 32);

// The bed she can have, best first: from wool she has, from wool sheared on the spot, a straw bed from hay or wheat she
// carries (never fetched: the wheat near her is someone's field); null: none, and the camp goes up without one (a
// camp is still a place to craft, cook and warm up). A straw bed is 3 hay bales (27 wheat) for 4 beds; her 26.1 client
// has no straw bed and sees it, in hand and set down, as a yellow bed (ViaBackwards), so that's the name it's crafted
// and placed by, through her recipe book only (minecraft-data doesn't know the recipe).
const StrawHay = 3;

function campBed() {
  if (woolColour()) return 'wool';
  if (hasItem((n) => n === 'shears') && campSheep()) return 'shear';
  if (held((n) => n === 'hay_block') * 9 + held((n) => n === 'wheat') >= StrawHay * 9) return 'straw';
  return null;
}

// Wooden things of any wood (an iron trapdoor or a nether brick fence is no camp's).
const WoodKind = /^(oak|spruce|birch|jungle|acacia|dark_oak|mangrove|cherry|pale_oak|bamboo|crimson|warped)_/;
const isWooden = (suffix) => (n) => n.endsWith(suffix) && WoodKind.test(n);

// What the camp still needs, from what she carries: the items to craft (a count where there are several) and the raw
// materials short. `placed`: what's already set down (a camp taken up again) counts as done.
function campNeeds(placed = [], bed = campBed()) {
  const on = (match) => placed.filter((b) => match(b.name)).length;
  const has = (match) => hasItem(match) || on(match) > 0;
  const left = (match, total) => Math.max(0, total - held(match) - on(match));
  const need = {
    table: !has((n) => n === 'crafting_table'),
    chest: !has((n) => n === 'chest'),
    furnace: !has((n) => n === 'furnace'),
    campfire: !has((n) => n === 'campfire'),
    trapdoors: left(isWooden('_trapdoor'), CampTrapdoors),
    slabs: left(isWooden('_slab'), CampSlabs),
    fences: left(isWooden('_fence'), CampFences),
    torches: left((n) => n === 'torch', CampTorches),
    bed: bed !== null && !has((n) => n.endsWith('_bed')),
    pickaxe: !hasItem((n) => n.endsWith('_pickaxe')),
    axe: !hasItem((n) => n.endsWith('_axe') && !n.endsWith('_pickaxe')),
    shovel: !hasItem((n) => n.endsWith('_shovel')),
  };
  // Crafts: 6 planks make 2 trapdoors, 3 make 6 slabs, 4 and 2 sticks make 3 fences, a coal and a stick 4 torches.
  const trapCrafts = Math.ceil(need.trapdoors / 2);
  const slabCrafts = Math.ceil(need.slabs / 6);
  const fenceCrafts = Math.ceil(need.fences / 3);
  const torchCrafts = Math.ceil(need.torches / 4);
  const woolBed = need.bed && bed !== 'straw';
  const sticks = fenceCrafts * 2 + torchCrafts + (need.campfire ? 3 : 0) + (need.pickaxe ? 2 : 0) + (need.axe ? 2 : 0) + (need.shovel ? 2 : 0);
  const stickCrafts = Math.ceil(Math.max(0, sticks - held((n) => n === 'stick')) / 4);
  const planks = (need.table ? 4 : 0) + (need.chest ? 8 : 0) + trapCrafts * 6 + slabCrafts * 3 + fenceCrafts * 4 + (woolBed ? 3 : 0)
    + (need.pickaxe ? 3 : 0) + (need.axe ? 3 : 0) + (need.shovel ? 1 : 0) + stickCrafts * 2;
  const coal = torchCrafts + (need.campfire ? 1 : 0);
  const coalShort = Math.max(0, coal - held(isCoal));
  const benchLogs = Math.max(0, CampBenchLogs - on(isLog));
  const logs = Math.ceil(Math.max(0, planks - held(isPlanks)) / 4) + benchLogs + (need.campfire ? 3 : 0) + coalShort; // charcoal at worst
  return {
    need,
    bed,
    planks,
    benchLogs,
    logs: Math.max(0, logs - held(isLog)),
    cobble: need.furnace ? Math.max(0, 8 - held((n) => n === 'cobblestone')) : 0,
    coalShort,
  };
}

// In reach of `pos` to place there, and not standing in it.
function campInReach(pos) {
  const me = bot.entity.position;
  const inIt = Math.floor(me.x) === pos.x && Math.floor(me.z) === pos.z && pos.y - Math.floor(me.y) >= 0 && pos.y - Math.floor(me.y) <= 1;
  return !inIt && me.offset(0, 1.62, 0).distanceTo(pos.offset(0.5, 0.5, 0.5)) <= CampReach;
}

// Places `match` (an item) at `cell` (from the camp's corner): from `stand` when given (the bed is laid the way she
// looks, the workstation turns to face her), else from wherever she can reach it. Against the block under it, or
// `against` (an offset): a log lies along the face it's set against, so a bench's log goes against its slab's side.
// True when the block there is then what was placed (or already was: started again).
async function campPlace(o, mine, cell, match, placed, stand = null, against = null) {
  const pos = At(o, ...cell);
  const now = bot.blockAt(pos);
  if (now && match(now.name)) return true;
  try {
    if (stand) {
      const s = At(o, ...stand);
      if (bot.entity.position.distanceTo(s.offset(0.5, 0, 0.5)) > 0.6) await bot.pathfinder.goto(new goals.GoalBlock(s.x, s.y, s.z));
    } else if (!campInReach(pos)) {
      await bot.pathfinder.goto(new goals.GoalPlaceBlock(pos, bot.world, { range: CampReach - 0.4 }));
    }
  } catch { /* placed from where she is, if in reach */ }
  if (task !== mine) return false;
  const item = bot.inventory.items().find((i) => match(i.name));
  const why = (what) => { log(`camp: nothing placed at ${cell.join(' ')}: ${what}`); return false; };
  if (!item) return why('nothing to place');
  const faces = against ? [against] : [[0, -1, 0], [-1, 0, 0], [1, 0, 0], [0, 0, -1], [0, 0, 1], [0, 1, 0]];
  const ref = faces.map(([dx, dy, dz]) => ({ block: bot.blockAt(pos.offset(dx, dy, dz)), face: new Vec3(-dx, -dy, -dz) }))
    .find((r) => r.block && r.block.boundingBox === 'block' && !/door|bed|chest|furnace|crafting|campfire|trapdoor/.test(r.block.name));
  if (!ref) return why('nothing to place it against');
  let failed = '';
  // Twice: just after a fight the sword or the bow can still be in her hand when the block is placed, and it's refused.
  for (let attempt = 0; attempt < 2; attempt++) {
    failed = '';
    try {
      await bot.equip(item, 'hand');
      await bot.placeBlock(ref.block, ref.face);
    } catch (err) { failed = err && err.message ? err.message : String(err); } // checked below: it often lands before its event
    await wait(150);
    const now2 = bot.blockAt(pos);
    if (now2 && match(now2.name)) break;
    await wait(300);
  }
  const after = bot.blockAt(pos);
  if (!after || !match(after.name)) return why(`${after ? after.name : '?'} there after placing ${item.name}${failed ? ` (${failed})` : ''}`);
  placed.push({ x: pos.x, y: pos.y, z: pos.z, name: after.name });
  return true;
}

// What fills a floor cell found dug out, best first: earth, else stone, else planks. And what she sets on a path for a
// moment to turn it to plain dirt (the game's rule: a path under a full block is dirt again), then takes back.
const CampFiller = (n) => ['dirt', 'coarse_dirt', 'rooted_dirt', 'cobblestone'].includes(n) || isPlanks(n);

// The ground the floor is trodden into: a path is made from any of these, plain dirt only from grass.
const Treadable = new Set(['grass_block', 'dirt', 'coarse_dirt', 'podzol', 'mycelium', 'rooted_dirt', 'dirt_path']);

// The floor, last, the nearest cell first: a path with the shovel, nothing placed; plain dirt the same, then a block
// set on it and taken back (CampFiller). Dug out and its dirt set back instead, the drop sometimes popped onto the table
// out of reach, and a dip's hole was two deep (she stepped in for the dirt and stayed down there): nothing is dug now.
// None of it goes in `placed` (a block set on a path is, while it's there: a fight in between, it comes down with the
// camp): the floor stays when the camp comes down. Ground that isn't earth (sand, stone, snow) is left as it is. A cell
// found dug out is filled, first and once more at the end.
async function campTread(o, mine) {
  const h = (x, z) => (mine.heights && `${x},${z}` in mine.heights ? mine.heights[`${x},${z}`] : 0);
  const cells = mine.floor.filter(([x, z]) => h(x, z) !== null)
    .map(([x, z, kind]) => ({ cell: [x, h(x, z) - 1, z], pos: At(o, x, h(x, z) - 1, z), kind }))
    .sort((a, b) => a.pos.distanceTo(bot.entity.position) - b.pos.distanceTo(bot.entity.position));
  const isDirt = (n) => n === 'dirt';
  // Hers only: whatever else lies about may be someone's, and a player's dropped things stay where they are.
  const lying = (pos, r, match) => (e) => e.name === 'item' && typeof e.getDroppedItem === 'function'
    && match((e.getDroppedItem() || {}).name || '') && e.position.distanceTo(pos.offset(0.5, 0.5, 0.5)) < r;
  const hole = (pos) => { const g = bot.blockAt(pos); return Boolean(g) && campReplaceable(g); };
  // Someone standing on it: nothing can be set there, a path it stays.
  const stoodOn = (pos) => Object.values(bot.players).some(({ entity: e }) => e && e !== bot.entity
    && Math.floor(e.position.x) === pos.x && Math.floor(e.position.z) === pos.z && Math.abs(e.position.y - (pos.y + 1)) < 2);
  const fill = async ({ cell, pos }) => {
    if (!hasItem(isDirt)) await pickUp(lying(pos, 6, isDirt), 8000, mine);
    if (task === mine && !(await campPlace(o, mine, cell, isDirt, [])) && task === mine && hole(pos))
      await campPlace(o, mine, cell, CampFiller, []);
  };
  const capOf = (above) => mine.placed.findIndex((b) => b.x === above.x && b.y === above.y && b.z === above.z);
  for (const c of cells) {
    if (task !== mine) return;
    const { pos } = c;
    const kind = c.kind === 'dirt' && stoodOn(pos) ? 'path' : c.kind;
    if (hole(pos)) { await fill(c); continue; }
    const ground = bot.blockAt(pos);
    const above = pos.offset(0, 1, 0);
    if (!ground || !Treadable.has(ground.name)) continue;
    const capped = capOf(above) >= 0; // set down before a fight took her off
    if (!capped && (kind === 'path' ? ground.name === 'dirt_path' : ground.name === 'dirt')) continue;
    if (!campInReach(above)) {
      try { await bot.pathfinder.goto(new goals.GoalPlaceBlock(above, bot.world, { range: CampReach - 0.4 })); } catch { /* from here */ }
      if (task !== mine) return;
    }
    try {
      const over = bot.blockAt(above);
      if (!capped && over && over.name !== 'air') await bot.dig(over, true); // short grass: a shovel needs the top bare
      if (bot.blockAt(pos).name !== 'dirt_path' && bot.blockAt(pos).name !== 'dirt') {
        const shovel = bot.inventory.items().find((i) => i.name.endsWith('_shovel'));
        if (!shovel) continue;
        await bot.equip(shovel, 'hand');
        await bot.activateBlock(bot.blockAt(pos));
      }
      if (kind !== 'dirt' || task !== mine) continue;
      if (!capped) {
        await wait(150);
        if (!(await campPlace(o, mine, [c.cell[0], c.cell[1] + 1, c.cell[2]], CampFiller, mine.placed))) continue;
      }
      for (let i = 0; i < 10 && bot.blockAt(pos).name !== 'dirt'; i++) await wait(100);
      const cap = bot.blockAt(above);
      if (task !== mine || !cap || !CampFiller(cap.name)) continue;
      const name = cap.name;
      if (bot.tool && bot.tool.equipForBlock) await bot.tool.equipForBlock(cap, {});
      await bot.dig(cap, true);
      const at = capOf(above);
      if (at >= 0) mine.placed.splice(at, 1);
      await pickUp(lying(above, 3, (n) => n === name), 3000, mine);
    } catch (err) {
      log('camp: floor:', err && err.message ? err.message : String(err));
    }
  }
  for (const c of cells) if (task === mine && hole(c.pos)) await fill(c);
}

// Charcoal from her logs in the camp's furnace, planks burning: `n` of them.
async function campCharcoal(o, mine, n) {
  const at = At(o, ...CampFurnace);
  let box;
  try {
    box = await bot.openFurnace(bot.blockAt(at));
    mine.window = box;
    const logs = bot.inventory.items().find((i) => isLog(i.name));
    const planks = bot.inventory.items().find((i) => isPlanks(i.name));
    if (!logs || !planks) return;
    await box.putFuel(planks.type, null, Math.min(planks.count, Math.ceil(n / 1.5)));
    await box.putInput(logs.type, null, Math.min(logs.count, n));
    const until = Date.now() + n * 12000 + 5000;
    let got = 0;
    while (task === mine && got < n && Date.now() < until) {
      await wait(1000);
      if (box.outputItem()) {
        got += box.outputItem().count;
        await box.takeOutput();
      }
    }
  } catch (err) {
    log('camp: charcoal failed:', err && err.message ? err.message : String(err));
  } finally {
    if (box) try { box.close(); } catch { /* closed */ }
    mine.window = null;
  }
}

// `resume`: taken up again after a fight or a creeper (restartOrder), at the same site with what was already placed and
// the same floor: a new search would have started a second camp beside the half-built one. Everything placed is skipped
// (campPlace).
export async function makeCamp(player, resume = null) {
  const again = resume && resume.site;
  // What's still there of what she placed: a creeper may have blown some of it away, and counted as done it was never
  // made again (a table gone ended the camp).
  const stands = (b) => { const now = bot.blockAt(new Vec3(b.x, b.y, b.z)); return !now || now.name === b.name; };
  const placed = again && Array.isArray(resume.placed) ? resume.placed.filter(stands) : [];
  if (!again && campNeeds(placed).logs > 0 && treesNear().length === 0) return emit('cantCamp', { player, why: 'missing', missing: ['wood'] });
  const o = again ? new Vec3(resume.site.x, resume.site.y, resume.site.z) : campSite();
  if (!o) return emit('cantCamp', { player, why: 'noRoom' });
  stay();
  // Interrupted, what to take up again: this site, what's on it and the floor's pattern (resumeFor).
  const mine = { kind: 'camp', player, window: null, site: { x: o.x, y: o.y, z: o.z }, placed,
    floor: again && Array.isArray(resume.floor) ? resume.floor : campFloor(),
    heights: again && resume.heights && typeof resume.heights === 'object' ? resume.heights : campHeights(o) };
  setTask(mine);
  if (!again) emit('camping', { player });
  const fail = (why, missing = []) => {
    if (task !== mine) return;
    setTask(null);
    emit('cantCamp', { player, why, missing, blocks: placed });
  };
  const work = async () => {
    const s = At(o, ...CampWork);
    try { await bot.pathfinder.goto(new goals.GoalBlock(s.x, s.y, s.z)); } catch { /* close enough */ }
  };

  // Nothing escapes: a throw left her on a dead task, the order unanswered, and the blocks she had placed unknown to C#
  // (nothing to take down later). It ends the camp as a failed craft would, with what's on the ground.
  try {
    // Wool, sheared, before anything else is cut or crafted.
    for (let n = 0; n < 6 && task === mine && campNeeds(placed).need.bed && campBed() === 'shear'; n++) {
      const sheep = campSheep();
      await useOn(sheep, (x) => x === 'shears');
      await wait(600);
      await pickUp((e) => e.position.distanceTo(sheep.position) < 4, 6000, mine);
    }
    // Wood, all of it at once: planks for the crafting, logs for the benches, the campfire and charcoal. Again while
    // there are trees in reach: a plain's trees are scattered, and a pass whose drops she missed brought nothing in, which
    // ended the camp (« missing wood ») one log short at a forest's edge.
    for (let pass = 0; pass < 6 && task === mine && campNeeds(placed).logs > 0; pass++) {
      const trees = treesNear();
      if (trees.length === 0) break;
      await fellTrees(trees, campNeeds(placed).logs, mine);
    }
    if (task !== mine) return;
    if (campNeeds(placed).logs > 0) return fail('missing', ['wood']);
    await work();
    const plan = campNeeds(placed);
    await craftPlanks(plan.planks + 4, plan.benchLogs + (plan.need.campfire ? 3 : 0) + plan.coalShort, mine);
    if (task !== mine) return;
    // The wood she has most of: the trapdoors, the slabs, the fences, and the benches' logs when she has them, all one.
    const kind = bot.inventory.items().filter((i) => isPlanks(i.name)).sort((a, b) => b.count - a.count)[0];
    const wood = kind ? kind.name.replace(/_planks$/, '') : 'oak';
    const benchLog = (n) => (hasItem((x) => x === `${wood}_log`) || placed.some((b) => b.name === `${wood}_log`) ? n === `${wood}_log` : isLog(n));
    if (plan.need.table && !(await craftOne('crafting_table'))) return fail('craft', ['crafting_table']);
    if (!(await campPlace(o, mine, CampTable, (n) => n === 'crafting_table', placed, [6, 0, 4]))) return fail('place', ['crafting_table']);
    const table = bot.blockAt(At(o, ...CampTable));
    const sticks = async (n) => { while (task === mine && held((x) => x === 'stick') < n && (await craftOne('stick'))); };
    const several = async (name, crafts) => { for (let i = 0; i < crafts && task === mine; i++) if (!(await craftOne(name, table))) return; };
    // Tools, then stone and coal.
    if (campNeeds(placed).need.pickaxe) { await sticks(2); await craftOne('wooden_pickaxe', table); }
    if (campNeeds(placed).need.axe) { await sticks(2); await craftOne('wooden_axe', table); }
    if (campNeeds(placed).need.shovel) { await sticks(2); await craftOne('wooden_shovel', table); }
    if (task === mine && campNeeds(placed).cobble > 0) await gatherRounds('stone', campNeeds(placed).cobble, mine);
    if (task === mine && campNeeds(placed).coalShort > 0 && gatherCandidates('coal').open.length > 0) await gatherRounds('coal', campNeeds(placed).coalShort, mine);
    if (task !== mine) return;
    if (campNeeds(placed).cobble > 0) return fail('missing', ['stone']);
    await work();
    // What's crafted at the table.
    const now = campNeeds(placed);
    if (now.need.chest) await craftOne('chest', table);
    if (now.need.furnace) await craftOne('furnace', table);
    if (now.need.bed && woolColour()) await craftOne(`${woolColour()}_bed`, table);
    else if (now.need.bed && now.bed === 'straw') {
      while (task === mine && held((n) => n === 'hay_block') < StrawHay && (await craftOne('hay_block', table)));
      await craftOne('yellow_bed', table);
    }
    const keep = () => { const n = campNeeds(placed); return n.benchLogs + (n.need.campfire ? 3 : 0) + n.coalShort; };
    const wooden = async (suffix, per, crafts) => {
      for (let i = 0; i < crafts && task === mine; i++) if (!(await craftWooden(suffix, per, wood, table, keep))) return;
    };
    await wooden('_trapdoor', 6, Math.ceil(now.need.trapdoors / 2));
    await wooden('_slab', 3, Math.ceil(now.need.slabs / 6));
    if (now.need.fences > 0) { await sticks(2 * Math.ceil(now.need.fences / 3)); await wooden('_fence', 4, Math.ceil(now.need.fences / 3)); }
    if (task !== mine) return;
    // The rest of the workstation, then charcoal in its furnace when there was no coal.
    await campPlace(o, mine, CampFurnace, (n) => n === 'furnace', placed, [6, 0, 5]);
    await campPlace(o, mine, CampChest, (n) => n === 'chest', placed, [6, 0, 6]);
    if (task === mine && campNeeds(placed).coalShort > 0) await campCharcoal(o, mine, campNeeds(placed).coalShort);
    if (task !== mine) return;
    const lit = campNeeds(placed).need;
    if (lit.torches > 0 || lit.campfire) await sticks(Math.ceil(lit.torches / 4) + (lit.campfire ? 3 : 0));
    if (lit.torches > 0) await several('torch', Math.ceil(lit.torches / 4));
    if (lit.campfire) await craftOne('campfire', table);
    if (task !== mine) return;
    // The bed, the fire in its pit, the benches (slabs first: the log lies along the slab it's set against), the posts.
    // Twice: a block the server refused (a sheep standing in its cell, in the wild) is tried again once the rest is
    // down; what's already there is passed over.
    const isBed = (n) => n.endsWith('_bed');
    const post = (x, z) => mine.heights[`${x},${z}`] || 0;
    let bedOk = false;
    for (let round = 0; round < 2 && task === mine; round++) {
      if (round > 0) await wait(2000);
      bedOk = (hasItem(isBed) || placed.some((b) => isBed(b.name))) && (await campPlace(o, mine, CampBed, isBed, placed, CampBedStand));
      for (const [x, z] of campCells('d')) await campPlace(o, mine, [x, 0, z], isWooden('_trapdoor'), placed);
      await campPlace(o, mine, CampFire, (n) => n === 'campfire', placed);
      for (const [x, z] of campCells('S')) await campPlace(o, mine, [x, 0, z], isWooden('_slab'), placed);
      for (const [x, z] of campCells('L')) {
        const slab = campCells('S').find(([sx, sz]) => Math.abs(sx - x) + Math.abs(sz - z) === 1);
        await campPlace(o, mine, [x, 0, z], benchLog, placed, null, slab ? [slab[0] - x, 0, slab[1] - z] : null);
      }
      for (const [x, z] of campCells('l')) await campPlace(o, mine, [x, post(x, z), z], isWooden('_fence'), placed);
      for (const [x, z] of campCells('l')) await campPlace(o, mine, [x, post(x, z) + 1, z], (n) => n === 'torch', placed);
      if (placed.filter((b) => !isBed(b.name)).length >= CampBlocks && (bedOk || !hasItem(isBed))) break;
    }
    if (task !== mine) return;
    await campTread(o, mine);
    if (task !== mine) return;
    setTask(null);
    emit('camped', { player, blocks: placed, complete: placed.filter((b) => !b.name.endsWith('_bed')).length >= CampBlocks,
      bed: bedOk && placed.some((b) => b.name.endsWith('_bed')), x: o.x + CampFire[0], y: o.y, z: o.z + CampFire[2], dimension: dimension() });
  } catch (err) {
    log('camp failed:', err && err.message ? err.message : String(err));
    fail('craft');
  }
}

// « démonte le camp »: the blocks C# kept, each broken only if it's still what she placed there: the torches first
// (they'd drop off their posts), then whatever is nearest from where she stands, out of reach through collectblock;
// then what fell, the chest's contents with it. The floor isn't in the list, and stays. She keeps it all (a campfire
// broken without silk touch gives two charcoal, not itself: the game's rule).
const CampReach = 4.4;

async function campDig(block, mine) {
  if (task !== mine) return false;
  const eyes = bot.entity.position.offset(0, 1.62, 0);
  if (eyes.distanceTo(block.position.offset(0.5, 0.5, 0.5)) > CampReach) {
    await gather([block], mine, 'uncamp'); // out of reach from here: collectblock finds a way
  } else {
    try {
      if (bot.tool && bot.tool.equipForBlock) await bot.tool.equipForBlock(block, {});
      await bot.dig(block, true);
    } catch (err) {
      log('uncamp: dig failed:', err && err.message ? err.message : String(err));
    }
  }
  const now = bot.blockAt(block.position);
  return !now || now.name !== block.name;
}

export async function breakCamp(player, blocks, dim, center) {
  if (!Array.isArray(blocks) || blocks.length === 0) return emit('cantUncamp', { player, why: 'none' });
  if (dim && dim !== dimension()) return emit('cantUncamp', { player, why: 'otherDimension' });
  const mid = center && Number.isFinite(center.x) ? new Vec3(center.x, center.y, center.z)
    : new Vec3(Math.floor(blocks.reduce((a, b) => a + b.x, 0) / blocks.length), blocks[0].y, Math.floor(blocks.reduce((a, b) => a + b.z, 0) / blocks.length));
  if (Math.hypot(mid.x - bot.entity.position.x, mid.z - bot.entity.position.z) > MaxTrip) return emit('cantUncamp', { player, why: 'tooFar' });
  stay();
  const mine = { kind: 'uncamp', player };
  setTask(mine);
  emit('uncamping', { player });
  const still = () => blocks.map((b) => bot.blockAt(new Vec3(b.x, b.y, b.z))).filter((now, i) => now && now.name === blocks[i].name);
  const was = still().length;
  for (const b of still().filter((x) => x.name === 'torch')) await campDig(b, mine);
  const near = (a, b) => a.position.distanceTo(bot.entity.position) - b.position.distanceTo(bot.entity.position);
  for (let i = 0; i < was * 2 && task === mine; i++) {
    const next = still().sort(near)[0];
    if (!next) break;
    await campDig(next, mine);
  }
  // Whatever is left, any way it can be got at.
  for (let round = 0; round < 2 && task === mine && still().length > 0; round++) await gather(still(), mine, 'uncamp');
  if (task !== mine) return;
  await wait(800); // the last drops land
  await pickUp((e) => e.name === 'item' && e.position.distanceTo(mid) <= 9, FieldSweep, mine);
  if (task !== mine) return;
  setTask(null);
  emit('uncamped', { player, count: was - still().length, left: still().length });
}
