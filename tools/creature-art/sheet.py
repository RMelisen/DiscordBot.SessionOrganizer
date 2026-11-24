"""The review sheet: every creature at 8 px per art pixel, then at about the size Discord shows a
picture in a card, on Discord's dark and light backgrounds. Writes out/sheet_<place>.png."""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

from creatures import CREATURES, N

HERE = os.path.dirname(os.path.abspath(__file__))
DARK, LIGHT = (49, 51, 56, 255), (255, 255, 255, 255)
BIG, SMALL = 8, 3                          # 256 px, and 96 px (a card's thumbnail is about 80)
TILE_W = N * BIG + 24
FONT = ImageFont.truetype("arial.ttf", 16)


def main(only=None):
    rows = [c for c in CREATURES if not only or c[0] in only]
    cols = 3
    th = N * BIG + N * SMALL + 56
    sheet = Image.new("RGBA", (cols * TILE_W * 1 + 16, ((len(rows) + cols - 1) // cols) * th + 16), DARK)
    d = ImageDraw.Draw(sheet)
    for i, (key, name, draw) in enumerate(rows):
        im = draw()
        x0, y0 = 16 + (i % cols) * TILE_W, 16 + (i // cols) * th
        d.text((x0, y0), name, font=FONT, fill=(230, 230, 236))
        big = im.resize((N * BIG, N * BIG), Image.NEAREST)
        sheet.alpha_composite(big, (x0, y0 + 24))
        small = im.resize((N * SMALL, N * SMALL), Image.NEAREST)
        sheet.alpha_composite(small, (x0, y0 + 32 + N * BIG))
        d.rectangle((x0 + N * SMALL + 8, y0 + 32 + N * BIG, x0 + 2 * N * SMALL + 7, y0 + 31 + N * BIG + N * SMALL),
                    fill=LIGHT)
        sheet.alpha_composite(small, (x0 + N * SMALL + 8, y0 + 32 + N * BIG))
    os.makedirs(os.path.join(HERE, "out"), exist_ok=True)
    path = os.path.join(HERE, "out", "sheet_combe.png")
    sheet.save(path)
    print(path)


if __name__ == "__main__":
    main(sys.argv[1:])
