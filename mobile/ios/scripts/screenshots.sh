#!/usr/bin/env bash
# Takes the App Store screenshots in the two simulators Apple asks for: an iPhone with a 6.9" display
# (1320 x 2868) and an iPad with a 13" display (2064 x 2752). The UI test SlipwaysScreenshots opens each scene with
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

# The newest iOS runtime and the newest device types of each kind it supports; Apple's sizes stay the same within a
# kind. Memory variants such as "iPad Pro 13-inch (M4) (16GB)" are left out, xcodebuild does not offer them.
runtimes="$(xcrun simctl list runtimes -j)"
runtime="$(jq -r '[.runtimes[] | select(.platform == "iOS" and .isAvailable)] | last | .identifier // empty' <<<"$runtimes")"
devicetype() {
  jq -r --arg runtime "$runtime" --arg pattern "$1" \
    '[.runtimes[] | select(.identifier == $runtime) | .supportedDeviceTypes[] | select(.name | test($pattern))] | last | .identifier // empty' \
    <<<"$runtimes"
}
iphone="$(devicetype '^iPhone [0-9]+ Pro Max$')"
ipad="$(devicetype '^iPad Pro 13-inch \(M[0-9]+\)$')"
if [ -z "$runtime" ] || [ -z "$iphone" ] || [ -z "$ipad" ]; then
  echo "No iOS runtime or no simulator for iPhone Pro Max / iPad Pro 13-inch found" >&2
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
  TEST_RUNNER_SCREENSHOT_DIR="$out/$folder" xcodebuild test-without-building \
    -project Slipways.xcodeproj \
    -scheme Slipways \
    -destination "id=$udid" \
    -derivedDataPath "$derived" \
    -resultBundlePath "$out/$folder.xcresult" \
    -quiet
}

rm -rf "$out"/*.xcresult
shoot "iPhone-6.9" "$iphone"
shoot "iPad-13" "$ipad"

echo "Screenshots in $out:"
find "$out" -name '*.png' | sort
