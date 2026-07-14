"""Generate the Android launcher icon set from OpenTaiko/OpenTaiko.ico (Pillow required).

Matches the iOS icon composition (OpenTaiko.iOS/scripts/compose_icon.py): the transparent
full-bleed logo centered on an opaque white background with a margin.

Outputs into OpenTaiko.Android/Resources/:
  mipmap-{mdpi..xxxhdpi}/ic_launcher.png            legacy icons (API 24-25)
  mipmap-{mdpi..xxxhdpi}/ic_launcher_foreground.png adaptive foreground (API 26+; the white
                                                    background is a color resource)

Usage: py -3 OpenTaiko.Android/scripts/make-appicons.py
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))          # OpenTaiko.Android/
ICO = os.path.join(os.path.dirname(ROOT), "OpenTaiko", "OpenTaiko.ico")
RES = os.path.join(ROOT, "Resources")

# density -> (legacy px, adaptive canvas px = 108dp * scale)
DENSITIES = {
    "mdpi": (48, 108),
    "hdpi": (72, 162),
    "xhdpi": (96, 216),
    "xxhdpi": (144, 324),
    "xxxhdpi": (192, 432),
}

im = Image.open(ICO)
if getattr(im, "ico", None):
    im = im.ico.getimage(max(im.ico.sizes()))
logo = im.convert("RGBA")


def fit_center(canvas, logo, frac):
    """Paste logo centered on canvas, scaled to frac of the canvas edge (keeps aspect)."""
    side = canvas.size[0]
    target = int(side * frac)
    ratio = min(target / logo.width, target / logo.height)
    scaled = logo.resize((max(1, int(logo.width * ratio)), max(1, int(logo.height * ratio))), Image.LANCZOS)
    pos = ((side - scaled.width) // 2, (side - scaled.height) // 2)
    canvas.alpha_composite(scaled, pos)
    return canvas


for density, (legacy_px, fg_px) in DENSITIES.items():
    outdir = os.path.join(RES, f"mipmap-{density}")
    os.makedirs(outdir, exist_ok=True)

    # Legacy: white background, logo with a margin (like the iOS icons).
    legacy = fit_center(Image.new("RGBA", (legacy_px, legacy_px), (255, 255, 255, 255)), logo, 0.80)
    legacy.convert("RGB").save(os.path.join(outdir, "ic_launcher.png"))

    # Adaptive foreground: transparent, logo inside the 66% safe zone (launchers mask/zoom it).
    fg = fit_center(Image.new("RGBA", (fg_px, fg_px), (0, 0, 0, 0)), logo, 0.58)
    fg.save(os.path.join(outdir, "ic_launcher_foreground.png"))

print("icons written under", RES)
