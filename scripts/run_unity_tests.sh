#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd "$(dirname "$0")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-}"

if [[ -z "$unity_editor" ]]; then
  project_version="$(awk -F': ' '/^m_EditorVersion:/{print $2}' "$project_dir/ProjectSettings/ProjectVersion.txt")"
  expected_editor="/Applications/Unity/Hub/Editor/$project_version/Unity.app/Contents/MacOS/Unity"
  if [[ -x "$expected_editor" ]]; then
    unity_editor="$expected_editor"
  fi
fi

if [[ -z "$unity_editor" || ! -x "$unity_editor" ]]; then
  echo "Unity Editor was not found. Install Unity 6.3 LTS with iOS Build Support,"
  echo "or set UNITY_EDITOR=/absolute/path/to/Unity.app/Contents/MacOS/Unity."
  exit 2
fi

mkdir -p "$project_dir/TestResults"

"$unity_editor" -batchmode -nographics \
  -projectPath "$project_dir" \
  -runTests -testPlatform EditMode \
  -testResults "$project_dir/TestResults/editmode.xml" \
  -logFile "$project_dir/TestResults/editmode.log"

"$unity_editor" -batchmode -nographics \
  -projectPath "$project_dir" \
  -runTests -testPlatform PlayMode \
  -testResults "$project_dir/TestResults/playmode.xml" \
  -logFile "$project_dir/TestResults/playmode.log"

if [[ ! -s "$project_dir/TestResults/editmode.xml" || ! -s "$project_dir/TestResults/playmode.xml" ]]; then
  echo "Unity did not generate both test result files. Inspect TestResults/*.log."
  exit 3
fi

echo "Unity EditMode and PlayMode tests completed. Results: $project_dir/TestResults"
