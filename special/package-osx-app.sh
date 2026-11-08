#!/bin/bash
# Package the osx-arm64 publish output as OpenTaiko.app + OpenTaiko-osx-arm64.zip.
# Run build-osx-arm64.sh first. Expects dotnet's publish output at
# OpenTaiko/bin/Release/net10.0/osx-arm64/publish/.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$PROJECT_DIR/OpenTaiko/bin/Release/net10.0/osx-arm64/publish"
APP="$PROJECT_DIR/OpenTaiko.app"

if [[ ! -f "$PUBLISH_DIR/OpenTaiko" ]]; then
  echo "[package-osx-app] publish output not found; run build-osx-arm64.sh first" >&2
  exit 1
fi

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

echo "[package-osx-app] copying publish output into bundle..."
# Everything lives next to the executable: Program.cs resets the CWD to the exe's folder on macOS
# (the apphost otherwise chdirs into Contents/Resources), and AppContext.BaseDirectory is MacOS/.
cp -R "$PUBLISH_DIR/." "$APP/Contents/MacOS/"

# keep only this platform's native libs (the publish dir carries all platforms' FFmpeg/Libs)
for dir in FFmpeg Libs; do
	find "$APP/Contents/MacOS/$dir" -mindepth 1 -maxdepth 1 ! -name osx-arm64 -exec rm -rf {} +
done

echo "[package-osx-app] writing Info.plist..."
cat > "$APP/Contents/Info.plist" <<'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CFBundleExecutable</key>
	<string>OpenTaiko</string>
	<key>CFBundleIdentifier</key>
	<string>io.opentaiko.OpenTaiko</string>
	<key>CFBundleName</key>
	<string>OpenTaiko</string>
	<key>CFBundleDisplayName</key>
	<string>OpenTaiko</string>
	<key>CFBundleIconFile</key>
	<string>OpenTaiko</string>
	<key>CFBundlePackageType</key>
	<string>APPL</string>
	<key>CFBundleInfoDictionaryVersion</key>
	<string>6.0</string>
	<key>CFBundleShortVersionString</key>
	<string>0.7.0.5</string>
	<key>CFBundleVersion</key>
	<string>0.7.0.5</string>
	<key>LSMinimumSystemVersion</key>
	<string>13.0</string>
	<key>NSHighResolutionCapable</key>
	<true/>
</dict>
</plist>
EOF

echo "[package-osx-app] building icon..."
TMP_ICON="$(mktemp -d)/OpenTaiko.png"
sips -s format png "$PROJECT_DIR/OpenTaiko/OpenTaiko.ico" --out "$TMP_ICON" >/dev/null
sips -s format icns "$TMP_ICON" --out "$APP/Contents/Resources/OpenTaiko.icns" >/dev/null

echo "[package-osx-app] skipping codesign (macOS 26 rejects bundles containing unsigned data files; \
an unsigned local build opens directly, other Macs need right-click -> Open)"

echo "[package-osx-app] NOTE: move the app out of Downloads (e.g. to /Applications) before first \
launch — macOS runs downloaded apps from a read-only temporary copy, where the game cannot save \
settings or scores."

echo "[package-osx-app] zipping..."
rm -f "$PROJECT_DIR/OpenTaiko-osx-arm64.zip"
ditto -c -k --keepParent "$APP" "$PROJECT_DIR/OpenTaiko-osx-arm64.zip"

echo "[package-osx-app] done: $APP ($(du -sh "$APP" | cut -f1)), $PROJECT_DIR/OpenTaiko-osx-arm64.zip"
