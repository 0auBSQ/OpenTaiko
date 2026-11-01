#!/usr/bin/env bash
# Resign an unsigned OpenTaiko .ipa with a signing identity and provisioning profile.
#
# Usage: ./OpenTaiko.iOS/scripts/resign-ipa.sh --input FILE [options]
#   --input FILE          Input .ipa file (required)
#   --output FILE         Output .ipa path (default: input path with -signed suffix)
#   --identity NAME       Codesign identity (default: auto-detect "Apple Development")
#   --profile FILE        Provisioning profile (default: auto-detect from installed profiles)
#   --bundle-id ID        Override the bundle identifier in Info.plist
#   --entitlements FILE   Custom entitlements plist (auto-extracted from profile if omitted)
#
# Examples:
#   # Resign with auto-detected identity and profile
#   ./OpenTaiko.iOS/scripts/resign-ipa.sh --input OpenTaiko.ipa
#
#   # Resign with a specific identity and custom bundle ID
#   ./OpenTaiko.iOS/scripts/resign-ipa.sh --input OpenTaiko.ipa \
#     --identity "Apple Distribution" --bundle-id com.example.OpenTaiko

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/_signing-helpers.sh"

INPUT=""
OUTPUT=""
IDENTITY=""
PROFILE=""
BUNDLE_ID=""
ENTITLEMENTS=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --input)        INPUT="$2"; shift 2 ;;
    --output)       OUTPUT="$2"; shift 2 ;;
    --identity)     IDENTITY="$2"; shift 2 ;;
    --profile)      PROFILE="$2"; shift 2 ;;
    --bundle-id)    BUNDLE_ID="$2"; shift 2 ;;
    --entitlements) ENTITLEMENTS="$2"; shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

if [[ -z "$INPUT" ]]; then
  echo "Error: --input is required."
  echo "Usage: $0 --input FILE [--output FILE] [--identity NAME] [--profile FILE] [--bundle-id ID]"
  exit 1
fi

if [[ ! -f "$INPUT" ]]; then
  echo "Error: Input file not found: $INPUT"
  exit 1
fi

# Default output: input-signed.ipa
if [[ -z "$OUTPUT" ]]; then
  OUTPUT="${INPUT%.ipa}-signed.ipa"
fi

WORKDIR=$(mktemp -d)
trap "rm -rf $WORKDIR" EXIT

echo "==> Extracting IPA..."
unzip -q "$INPUT" -d "$WORKDIR"

# Locate the .app bundle inside Payload/
APP_BUNDLE=$(find "$WORKDIR/Payload" -maxdepth 1 -name "*.app" -type d | head -1)
if [[ -z "$APP_BUNDLE" ]]; then
  echo "Error: No .app bundle found in IPA Payload/"
  exit 1
fi

APP_NAME=$(basename "$APP_BUNDLE")
echo "==> Found app: $APP_NAME"

# Determine the effective bundle ID (override or read from app)
if [[ -n "$BUNDLE_ID" ]]; then
  EFFECTIVE_BUNDLE_ID="$BUNDLE_ID"
  echo "==> Setting bundle ID to: $BUNDLE_ID"
  /usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier $BUNDLE_ID" "$APP_BUNDLE/Info.plist"
else
  EFFECTIVE_BUNDLE_ID=$(/usr/libexec/PlistBuddy -c "Print :CFBundleIdentifier" "$APP_BUNDLE/Info.plist" 2>/dev/null)
  echo "==> Bundle ID: $EFFECTIVE_BUNDLE_ID"
fi

# Auto-detect signing identity if not provided
if [[ -z "$IDENTITY" ]]; then
  echo "==> Auto-detecting signing identity..."
  IDENTITY=$(find_codesign_identity "Apple Development")
  echo "    Found: $IDENTITY"
fi

# Auto-detect provisioning profile if not provided
if [[ -z "$PROFILE" ]]; then
  echo "==> Auto-detecting provisioning profile for $EFFECTIVE_BUNDLE_ID..."
  PROFILE=$(find_provisioning_profile "$EFFECTIVE_BUNDLE_ID")
  echo "    Found: $(basename "$PROFILE")"
fi

if [[ ! -f "$PROFILE" ]]; then
  echo "Error: Provisioning profile not found: $PROFILE"
  exit 1
fi

# Embed provisioning profile
echo "==> Embedding provisioning profile..."
cp "$PROFILE" "$APP_BUNDLE/embedded.mobileprovision"

# Extract entitlements from the provisioning profile if not provided
if [[ -z "$ENTITLEMENTS" ]]; then
  ENTITLEMENTS="$WORKDIR/entitlements.plist"
  echo "==> Extracting entitlements from provisioning profile..."
  security cms -D -i "$PROFILE" > "$WORKDIR/profile.plist" 2>/dev/null
  /usr/libexec/PlistBuddy -x -c "Print :Entitlements" "$WORKDIR/profile.plist" > "$ENTITLEMENTS"
fi

# Remove existing code signature
rm -rf "$APP_BUNDLE/_CodeSignature"

# Re-sign all embedded frameworks and dylibs first
if [[ -d "$APP_BUNDLE/Frameworks" ]]; then
  echo "==> Signing embedded frameworks..."
  find "$APP_BUNDLE/Frameworks" -maxdepth 1 \( -name "*.framework" -o -name "*.dylib" \) -print0 | while IFS= read -r -d '' fw; do
    codesign --force --sign "$IDENTITY" --timestamp=none "$fw"
  done
fi

# Sign the main app bundle
echo "==> Signing app with identity: $IDENTITY"
codesign --force --sign "$IDENTITY" \
  --entitlements "$ENTITLEMENTS" \
  --generate-entitlement-der \
  "$APP_BUNDLE"

# Verify signature
echo "==> Verifying signature..."
codesign --verify --deep --strict "$APP_BUNDLE" 2>&1 || {
  echo "Error: Signature verification failed."
  exit 1
}

# Repackage as IPA
echo "==> Packaging signed IPA..."
mkdir -p "$(dirname "$OUTPUT")"
(cd "$WORKDIR" && zip -qr ipa.zip Payload)
mv "$WORKDIR/ipa.zip" "$OUTPUT"

echo "==> Done: $OUTPUT ($(du -h "$OUTPUT" | awk '{print $1}'))"
