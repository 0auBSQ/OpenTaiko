#!/usr/bin/env bash
# Download BASS, BASSmix, and BASS_FX iOS xcframeworks from un4seen.com.
# Output: OpenTaiko.iOS/Libs/{bass24-ios,bassmix24-ios,bass_fx24-ios}/*.xcframework
#
# Usage: bash OpenTaiko.iOS/scripts/download-bass.sh
# Called automatically by deploy.sh when the Libs are missing.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
LIBS_DIR="$PROJECT_DIR/Libs"

# bass_fx is hosted under /z/0/ on un4seen.com
BASS_URL="https://www.un4seen.com/files/bass24-ios.zip"
BASSMIX_URL="https://www.un4seen.com/files/bassmix24-ios.zip"
BASS_FX_URL="https://www.un4seen.com/files/z/0/bass_fx24-ios.zip"

download_lib() {
  local url="$1"
  local name="$2"
  local dest="$LIBS_DIR/$name"

  if [[ -d "$dest" ]]; then
    echo "[download-bass] $name already exists, skipping"
    return
  fi

  echo "[download-bass] Downloading $name..."
  local tmpdir
  tmpdir=$(mktemp -d)
  trap "rm -rf $tmpdir" RETURN

  curl -sfL "$url" -o "$tmpdir/$name.zip"
  mkdir -p "$dest"
  unzip -q "$tmpdir/$name.zip" -d "$dest"

  # Verify an xcframework was extracted
  local found
  found=$(find "$dest" -maxdepth 1 -name '*.xcframework' -type d | head -1)
  if [[ -z "$found" ]]; then
    echo "[download-bass] ERROR: No xcframework found in $name download" >&2
    rm -rf "$dest"
    return 1
  fi

  echo "[download-bass] Installed $(basename "$found") in $dest"
}

mkdir -p "$LIBS_DIR"

download_lib "$BASS_URL" "bass24-ios"
download_lib "$BASSMIX_URL" "bassmix24-ios"
download_lib "$BASS_FX_URL" "bass_fx24-ios"

echo "[download-bass] Done. Libraries in $LIBS_DIR"
