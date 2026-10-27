// Papa and Tata by their in-game names, as C# sends them on spawn ({"op":"family"}): whose hits she watches for
// (defend.js), whom she tags along with and falls back to (tagalong.js, combat.js), whom she greets (gaze.js). Names
// only pick who: offline mode lets anyone take one.

// In C#'s order, Papa first, in lower case.
export let familyOrder = [];
let family = new Set();

export function setFamilyNames(names) {
  familyOrder = names.filter((n) => typeof n === 'string' && n.length > 0).map((n) => n.toLowerCase());
  family = new Set(familyOrder);
}

export function isFamily(name) {
  return Boolean(name) && family.has(String(name).toLowerCase());
}
