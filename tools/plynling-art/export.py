"""Renders every image the bot links to, at 256x256, into assets/plynlings/.

Filenames carry a version (..._v1.png). Discord caches an image by its URL, so changing
the art means bumping ART_VERSION here *and* PlynlingArt.Version in the bot together —
overwriting a file in place can leave the old art showing for a long while.

The living Plynlings are animated WebP: the idle loop from motion.py, lossless so the soft
ground shadow and the Mycena's halo keep their partial transparency. Frame 0 is the still
sprite, so a client that shows only the first frame shows the picture it always did.
Memorials and foods do not move and stay PNG.

Every card face (not angry, a visit face) is also exported with each accessory look a Plynling can
wear (accessories.py): « _with_<accessory>[_<style>] » after the face and its « _dirty ». New
filenames, so they add to the folder without touching a single existing file. Must match
PlynlingArt.Sprite and AccessoryStyles in the bot (checks section 62 reads this folder).
"""
import os

from PIL import Image

from common import SPECIES
from memorials import memorial
from motion import FRAMES, FRAME_MS
from accessories import looks, loop, wear
from sprites import build

ART_VERSION = 4
SIZE = 256
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "plynlings"))
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


def save_visit_loop(frames, name):
    frames = [padded(f) for f in frames]
    frames[0].save(os.path.join(OUT, f"{name}_visit_v{ART_VERSION}.webp"), save_all=True,
                   append_images=frames[1:], duration=FRAME_MS, loop=0, lossless=True, method=6)


def save_loop(frames, name):
    frames = [big(f) for f in frames]
    frames[0].save(os.path.join(OUT, f"{name}_v{ART_VERSION}.webp"), save_all=True,
                   append_images=frames[1:], duration=FRAME_MS, loop=0, lossless=True, method=6)


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith((".png", ".webp")):
            os.remove(os.path.join(OUT, old))
    count = 0
    for sp in SPECIES:
        for state in STATES:
            save_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}")
            count += 1
            if sp in STAGED:
                save_loop([build(state, sp, f, stage="baby") for f in range(FRAMES)], f"plynling_{sp}_baby_{state}")
                count += 1
            if state in DIRTY_STATES:
                save_loop([build(state, sp, f, dirty=True) for f in range(FRAMES)], f"plynling_{sp}_{state}_dirty")
                count += 1
                if sp in STAGED:
                    save_loop([build(state, sp, f, stage="baby", dirty=True) for f in range(FRAMES)],
                              f"plynling_{sp}_baby_{state}_dirty")
                    count += 1
            if state in VISIT_STATES:
                save_visit_loop([build(state, sp, f) for f in range(FRAMES)], f"plynling_{sp}_{state}")
                count += 1
                if sp in STAGED:
                    save_visit_loop([build(state, sp, f, stage="baby") for f in range(FRAMES)], f"plynling_{sp}_baby_{state}")
                    count += 1
        for tier in range(1, 6):
            save(memorial(tier, sp), f"memorial_{sp}_{tier}")
            count += 1
    for food in FOODS:
        save(Image.open(os.path.join(HERE, "source", f"food_{food}.png")).convert("RGBA"), f"food_{food}")
        count += 1
    count += export_accessories()
    print(f"{count} files written to {OUT}")


def export_accessories():
    """Every card face of every shape, in every accessory look: the loop is measured once per
    face, then dressed in each look."""
    count = 0
    every = looks()
    for sp in SPECIES:
        for stage in (["adult", "baby"] if sp in STAGED else ["adult"]):
            for state in STATES:
                if state == "angry":
                    continue
                for dirty in ([False, True] if state in DIRTY_STATES else [False]):
                    frames = loop(state, sp, FRAMES, stage, dirty)
                    name = f"plynling_{sp}{'_baby' if stage == 'baby' else ''}_{state}{'_dirty' if dirty else ''}_with_"
                    for acc, style in every:
                        save_loop([wear(im, acc, body, state, style or None) for im, body in frames],
                                  name + acc + (f"_{style}" if style else ""))
                        count += 1
    return count


if __name__ == "__main__":
    main()
