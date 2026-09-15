# COOKED OUT! Visual Development Round 34

Date: 2026-09-12  
Style version: 1  
Scope: order-ticket scale, ingredient-source scale, chopping presentation, cookware readability, bike reset

## 1. Comparison and evaluation

| Candidate | Change | Evaluation |
| --- | --- | --- |
| A / Round 33 baseline | narrow two-material knife; small pot process icon; full-size ingredient-source photo; flat-looking empty pot; 0.50 ingredient badges | Maintains the approved hotel-kitchen palette, but the knife and cookware state are hard to read from the fixed camera. |
| B / Round 34 candidate | broad toy chef knife; 84 px pot process icon; 80% source photo; recessed empty-pot cavity; 1.00 two-row ingredient badges; active knife camera offset | Selected. Large silhouettes remain readable without changing station coordinates, colliders, or grid data. |

Preview: `docs/art/concepts/kitchen_knife_pot_ui_refinement_sv1_blender_preview_v1.png`

The candidate still reads as the same COOKED OUT! work because it retains the approved stainless/teal/ivory palette, rounded toy bevels, fixed-camera proportions, and simple color blocking. The broader knife is a local readability improvement rather than a new visual direction.

## 2. Implementation brief

- Set the vegetable-soup pot silhouette to 84 px: slightly smaller than the 90 px ingredient photo.
- Scale the circular ingredient-source photo root to 0.80.
- Rotate only a raw carrot placed on a chopping board by 90 degrees; held and world-drop carrots retain their authored orientation.
- While chopping is active, move the knife 0.18 m toward the active camera in addition to the existing vertical chop motion; restore the exact rest pose afterward.
- Rebuild the knife as an original broad chef-knife silhouette with a softly pointed blade, bright blade facet, rounded turquoise handle, and bright bolster.
- Add a dark, smaller recessed cylinder inside the empty pot rim so the vessel reads as hollow from the fixed camera.
- Double prepared-food ingredient badges from 0.50 to 1.00 and double their internal symbols. Arrange multi-ingredient badges across two rows, with up to two columns.
- On completed return, reset the delivery bike to its recorded start position and start rotation before dismounting the rider.
- Rebuild all 20 kitchen FBX files and all 20 Unity production prefabs. Do not modify gameplay grids, station anchors, colliders, recipes, or design canonical documents.

## 3. Reference and originality guardrail

Visual reference supplied by the user: `https://www.illust-box.jp/db_img/sozai/00019/192427/watermark.jpg`.

Reference extraction is limited to generic attributes: a broad chef-knife blade, clear blade/handle color separation, and friendly rounded proportions. The mesh, silhouette points, material arrangement, scale, and placement were authored specifically for COOKED OUT! No tracing, watermark reuse, texture extraction, or reproduction of a proprietary character, costume, UI layout, or exact illustration was used. Verify source terms before public release.

## 4. Authoring prompt / procedural brief

> Style version 1. Preserve the approved hotel-kitchen stainless, teal and ivory palette and rounded toy-like 3D forms. Create an original chef knife with a broad readable blade, subtle bright facet, rounded turquoise handle and separate bolster. It must read from a fixed elevated camera and remain compatible with the existing Knife Motion Root and Knife Blade Direction transforms. Do not trace the reference or copy its exact contour, colors, composition, or watermark. Do not alter grid coordinates, Item Anchor, station colliders, recipes, or gameplay rules.

Runtime presentation brief:

> Keep nonverbal information readable at gameplay scale. Pot process icon 84 px, ingredient-source photo 80%, raw board carrot +90 degrees, knife active camera offset 0.18 m, prepared-content badges 2x with a two-row maximum-two-column arrangement, and empty pot with a visibly recessed dark cavity. Restore the bike to its authored start transform after returning.

## 5. Verification

- Blender 5.2.1 LTS: generated 20 FBX models and saved `KitchenProductionAssets.blend`.
- Unity 6000.3.24f1: rebuilt and validated 20 production prefabs.
- EditMode: 45 / 45 passed.
- Focused PlayMode for this round: 6 / 6 passed.
- Full PlayMode snapshot: 48 / 50 passed. The two failures belong to a concurrent unfinished beef/pan asset change (`CookedPanTransfersOnCounterInBothCursorDirections` and missing `BeefRaw` in `KitchenProductionLibraryContainsTwentyThreeValidatedReusablePrefabs`); the Round 34 focused tests are unaffected.
- Preview SHA-256: `ea74d2b0335ea12d626af00b1bf937553c34fce7be7a84133a9ed5e4ed5b0b7d`
- Blender source SHA-256: `a783c3ba4077635c31177568e9d124f2f26d21f8e7c7ae11a2b8a968483cf06e`
- Generator SHA-256: `adf82c5ca89b33d3f5da54b7332b5150e68ff7ced8a8ac9ada18787075397e83`
- ChoppingBoard FBX SHA-256: `c46fcc561268ea64aa6f1a0f1220bf90ca3def3d91a42ed92f594606335cd35e`
- ChoppingBoard prefab SHA-256: `7ae5c8131d5c24fc4f6a58f1c337a17bd899ba3f883bd442da96b5948c3b4af1`
- Aggregate 20 FBX SHA-256: `8b7b5156292d77c7d32e42f0148fbf4bb69e9f5dcab140280f51f57d42341f3e`
- Focused PlayMode XML SHA-256: `9ea27a024d0bd55d9736fd5d59fbb4de3904b7a06d2499389fcaf1017efa54af`
- EditMode XML SHA-256: `12b5eb02d324585be27b9355109ce0357a6ff3badbd98fc60f5e61b777db63ce`
