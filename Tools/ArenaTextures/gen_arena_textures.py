"""Procedural arena textures for VoxelCircuit.

Wall: one vertical slice of the arena wall, 16 m wide (tiles around the ring) x full wall height (no vertical tiling).
  Layout (bottom to top): dark kick strip with a thin accent line near the floor, calm low-contrast panels across the
  play band (ships fly here, so it stays quiet), and a top trim with an accent line and small segment lights.
Floor: 16 m x ring depth, tiles around the ring. Dark plates with a thin accent lane near the wall.
Outputs albedo (sRGB), normal (OpenGL / +Y up, for Unity) and emission (sRGB colour, intensity set on the material).
"""
import sys, os
import numpy as np
from PIL import Image

PX = 64                      # pixels per metre
RNG = np.random.default_rng(7)


def hex_rgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32) / 255.0


PANEL = hex_rgb('#15172C')
PANEL_ALT = hex_rgb('#181A33')
SEAM = hex_rgb('#07060F')
TRIM = hex_rgb('#0D0E1C')
BOLT = hex_rgb('#262A4D')
ACCENT = hex_rgb('#3A4BFF')      # blue-violet: distinct from the player's cyan and the enemies' magenta/orange
ACCENT_DIM = hex_rgb('#2433A8')


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


class Canvas:
    def __init__(self, width_m, height_m):
        self.w, self.h = int(width_m * PX), int(height_m * PX)
        ys, xs = np.mgrid[0:self.h, 0:self.w]
        self.x = (xs + 0.5) / PX          # metres, left to right
        self.y = (ys + 0.5) / PX          # metres, bottom to top (row 0 = bottom)
        self.albedo = np.zeros((self.h, self.w, 3), np.float32)
        self.height = np.zeros((self.h, self.w), np.float32)
        self.emission = np.zeros((self.h, self.w, 3), np.float32)

    def rect_mask(self, x0, x1, y0, y1):
        return (self.x >= x0) & (self.x < x1) & (self.y >= y0) & (self.y < y1)

    def rect_inner_distance(self, x0, x1, y0, y1):
        return np.minimum.reduce([self.x - x0, x1 - self.x, self.y - y0, y1 - self.y])

    def panel(self, x0, x1, y0, y1, colour, seam=0.035, bevel=0.05, level=1.0):
        """A raised plate with dark seams and a bevelled edge."""
        m = self.rect_mask(x0, x1, y0, y1)
        d = self.rect_inner_distance(x0, x1, y0, y1)
        lift = smoothstep(seam, seam + bevel, d)
        self.height[m] = (level * lift)[m]
        shade = SEAM[None, :] * (1 - lift[m][:, None]) + colour[None, :] * lift[m][:, None]
        # a soft lighter rim on the bevel sells the edge without adding contrast to the face
        rim = (smoothstep(seam, seam + bevel * 0.5, d) * (1 - smoothstep(seam + bevel * 0.5, seam + bevel * 1.5, d)))[m]
        self.albedo[m] = shade + rim[:, None] * 0.025

    def inset(self, x0, x1, y0, y1, depth=0.12, bevel=0.06, darken=0.92):
        """A shallow recessed rectangle inside a panel: shows only in the lighting, barely in colour."""
        m = self.rect_mask(x0, x1, y0, y1)
        d = self.rect_inner_distance(x0, x1, y0, y1)
        t = smoothstep(0.0, bevel, d)[m]
        self.height[m] -= depth * t
        self.albedo[m] *= (1 - (1 - darken) * t)[:, None]

    def slots(self, x0, x1, y0, y1, count, vertical=False, depth=0.35):
        """Vent slots."""
        for i in range(count):
            if vertical:
                cx = x0 + (i + 0.5) * (x1 - x0) / count
                sx0, sx1, sy0, sy1 = cx - 0.035, cx + 0.035, y0, y1
            else:
                cy = y0 + (i + 0.5) * (y1 - y0) / count
                sx0, sx1, sy0, sy1 = x0, x1, cy - 0.035, cy + 0.035
            m = self.rect_mask(sx0, sx1, sy0, sy1)
            self.height[m] -= depth
            self.albedo[m] = SEAM

    def bolt(self, cx, cy, r=0.05):
        d = np.hypot(self.x - cx, self.y - cy)
        m = d < r * 1.6
        bump = (1 - smoothstep(r * 0.6, r * 1.6, d))[m]
        self.height[m] += 0.15 * bump
        self.albedo[m] = self.albedo[m] * (1 - bump[:, None]) + BOLT * bump[:, None]

    def glow_line(self, x0, x1, y, thickness=0.05, colour=ACCENT, dashed=None):
        m = self.rect_mask(x0, x1, y - thickness / 2, y + thickness / 2)
        if dashed:
            on, off = dashed
            m &= ((self.x - x0) % (on + off)) < on
        self.emission[m] = colour
        self.albedo[m] = colour * 0.35
        self.height[m] -= 0.05

    def glow_dot(self, cx, cy, size=0.08, colour=ACCENT_DIM):
        m = self.rect_mask(cx - size / 2, cx + size / 2, cy - size / 2, cy + size / 2)
        self.emission[m] = colour
        self.albedo[m] = colour * 0.35

    # ---------- output ----------
    def normal_map(self, strength=3.0, wrap_y=False):
        h = self.height
        dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5 * PX / PX
        if wrap_y:
            dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
        else:
            hp = np.pad(h, ((1, 1), (0, 0)), mode='edge')
            dy = (hp[2:] - hp[:-2]) * 0.5
        n = np.dstack([-dx * strength, -dy * strength, np.ones_like(h)])
        n /= np.linalg.norm(n, axis=2, keepdims=True)
        return n * 0.5 + 0.5

    def save(self, folder, name, wrap_y=False):
        def to_img(a):
            a = np.clip(a, 0, 1)
            return Image.fromarray((np.flipud(a) * 255 + 0.5).astype(np.uint8))   # flip: PNG row 0 is the top
        to_img(self.albedo).save(os.path.join(folder, f"{name}_Albedo.png"))
        to_img(self.normal_map(wrap_y=wrap_y)).save(os.path.join(folder, f"{name}_Normal.png"))
        to_img(self.emission).save(os.path.join(folder, f"{name}_Emission.png"))


def wall(folder):
    c = Canvas(16, 16)
    c.albedo[:] = SEAM
    # --- kick strip near the floor (0 - 1.2 m) ---
    for i in range(8):
        c.panel(i * 2, (i + 1) * 2, 0.0, 1.2, TRIM, level=0.8)
        c.slots(i * 2 + 0.35, i * 2 + 1.1, 0.25, 0.75, 4)
    c.glow_line(0, 16, 1.0, thickness=0.045)
    # --- main panels across the play band (1.2 - 14.4 m): calm, low contrast ---
    rows = [(1.2, 5.6), (5.6, 10.0), (10.0, 14.4)]
    for r, (y0, y1) in enumerate(rows):
        x = 0.0
        while x < 16 - 1e-6:
            width = 4.0 if RNG.random() < 0.65 or x > 12 - 1e-6 else 2.0
            colour = (PANEL if (r + int(x / 2)) % 2 == 0 else PANEL_ALT) * (0.96 + 0.08 * RNG.random())
            c.panel(x, x + width, y0, y1, colour)
            if width == 4.0 and RNG.random() < 0.5:
                c.inset(x + 0.45, x + width - 0.45, y0 + 0.45, y1 - 0.45)
            for bx, by in ((x + 0.18, y0 + 0.18), (x + width - 0.18, y0 + 0.18),
                           (x + 0.18, y1 - 0.18), (x + width - 0.18, y1 - 0.18)):
                c.bolt(bx, by)
            if r == 0 and RNG.random() < 0.35:
                c.slots(x + 0.5, x + width - 0.5, y0 + 0.5, y0 + 1.0, 3)
            x += width
    # --- top trim (14.4 - 16 m) ---
    for i in range(4):
        c.panel(i * 4, (i + 1) * 4, 14.4, 16.0, TRIM, level=0.8)
    c.glow_line(0, 16, 14.65, thickness=0.04)
    for i in range(16):
        c.glow_dot(i + 0.5, 15.35)
    c.save(folder, "Arena_Wall")


def floor(folder):
    c = Canvas(16, 8)
    c.albedo[:] = SEAM
    for gx in range(8):
        for gy in range(4):
            x0, y0 = gx * 2, gy * 2
            colour = TRIM * (0.9 + 0.2 * RNG.random())
            c.panel(x0, x0 + 2, y0, y0 + 2, colour, level=0.8)
            if (gx + gy) % 3 == 0:
                c.slots(x0 + 0.3, x0 + 1.7, y0 + 0.4, y0 + 1.6, 6, vertical=True, depth=0.25)
    # thin lane line near the wall edge (v small = near the wall)
    c.glow_line(0, 16, 0.35, thickness=0.04, colour=ACCENT_DIM, dashed=(1.5, 0.5))
    c.save(folder, "Arena_Floor", wrap_y=True)


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    wall(out)
    floor(out)
    print("ok")
