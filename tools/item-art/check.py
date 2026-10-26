"""Checks every icon without writing into the bot: grid shape, palette, outline, margins, and that
an exported PNG is an exact ×8 (16×16) or ×4 (22×22 badges and traits) of its grid. Run: python check.py (exit
code 1 on any failure)."""
import sys
import tempfile

from PIL import Image

import export
from icons import GROUPS, ICONS

failures = []


def check(ok, what):
    if not ok:
        failures.append(what)


check(sorted(k for keys in GROUPS.values() for k in keys) == sorted(ICONS), "GROUPS and ICONS disagree")
check(all(k.startswith(("col.", "set.", "cos.theme.", "cos.title.", "cos.accessory.", "cos.grave.", "trait.",
                         "badge."))
          for k in ICONS), "a key of no known kind")

with tempfile.TemporaryDirectory() as tmp:
    for key in ICONS:
        try:
            small = export.render(key)          # runs check_image
        except SystemExit as e:
            failures.append(str(e))
            continue
        path = export.write_png(key, small, tmp)
        big = Image.open(path).convert("RGBA")
        n = export.size_of(key)
        scale = export.SCALES[n]
        check(big.size == (n * scale, n * scale), f"{key}: exported {big.size}")
        check(big.resize((n, n), Image.NEAREST).tobytes() == small.tobytes(), f"{key}: not an exact ×{scale}")
        check(big.resize((n, n), Image.NEAREST).resize(big.size, Image.NEAREST).tobytes() == big.tobytes(),
              f"{key}: not made of {scale}×{scale} blocks")

print(f"{len(ICONS)} icons checked")
if failures:
    print("FAIL\n" + "\n".join(failures))
    sys.exit(1)
print("OK")
