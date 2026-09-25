"""The idle loop every Plynling shares: one beat of 16 frames at 125 ms (2 s), so every species
breathes, blinks and fidgets in time with the others.

pose(sp, state, f) says where everything is on frame f. Frame 0 is always the rest pose — the
still sprite exactly — so a client that shows only the first frame of the animation shows the
picture it always did.
"""
import math

from common import N, Grid

FRAMES = 16
FRAME_MS = 125


def _within(f, a, b):
    return a <= f <= b


class Pose:
    """Where everything sits on one frame. Every field is 0 / False / None at rest."""

    def __init__(self, sp, state, f):
        self.f = f
        self.state = state
        # species touches play in every mood but these three: too weak, frozen solid, asleep
        self.touch = state not in ("starving", "frozen", "sleeping")
        still = state == "frozen"

        # Nothing moves side to side: every motion here is up and down, or in place.
        # the breath: the body sinks one row for most of the second half of the beat;
        # starving has no breath, only a weak shudder — the same sink, one frame at a time
        if state == "starving":
            self.bob = 1 if f in (4, 6, 12, 14) else 0
        else:
            self.bob = 1 if (not still and _within(f, 6, 12)) else 0
        # happy: a little two-pixel hop once the breath is out
        self.hop = {13: 1, 14: 2, 15: 1}.get(f, 0) if state == "happy" else 0
        # one blink per loop, where the eyes are open to begin with
        self.blink = state in ("content", "sad", "hungry") and f == 14

        # sad: the tear rolls down and falls off; a new one wells up before the loop restarts
        self.tear = None
        if state == "sad":
            self.tear = 0 if f <= 7 else 1 if f <= 9 else 2 if f <= 11 else 3 if f <= 13 else -1
        # hungry: the drool drips one pixel longer, once
        self.drool = 1 if state == "hungry" and _within(f, 8, 11) else 0
        # hungry / starving: the sweat drops slide down the face and vanish, then reappear
        self.sweat = None
        if state in ("hungry", "starving"):
            self.sweat = 0 if f <= 5 else 1 if f <= 9 else 2 if f <= 12 else -1
        # happy: the heart lifts twice, once on its own and once with the hop
        self.heart = -1 if state == "happy" and (f in (3, 4) or f in (13, 14)) else 0
        # frozen: the frost star shrinks to a point, then flares
        self.star = 0
        if still:
            self.star = -1 if f in (6, 7) else 1 if f in (8, 9) else 0

        # species touches
        self.widen = self.bob if (sp == "cepe" and self.touch) else 0          # the cap squashes wider
        # the skirt follows the body like fabric: it billows out as the body sinks into the
        # breath (1, then half-way back), and lags behind, hanging long, as it rises (-1)
        self.skirt = 0
        if sp == "amanite" and self.touch:
            self.skirt = {6: 1, 7: 1, 8: 0.5, 13: -1, 14: -1, 15: -0.5}.get(f, 0)
        self.puff = 1 if (sp == "rose" and self.touch and _within(f, 2, 4)) else 0   # the button cap puffs up
        self.tilt = 0                                                            # the flat cap tips
        if sp == "russule" and self.touch:
            self.tilt = 1 if _within(f, 3, 6) else -1 if _within(f, 11, 14) else 0
        # the Mycena's glow pulses: its whole rim lights up while the halo swells
        self.glow = 1 if (sp == "mystique" and self.touch and _within(f, 2, 6)) else 0
        self.halo = int(round(110 + 50 * math.sin(2 * math.pi * f / FRAMES))) if sp == "mystique" else 110
        # the chanterelle's wavy rim ripples in place: the waves deepen and flatten, never travel
        self.ripple = 1 + 0.45 * math.sin(2 * math.pi * f / FRAMES) if (sp == "dore" and self.touch) else 1.0
        # the Coprin's ink drop: forms at the rim, falls straight down, splashes (see coprin())
        self.drip = f if (sp == "coprin" and self.touch) else None

    @property
    def body(self):
        """How far the head has moved from rest: (dx, dy). Things stuck to the face follow it."""
        return 0, self.bob - self.hop


def pose(sp, state, f=0):
    return Pose(sp, state, f % FRAMES)


def moved(g, pose):
    """The grid with this frame's whole-body motion applied: the breath removes the bottom row
    of the body and lowers everything above it by one; the hop lifts it off the ground."""
    if not (pose.bob or pose.hop):
        return g
    out = Grid()
    for y in range(N):
        for x in range(N):
            sy = y + pose.hop
            if pose.bob and sy <= 27:
                sy -= 1
            if 0 <= sy < N and g.c[sy][x] is not None:
                out.put(x, y, g.c[sy][x], g.r[sy][x])
    return out
