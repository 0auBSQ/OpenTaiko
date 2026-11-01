#!/usr/bin/env bash
# Boot an iOS simulator and open the Simulator app.
# Usage: ./OpenTaiko.iOS/scripts/sim-boot.sh [device-name]
#   Defaults to the first available iPhone.
set -euo pipefail

DEVICE="${1:-}"

if [[ -z "$DEVICE" ]]; then
  # Pick the first available iPhone
  DEVICE=$(xcrun simctl list devices available | grep -i iphone | head -1 | sed 's/.*(\([-A-F0-9]*\)).*/\1/')
  if [[ -z "$DEVICE" ]]; then
    echo "No iPhone simulators found. Install an iOS simulator runtime in Xcode."
    exit 1
  fi
fi

echo "Booting simulator: $DEVICE"
xcrun simctl boot "$DEVICE" 2>/dev/null || echo "(already booted)"
open -a Simulator
