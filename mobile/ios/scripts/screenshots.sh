#!/usr/bin/env bash
# Takes the App Store screenshots in the simulators Apple asks for: iPhones with a 6.9" display (1320 x 2868) and a
# 6.3" display (1206 x 2622), and an iPad with a 13" display (2064 x 2752). The UI test SlipwaysScreenshots opens each scene with
# the live data from slipways.de; the images land in one folder per device.
#
#   mobile/ios/scripts/screenshots.sh [output folder]     (default: mobile/ios/build/screenshots)
#
# Needs Xcode, xcodegen and jq. The simulators are created for the run and deleted afterwards.
set -euo pipefail

cd "$(dirname "$0")/.."
out="${1:-$PWD/build/screenshots}"
mkdir -p "$out"
out="$(cd "$out" && pwd)"
derived="$PWD/build/DerivedData"

# The newest iOS runtime and, of the devices it supports, the first of a fixed list with exactly Apple's sizes:
# see the list below. The runtime's own order is not by age (it once picked the
# iPhone 11 Pro Max, 6.5"), so the names are listed by hand; a newer model goes on its list once its size is checked.
runtimes="$(xcrun simctl list runtimes -j)"
runtime="$(jq -r '[.runtimes[] | select(.platform == "iOS" and .isAvailable)] | last | .identifier // empty' <<<"$runtimes")"
devicetype() {
  local name id
  for name in "$@"; do
    id="$(jq -r --arg runtime "$runtime" --arg name "$name" \
      '[.runtimes[] | select(.identifier == $runtime) | .supportedDeviceTypes[] | select(.name == $name)] | first | .identifier // empty' \
      <<<"$runtimes")"
    if [ -n "$id" ]; then echo "$id"; return; fi
  done
}

# Folder, size App Store Connect expects, and the simulators that have it, best first.
devices=(
  "iPhone-6.9|1320x2868|iPhone 17 Pro Max|iPhone 16 Pro Max"
  "iPhone-6.3|1206x2622|iPhone 17 Pro|iPhone 16 Pro"
  "iPad-13|2064x2752|iPad Pro 13-inch (M5)|iPad Pro 13-inch (M4)"
)
# SCREENSHOT_DEVICES picks some of the folders, e.g. "iPhone-6.3"; the CI runs one per job.
wanted="${SCREENSHOT_DEVICES:-iPhone-6.9 iPhone-6.3 iPad-13}"
if [ -z "$runtime" ]; then
  echo "No iOS runtime found" >&2
  exit 1
fi

created=()
cleanup() {
  for udid in ${created[@]+"${created[@]}"}; do
    xcrun simctl shutdown "$udid" >/dev/null 2>&1 || true
    xcrun simctl delete "$udid" >/dev/null 2>&1 || true
  done
}
trap cleanup EXIT

[ -d Slipways.xcodeproj ] || xcodegen generate

echo "Building for testing"
xcodebuild build-for-testing \
  -project Slipways.xcodeproj \
  -scheme Slipways \
  -destination 'generic/platform=iOS Simulator' \
  -derivedDataPath "$derived" \
  CODE_SIGNING_ALLOWED=NO \
  -quiet

shoot() {
  local folder="$1" type="$2"
  local udid
  udid="$(xcrun simctl create "Slipways $folder" "$type" "$runtime")"
  created+=("$udid")
  xcrun simctl boot "$udid"
  xcrun simctl bootstatus "$udid" -b >/dev/null
  xcrun simctl ui "$udid" appearance light
  # Apple's usual status bar: 9:41, full battery and signal.
  xcrun simctl status_bar "$udid" override \
    --time 9:41 --dataNetwork wifi --wifiMode active --wifiBars 3 \
    --cellularMode active --cellularBars 4 --batteryState charged --batteryLevel 100

  echo "Taking screenshots on $folder ($type)"
  rm -rf "$out/$folder"
  # The size App Store Connect expects for this device, checked once the images are there.
  local expected="$3"
  TEST_RUNNER_SCREENSHOT_DIR="$out/$folder" xcodebuild test-without-building \
    -project Slipways.xcodeproj \
    -scheme Slipways \
    -destination "id=$udid" \
    -derivedDataPath "$derived" \
    -resultBundlePath "$out/$folder.xcresult" \
    -quiet

  local file size failed=0
  for file in "$out/$folder"/*.png; do
    size="$(sips -g pixelWidth -g pixelHeight "$file" | awk '/pixelWidth/ {w=$2} /pixelHeight/ {h=$2} END {print w "x" h}')"
    echo "$folder/$(basename "$file"): $size"
    if [ "$size" != "$expected" ]; then
      echo "::error::$folder/$(basename "$file") is $size, App Store Connect expects $expected" >&2
      failed=1
    fi
  done
  return "$failed"
}

rm -rf "$out"/*.xcresult
for device in "${devices[@]}"; do
  IFS='|' read -r folder size names <<<"$device"
  case " $wanted " in *" $folder "*) ;; *) continue ;; esac
  IFS='|' read -r -a candidates <<<"$names"
  type="$(devicetype "${candidates[@]}")"
  if [ -z "$type" ]; then
    echo "None of the simulators for $folder found (${candidates[*]}); add the current model to the list above" >&2
    exit 1
  fi
  shoot "$folder" "$type" "$size"
done

echo "Screenshots in $out:"
find "$out" -name '*.png' | sort
