"""Renders every image the bot links to, at 256x256, into assets/plynlings/.

Filenames carry a version (..._v1.png). Discord caches an image by its URL, so changing
the art means bumping ART_VERSION here *and* PlynlingArt.Version in the bot together —
overwriting a file in place can leave the old art showing for a long while.

The living Plynlings are animated WebP: the idle loop from motion.py, lossless so the soft
ground shadow and the Mycena's halo keep their partial transparency. Frame 0 is the still
sprite, so a client that shows only the first frame shows the picture it always did.
Memorials and foods do not move and stay PNG.
"""
import os

from PIL import Image

from common import SPECIES
from memorials import memorial
from motion import FRAMES, FRAME_MS
from sprites import build

ART_VERSION = 3
SIZE = 256
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "plynlings"))
STATES = ["happy", "content", "sad", "hungry", "starving", "frozen", "sleeping"]
FOODS = ["mushroom", "shiitake", "morel", "truffle"]
# Species whose bébé has its own art. Must match PlynlingArt.StagedSpecies in the bot
# (artcheck compares them). The adult keeps its stage-less filename, and ado and ancien wear
# it too; the baby inserts "_baby".
STAGED = {"amanite", "cepe", "rose", "russule", "mystique", "dore", "coprin"}


def big(im):
    # Nearest-neighbour: Discord smooths a small image when it enlarges it, which blurs
    # pixel art, so the files are exported large with hard edges.
    return im.resize((SIZE, SIZE), Image.NEAREST)


def save(im, name):
    big(im).save(os.path.join(OUT, f"{name}_v{ART_VERSION}.png"), optimize=True)


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
        for tier in range(1, 6):
            save(memorial(tier, sp), f"memorial_{sp}_{tier}")
            count += 1
    for food in FOODS:
        save(Image.open(os.path.join(HERE, "source", f"food_{food}.png")).convert("RGBA"), f"food_{food}")
        count += 1
    print(f"{count} files written to {OUT}")


if __name__ == "__main__":
    main()
