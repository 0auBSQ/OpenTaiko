#!/bin/bash
# Fetch the FFmpeg.AutoGen 5.1.1 binding source (pinned upstream commit) and apply the iOS patch.
# Called automatically by deploy.sh.
set -euo pipefail

# The last 5.1.1 commit before the abstractions rewrite; matches the published NuGet 5.1.1.
COMMIT=40873965266b526eeb7982ad45b1e51957eb5411
REPO=https://github.com/Ruslan-B/FFmpeg.AutoGen

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DEST="$SCRIPT_DIR/../third_party/FFmpeg.AutoGen/upstream"

if [[ -f "$DEST/FFmpeg.cs" ]]; then
  echo "[fetch-ffmpeg-autogen] upstream/ already present"
  exit 0
fi

WORK=$(mktemp -d); trap 'rm -rf "$WORK"' EXIT
echo "[fetch-ffmpeg-autogen] fetching $REPO @ $COMMIT ..."
git -C "$WORK" init -q
git -C "$WORK" remote add origin "$REPO"
git -C "$WORK" fetch -q --depth 1 origin "$COMMIT"
git -C "$WORK" checkout -q FETCH_HEAD -- FFmpeg.AutoGen

rm -rf "$DEST"
mkdir -p "$DEST"
cp -R "$WORK/FFmpeg.AutoGen/." "$DEST/"
rm -f "$DEST/FFmpeg.AutoGen.csproj"   # replaced by ours
patch -p1 -d "$DEST" < "$SCRIPT_DIR/../third_party/FFmpeg.AutoGen/ios-darwin-fallback.patch"
echo "[fetch-ffmpeg-autogen] done -> $DEST"
