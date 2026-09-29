"""The icons' palette: Pear36 (the palette the Champignons pack is drawn in) plus the pack's eight
extra browns and blues. One character per colour; "." is transparent. A grid using any other
character is refused by export.py."""

TRANSPARENT = "."
OUTLINE = "#"

PALETTE = {
    # outline and greys
    "#": "#272736", "k": "#43434f", "g": "#606070", "G": "#7e7e8f", "s": "#c2c2d1", "w": "#ffffeb",
    # purples
    "p": "#322947", "P": "#473b78", "q": "#3e2347", "v": "#422445", "V": "#5a265e", "m": "#57294b",
    "M": "#73275c", "n": "#80366b",
    # reds and pinks
    "z": "#5e315b", "r": "#8c3f5d", "R": "#964253", "c": "#b0305c", "C": "#bd4882", "o": "#ba6156",
    "e": "#e36956", "E": "#eb564b", "f": "#ff9166", "i": "#ff6b97", "I": "#ffb5b5",
    # oranges and yellows
    "a": "#f2a65e", "A": "#ffb570", "y": "#ffe478", "Y": "#cfff70",
    # greens
    "l": "#8fde5d", "L": "#3ca370", "t": "#3d6e70", "T": "#323e4f",
    # blues (d, D, h are the pack's)
    "b": "#4b5bab", "B": "#4da6ff", "x": "#66ffe3", "d": "#193a66", "D": "#316196", "h": "#79a5d3",
    # the pack's browns
    "u": "#4b3837", "U": "#7d5d5c", "j": "#a27c6e", "J": "#cfa385", "Z": "#732c47",
}

assert len(PALETTE) == 44 and len(set(PALETTE.values())) == 44
