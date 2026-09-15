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

"$unity_editor" -batchmode -quit \
  -projectPath "$project_dir" \
  -executeMethod CookedOut.Editor.IosBuildCommand.BuildDevelopment \
  -logFile "$project_dir/ios-build.log"

echo "Generated Xcode project: $project_dir/Builds/iOS"
