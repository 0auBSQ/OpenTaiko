#!/bin/bash
# Show OpenTaiko logs from the iOS simulator system console.
# Usage: bash OpenTaiko.iOS/scripts/logs.sh [options]
#   --last N      Seconds to look back (default: 30)
#   --filter PAT  Grep pattern to filter (default: all [OpenTaiko] lines)
#   --max N       Max lines to show (default: 40)
#   --exclude PAT Grep -v pattern to exclude repetitive lines
#   --all         Show all app messages (not just [OpenTaiko] tagged)
#   --errors      Show only errors/exceptions/crashes
#   -h, --help    Show this help

SELF="${BASH_SOURCE[0]}"; [[ "$SELF" = /* ]] || SELF="$PWD/$SELF"
usage() { awk 'NR==1 {next} /^#/ {sub(/^# ?/, ""); print; next} {exit}' "$SELF"; }
for _a in "$@"; do case "$_a" in -h|--help) usage; exit 0 ;; esac; done

LAST=30
FILTER=""
MAX=40
EXCLUDE=""
ALL=false
ERRORS=false

while [ $# -gt 0 ]; do
  case "$1" in
    --last)    LAST="$2"; shift 2 ;;
    --filter)  FILTER="$2"; shift 2 ;;
    --max)     MAX="$2"; shift 2 ;;
    --exclude) EXCLUDE="$2"; shift 2 ;;
    --all)     ALL=true; shift ;;
    --errors)  ERRORS=true; ALL=true; shift ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

if $ALL; then
  # Show all messages from the OpenTaiko process
  OUTPUT=$(xcrun simctl spawn booted log show \
    --predicate "senderImagePath CONTAINS \"OpenTaiko\"" \
    --last "${LAST}s" \
    --style compact 2>/dev/null)
else
  # Show only [OpenTaiko] tagged messages
  OUTPUT=$(xcrun simctl spawn booted log show \
    --predicate "eventMessage CONTAINS \"[OpenTaiko]\"" \
    --last "${LAST}s" \
    --style compact 2>/dev/null)
fi

if $ERRORS; then
  FILTER="${FILTER:+$FILTER|}Unhandled|Exception|JIT|error:|SIGABRT|SIGSEGV|NullRef|Chip playback"
fi

if [ -n "$FILTER" ]; then
  OUTPUT=$(echo "$OUTPUT" | grep -E "$FILTER" || true)
fi

if [ -n "$EXCLUDE" ]; then
  OUTPUT=$(echo "$OUTPUT" | grep -v -E "$EXCLUDE" || true)
fi

echo "$OUTPUT" | head -n "$MAX"
