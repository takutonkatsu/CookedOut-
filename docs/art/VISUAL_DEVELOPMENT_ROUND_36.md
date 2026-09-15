# COOKED OUT! Visual Development Round 36

Date: 2026-09-12  
Style version: 1  
Scope: prepared-food ingredient cards, dash feedback, chopping-knife posture

## 1. Comparison and evaluation

| Candidate | Change | Evaluation |
| --- | --- | --- |
| A / Round 35 baseline | Procedural 3D ingredient symbols on ivory badges; three cyan dash streaks plus smoke; knife blade lying flat | Ingredient identity differs from the established ingredient-source language. Straight streaks add visual noise. The flat knife posture reads as striking with the blade face. |
| B / Round 36 candidate | Existing IngredientSourceCards on horizontal world-space quads; smoke-only dash at 130% size; knife standing on its cutting edge | Selected. Ingredient identity is consistent across crates, orders, cookware, prepared ingredients, and completed dishes. Motion remains readable with fewer effects, and the knife posture communicates chopping correctly. |

Preview: `docs/art/concepts/kitchen_ingredient_cards_dash_knife_sv1_blender_preview_v1.png`

The candidate remains visually consistent with style version 1 because it reuses approved source-card artwork and preserves the stainless, teal, ivory, rounded-toy kitchen language. No new external artwork was introduced.

## 2. Implementation brief

- Replace procedural ingredient badge geometry with the existing `IngredientSourceCards` textures.
- Use `ingredient_lettuce_source_card_sv3`, plus the carrot, onion, and beef `sv2` cards.
- Keep the existing maximum of four indicators and the two-row, maximum-two-column layout.
- Remove all three cyan straight dash streaks.
- Parent the five smoke puffs under the dash effect root and multiply their animated size by 1.30.
- Rotate the authored knife geometry 90 degrees around its blade direction so it stands on the cutting edge.
- Raise the knife motion root so the upright blade rests on the board instead of clipping through it.
- Apply the same upright correction to the code-generated fallback knife.
- Regenerate 23 FBX models and rebuild 23 Unity production prefabs without changing Item Anchor, station colliders, grid data, recipes, or gameplay timing.

## 3. Procedural authoring prompt

> Preserve the approved style-version-1 hotel kitchen and broad toy chef-knife design. Rotate only the knife geometry 90 degrees around the blade-length axis so the blade face is vertical and the cutting edge points toward the chopping board. Raise the motion root only enough to rest the blade on the board. Preserve Knife Motion Root, Knife Blade Direction, grid footprint, Item Anchor, materials, colliders, and all gameplay values. Do not introduce new artwork for ingredient indicators; reuse the existing IngredientSourceCards textures.

Runtime presentation brief:

> Show the same IngredientSourceCards used on ingredient sources above processed ingredients, cookware contents, and completed dishes. Keep the established 2x2 maximum layout. Remove cyan dash lines completely and retain only five smoke puffs at 130% of their previous animated size.

## 4. Verification

- Blender 5.2.1 LTS: regenerated and inspected all 23 FBX models.
- Unity 6000.3.24f1: rebuilt and validated all 23 production prefabs.
- PlayMode: 50 / 50 passed.
- EditMode: 45 / 45 passed.
- Preview SHA-256: `90573b37efe4de1beb40813d02e93b200b0637f8774e73aa67a56de56dce0622`
- Blender source SHA-256: `34430e10fb74c675b2e8142d5bae6dd00013f51ede57ac3aaaf916cbfedf00b1`
- Generator SHA-256: `a4cfa1cd518a63bc137073a77ff827bc6182a70f32ca93a19a805a841e5c2dc9`
- ChoppingBoard FBX SHA-256: `282c7475e07c822a6390d57870c17299d1d258dd8bac5c26913c77517dd91dae`
- ChoppingBoard prefab SHA-256: `98bd764e995af5685557bd33afea8541737c14a1e867c9b37524cd8450cb0c42`
- Aggregate 23 FBX SHA-256: `78b92ef41f42c26917fc5857f4f4fccc5655b9db5d5ff5c3b7d006b88e5a434b`
- PlayMode XML SHA-256: `f3c5e0a0bd02117d0441f455521a7053350134931304930dfc302fc71de4de7c`
- EditMode XML SHA-256: `a4749070c5e79c1b7492a1af4b303d69ffb5361694e9c577e16108772b724152`
