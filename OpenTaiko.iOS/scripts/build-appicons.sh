#!/bin/bash
# Generate the iOS AppIcon PNGs from the master OpenTaiko.ico (shared with the desktop build).
# The .ico is a full-bleed logo on transparency. The checked-in icons sit on a white
# background with a centered margin, so each size is scaled to 70 percent and composited
# onto white by compose_icon.py. The per-size PNGs are not committed.
# Run at build time by deploy.sh (bootstrap_ios_deps) when the icons are missing. To refresh
# after changing the logo, update OpenTaiko/OpenTaiko.ico and delete the generated icon_*.png.
#
# NOTE: OpenTaiko.ico is 256x256, so icon_1024.png (the App Store marketing icon) is upscaled.
# Provide a higher-resolution master if a crisp 1024 is needed.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$SCRIPT_DIR/../.." && pwd)"
ICO="$REPO/OpenTaiko/OpenTaiko.ico"
OUT="$REPO/OpenTaiko.iOS/Assets.xcassets/AppIcon.appiconset"

# Sizes referenced by AppIcon.appiconset/Contents.json.
SIZES="20 29 40 58 60 76 80 87 120 152 167 180 1024"

# Logo occupies 70 percent of the icon, centered, matching the original artwork.
SCALE_PCT=70

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

echo "[build-appicons] generating AppIcon PNGs from $ICO"
for s in $SIZES; do
  content=$(( (s * SCALE_PCT + 50) / 100 ))
  sips -s format png -z "$content" "$content" "$ICO" --out "$TMP/logo_$s.png" >/dev/null
  python3 "$SCRIPT_DIR/compose_icon.py" "$TMP/logo_$s.png" "$s" "$OUT/icon_$s.png"
done
echo "[build-appicons] done -> $OUT"
