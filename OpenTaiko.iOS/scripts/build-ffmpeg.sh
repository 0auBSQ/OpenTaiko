#!/bin/bash
# Build FFmpeg (decode-only, FFmpeg 5.1 ABI to match FFmpeg.AutoGen 5.1.1) for iOS device and
# simulator as OpenTaiko.iOS/Frameworks/ffmpeg.xcframework.
#
# The static libs are merged into one dynamic framework (clang -dynamiclib -all_load): a dylib
# exports its symbols so FFmpeg.AutoGen can dlsym them from the main program handle. Linking
# the static .a's directly into the app localizes the symbols and breaks that lookup.
#
# Called automatically by deploy.sh.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_DIR/Frameworks"
LIBS=(avformat avcodec swscale swresample avutil)   # link order: consumers before providers

if [[ -d "$OUTPUT_DIR/ffmpeg.xcframework" ]]; then
  echo "[build-ffmpeg] ffmpeg.xcframework already exists in $OUTPUT_DIR"
  exit 0
fi

FFMPEG_VERSION="5.1.6"
MIN_IOS="14.0"
WORK="$(mktemp -d)"
trap "rm -rf $WORK" EXIT

echo "[build-ffmpeg] Downloading FFmpeg ${FFMPEG_VERSION}..."
curl -sL "https://ffmpeg.org/releases/ffmpeg-${FFMPEG_VERSION}.tar.xz" | tar xJ -C "$WORK" \
  || curl -sL "https://github.com/FFmpeg/FFmpeg/archive/refs/tags/n${FFMPEG_VERSION}.tar.gz" | tar xz -C "$WORK"
SRC="$(echo "$WORK"/ffmpeg-* "$WORK"/FFmpeg-* 2>/dev/null | tr ' ' '\n' | grep -vE '\*' | head -1)"
echo "[build-ffmpeg] source: $SRC"

# Decode-only, no encoders/muxers/programs/network/filters.
COMMON_CONFIG=(
  --enable-cross-compile --target-os=darwin --arch=arm64 --enable-pic
  --enable-static --disable-shared
  --disable-programs --disable-doc --disable-debug --disable-autodetect
  --disable-encoders --disable-muxers --disable-network --disable-bsfs
  --disable-avdevice --disable-avfilter --disable-postproc
  --disable-gpl --disable-nonfree
  --enable-bsf=h264_mp4toannexb,hevc_mp4toannexb,extract_extradata,vp9_superframe
  --enable-protocol=file
)

build_arch() {
  local SDK="$1" MINFLAG="$2"
  [[ "$SDK" == iphoneos || "$SDK" == iphonesimulator ]] || { echo "[build-ffmpeg] build_arch: unknown sdk '$SDK'"; exit 1; }
  local SYSROOT; SYSROOT="$(xcrun -sdk "$SDK" --show-sdk-path)"
  local CC; CC="$(xcrun -sdk "$SDK" -f clang)"
  local THIN="$WORK/thin/$SDK-arm64"
  echo "[build-ffmpeg] === configuring $SDK arm64 ==="
  ( cd "$SRC" && make distclean >/dev/null 2>&1 || true
    ./configure "${COMMON_CONFIG[@]}" \
      --prefix="$THIN" \
      --cc="$CC" \
      --as="$CC" \
      --sysroot="$SYSROOT" \
      --extra-cflags="-arch arm64 $MINFLAG -isysroot $SYSROOT -fno-stack-check" \
      --extra-ldflags="-arch arm64 $MINFLAG -isysroot $SYSROOT" \
      >/tmp/ffmpeg-configure-$SDK.log 2>&1 \
      || { echo "[build-ffmpeg] configure FAILED for $SDK (see /tmp/ffmpeg-configure-$SDK.log)"; tail -25 /tmp/ffmpeg-configure-$SDK.log; exit 1; }
    echo "[build-ffmpeg] === building $SDK arm64 ($(sysctl -n hw.ncpu) jobs) ==="
    make -j"$(sysctl -n hw.ncpu)" >/tmp/ffmpeg-build-$SDK.log 2>&1 \
      || { echo "[build-ffmpeg] make FAILED for $SDK (see /tmp/ffmpeg-build-$SDK.log)"; tail -25 /tmp/ffmpeg-build-$SDK.log; exit 1; }
    make install >/dev/null 2>&1 )
}

# Merge one arch's static libs into a single dynamic ffmpeg.framework.
merge_dynamic() {  # $1 = sdk, $2 = min-flag, $3 = out framework parent dir
  local SDK="$1" MINFLAG="$2" OUT="$3"
  local SYSROOT; SYSROOT="$(xcrun -sdk "$SDK" --show-sdk-path)"
  local THIN="$WORK/thin/$SDK-arm64"
  local archives=""
  for l in "${LIBS[@]}"; do archives="$archives $THIN/lib/lib$l.a"; done
  mkdir -p "$OUT/ffmpeg.framework"
  echo "[build-ffmpeg] === linking $SDK dynamic ffmpeg ==="
  xcrun clang -dynamiclib -arch arm64 -isysroot "$SYSROOT" $MINFLAG \
    -Wl,-all_load $archives \
    -install_name @rpath/ffmpeg.framework/ffmpeg \
    -o "$OUT/ffmpeg.framework/ffmpeg"
  cat > "$OUT/ffmpeg.framework/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleExecutable</key><string>ffmpeg</string>
  <key>CFBundleIdentifier</key><string>org.ffmpeg.ffmpeg</string>
  <key>CFBundleName</key><string>ffmpeg</string>
  <key>CFBundleVersion</key><string>${FFMPEG_VERSION}</string>
  <key>CFBundleShortVersionString</key><string>${FFMPEG_VERSION}</string>
  <key>CFBundlePackageType</key><string>FMWK</string>
  <key>MinimumOSVersion</key><string>${MIN_IOS}</string>
</dict></plist>
PLIST
}

build_arch iphoneos        "-mios-version-min=$MIN_IOS"
build_arch iphonesimulator "-mios-simulator-version-min=$MIN_IOS"

merge_dynamic iphoneos        "-mios-version-min=$MIN_IOS"           "$WORK/fw/dev"
merge_dynamic iphonesimulator "-mios-simulator-version-min=$MIN_IOS" "$WORK/fw/sim"

echo "[build-ffmpeg] Creating ffmpeg.xcframework..."
rm -rf "$OUTPUT_DIR/ffmpeg.xcframework"
xcodebuild -create-xcframework \
  -framework "$WORK/fw/dev/ffmpeg.framework" \
  -framework "$WORK/fw/sim/ffmpeg.framework" \
  -output "$OUTPUT_DIR/ffmpeg.xcframework" >/dev/null

echo "[build-ffmpeg] Done -> $OUTPUT_DIR/ffmpeg.xcframework"
