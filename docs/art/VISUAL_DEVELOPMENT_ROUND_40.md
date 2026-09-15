# COOKED OUT! ビジュアル開発 第40ラウンド — チュートリアル1-6〜1-8

- 更新日: 2026-09-12
- 対象: 火災復旧、皿洗い、配達代行、可動厨房接続部
- 状態: Unity統合・自動検証済み。横持ちiPhone実機承認待ち

## 1. 実装内容

- 1-6: 誘導火災、消火器取得、2.5秒の継続消火、炎縮小・泡回転、鎮火まで営業時計停止、スープ＋ハンバーグ、最低2提供
- 1-7: 初期2皿、提供後の汚れ皿返却、2.5秒の継続洗浄、洗浄進捗保持、皿再利用、サラダ＋スープ、最低3提供
- 1-8: 既習3料理、1件目配達代行、2件目バイク自前配達、以降自由選択、警告点滅付き低速可動接続部、最低3提供
- 新規production model: ExtinguisherCabinet、FireExtinguisher、WashingSink、PlateDispenser、DishReturn、CourierShelf、DynamicBridge
- 新規ノンバーバルカード: 消火器、清潔な皿、汚れ皿、配達代行

## 2. 画像生成方式

OpenAI Codexのbuilt-in image generationを使用。4資産は互いの内容を混ぜない独立生成で、Unity用512×512へ縮小した。コンセプト正本は2048×2048を保持する。

### 消火器

```text
Use case: stylized-concept
Asset type: nonverbal equipment card for a cheerful stylized 3D cooking game
Primary request: one compact red fire extinguisher, standing upright, with a short black hose and simple pressure handle
Scene/backdrop: clean warm off-white studio background
Style/medium: polished chunky toy-like 3D mobile-game icon, rounded bevels, soft matte materials
Composition/framing: square 1:1, centered, gentle elevated three-quarter view, generous margin, strong readable silhouette
Lighting/mood: bright soft studio lighting, friendly and safe
Color palette: vivid fire-engine red, charcoal hose, small cream highlights
Constraints: only the extinguisher; no fire, smoke, people, hands, text, letters, numbers, logo, watermark, border, or extra props
```

### 清潔な再利用皿

```text
Use case: stylized-concept
Asset type: nonverbal equipment card for a cheerful stylized 3D cooking game
Primary request: a short neat stack of three sparkling clean reusable restaurant plates
Scene/backdrop: clean warm off-white studio background
Style/medium: polished chunky toy-like 3D mobile-game icon, rounded bevels, soft ceramic
Composition/framing: square 1:1, centered, gentle elevated three-quarter view, generous margin, strong readable silhouette
Lighting/mood: bright soft studio lighting, hygienic and inviting
Color palette: warm ivory plates with small turquoise rims and subtle sparkle highlights
Constraints: only clean empty plates; no food, dirt, sink, hands, text, letters, numbers, logo, watermark, border, or extra props
```

### 汚れ皿

```text
Use case: stylized-concept
Asset type: nonverbal equipment card for a cheerful stylized 3D cooking game
Primary request: one returned reusable restaurant plate with a few obvious brown sauce smears and crumbs, clearly dirty but not disgusting
Scene/backdrop: clean warm off-white studio background
Style/medium: polished chunky toy-like 3D mobile-game icon, rounded bevels, soft ceramic
Composition/framing: square 1:1, centered, gentle elevated three-quarter view, generous margin, strong readable silhouette
Lighting/mood: bright soft studio lighting, playful cleanup cue
Color palette: warm ivory plate with turquoise rim, warm brown sauce marks
Constraints: only one dirty plate; no food portion, sink, sponge, insects, people, hands, text, letters, numbers, logo, watermark, border, or extra props
```

### 配達代行

```text
Use case: stylized-concept
Asset type: nonverbal equipment card for a cheerful stylized 3D cooking game
Primary request: a small cream-and-turquoise insulated meal carrier box moving quickly toward a simple courier scooter helmet silhouette, communicating delivery agency pickup
Scene/backdrop: clean warm off-white studio background
Style/medium: polished chunky toy-like 3D mobile-game icon, rounded bevels, soft matte materials
Composition/framing: square 1:1, centered compact arrangement, gentle elevated three-quarter view, generous margin, clear left-to-right motion cue
Lighting/mood: bright soft studio lighting, reliable and energetic
Color palette: cream, turquoise, ochre accent, deep plum
Constraints: no person, no face, no brand, no text, letters, numbers, logo, watermark, border, city scene, or extra props
```

## 3. 検証

- kitchen production prefab: 30/30 valid
- EditMode: 52/52成功
- PlayMode: 53/53成功
- 1-6〜1-8の各シーン生成、必須設備、時計停止解除、皿数、代行棚、バイク、可動接続部を統合テストで確認

## 4. ハッシュ

- 消火器カード: `bcba4275cee5fb6f2a8a55555dcc342e38c2d578059ec97bafceb2f9eba0dd3f`
- 清潔皿カード: `f14746ee32d6a4122ff3d58e6d54a5994d825740961fff468bab6d7927695375`
- 汚れ皿カード: `0b290bc4a70fb92c298e4f221cf64878312098bc480b99c67146a623e66baa48`
- 配達代行カード: `69c27b52c9fd37199410a58cdd14726a4b0480e5485cc8a1358918bd1b2be935`
- Blender preview: `a14a45ec37e8a75c471e83c42b9c5504b7ea1728d44dd4ee1b2a0ddd33a30593`
- Blender source: `1b20faab7fbecfc82319fa9d0b5897e2f5c37600c8e38de37e8431013fa5bc5e`
- generator: `e7b439446332ebd2df96a49ef40048d1e49a2fd8151514621b921e4c16471f7e90`
- 30 prefab aggregate: `3fa0d631d74d75ab66f0e7b8ee1cc4e509d31716ef8ca5aea415b846f133d6ef`
- EditMode XML: `1c5253dc44e16443a95f1aca25b647dc5e1529e1be7bce8d519d5ca637a859dd`
- PlayMode XML: `d66ab7b4feb02befd5807cf7ccd8c1b571c481f3243ea8f8a6f36d0b8f1861bc`

## 5. 実機確認ゲート

- 1-6で炎と消火器が注文票・厨房設備に隠れず、泡演出が操作中だけ読めること
- 1-7で清潔皿と汚れ皿を固定カメラ距離から区別できること
- 1-8で接続部の点滅から移動開始までを予測でき、バイク導線を塞がないこと
