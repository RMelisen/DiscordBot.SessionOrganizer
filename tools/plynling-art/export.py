"""Renders every image the bot links to, at 256x256, into assets/plynlings/.

Filenames carry a version (..._v1.png). Discord caches an image by its URL, so changing
the art means bumping ART_VERSION here *and* PlynlingArt.Version in the bot together —
overwriting a file in place can leave the old art showing for a long while.
"""
import os

from PIL import Image

from common import SPECIES
from memorials import memorial
from sprites import build

ART_VERSION = 1
SIZE = 256
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "plynlings"))
STATES = ["happy", "content", "sad", "hungry", "starving", "frozen"]
FOODS = ["mushroom", "shiitake", "morel", "truffle"]


def save(im, name):
    # Nearest-neighbour: Discord smooths a small image when it enlarges it, which blurs
    # pixel art, so the files are exported large with hard edges.
    im.resize((SIZE, SIZE), Image.NEAREST).save(os.path.join(OUT, f"{name}_v{ART_VERSION}.png"), optimize=True)


def main():
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith(".png"):
            os.remove(os.path.join(OUT, old))
    count = 0
    for sp in SPECIES:
        for state in STATES:
            save(build(state, sp), f"plynling_{sp}_{state}")
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
