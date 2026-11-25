"""Renders every image the bot links to, at 256x256, into assets/plynlings/.

Filenames carry a version (..._v1.png). Discord caches an image by its URL, so changing
the art means bumping ART_VERSION here *and* PlynlingArt.Version in the bot together —
overwriting a file in place can leave the old art showing for a long while.

The living Plynlings are animated WebP: the idle loop from motion.py, lossless so the soft
ground shadow and the Mycena's halo keep their partial transparency. Frame 0 is the still
sprite, so a client that shows only the first frame shows the picture it always did.
Memorials and foods do not move and stay PNG.

Dressed pictures are not exported: the bot draws them from the layers. The pre-rendered « _with_ » /
« _aura_ » files were retired on 2026-11-25.

    python export.py          # everything, after deleting the old files
    python export.py redraw coprin   # a species in REDRAWN: its new bare files; then re-export the layers
    python export.py layers   # the layers the bot draws dressed pictures from, into ProjectSYNCS/Assets/Plynlings

The layers (export_layers) are the same pictures taken apart: the plain sprite, each accessory
alone, each aura's raw particles and the mood extras alone, every one an 8x lossless loop on the
shared timing, drawn on a transparent canvas at the picture's foot (a hat on an adult: the 320 px
hat canvas, every other layer 256 px). The bot lays them over each other at runtime.
"""
import os
import shutil
import sys

from PIL import Image

from common import SPECIES
from memorials import memorial
from motion import FRAMES, FRAME_MS
from accessories import looks, loop, wear
from auras import AURA_STATES, AURAS, _Spray, _bounds
from accessories import HAT_AT, HAT_CANVAS
from sprites import build, marks

ART_VERSION = 5
# A species redrawn since ART_VERSION: its bare and visit pictures (the ones the bot links) take this
# version instead, so Discord's URL cache can't keep the old drawing, without renaming every other
# species' files. Its old bare files stay (posted messages link them). Must match PlynlingArt.Redrawn
# in the bot.
# coprin 6: the idle ink drop turned black.
REDRAWN = {"coprin": 6}
SIZE = 256
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "plynlings"))
LAYERS = os.path.normpath(os.path.join(HERE, "..", "..", "ProjectSYNCS", "Assets", "Plynlings"))
STATES = ["happy", "content", "sad", "hungry", "starving", "frozen", "sleeping", "angry", "sick"]
# The card's living faces that also get a « _dirty » version (dirt.py): a Plynling whose hygiene is
# below 33 %. Not frozen (the ice hides it), not angry and not the visit pictures (visits stay
# clean). Must match PlynlingArt.DirtyMoods in the bot.
DIRTY_STATES = ["happy", "content", "sad", "hungry", "starving", "sleeping", "sick"]
FOODS = ["mushroom", "shiitake", "morel", "truffle"]
# Species whose bébé has its own art. Must match PlynlingArt.StagedSpecies in the bot
# (artcheck compares them). The adult keeps its stage-less filename, and ado and ancien wear
# it too; the baby inserts "_baby".
STAGED = {"amanite", "cepe", "rose", "russule", "mystique", "dore", "coprin"}
# A visit shows two Plynlings side by side in a gallery, and Discord stretches each to half the
# message width. To make them a little smaller, the visit pictures put the same 32x32 sprite on a
# larger transparent canvas: VISIT_CANVAS grid cells wide, so the sprite fills 32 / VISIT_CANVAS of
# its tile (40 -> 80 %). Whole-pixel scaling only, so the pixel art stays sharp. Only the four faces
# a visit uses. Must match PlynlingArt.VisitSprite in the bot.
VISIT_CANVAS = 40
VISIT_STATES = ["happy", "content", "sad", "angry"]


def big(im):
    # Nearest-neighbour: Discord smooths a small image when it enlarges it, which blurs
    # pixel art, so the files are exported large with hard edges. A picture up to 32 wide fills
    # SIZE (the 16 px foods too); a wider one — an adult in a hat, on its 40 px canvas — keeps the
    # sprite's scale (32 -> SIZE), so its pixels are the same size as on every other picture.
    k = SIZE // min(im.width, 32)
    return im.resize((im.width * k, im.height * k), Image.NEAREST)


def save(im, name):
    big(im).save(os.path.join(OUT, f"{name}_v{ART_VERSION}.png"), optimize=True)


def padded(im):
    """The sprite centred on the larger visit canvas, then scaled up without smoothing."""
    canvas = Image.new("RGBA", (VISIT_CANVAS, VISIT_CANVAS), (0, 0, 0, 0))
    pad = (VISIT_CANVAS - im.width) // 2
    canvas.alpha_composite(im, (pad, pad))
    scale = SIZE // 32
    return canvas.resize((VISIT_CANVAS * scale, VISIT_CANVAS * scale), Image.NEAREST)


def save_visit_loop(frames, name, version=ART_VERSION):
    frames = [padded(f) for f in frames]
    frames[0].save(os.path.join(OUT, f"{name}_visit_v{version}.webp"), save_all=True,
                   append_images=frames[1:], duration=FRAME_MS, loop=0, lossless=True, method=6)


def save_loop(frames, name, version=ART_VERSION):
    frames = [big(f) for f in frames]
    frames[0].save(os.path.join(OUT, f"{name}_v{version}.webp"), save_all=True,
                   append_images=frames[1:], duration=FRAME_MS, loop=0, lossless=True, method=6)


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith((".png", ".webp")):
            os.remove(os.path.join(OUT, old))
    count = 0
    for sp in SPECIES:
        count += export_bare(sp)
        for tier in range(1, 6):
            save(memorial(tier, sp), f"memorial_{sp}_{tier}")
            count += 1
    for food in FOODS:
        save(Image.open(os.path.join(HERE, "source", f"food_{food}.png")).convert("RGBA"), f"food_{food}")
        count += 1
    print(f"{count} files written to {OUT}")


def export_bare(sp):
    """One species' bare pictures, the ones the bot links: every face, baby, dirty and visit, at
    its version (REDRAWN, else ART_VERSION)."""
    v = REDRAWN.get(sp, ART_VERSION)
    count = 0
    for state in STATES:
        save_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}", v)
        count += 1
        if sp in STAGED:
            save_loop([build(state, sp, f, stage="baby") for f in range(FRAMES)], f"plynling_{sp}_baby_{state}", v)
            count += 1
        if state in DIRTY_STATES:
            save_loop([build(state, sp, f, dirty=True) for f in range(FRAMES)], f"plynling_{sp}_{state}_dirty", v)
            count += 1
            if sp in STAGED:
                save_loop([build(state, sp, f, stage="baby", dirty=True) for f in range(FRAMES)],
                          f"plynling_{sp}_baby_{state}_dirty", v)
                count += 1
        if state in VISIT_STATES:
            save_visit_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}", v)
            count += 1
            if sp in STAGED:
                save_visit_loop([build(state, sp, f, stage="baby") for f in range(FRAMES)], f"plynling_{sp}_baby_{state}", v)
                count += 1
    return count


def save_layer(frames, folder, name):
    frames = [big(f) for f in frames]
    path = os.path.join(LAYERS, folder, f"{name}.webp")
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=FRAME_MS, loop=0,
                   lossless=True, method=6)


def raw_aura(body, state, aura, frame, tall_canvas):
    """The aura's particles alone, unmasked: sprayed over an empty frame, so every pixel lands.
    The bot keeps only those that fall where the picture under them is nearly empty."""
    size = HAT_CANVAS if tall_canvas else 32
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ox, oy = HAT_AT if tall_canvas else (0, 0)
    AURAS[aura](_Spray(out, Image.new("RGBA", (size, size), (0, 0, 0, 0)), ox, oy), _bounds(body), frame)
    return out


def export_layers():
    """base/, acc/, aura/ and marks/ under ProjectSYNCS/Assets/Plynlings (see the module doc).
    Dirty faces get a base of their own only: an accessory, an aura or a mark is the same drawing
    on the clean and the dirty sprite (checked when the layers were made)."""
    if os.path.isdir(LAYERS):
        shutil.rmtree(LAYERS)
    for folder in ("base", "acc", "aura", "marks"):
        os.makedirs(os.path.join(LAYERS, folder))
    counts = {"base": 0, "acc": 0, "aura": 0, "marks": 0}
    skipped_marks = set()
    every = looks()
    for sp in SPECIES:
        for stage in (["adult", "baby"] if sp in STAGED else ["adult"]):
            shape = f"{sp}{'_baby' if stage == 'baby' else ''}"
            for state in STATES:
                if state == "angry":
                    continue
                frames = loop(state, sp, FRAMES, stage)
                save_layer([im for im, _ in frames], "base", f"{shape}_{state}")
                counts["base"] += 1
                if state in DIRTY_STATES:
                    save_layer([build(state, sp, f, stage=stage, dirty=True) for f in range(FRAMES)],
                               "base", f"{shape}_{state}_dirty")
                    counts["base"] += 1
                for acc, style in every:
                    layer = []
                    for im, body in frames:
                        blank = Image.new("RGBA", im.size, (0, 0, 0, 0))
                        layer.append(wear(blank, acc, body, state, style or None))
                    save_layer(layer, "acc", f"{shape}_{state}_{acc}" + (f"_{style}" if style else ""))
                    counts["acc"] += 1
                if state in AURA_STATES:
                    for aura in AURAS:
                        for tall_canvas in ([False, True] if stage == "adult" else [False]):
                            save_layer([raw_aura(body, state, aura, f, tall_canvas) for f, (_, body) in enumerate(frames)],
                                       "aura", f"{shape}_{state}_{aura}" + ("_tall" if tall_canvas else ""))
                            counts["aura"] += 1
                mk = [marks(state, sp, f, stage) for f in range(FRAMES)]
                if any(m.getbbox() for m in mk):
                    save_layer(mk, "marks", f"{shape}_{state}")
                    counts["marks"] += 1
                else:
                    skipped_marks.add(state)
    total = sum(counts.values())
    size = sum(os.path.getsize(os.path.join(d, f)) for d, _, fs in os.walk(LAYERS) for f in fs)
    print(counts, f"no marks for: {sorted(skipped_marks)}")
    print(f"{total} files, {size / 1e6:.1f} MB written to {LAYERS}")
    return total


if __name__ == "__main__":
    if sys.argv[1:] == ["layers"]:
        export_layers()
    elif sys.argv[1:2] == ["redraw"] and len(sys.argv) == 3 and sys.argv[2] in REDRAWN:
        # A species redrawn: its bare pictures under their new version. Nothing deleted.
        sp = sys.argv[2]
        os.makedirs(OUT, exist_ok=True)
        print(f"{export_bare(sp)} bare files written to {OUT}; now run `python export.py layers`")
    elif sys.argv[1:] == []:
        main()
    else:
        # Never fall through to main(): it deletes the whole folder first.
        sys.exit(f"unknown mode {sys.argv[1:]}: use no argument, `layers` or `redraw <{'|'.join(REDRAWN)}>`")
