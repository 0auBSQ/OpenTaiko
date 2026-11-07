#!/bin/bash
# Build FFmpeg (FFmpeg 5.1 ABI to match FFmpeg.AutoGen 5.1.1) for macOS arm64 into
# OpenTaiko/FFmpeg/osx-arm64/. Native host build (no cross-compile), same 7 libraries and
# plain dylib names as FFmpeg/osx-x64/.
#
# The dylibs keep a consistent --prefix install_name, mirroring osx-x64 where dependency
# resolution works because FFmpeg.AutoGen loads providers before consumers.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_DIR/OpenTaiko/FFmpeg/osx-arm64"
LIBS=(avcodec avformat avutil swresample swscale avdevice avfilter)

if [[ -f "$OUTPUT_DIR/libavcodec.59.dylib" ]]; then
  echo "[build-ffmpeg-osx-arm64] output already exists in $OUTPUT_DIR"
  exit 0
fi

FFMPEG_VERSION="5.1.6"
WORK="$(mktemp -d)"
trap "rm -rf $WORK" EXIT

echo "[build-ffmpeg-osx-arm64] Downloading FFmpeg ${FFMPEG_VERSION}..."
curl -sL "https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.xz" | tar xJ -C "$WORK" \
  || curl -sL "https://github.com/FFmpeg/FFmpeg/archive/refs/tags/n${FFMPEG_VERSION}.tar.gz" | tar xz -C "$WORK"
SRC="$(echo "$WORK"/ffmpeg-* "$WORK"/FFmpeg-* 2>/dev/null | tr ' ' '\n' | grep -vE '\*' | head -1)"
echo "[build-ffmpeg-osx-arm64] source: $SRC"

# Decode-focused but keeps encoders/muxers/avfilter (VideoExporter uses avfilter).
# Decode-only bits match OpenTaiko.iOS/scripts/build-ffmpeg.sh.
COMMON_CONFIG=(
  --target-os=darwin --arch=arm64 --enable-pic
  --enable-shared --disable-static
  --disable-programs --disable-doc --disable-debug --disable-autodetect
  --disable-network --disable-bsfs
  --enable-bsf=h264_mp4toannexb,hevc_mp4toannexb,extract_extradata,vp9_superframe
  --enable-protocol=file
  --disable-gpl --disable-nonfree
)

echo "[build-ffmpeg-osx-arm64] configuring..."
( cd "$SRC" && ./configure "${COMMON_CONFIG[@]}" --prefix="$WORK/install" )

echo "[build-ffmpeg-osx-arm64] compiling (this takes a while)..."
( cd "$SRC" && make -j"$(sysctl -n hw.ncpu)" )
( cd "$SRC" && make install )

mkdir -p "$OUTPUT_DIR"
for lib in "${LIBS[@]}"; do
  # copy dereferenced: plain libX.N.dylib names like FFmpeg/osx-x64
  major="$(basename "$(readlink "$WORK/install/lib/lib${lib}.dylib")" | cut -d. -f2)"
  cp -L "$WORK/install/lib/lib${lib}.dylib" "$OUTPUT_DIR/lib${lib}.${major}.dylib"
done

echo "[build-ffmpeg-osx-arm64] done:"
ls -la "$OUTPUT_DIR"
