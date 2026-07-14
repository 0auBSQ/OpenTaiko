#!/usr/bin/env bash
# Unified build/deploy script for OpenTaiko iOS.
#
# Usage: ./OpenTaiko.iOS/scripts/deploy.sh [TARGET] [options]
#
# TARGET (positional, default: sim):
#   sim         Build, install, and launch on a booted simulator (with console/screenshot)
#   device      Build, install, and launch on a connected physical device
#   ipa         Build Release and package a distributable .ipa (+ dSYM zip)
#
# Common options:
#   --clean            Wipe obj/+bin/ (true clean rebuild) and uninstall the app before installing
#   --no-build         Skip the build step (reuse an existing .app)
#   --release          Build Release instead of Debug (sim/device; ipa is always Release)
#   --bundle-id ID     Override the bundle identifier (default: from .csproj)
#   --verbose          Stream the full build log instead of a filtered summary
#   -h, --help         Show this help
#
# sim options:
#   --device ID        Simulator device (default: booted)
#   --timeout N        Seconds to stream console output (default: 10, 0=unlimited)
#   --screenshot [F]   Take a screenshot after launch (default file: /tmp/opentaiko.png)
#   --wait N           Seconds to wait before the screenshot (default: 20)
#   --launch-check     Startup smoke-test: exit non-zero unless the app reached its first
#                      frame without a startup crash (for CI / change validation)
#
# device options:
#   --device ID        devicectl device id (default: auto-detect)
#   --udid UDID        libimobiledevice udid (implies --imobile)
#   --imobile          Install via ideviceinstaller instead of devicectl
#   --timeout N        Seconds to stream console output (default: 30, 0=unlimited)
#   --identity NAME    Codesign identity (default: auto-detect "Apple Development")
#
# ipa options:
#   --output PATH      Output .ipa path (default: OpenTaiko.iOS/dist/OpenTaiko_unsigned.ipa)
#   --sign MODE        none (default, unsigned) | development | distribution
#   --identity NAME    Codesign identity (default depends on --sign)
#
set -euo pipefail
SELF="${BASH_SOURCE[0]}"; [[ "$SELF" = /* ]] || SELF="$PWD/$SELF"
cd "$(dirname "$0")/../.."
source "OpenTaiko.iOS/scripts/_signing-helpers.sh"

# Print the usage/doc block at the top of this file, then exit, on --help / -h.
usage() { awk 'NR==1 {next} /^#/ {sub(/^# ?/, ""); print; next} {exit}' "$SELF"; }
for _a in "$@"; do case "$_a" in -h|--help) usage; exit 0 ;; esac; done

CSPROJ="OpenTaiko.iOS/OpenTaiko.iOS.csproj"

# ---- target (first positional arg) -------------------------------------------------------
TARGET="sim"
if [[ $# -gt 0 && "$1" != --* ]]; then
  TARGET="$1"; shift
fi
case "$TARGET" in
  sim|device|ipa) ;;
  *) echo "Unknown target: $TARGET (expected sim|device|ipa)"; exit 1 ;;
esac

# ---- options -----------------------------------------------------------------------------
BUNDLE_ID=""
IDENTITY=""
DEVICE=""
UDID=""
IMOBILE=false
CLEAN=false
BUILD=true
VERBOSE=false
CONFIG="Debug"
TIMEOUT=""           # per-target default applied below
SCREENSHOT=""
WAIT=20
LAUNCH_CHECK=false
OUTPUT=""             # per-target default applied below
SIGN="none"
UPLOAD=true
TEAM_ID="8LW2EYFXQD"
API_KEY=""
API_ISSUER=""
API_KEY_ID=""
TAG=""
DRY_RUN=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --clean)      CLEAN=true; shift ;;
    --no-build)   BUILD=false; shift ;;
    --release)    CONFIG="Release"; shift ;;
    --bundle-id)  BUNDLE_ID="$2"; shift 2 ;;
    --verbose)    VERBOSE=true; shift ;;
    --device)     DEVICE="$2"; shift 2 ;;
    --udid)       UDID="$2"; IMOBILE=true; shift 2 ;;
    --imobile)    IMOBILE=true; shift ;;
    --timeout)    TIMEOUT="$2"; shift 2 ;;
    --identity)   IDENTITY="$2"; shift 2 ;;
    --screenshot) SCREENSHOT="${2:-/tmp/opentaiko.png}"; shift; [[ "${1:-}" != --* && -n "${1:-}" ]] && shift || true ;;
    --wait)       WAIT="$2"; shift 2 ;;
    --launch-check) LAUNCH_CHECK=true; shift ;;
    --output)     OUTPUT="$2"; shift 2 ;;
    --sign)       SIGN="$2"; shift 2 ;;
    --no-upload)  UPLOAD=false; shift ;;
    --team-id)    TEAM_ID="$2"; shift 2 ;;
    --api-key)    API_KEY="$2"; shift 2 ;;
    --api-issuer) API_ISSUER="$2"; shift 2 ;;
    --api-key-id) API_KEY_ID="$2"; shift 2 ;;
    --tag)        TAG="$2"; shift 2 ;;
    --dry-run)    DRY_RUN=true; shift ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

# Resolve bundle ID and the matching dotnet -p:ApplicationId override (if any).
DEFAULT_BUNDLE_ID=$(grep '<ApplicationId' "$CSPROJ" | sed 's/.*>\(.*\)<.*/\1/')
APP_ID="${BUNDLE_ID:-$DEFAULT_BUNDLE_ID}"
BUNDLE_ID_ARG=()
[[ -n "$BUNDLE_ID" && "$BUNDLE_ID" != "$DEFAULT_BUNDLE_ID" ]] && BUNDLE_ID_ARG=(-p:ApplicationId="$BUNDLE_ID")

# ==========================================================================================
#  Shared helpers
# ==========================================================================================

# Download/build native dependencies (BASS xcframeworks, liblua54) if missing.
bootstrap_ios_deps() {
  [[ -d "OpenTaiko.iOS/Libs/bass24-ios" ]] || bash OpenTaiko.iOS/scripts/download-bass.sh
  [[ -d "OpenTaiko.iOS/Frameworks/liblua54.xcframework" ]] || bash OpenTaiko.iOS/scripts/build-lua54.sh
  [[ -d "OpenTaiko.iOS/Frameworks/liblmdb.xcframework" ]] || bash OpenTaiko.iOS/scripts/build-lmdb.sh
  [[ -f "OpenTaiko.iOS/Assets.xcassets/AppIcon.appiconset/icon_1024.png" ]] || bash OpenTaiko.iOS/scripts/build-appicons.sh
  [[ -d "OpenTaiko.iOS/Frameworks/ffmpeg.xcframework" ]] || bash OpenTaiko.iOS/scripts/build-ffmpeg.sh
  [[ -f "third_party/FFmpeg.AutoGen/upstream/FFmpeg.cs" ]] || bash OpenTaiko.iOS/scripts/fetch-ffmpeg-autogen.sh
}

# ios_build <config> <rid> [extra dotnet args...]
# Builds the app, prints a filtered summary, and verifies it succeeded.
# Sets the global APP_PATH. Returns non-zero on any build failure (a stale .app from a
# previous build is NOT treated as success — the dotnet exit code is authoritative).
ios_build() {
  local config="$1" rid="$2"; shift 2
  APP_PATH="OpenTaiko.iOS/bin/${config}/net10.0-ios/${rid}/OpenTaiko.iOS.app"
  bootstrap_ios_deps

  # Serialize builds that share this obj/bin output (<config>/<rid>). iOS AOT writes many
  # intermediate files (per-assembly *.llvm.o, linked/, aot-output/); two builds of the same
  # output at once (e.g. a broker `ipa` build + a manual `deploy.sh github`) corrupt each other ->
  # "file is empty in *.llvm.o" / Mono.Cecil ReadModule failures that otherwise need a manual obj
  # wipe. A per-output lock prevents that; different outputs (Debug sim vs Release device) still
  # build concurrently. A lock older than 30 min is assumed orphaned and stolen.
  local lockdir="${TMPDIR:-/tmp}/opentaiko-build-${config}-${rid}.lock"
  while ! mkdir "$lockdir" 2>/dev/null; do
    if [[ -d "$lockdir" && $(( $(date +%s) - $(stat -f %m "$lockdir" 2>/dev/null || date +%s) )) -gt 1800 ]]; then
      rm -rf "$lockdir" 2>/dev/null || true
    else
      echo "==> Another build of ${config}/${rid} is in progress; waiting..."; sleep 2
    fi
  done
  # ${lockdir:-} guard: bash RETURN traps are not auto-cleared, so this also fires when the
  # caller (do_sim/do_device) returns, where lockdir is out of scope -> unbound under `set -u`.
  trap 'rmdir "${lockdir:-}" 2>/dev/null || true' RETURN

  # A clean build must wipe obj/+bin/ of all three projects: incremental AOT rebuilds pollute
  # obj/ and produce a load_aot_module / mono_runtime_init_checked abort at startup. (--clean
  # also uninstalls the installed app below; this handles the build artifacts.)
  if $CLEAN; then
    echo "==> Clean: wiping obj/ and bin/ (OpenTaiko.iOS, FDK, OpenTaiko)..."
    rm -rf OpenTaiko.iOS/obj OpenTaiko.iOS/bin FDK/obj FDK/bin OpenTaiko/obj OpenTaiko/bin
  fi
  local log rc attempt
  log=$(mktemp)
  # UseVendoredFFmpeg must be a GLOBAL property: NuGet restore ignores per-reference
  # AdditionalProperties and would never discover the vendored FFmpeg.AutoGen project.
  for attempt in 1 2; do
    echo "==> Building $rid ($config)..."
    rc=0  # `|| rc=$?` keeps `set -e`/pipefail from aborting before we capture the status
    if $VERBOSE; then
      dotnet build "$CSPROJ" -c "$config" -r "$rid" -p:UseVendoredFFmpeg=true "$@" 2>&1 | tee "$log" || rc=$?
    else
      dotnet build "$CSPROJ" -c "$config" -r "$rid" -p:UseVendoredFFmpeg=true "$@" > "$log" 2>&1 || rc=$?
      { grep -E "(error CS|error MT|error MSB|Error\(s\)|Build succeeded)" "$log" || true; } | tail -10
    fi
    if [[ $rc -eq 0 && -d "$APP_PATH" ]]; then
      rm -f "$log"; return 0
    fi
    # The MSBuild build server occasionally holds a lock on a dependency project's
    # deps.json ("being used by another process" / GenerateDepsFile). Shut it down and
    # retry once before treating the build as failed.
    if [[ $attempt -eq 1 ]] && grep -q "being used by another process\|GenerateDepsFile" "$log"; then
      echo "==> Transient build-server file lock detected; shutting it down and retrying once..."
      dotnet build-server shutdown >/dev/null 2>&1 || true
      sleep 1
      continue
    fi
    # Corrupt/partial AOT artifacts (from an interrupted build, or a config change the incremental
    # up-to-date check missed): empty *.llvm.o or a Mono.Cecil ReadModule failure in AOTCompile.
    # Wipe just this config's obj/bin and rebuild clean, once — self-heals without a manual wipe.
    if [[ $attempt -eq 1 ]] && grep -qiE "file is empty in|Mono\.Cecil|AOTCompile|aot-instances\.dll" "$log"; then
      echo "==> Corrupt/partial AOT artifacts detected; wiping ${config} obj/bin and rebuilding clean (one retry)..."
      dotnet build-server shutdown >/dev/null 2>&1 || true
      rm -rf "OpenTaiko.iOS/obj/${config}" "OpenTaiko.iOS/bin/${config}" \
             "FDK/obj/${config}" "FDK/bin/${config}" "OpenTaiko/obj/${config}" "OpenTaiko/bin/${config}"
      sleep 1
      continue
    fi
    break
  done
  echo "Build failed (dotnet exit ${rc:-?}). Full build output:"
  echo "----------------------------------------"; cat "$log"; echo "----------------------------------------"
  rm -f "$log"; return 1
}

# make_ipa <app_src> <output.ipa> [strip_signature(true|false)]
make_ipa() {
  local app_src="$1" out="$2" strip="${3:-false}"
  mkdir -p "$(dirname "$out")"
  local tmp; tmp=$(mktemp -d)
  mkdir -p "$tmp/Payload"
  cp -R "$app_src" "$tmp/Payload/"
  if [[ "$strip" == "true" ]]; then
    # App Store / unsigned IPAs must not carry stale signing artifacts.
    rm -rf "$tmp/Payload/$(basename "$app_src")/_CodeSignature"
    rm -f "$tmp/Payload/$(basename "$app_src")/embedded.mobileprovision"
  fi
  (cd "$tmp" && zip -qr ipa.zip Payload)
  mv "$tmp/ipa.zip" "$out"
  rm -rf "$tmp"
  echo "==> IPA: $out ($(du -h "$out" | awk '{print $1}'))"
}

# make_dsym_zip <app_src> <output.ipa>  ->  writes <output_base>.dSYM.zip next to the ipa.
make_dsym_zip() {
  local app_src="$1" ipa_out="$2"
  local dsym_src="${app_src}.dSYM"
  local dsym_zip="${ipa_out%.ipa}.dSYM.zip"
  if [[ -d "$dsym_src" ]]; then
    local tmp; tmp=$(mktemp -d)
    cp -R "$dsym_src" "$tmp/"
    (cd "$tmp" && zip -qr dsym.zip "$(basename "$dsym_src")")
    mv "$tmp/dsym.zip" "$dsym_zip"
    rm -rf "$tmp"
    echo "==> dSYM zip: $dsym_zip ($(du -h "$dsym_zip" | awk '{print $1}'))"
  else
    echo "Warning: dSYM not found at $dsym_src, skipping dSYM zip."
  fi
}

# Stream a process's console for N seconds then stop (N=0 streams until Ctrl-C).
# stream_console <timeout> <launch-cmd...>
stream_console() {
  local timeout="$1"; shift
  # Capture the console so callers can scan it for a startup crash. A stale-incremental-AOT abort
  # (build succeeds, app aborts at launch with a Mono AOT-load failure) is only visible at runtime,
  # never in the build log, so this capture is the only place it can be caught.
  CONSOLE_LOG="${TMPDIR:-/tmp}/opentaiko-console.log"; : > "$CONSOLE_LOG"
  if [[ "$timeout" -eq 0 ]]; then
    echo "==> Launching (console output below, Ctrl-C to stop)..."
    "$@" 2>&1 | tee "$CONSOLE_LOG"
  else
    echo "==> Launching (showing ${timeout}s of console output)..."
    "$@" > "$CONSOLE_LOG" 2>&1 &
    local pid=$!
    tail -n +1 -f "$CONSOLE_LOG" 2>/dev/null & local tailpid=$!
    sleep "$timeout"
    kill "$pid" 2>/dev/null || true
    wait "$pid" 2>/dev/null || true
    kill "$tailpid" 2>/dev/null || true
    echo "==> Console timeout reached."
  fi
}

# Detect a stale-incremental-AOT startup abort: the build succeeded but the app aborts at launch
# with a Mono AOT-load failure (only a clean obj fixes it; it can't be seen in the build log).
# Conservative — requires BOTH a crash marker and an AOT marker, so a genuine app crash does not
# trigger a needless clean rebuild. Args: log files to scan.
aot_startup_abort() {
  local f
  for f in "$@"; do
    [[ -f "$f" ]] || continue
    if grep -qiE "signal 6|Native stacktrace|\babort\b" "$f" 2>/dev/null \
       && grep -qiE "load_aot|mono_aot|cannot load AOT|AOT module|aot[_ ].*out of date|used by your application" "$f" 2>/dev/null; then
      return 0
    fi
  done
  return 1
}

# Startup smoke-test (--launch-check): scan the captured console for a startup crash vs. a
# reached first frame, print a RESULT line, and exit non-zero on crash / no-frame. Used for
# CI / change validation. Arg: the console log file.
launch_check_result() {
  local log="${1:-}"
  if [[ -z "$log" || ! -f "$log" ]]; then
    echo "RESULT: NO CONSOLE LOG (inconclusive)"; exit 1
  fi
  if grep -q "Got a SIGABRT\|Native Crash Reporting" "$log"; then
    echo "RESULT: CRASHED AT STARTUP"
    echo "--- crashing frame (top of native stacktrace) ---"
    grep -A6 "Native stacktrace:" "$log" | sed 's|/Users.*dylib|<dylib>|' | head -8
    exit 1
  fi
  local frames; frames=$(grep -c "ViewDidLayoutSubviews" "$log")
  if [[ "$frames" -gt 0 ]]; then
    echo "RESULT: LAUNCHED OK (reached first frame x$frames)"
    exit 0
  fi
  echo "RESULT: NO FRAME, NO CRASH (inconclusive — app may not have started)"
  exit 1
}

# ==========================================================================================
#  Target: sim
# ==========================================================================================
do_sim() {
  : "${TIMEOUT:=10}"
  local dev="${DEVICE:-booted}"

  if ! xcrun simctl list devices booted | grep -q "Booted"; then
    echo "No simulator booted. Boot one with: OpenTaiko.iOS/scripts/sim-boot.sh"
    exit 1
  fi

  $BUILD && ios_build "$CONFIG" iossimulator-arm64 "${BUNDLE_ID_ARG[@]}"
  APP_PATH="OpenTaiko.iOS/bin/${CONFIG}/net10.0-ios/iossimulator-arm64/OpenTaiko.iOS.app"

  echo "==> Terminating existing app..."
  xcrun simctl terminate "$dev" "$APP_ID" 2>/dev/null || true
  if $CLEAN; then
    echo "==> Uninstalling previous app..."
    xcrun simctl uninstall "$dev" "$APP_ID" 2>/dev/null || true
  fi

  echo "==> Installing..."
  xcrun simctl install "$dev" "$APP_PATH"

  if [[ -n "$SCREENSHOT" ]] && ! $LAUNCH_CHECK; then
    echo "==> Launching in background..."
    xcrun simctl launch "$dev" "$APP_ID"
    echo "==> Waiting ${WAIT}s before screenshot..."
    sleep "$WAIT"
    echo "==> Taking screenshot -> $SCREENSHOT"
    xcrun simctl io "$dev" screenshot "$SCREENSHOT"
    # Simulator always captures portrait; rotate to match the game's landscape orientation.
    local orientation
    orientation=$(xcrun simctl spawn "$dev" launchctl getenv SIMULATOR_DEVICE_ORIENTATION 2>/dev/null || true)
    [[ -z "$orientation" ]] && orientation="LandscapeLeft"
    case "$orientation" in
      LandscapeLeft|UIInterfaceOrientationLandscapeLeft)   sips -r 90  "$SCREENSHOT" >/dev/null 2>&1 && echo "==> Rotated screenshot to landscape." ;;
      LandscapeRight|UIInterfaceOrientationLandscapeRight) sips -r 270 "$SCREENSHOT" >/dev/null 2>&1 && echo "==> Rotated screenshot to landscape." ;;
    esac
    echo "==> Done. App is still running in the simulator."
  else
    stream_console "$TIMEOUT" xcrun simctl launch --console "$dev" "$APP_ID"
    # Build-but-won't-run recovery (mirrors do_device): a stale-AOT startup abort -> one clean
    # rebuild + reinstall + relaunch. Rare on the simulator (it runs the interpreter/JIT, not full
    # AOT) but harmless to guard, and self-heals if it ever does. Detection is from the console only.
    if aot_startup_abort "${CONSOLE_LOG:-}"; then
      echo "==> App built but ABORTED at startup with a Mono AOT-load failure (stale incremental AOT)."
      echo "==> Auto-recovering: wiping ${CONFIG} obj/bin + clean rebuild + reinstall (one attempt)..."
      dotnet build-server shutdown >/dev/null 2>&1 || true
      rm -rf "OpenTaiko.iOS/obj/${CONFIG}" "OpenTaiko.iOS/bin/${CONFIG}" \
             "FDK/obj/${CONFIG}" "FDK/bin/${CONFIG}" "OpenTaiko/obj/${CONFIG}" "OpenTaiko/bin/${CONFIG}"
      ios_build "$CONFIG" iossimulator-arm64 "${BUNDLE_ID_ARG[@]}"
      echo "==> Reinstalling rebuilt app..."
      xcrun simctl install "$dev" "$APP_PATH"
      stream_console "$TIMEOUT" xcrun simctl launch --console "$dev" "$APP_ID"
    fi
    $LAUNCH_CHECK && launch_check_result "${CONSOLE_LOG:-}"
  fi
}

# ==========================================================================================
#  Target: device
# ==========================================================================================
do_device() {
  : "${TIMEOUT:=30}"

  # Resolve the target device.
  if $IMOBILE; then
    if [[ -z "$UDID" ]]; then
      UDID=$(idevice_id -l 2>/dev/null | head -1)
      [[ -z "$UDID" ]] && { echo "No device found via idevice_id."; exit 1; }
      echo "==> Found device (libimobiledevice): $UDID"
    fi
  else
    if [[ -z "$DEVICE" ]]; then
      # devicectl auto-detect grabs the WiFi transport first (install times out); honor a wired-id override.
      [ -f /tmp/opentaiko-wired-device ] && DEVICE=$(cat /tmp/opentaiko-wired-device)
      [ -z "$DEVICE" ] && DEVICE=$(xcrun devicectl list devices 2>/dev/null | { grep -E '(available|connected).*paired|connected' || true; } | awk '{for(i=1;i<=NF;i++) if($i ~ /^[A-F0-9]{8}-/) print $i}' | head -1)
      if [[ -z "$DEVICE" ]]; then
        echo "No device found via devicectl. Use --imobile for libimobiledevice."
        echo ""; echo "Available devices:"; xcrun devicectl list devices 2>&1
        exit 1
      fi
      echo "==> Found device: $DEVICE"
    fi
  fi

  if [[ -z "$IDENTITY" ]]; then
    IDENTITY=$(find_codesign_identity "Apple Development")
    echo "==> Using identity: $IDENTITY"
  fi

  if $BUILD; then
    ios_build "$CONFIG" ios-arm64 \
      -p:RuntimeIdentifier=ios-arm64 \
      -p:CodesignKey="$IDENTITY" \
      -p:CodesignProvision="" \
      "${BUNDLE_ID_ARG[@]}"
  fi
  APP_PATH="OpenTaiko.iOS/bin/${CONFIG}/net10.0-ios/ios-arm64/OpenTaiko.iOS.app"

  if $IMOBILE; then
    local udid_flag=(); [[ -n "$UDID" ]] && udid_flag=(-u "$UDID")
    if $CLEAN; then
      echo "==> Uninstalling previous app..."
      ideviceinstaller "${udid_flag[@]}" uninstall "$APP_ID" 2>/dev/null || true
    fi
    local ipa_tmp; ipa_tmp=$(mktemp -d); trap "rm -rf $ipa_tmp" RETURN
    make_ipa "$APP_PATH" "$ipa_tmp/app.ipa"
    echo "==> Installing via ideviceinstaller..."
    ideviceinstaller "${udid_flag[@]}" install "$ipa_tmp/app.ipa"
    echo "==> Installed. Launch the app manually on the device."
    echo "    (ideviceinstaller does not support remote launch)"
  else
    if $CLEAN; then
      echo "==> Uninstalling previous app..."
      xcrun devicectl device uninstall app --device "$DEVICE" "$APP_ID" 2>/dev/null || true
    fi
    echo "==> Installing on device..."
    xcrun devicectl device install app --device "$DEVICE" "$APP_PATH" 2>&1 | tail -3
    stream_console "$TIMEOUT" xcrun devicectl device process launch --device "$DEVICE" --console "$APP_ID"
    # Pull the app's full OpenTaiko.log — the console stream misses early-launch lines (FBO /
    # MetalPresenter init). Copies to /tmp/opentaiko-device.log (best-effort, never fails the run).
    rm -f /tmp/opentaiko-device.log
    xcrun devicectl device copy from --device "$DEVICE" --domain-type appDataContainer \
      --domain-identifier "$APP_ID" --source Documents/OpenTaiko.log --destination /tmp/opentaiko-device.log 2>&1 | tail -3 || true
    if [[ -s /tmp/opentaiko-device.log ]]; then
      echo "==> Pulled device OpenTaiko.log -> /tmp/opentaiko-device.log ($(wc -l < /tmp/opentaiko-device.log) lines)"
    else
      echo "==> Could not pull OpenTaiko.log via devicectl (continuing)."
    fi
    # Build-but-won't-run recovery: if the app aborted at launch with a stale-AOT signature, the
    # only fix is a clean rebuild. Do it once, automatically (runs at most once — not in a loop, so
    # a genuine crash that survives the clean rebuild just surfaces in the relaunch, no infinite redo).
    if aot_startup_abort "${CONSOLE_LOG:-}" /tmp/opentaiko-device.log; then
      echo "==> App built but ABORTED at startup with a Mono AOT-load failure (stale incremental AOT)."
      echo "==> Auto-recovering: wiping ${CONFIG} obj/bin + clean rebuild + reinstall (one attempt)..."
      dotnet build-server shutdown >/dev/null 2>&1 || true
      rm -rf "OpenTaiko.iOS/obj/${CONFIG}" "OpenTaiko.iOS/bin/${CONFIG}" \
             "FDK/obj/${CONFIG}" "FDK/bin/${CONFIG}" "OpenTaiko/obj/${CONFIG}" "OpenTaiko/bin/${CONFIG}"
      ios_build "$CONFIG" ios-arm64 -p:RuntimeIdentifier=ios-arm64 -p:CodesignKey="$IDENTITY" -p:CodesignProvision="" "${BUNDLE_ID_ARG[@]}"
      echo "==> Reinstalling rebuilt app..."
      xcrun devicectl device install app --device "$DEVICE" "$APP_PATH" 2>&1 | tail -3
      stream_console "$TIMEOUT" xcrun devicectl device process launch --device "$DEVICE" --console "$APP_ID"
    fi
  fi
}

# Read a single-line <Field>value</Field> from the .csproj.
read_csproj_field() { grep "<$1>" "$CSPROJ" | sed "s/.*<$1>\(.*\)<\/$1>.*/\1/"; }

# Build Release ios-arm64 with the requested signing and package it to $OUTPUT (+ dSYM zip).
# build_and_package_ipa  (uses globals SIGN, OUTPUT, IDENTITY, BUILD, BUNDLE_ID_ARG)
build_and_package_ipa() {
  CONFIG="Release"  # distribution packaging is always Release
  local sign_args=() strip="false"
  case "$SIGN" in
    none)
      sign_args=(-p:EnableCodeSigning=false); strip="true" ;;
    development|distribution)
      local pref; [[ "$SIGN" == "distribution" ]] && pref="Apple Distribution" || pref="Apple Development"
      [[ -z "$IDENTITY" ]] && IDENTITY=$(find_codesign_identity "$pref")
      echo "==> Signing identity: $IDENTITY"
      sign_args=(-p:CodesignKey="$IDENTITY" -p:CodesignProvision="") ;;
    *) echo "Unknown --sign mode: $SIGN (expected none|development|distribution)"; exit 1 ;;
  esac

  if $BUILD; then
    ios_build Release ios-arm64 -p:RuntimeIdentifier=ios-arm64 "${sign_args[@]}" "${BUNDLE_ID_ARG[@]}"
  fi
  APP_PATH="OpenTaiko.iOS/bin/Release/net10.0-ios/ios-arm64/OpenTaiko.iOS.app"
  [[ -d "$APP_PATH" ]] || { echo "Error: $APP_PATH not found (build skipped or failed)."; exit 1; }

  make_ipa "$APP_PATH" "$OUTPUT" "$strip"
  make_dsym_zip "$APP_PATH" "$OUTPUT"
}

# ==========================================================================================
#  Target: ipa  (release packaging)
# ==========================================================================================
do_ipa() {
  : "${OUTPUT:=OpenTaiko.iOS/dist/OpenTaiko_unsigned.ipa}"
  build_and_package_ipa
}
case "$TARGET" in
  sim)        do_sim ;;
  device)     do_device ;;
  ipa)        do_ipa ;;
esac
