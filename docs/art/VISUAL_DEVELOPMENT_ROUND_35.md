# COOKED OUT! Visual Development Round 35

Date: 2026-09-12  
Style version: 1  
Scope: tutorial 1-5 completion, beef source art, hamburger order art, beef state models, frying animation

## 1. Stage 1-5 completion

- Dish: browned hamburger patty with a chopped-lettuce side in the universal delivery container.
- Required flow: take beef -> chop/form -> fry while watching heat -> move cooked patty to container -> add chopped lettuce -> serve.
- Cooked beef and chopped lettuce can be merged in either order, but neither component completes the dish alone.
- Kitchen is split into a north cooking line and south assembly line. It contains dedicated beef and lettuce freezer-style ingredient sources, one chopping board, pan heat, pan rest, assembly counter, container dispenser, serving hatch, trash, and staging counters.
- Shift duration is 180 seconds; order lifetime is 105 seconds; orders enter every 32 seconds; at most two are visible.
- Two served meals are required before any star can be awarded. The score thresholds are 130 / 210 / 290.

## 2. Generated 2D assets

Built-in image generation mode was used for each standalone image. Text, logos, watermarks, people, extra utensils, and unrelated ingredients were excluded.

### Beef ingredient-source card

Concept: `docs/art/concepts/ingredient_beef_source_card_sv1_round34_v1.png`  
Unity resource: `Assets/_CookedOut/Art/Resources/IngredientSourceCards/ingredient_beef_source_card_sv2.png`

Prompt:

> Create one square 1:1 ingredient-source card asset for a cheerful stylized 3D cooking game. Show only a single raw ground-beef hamburger patty, centered, viewed from a gentle elevated three-quarter angle. Chunky toy-like form, rounded edges, appetizing pink-red minced texture with subtle pale fat flecks, soft studio lighting and a clean warm off-white background. Match a polished mobile-game icon: clear silhouette, generous margin, no plate, no garnish, no text, no logo, no watermark, no border, no hands or utensils.

### Completed hamburger order image

Concept: `docs/art/concepts/order_hamburger_plate_open_lid_sv1_round34_v1.png`  
Unity resource: `Assets/_CookedOut/Art/Resources/OrderFoodCards/order_hamburger_plate_sv1.png`

Prompt:

> Create one square 1:1 completed-food card for a cheerful stylized 3D cooking game. Use the same universal cream delivery container with turquoise corner guards and a clear lid opened upward. Inside, show one browned hamburger patty with a small glossy sauce accent and a simple chopped-lettuce side. Gentle elevated three-quarter view, centered without horizontal stretching, chunky toy-like geometry, rounded edges, soft studio lighting, clean warm off-white background. No bun, cheese, tomato, onion, fries, utensils, text, logo, watermark, border, hands or characters.

Both runtime copies are 512 x 512 and retain the full square composition.

## 3. Beef 3D production assets

The procedural Blender source now exports three new reusable FBX models and Unity prefabs:

- `BeefRaw`: loose pink-red minced-beef clusters with visible pale marbling.
- `BeefChopped`: a formed raw patty with a flatter, unified silhouette.
- `BeefCooked`: a browned patty with three dark grill marks.

The kitchen production library now contains 23 validated prefabs. Materials added: `MAT_BeefRaw`, `MAT_BeefFat`, `MAT_BeefCooked`, and `MAT_Sauce`.

## 4. Runtime animation

`FryingPanCookingMotion` reads the authoritative pan state every frame. While cooking, the patty changes continuously from raw red to cooked brown, makes a restrained sizzle movement, and emits three rising steam puffs. During the last two seconds before burning it adds a fast warm-color and scale pulse. Removing the pan from heat naturally pauses progression because the animation never advances gameplay time itself.

## 5. Verification

- Blender 5.2.1 LTS: generated and visually inspected 23 FBX models and the kitchen production preview.
- Unity 6000.3.24f1: rebuilt and validated all 23 production prefabs.
- EditMode: 45 / 45 passed.
- PlayMode: 50 / 50 passed.
- Beef source concept SHA-256: `c5570d1caa1e469f47f800768e23db28858705701e3d91cbab1093b448a4e96e`
- Hamburger order concept SHA-256: `89099497ce312edad91a3ddef1c12ad0187e8b043b1a75e57b50d85bc16f2216`
- BeefRaw FBX SHA-256: `72659a1119048af37fa89ad5040ce4b30d76da860b785508f7ac4c7cb45543c6`
- BeefChopped FBX SHA-256: `ea8a86c6b45dce82ab512c40ca8d9b95ca9feb30164b1c8183037f0c81fbf800`
- BeefCooked FBX SHA-256: `a4ba03f4af56b412a573d298b5380816e94eb0ed80b47525b87f7c2160bb0f0b`
- PlayMode XML SHA-256: `45d034ad426c2c1a7ad7db2b67f9cfbce4baa1d8d4233a40f7900ad52523e409`
- EditMode XML SHA-256: `ef65b9f345ddbf7bec48e31447ae6c38db541c7a42e9fc7112cca17820e85904`
