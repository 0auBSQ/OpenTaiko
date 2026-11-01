#!/bin/bash
# Build liblmdb.xcframework from LMDB source for iOS device + simulator.
# Output: OpenTaiko.iOS/Frameworks/liblmdb.xcframework/
#
# LightningDB (the .NET wrapper OpenTaiko uses for Lua data persistence) P/Invokes the native
# "lmdb" library. The NuGet package ships only desktop natives, so on iOS we build LMDB from
# source (it is portable C: mdb.c + midl.c) the same way we build liblua54.
#
# Usage: bash OpenTaiko.iOS/scripts/build-lmdb.sh
# Called automatically by deploy.sh when the xcframework is missing.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_DIR/Frameworks/liblmdb.xcframework"

if [[ -d "$OUTPUT_DIR" ]]; then
  echo "[build-lmdb] xcframework already exists at $OUTPUT_DIR"
  exit 0
fi

LMDB_VERSION="0.9.33"
LMDB_URL="https://github.com/LMDB/lmdb/archive/refs/tags/LMDB_${LMDB_VERSION}.tar.gz"
TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

echo "[build-lmdb] Downloading LMDB ${LMDB_VERSION}..."
curl -sL "$LMDB_URL" | tar xz -C "$TMPDIR"

SRC_DIR="$TMPDIR/lmdb-LMDB_${LMDB_VERSION}/libraries/liblmdb"
SRC_FILES="$SRC_DIR/mdb.c $SRC_DIR/midl.c"

SIM_SDK="$(xcrun -sdk iphonesimulator --show-sdk-path)"
DEV_SDK="$(xcrun -sdk iphoneos --show-sdk-path)"

# LMDB auto-disables robust mutexes on Apple platforms; no extra flags needed.
echo "[build-lmdb] Building for simulator (arm64)..."
xcrun clang -arch arm64 \
  -isysroot "$SIM_SDK" \
  -mios-simulator-version-min=14.0 \
  -O2 \
  -dynamiclib \
  -install_name @rpath/liblmdb.framework/liblmdb \
  -current_version ${LMDB_VERSION} -compatibility_version 0.9.0 \
  -o "$TMPDIR/liblmdb-sim.dylib" \
  $SRC_FILES

echo "[build-lmdb] Building for device (arm64)..."
xcrun clang -arch arm64 \
  -isysroot "$DEV_SDK" \
  -mios-version-min=14.0 \
  -O2 \
  -dynamiclib \
  -install_name @rpath/liblmdb.framework/liblmdb \
  -current_version ${LMDB_VERSION} -compatibility_version 0.9.0 \
  -o "$TMPDIR/liblmdb-dev.dylib" \
  $SRC_FILES

INFOPLIST="$TMPDIR/Info.plist"
cat > "$INFOPLIST" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>liblmdb</string>
  <key>CFBundleIdentifier</key>
  <string>org.symas.liblmdb</string>
  <key>CFBundleName</key>
  <string>liblmdb</string>
  <key>CFBundleVersion</key>
  <string>0.9.33</string>
  <key>CFBundleShortVersionString</key>
  <string>0.9.33</string>
  <key>CFBundlePackageType</key>
  <string>FMWK</string>
  <key>MinimumOSVersion</key>
  <string>14.0</string>
</dict>
</plist>
PLIST

mkdir -p "$TMPDIR/sim/liblmdb.framework" "$TMPDIR/dev/liblmdb.framework"
cp "$TMPDIR/liblmdb-sim.dylib" "$TMPDIR/sim/liblmdb.framework/liblmdb"
cp "$INFOPLIST" "$TMPDIR/sim/liblmdb.framework/Info.plist"
cp "$TMPDIR/liblmdb-dev.dylib" "$TMPDIR/dev/liblmdb.framework/liblmdb"
cp "$INFOPLIST" "$TMPDIR/dev/liblmdb.framework/Info.plist"

echo "[build-lmdb] Creating xcframework..."
xcodebuild -create-xcframework \
  -framework "$TMPDIR/sim/liblmdb.framework" \
  -framework "$TMPDIR/dev/liblmdb.framework" \
  -output "$OUTPUT_DIR"

echo "[build-lmdb] Done: $OUTPUT_DIR"
ls "$OUTPUT_DIR"
