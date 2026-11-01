#!/bin/bash
# Build liblua54.xcframework from Lua 5.4.6 source for iOS device + simulator.
# Output: OpenTaiko.iOS/Frameworks/liblua54.xcframework/
#
# Usage: bash OpenTaiko.iOS/scripts/build-lua54.sh
# Called automatically by deploy.sh when the xcframework is missing.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_DIR/Frameworks/liblua54.xcframework"

if [[ -d "$OUTPUT_DIR" ]]; then
  echo "[build-lua54] xcframework already exists at $OUTPUT_DIR"
  exit 0
fi

LUA_VERSION="5.4.6"
LUA_URL="https://www.lua.org/ftp/lua-${LUA_VERSION}.tar.gz"
TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

echo "[build-lua54] Downloading Lua ${LUA_VERSION}..."
curl -sL "$LUA_URL" | tar xz -C "$TMPDIR"

SRC_DIR="$TMPDIR/lua-${LUA_VERSION}"

SRC="lapi.c lauxlib.c lbaselib.c lcode.c lcorolib.c lctype.c ldblib.c ldebug.c ldo.c ldump.c lfunc.c lgc.c linit.c liolib.c llex.c lmathlib.c lmem.c loadlib.c lobject.c lopcodes.c loslib.c lparser.c lstate.c lstring.c lstrlib.c ltable.c ltablib.c ltm.c lundump.c lutf8lib.c lvm.c lzio.c"

# Prepend src/ path
SRC_FILES=""
for f in $SRC; do
  SRC_FILES="$SRC_FILES $SRC_DIR/src/$f"
done

SIM_SDK="$(xcrun -sdk iphonesimulator --show-sdk-path)"
DEV_SDK="$(xcrun -sdk iphoneos --show-sdk-path)"

echo "[build-lua54] Building for simulator (arm64)..."
xcrun clang -arch arm64 \
  -isysroot "$SIM_SDK" \
  -mios-simulator-version-min=14.0 \
  -O2 -DLUA_COMPAT_5_3 '-Dl_system(cmd)=(-1)' \
  -dynamiclib \
  -install_name @rpath/liblua54.framework/liblua54 \
  -current_version 5.4.6 -compatibility_version 5.4.0 \
  -o "$TMPDIR/liblua54-sim.dylib" \
  $SRC_FILES

echo "[build-lua54] Building for device (arm64)..."
xcrun clang -arch arm64 \
  -isysroot "$DEV_SDK" \
  -mios-version-min=14.0 \
  -O2 -DLUA_COMPAT_5_3 '-Dl_system(cmd)=(-1)' \
  -dynamiclib \
  -install_name @rpath/liblua54.framework/liblua54 \
  -current_version 5.4.6 -compatibility_version 5.4.0 \
  -o "$TMPDIR/liblua54-dev.dylib" \
  $SRC_FILES

# Create framework bundles
INFOPLIST="$TMPDIR/Info.plist"
cat > "$INFOPLIST" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>liblua54</string>
  <key>CFBundleIdentifier</key>
  <string>org.lua.liblua54</string>
  <key>CFBundleName</key>
  <string>liblua54</string>
  <key>CFBundleVersion</key>
  <string>5.4.6</string>
  <key>CFBundleShortVersionString</key>
  <string>5.4.6</string>
  <key>CFBundlePackageType</key>
  <string>FMWK</string>
  <key>MinimumOSVersion</key>
  <string>14.0</string>
</dict>
</plist>
PLIST

mkdir -p "$TMPDIR/sim/liblua54.framework" "$TMPDIR/dev/liblua54.framework"
cp "$TMPDIR/liblua54-sim.dylib" "$TMPDIR/sim/liblua54.framework/liblua54"
cp "$INFOPLIST" "$TMPDIR/sim/liblua54.framework/Info.plist"
cp "$TMPDIR/liblua54-dev.dylib" "$TMPDIR/dev/liblua54.framework/liblua54"
cp "$INFOPLIST" "$TMPDIR/dev/liblua54.framework/Info.plist"

echo "[build-lua54] Creating xcframework..."
xcodebuild -create-xcframework \
  -framework "$TMPDIR/sim/liblua54.framework" \
  -framework "$TMPDIR/dev/liblua54.framework" \
  -output "$OUTPUT_DIR"

echo "[build-lua54] Done: $OUTPUT_DIR"
ls "$OUTPUT_DIR"
