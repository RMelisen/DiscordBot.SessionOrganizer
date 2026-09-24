"""The face and mood extras every Plynling shares, and build(), which hands each species to
its own drawing in species.py."""
from common import N, SPECIES, STEM, STEM_OUT, INK, sheet

WHITE = (255, 255, 255)
IRIS = (104, 80, 118)                      # the soft reflection low in each eye
PINK, PINK_SOFT = (246, 146, 160), (244, 190, 190)
MOUTH, TONGUE = (192, 58, 72), (255, 142, 152)
TEAR, TEAR_HI = (130, 200, 255), (220, 242, 255)
LID = (150, 122, 104)
PALE = (206, 204, 204)


def build(state, sp, frame=0, shadow=True):
    """The living Plynling of species `sp` in one of the six moods. Each species draws its own
    silhouette in species.py, and every one of them wears the face below."""
    from species import DRAW          # imported here: species.py imports this module
    return DRAW[sp](state, frame, shadow)


# ---- the face -------------------------------------------------------------------------
# Eyes sit at columns 12-13 and 18-19, rows 19-21; the mouth at rows 23-25 — before the
# (ox, oy) offset. Every species wears this face; skin / skin_out replace the stem tones
# used for the eye bags and the brows, so a tinted stem keeps a matching face.

def face(g, state, f, ox=0, oy=0, skin=None, skin_out=None):
    skin = STEM if skin is None else skin
    skin_out = STEM_OUT if skin_out is None else skin_out

    def P(x, y, c):
        g.put(x + ox, y + f + oy, c)

    def glossy_eye(x0, shine2=False, watery=False):
        for x in (x0, x0 + 1):
            for y in (19, 20, 21):
                P(x, y, INK)
        P(x0, 19, WHITE)                               # the shine
        P(x0 + 1, 21, TEAR if watery else IRIS)        # a soft reflection, or welling up
        if shine2:
            P(x0 + 1, 20, WHITE)                       # a second sparkle: wanting something

    def happy_eye(x0):                                 # closed, smiling "^"
        P(x0 - 1, 20, INK)
        P(x0, 19, INK)
        P(x0 + 1, 19, INK)
        P(x0 + 2, 20, INK)

    def tired_eye(x0):                                 # half-closed, heavy-lidded
        for x in (x0 - 1, x0, x0 + 1, x0 + 2):
            P(x, 20, LID)
        P(x0, 21, INK)
        P(x0 + 1, 21, INK)
        P(x0, 22, skin[4])                             # bags under the eyes
        P(x0 + 1, 22, skin[4])

    def closed_eye(x0):
        for x in (x0 - 1, x0, x0 + 1, x0 + 2):
            P(x, 20, INK)

    def blush(c):
        for x in (10, 11, 20, 21):
            P(x, 22, c)

    def brows(inner_raise=True, flat=False):
        if flat:
            for x in (11, 12, 13, 18, 19, 20):
                P(x, 17, skin_out)
            return
        for x, y in ((11, 18), (12, 17), (19, 17), (20, 18)) if not inner_raise else \
                    ((11, 17), (12, 17), (13, 16), (18, 16), (19, 17), (20, 17)):
            P(x, y, INK if inner_raise else skin_out)

    if state == "happy":
        happy_eye(12)
        happy_eye(18)
        for x, c in ((14, INK), (15, MOUTH), (16, MOUTH), (17, INK)):   # open laugh
            P(x, 23, c)
        P(15, 24, TONGUE)
        P(16, 24, TONGUE)
        P(14, 24, INK)
        P(17, 24, INK)
        P(15, 25, INK)
        P(16, 25, INK)
        blush(PINK)
    elif state == "content":
        glossy_eye(12)
        glossy_eye(18)
        for x, y in ((13, 23), (14, 24), (15, 23), (16, 23), (17, 24), (18, 23)):   # a little "ω"
            P(x, y, INK)
        blush(PINK_SOFT)
    elif state == "sad":
        glossy_eye(12, watery=True)
        glossy_eye(18, watery=True)
        brows(inner_raise=True)
        for x, y in ((14, 24), (15, 23), (16, 23), (17, 24)):
            P(x, y, INK)
        P(12, 22, TEAR_HI)                                              # a tear rolling down
        P(12, 23, TEAR)
        P(11, 24, TEAR)
    elif state == "hungry":
        glossy_eye(12, shine2=True)
        glossy_eye(18, shine2=True)
        P(15, 23, INK)
        P(16, 23, INK)
        P(14, 24, INK)
        P(15, 24, MOUTH)
        P(16, 24, MOUTH)
        P(17, 24, INK)
        P(15, 25, INK)
        P(16, 25, INK)
        P(17, 25, TEAR)                                                 # drooling
        P(17, 26, TEAR_HI)
        blush(PINK_SOFT)
    elif state == "starving":
        tired_eye(12)
        tired_eye(18)
        brows(inner_raise=True)
        for x, y in ((13, 25), (14, 24), (15, 25), (16, 24), (17, 25), (18, 24)):   # wobbling
            P(x, y, INK)
    elif state == "frozen":
        closed_eye(12)
        closed_eye(18)
        for x in range(14, 18):                                         # a closed, flat mouth
            P(x, 24, INK)


def extras(im, state, p, frame):
    def px(x, y, c, a=255):
        if 0 <= x < N and 0 <= y < N:
            im.putpixel((x, y), c + (a,))

    if state == "happy":                                                 # a little heart
        for x, y in ((25, 17), (27, 17), (24, 18), (25, 18), (26, 18), (27, 18), (28, 18),
                     (25, 19), (26, 19), (27, 19), (26, 20)):
            px(x, y, (246, 116, 140))
        px(25, 18, (255, 196, 206))
    if state == "hungry":                                                # sweat drop
        for x, y in ((25, 17), (25, 18), (24, 18), (25, 19)):
            px(x, y, TEAR)
        px(25, 17, TEAR_HI)
    if state == "starving":                                              # a cold sweat, both sides
        for x, y in ((25, 18), (25, 19), (24, 19), (25, 20), (6, 19), (6, 20), (7, 20), (6, 21)):
            px(x, y, TEAR)
    if state == "frozen":
        for x, y in ((4, 16), (4, 17), (7, 16), (24, 16), (27, 16), (27, 17), (26, 16)):
            px(x, y, (206, 242, 255))
        for x, y in ((9, 5), (10, 4), (15, 3), (20, 4)):
            px(x, y, WHITE)
        for x, y in ((28, 2), (27, 3), (28, 3), (29, 3), (28, 4)):
            px(x, y, (220, 246, 255))
    if p["sparkle"] and state not in ("frozen", "starving"):
        s = p["sparkle"]
        for x, y in ([(2, 6), (29, 9), (27, 1), (3, 1)] if frame == 0 else [(1, 9), (30, 5), (26, 3), (5, 2)]):
            px(x, y, s)


if __name__ == "__main__":
    species = list(SPECIES)
    states = ["happy", "content", "sad", "hungry", "starving", "frozen"]
    print(sheet(species, states, lambda s, st: build(st, s), species, states, "plynling_faces_v5.png", K=4))
    # a close-up of one species, big enough to judge the faces pixel by pixel
    print(sheet(["amanite"], states, lambda s, st: build(st, s), ["amanite"], states,
                "plynling_faces_closeup_v5.png", K=7))
