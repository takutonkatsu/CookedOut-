#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "$0")/.." && pwd)"
blender_binary="${BLENDER_BINARY:-/Applications/Blender.app/Contents/MacOS/Blender}"

if [[ ! -x "$blender_binary" ]]; then
  echo "Blender was not found: $blender_binary"
  echo "Set BLENDER_BINARY to the Blender executable path."
  exit 2
fi

"$blender_binary" --background --python "$project_root/tools/blender/generate_kitchen_assets.py" -- \
  --blend "$project_root/art-source/kitchen/KitchenProductionAssets.blend" \
  --model-dir "$project_root/Assets/_CookedOut/Art/KitchenProduction/Models" \
  --preview "$project_root/docs/art/concepts/kitchen_hotel_stainless_sv1_blender_preview_v4.png"

echo "Generated Blender source, kitchen FBX models, and preview."
echo "In Unity, run COOKED OUT! > Build Kitchen Production Prefabs."
