"""VoxelCircuit city kit: modular voxel skyscraper pieces on a shared grid.

Grid: 1 voxel = 0.5 m. 1 tile = 8 voxels = 4 m. Heights are multiples of 8 voxels (4 m).
Every piece's pivot is the bottom centre of its footprint, so any piece stacks on any piece of the same footprint.
Axes in this file: x, y = footprint, z = up.
"""
import random

V = 0.5
TILE = 8

PALETTE = {
    # One grey theme; lighting comes from the scene's lights. Each role keeps its own palette slot,
    # so a single colour change in the shared palette texture restyles every building at once.
    "concrete":  (112, 114, 118),  # main walls
    "dark":      (58, 60, 64),     # recesses, plinths, corner piers
    "trim":      (148, 150, 154),  # fins, ledges, caps
    "glass":     (40, 42, 46),     # window cells (darkest, reads as openings)
    "band":      (90, 92, 96),     # band lines between floors
    "lit_warm":  (40, 42, 46),     # window slot reserved for lit windows later (same as glass for now)
    "lit_cool":  (128, 130, 134),  # light-strip slot (plain grey for now)
    "beacon":    (128, 130, 134),  # antenna tip slot (plain grey for now)
}
ORDER = list(PALETTE)
EMISSIVE = set()   # nothing glows; light the city with scene lights


class Piece:
    def __init__(self, name, tiles_x, tiles_y, height_steps, role):
        self.name, self.role = name, role
        self.fx, self.fy = tiles_x * TILE, tiles_y * TILE
        self.h = height_steps * TILE
        self.v = {}

    def box(self, x0, x1, y0, y1, z0, z1, c):
        for x in range(x0, x1):
            for y in range(y0, y1):
                for z in range(z0, z1):
                    self.v[(x, y, z)] = c

    def carve(self, x0, x1, y0, y1, z0, z1):
        for x in range(x0, x1):
            for y in range(y0, y1):
                for z in range(z0, z1):
                    self.v.pop((x, y, z), None)

    def paint(self, x0, x1, y0, y1, z0, z1, c, only_existing=True):
        for x in range(x0, x1):
            for y in range(y0, y1):
                for z in range(z0, z1):
                    if not only_existing or (x, y, z) in self.v:
                        self.v[(x, y, z)] = c

    def ring(self, inset, z0, z1, c):
        """A hollow band one voxel deep around the footprint at the given inset."""
        a, bx, by = inset, self.fx - inset, self.fy - inset
        for z in range(z0, z1):
            for x in range(a, bx):
                for y in (a, by - 1):
                    self.v[(x, y, z)] = c
            for y in range(a, by):
                for x in (a, bx - 1):
                    self.v[(x, y, z)] = c

    def faces(self, fn):
        """Call fn(axis, side, along, depth_coord) for the 4 outer faces: helps decorate all sides alike."""
        return fn


def windows_on_faces(p, inset, z_rows, cols, lit_chance, rng, width=1, cool_chance=0.0):
    """Recessed window cells on all four faces of a body inset by `inset`, at the given rows/columns."""
    lo_x, hi_x, lo_y, hi_y = inset, p.fx - inset - 1, inset, p.fy - inset - 1
    for z in z_rows:
        for c in cols:
            for w in range(width):
                for (x, y, ox, oy) in ((c + w, lo_y, 0, 1), (c + w, hi_y, 0, -1),
                                       (lo_x, c + w, 1, 0), (hi_x, c + w, -1, 0)):
                    r = rng.random()
                    colour = "lit_cool" if r < cool_chance else ("lit_warm" if r < cool_chance + lit_chance else "glass")
                    p.v[(x, y, z)] = colour
                    p.v.pop((x - ox, y - oy, z), None)          # nothing in front of the window
                    p.v[(x + ox, y + oy, z)] = "dark"            # dark backing one voxel in


def base_2x2(rng):
    p = Piece("Base_2x2", 2, 2, 2, "base")
    f = p.fx
    p.box(0, f, 0, f, 0, 2, "dark")                     # plinth
    p.box(1, f - 1, 1, f - 1, 2, 15, "concrete")        # body
    for cx in (0, f - 3):                               # corner piers to the footprint edge
        for cy in (0, f - 3):
            p.box(cx, cx + 3, cy, cy + 3, 2, 15, "trim")
    for i in range(4):                                  # entrances, centre of each face
        c0, c1 = f // 2 - 2, f // 2 + 2
        p.carve(c0, c1, 1, 3, 2, 7) if i == 0 else None
        p.carve(c0, c1, f - 3, f - 1, 2, 7) if i == 1 else None
        p.carve(1, 3, c0, c1, 2, 7) if i == 2 else None
        p.carve(f - 3, f - 1, c0, c1, 2, 7) if i == 3 else None
    p.box(c0, c1, 3, 4, 2, 7, "dark"); p.box(c0, c1, f - 4, f - 3, 2, 7, "dark")
    p.box(3, 4, c0, c1, 2, 7, "dark"); p.box(f - 4, f - 3, c0, c1, 2, 7, "dark")
    for x in range(c0, c1):                             # warm light over each entrance
        p.v[(x, 2, 7)] = "lit_warm"; p.v[(x, f - 3, 7)] = "lit_warm"
        p.v[(2, x, 7)] = "lit_warm"; p.v[(f - 3, x, 7)] = "lit_warm"
    windows_on_faces(p, 1, [10], range(4, f - 4, 2), 0.35, rng)
    p.ring(1, 12, 13, "band")                           # accent band
    p.box(0, f, 0, f, 15, 16, "trim")                   # cap slab
    return p


def base_1x1(rng):
    p = Piece("Base_1x1", 1, 1, 2, "base")
    f = p.fx
    p.box(0, f, 0, f, 0, 2, "dark")
    p.box(1, f - 1, 1, f - 1, 2, 15, "concrete")
    for cx in (0, f - 2):
        for cy in (0, f - 2):
            p.box(cx, cx + 2, cy, cy + 2, 2, 15, "trim")
    p.carve(3, 5, 1, 2, 2, 6); p.box(3, 5, 2, 3, 2, 6, "dark")
    p.v[(3, 1, 6)] = p.v[(4, 1, 6)] = "lit_warm"
    windows_on_faces(p, 1, [9, 11], [3, 4], 0.3, rng)
    p.ring(1, 13, 14, "band")
    p.box(0, f, 0, f, 15, 16, "trim")
    return p


def shaft_2x2_fins(rng, variant):
    p = Piece(f"Shaft_2x2_Fins{variant}", 2, 2, 2, "shaft")
    f = p.fx
    p.box(2, f - 2, 2, f - 2, 0, 16, "concrete")
    for cx in (0, f - 2):                               # corner piers
        for cy in (0, f - 2):
            p.box(cx, cx + 2, cy, cy + 2, 0, 16, "dark")
    for c in range(4, f - 3, 3):                        # vertical fins on every face
        p.box(c, c + 1, 0, 2, 0, 16, "trim"); p.box(c, c + 1, f - 2, f, 0, 16, "trim")
        p.box(0, 2, c, c + 1, 0, 16, "trim"); p.box(f - 2, f, c, c + 1, 0, 16, "trim")
    cols = [c + 1 for c in range(4, f - 3, 3)] + [c + 2 for c in range(4, f - 3, 3)]
    cols = [c for c in cols if 3 <= c <= f - 4]
    windows_on_faces(p, 2, range(1, 16, 2), cols, 0.22 if variant == "A" else 0.12, rng,
                     cool_chance=0.0 if variant == "A" else 0.04)
    return p


def shaft_2x2_banded(rng):
    p = Piece("Shaft_2x2_Banded", 2, 2, 2, "shaft")
    f = p.fx
    for z0 in (0, 8):                                   # two stacked tiers, each with a ledge on top
        p.box(1, f - 1, 1, f - 1, z0, z0 + 7, "concrete")
        p.box(0, f, 0, f, z0 + 7, z0 + 8, "trim")
        windows_on_faces(p, 1, [z0 + 2, z0 + 4], range(3, f - 3), 0.18, rng)
        p.ring(1, z0 + 6, z0 + 7, "dark")               # shadow line under the ledge
    for cx in (0, f - 2):
        for cy in (0, f - 2):
            p.box(cx, cx + 2, cy, cy + 2, 0, 16, "dark")
    return p


def setback_2x2_to_1x1(rng):
    p = Piece("Setback_2x2_to_1x1", 2, 2, 1, "setback")
    f = p.fx
    steps = [(1, 0, 2, "trim"), (2, 2, 4, "concrete"), (3, 4, 6, "concrete"), (4, 6, 8, "trim")]
    for inset, z0, z1, c in steps:
        p.box(inset, f - inset, inset, f - inset, z0, z1, c)
    for x in range(3, f - 3, 2):                        # cool light strip on the first step
        for y in (2, f - 3):
            p.v[(x, y, 2)] = "lit_cool"
        for y2 in (2, f - 3):
            p.v[(y2, x, 2)] = "lit_cool"
    return p


def shaft_1x1(rng, variant):
    p = Piece(f"Shaft_1x1_{variant}", 1, 1, 2, "shaft")
    f = p.fx
    p.box(1, f - 1, 1, f - 1, 0, 16, "concrete")
    for cx in (0, f - 2):
        for cy in (0, f - 2):
            p.box(cx, cx + 2, cy, cy + 2, 0, 16, "dark")
    if variant == "A":                                  # vertical slit windows
        for z in range(1, 15):
            for c in (3, 4):
                for (x, y) in ((c, 1), (c, f - 2), (1, c), (f - 2, c)):
                    p.v[(x, y, z)] = "lit_warm" if rng.random() < 0.18 else "glass"
    else:                                               # horizontal bands
        for z0 in (3, 7, 11):
            p.ring(1, z0, z0 + 1, "trim")
        windows_on_faces(p, 1, [1, 5, 9, 13], [3, 4], 0.25, rng)
    return p


def crown_2x2(rng):
    p = Piece("Crown_2x2", 2, 2, 1, "crown")
    f = p.fx
    p.box(1, f - 1, 1, f - 1, 0, 2, "concrete")
    p.box(0, f, 0, f, 2, 3, "trim")                     # overhanging rim
    p.box(2, f - 2, 2, f - 2, 3, 6, "dark")             # mechanical floor
    for x in range(3, f - 3, 2):                        # vent slots
        p.carve(x, x + 1, 2, 3, 3, 5)
        p.carve(x, x + 1, f - 3, f - 2, 3, 5)
    p.box(4, 8, 4, 8, 6, 8, "trim")                     # rooftop units
    p.box(10, 12, 9, 13, 6, 7, "trim")
    for z in range(6, 8 + 14):                          # antenna mast with beacon
        p.v[(12, 4, z)] = "trim"
    p.v[(12, 4, 22)] = "beacon"
    for x in range(2, f - 2):                           # cool crown light line
        p.v[(x, 1, 1)] = "lit_cool"; p.v[(x, f - 2, 1)] = "lit_cool"
        p.v[(1, x, 1)] = "lit_cool"; p.v[(f - 2, x, 1)] = "lit_cool"
    return p


def crown_1x1(rng):
    p = Piece("Crown_1x1", 1, 1, 1, "crown")
    f = p.fx
    p.box(1, f - 1, 1, f - 1, 0, 3, "concrete")
    p.box(0, f, 0, f, 3, 4, "trim")
    p.box(2, f - 2, 2, f - 2, 4, 6, "dark")
    p.box(3, 5, 3, 5, 6, 8, "trim")
    for z in range(8, 8 + 16):
        p.v[(3, 3, z)] = "trim"
    p.v[(3, 3, 24)] = "beacon"
    for x in range(2, f - 2):
        p.v[(x, 1, 2)] = "lit_cool"; p.v[(x, f - 2, 2)] = "lit_cool"
    return p


def build_kit(seed=11):
    rng = random.Random(seed)
    return [base_2x2(rng), base_1x1(rng), shaft_2x2_fins(rng, "A"), shaft_2x2_fins(rng, "B"),
            shaft_2x2_banded(rng), setback_2x2_to_1x1(rng), shaft_1x1(rng, "A"), shaft_1x1(rng, "B"),
            crown_2x2(rng), crown_1x1(rng)]


if __name__ == "__main__":
    for p in build_kit():
        print(f"{p.name:22s} footprint {p.fx // TILE}x{p.fy // TILE} tiles, height {p.h * V:.0f} m, {len(p.v)} voxels")
