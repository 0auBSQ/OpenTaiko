# Shared helper functions for iOS signing scripts.
# Source this file; do not execute directly.

# Find the first valid codesign identity matching a type string.
# Usage: find_codesign_identity "Apple Development"
#        find_codesign_identity "Apple Distribution"
# Prints the full identity name (e.g. "Apple Development: Name (TEAMID)").
find_codesign_identity() {
  local preferred="${1:-Apple Development}"
  local match
  match=$(security find-identity -v -p codesigning 2>/dev/null \
    | grep "$preferred" | head -1 \
    | sed 's/.*"\(.*\)"/\1/')
  if [[ -n "$match" ]]; then
    echo "$match"
    return
  fi
  echo "Error: No codesign identity found matching \"$preferred\"." >&2
  echo "  Available identities:" >&2
  security find-identity -v -p codesigning 2>/dev/null | grep -v "valid identities" >&2
  return 1
}

# Find an installed provisioning profile matching a bundle ID.
# Usage: find_provisioning_profile "com.example.app"
# Prints the path to the best matching .mobileprovision file.
# Prefers exact bundle ID match over wildcard (*).
find_provisioning_profile() {
  local target_id="$1"
  local profile_dir="$HOME/Library/MobileDevice/Provisioning Profiles"
  local best_match=""
  local wildcard_match=""

  if [[ ! -d "$profile_dir" ]]; then
    echo "Error: No provisioning profiles directory found at $profile_dir" >&2
    return 1
  fi

  local tmp_plist
  tmp_plist=$(mktemp)

  for f in "$profile_dir"/*.mobileprovision; do
    [[ -f "$f" ]] || continue
    security cms -D -i "$f" > "$tmp_plist" 2>/dev/null || continue

    # Extract bundle ID from entitlements (strip team-id prefix)
    local app_id
    app_id=$(/usr/libexec/PlistBuddy -c "Print :Entitlements:application-identifier" "$tmp_plist" 2>/dev/null) || continue
    local profile_bundle_id="${app_id#*.}"

    if [[ "$profile_bundle_id" == "$target_id" ]]; then
      best_match="$f"
      break
    elif [[ "$profile_bundle_id" == "*" && -z "$wildcard_match" ]]; then
      wildcard_match="$f"
    fi
  done

  rm -f "$tmp_plist"

  local result="${best_match:-$wildcard_match}"
  if [[ -z "$result" ]]; then
    echo "Error: No provisioning profile found matching bundle ID \"$target_id\"." >&2
    echo "  Install a profile in: $profile_dir" >&2
    return 1
  fi
  echo "$result"
}
