# COOKED OUT! ビジュアル開発 第10ラウンド

- 更新日: 2026-09-11
- 対象: `style_version 1` 第1〜9章厨房キーアート
- 状態: 9章分の承認候補が完成
- 生成方式: Codex組み込み画像生成。各章を一資産一生成し、既存承認候補を参照画像として使用

## 1. 成果物

| 章 | 工程テーマ | ファイル | 主な環境・機構 |
|---:|---|---|---|
| 1 | 基礎免許 | `concepts/chapter_01_basic_license_kitchen_sv1_key_art_v1.png` | 町の屋上訓練厨房、基本設備、低い壁、配達バイク |
| 2 | 切って組む | `concepts/chapter_02_cut_and_assemble_kitchen_sv1_key_art_v1.png` | 青果市場、半分断、長い組立・受渡し台、共有組立区 |
| 3 | 鍋を回す | `concepts/chapter_03_keep_pots_moving_kitchen_sv1_key_art_v1.png` | 運河沿い、複数鍋、遠隔ボタン、圧力板、接続橋 |
| 4 | 焼いて揃える | `concepts/chapter_04_grill_and_match_kitchen_sv1_key_art_v1.png` | 祭り通り、複数フライパン、短いコンベア、開閉ゲート |
| 5 | 米と麺を合わせる | `concepts/chapter_05_rice_and_noodles_kitchen_sv1_key_art_v1.png` | 川下り厨房、米・麺の二系統、長い受渡し、接岸部 |
| 6 | 包んで仕上げる | `concepts/chapter_06_wrap_and_finish_kitchen_sv1_key_art_v1.png` | 空中テラス、可動橋、小型回転島、包み組立 |
| 7 | 二系統を同期する | `concepts/chapter_07_synchronize_two_lines_kitchen_sv1_key_art_v1.png` | 工事中屋上、上下作業区、大型リフト、配達路 |
| 8 | セットを完成させる | `concepts/chapter_08_complete_the_set_kitchen_sv1_key_art_v1.png` | 雪上移動舞台、複数完成部品、容器占有、予告床 |
| 9 | 最終大型出張 | `concepts/chapter_09_final_large_outing_kitchen_sv1_key_art_v1.png` | 飛行旗艦、三区画、橋・搬送・リフトの総合試験 |

全画像は1448×1086 PNG。生成画像内の設備数、セル数、座標、橋やリフトの接続状態は実装仕様ではない。実装はグリッドステージデータを正とする。

## 2. 比較・評価

| 評価軸 | 第1〜9章の結果 | 判定 |
|---|---|---|
| 同一作品性 | クリーム天板、濃灰筐体、琥珀・ティールの識別色、丸い面取り、マットな玩具質感を全章で維持 | 合格 |
| カメラ | 厨房全体と外周を一画面へ収める高角度三人称俯瞰を維持 | 合格 |
| キャラクター | 人間なし。約2頭身、共通帽子・制服、種固有の顔・耳・尾等を維持 | 合格 |
| 工程テーマ | 各章の工程と再利用ギミックを環境の主役にできた | 合格 |
| 食材供給箱 | 共通筐体と差替え上面カードの規則が全章で読める | 合格。ただし実装では正本のカード画像を貼る |
| 容器 | 組立中は開蓋、運搬中の完成品は閉蓋という状態差を概ね維持 | 合格。個別素材化時に再検査 |
| 背景移動 | 川・雪上車・飛行舞台は見た目上のみ移動し、論理厨房は固定して見える | 合格 |
| 既存作品との差別化 | 固有キャラクター、衣装、UI、厨房配置、配色の複製なし | 合格 |
| 実装座標との分離 | 全画像を美術基準として扱い、座標を正本へ逆輸入しない | 合格 |

第1章を基準に第2〜9章を比較した結果、設備の共通部品性と章ごとの外周景観差が両立している。難度上昇も、装飾量だけでなく、受渡し、鍋、搬送、橋、回転島、リフト、環境予告、三区画総合という段階で読める。

## 3. 共通プロンプト

各章では次の共通条件を固定し、章別ブリーフだけを差し替えた。

```text
Create one polished 3D game environment key art image for COOKED OUT!. Continue the exact same original style_version 1 visual language as the attached references: one complete high-angle fixed three-quarter top-down view, readable square floor grid, chunky rounded modular one-cell equipment, cream worktops, charcoal bodies, restrained amber and teal accents, matte toy-like surfaces, soft shadows, large readable forms and low detail density. Include approximately two-heads-tall animal chefs from the approved roster, shared white chef hats, cream shirts and dark aprons, species-specific anatomy, no humans. Common ingredient supply crates use identical bodies and interchangeable rounded top photo cards. Food uses large toy-like forms and strong color blocks. Any completed container carried in gameplay has its clear lid closed; an order or assembly view may show the lid fully open. No text, logo, HUD, labels, arrows, borders or split panels. Do not reproduce any existing game’s character, costume, UI, layout or color placement. Concept key art only; depicted coordinates are not implementation specifications.
```

## 4. 章別プロンプト差分

### 第1章

```text
Chapter 1 “Basic License”. Show a bright neighborhood rooftop / courtyard training kitchen above a warm coastal town. Include the complete basic equipment set, a lettuce source crate, low practice wall and small teal delivery scooter. Three chefs chop lettuce, watch a pot and carry a closed delivery container. Warm morning sunlight.
```

### 第2章

```text
Chapter 2 “Cut and Assemble”. Show a covered produce-market courtyard focused on ingredient differences, common bases, advance preparation and handoff cooperation. Divide it with a low wall into two work zones linked by one long handoff counter and a shared assembly zone. Three chefs chop fruit, assemble a sandwich-like food and pass an ingredient. Green awnings and filtered midday light.
```

### 第3章

```text
Chapter 3 “Keep the Pots Moving”. Show a canal-side boiler courtyard built around batch cooking and monitoring multiple heavy pots. Use two cooking zones joined by a small bridge or movable counter, with three pot stoves, finished-pot staging, a teal remote button, pressure plate and amber-framed gate. Three chefs stir tomato soup, move a lidded pot and press the button. Cream brick, copper pipes, steam and late-afternoon light.
```

### 第4章

```text
Chapter 4 “Grill and Match”. Show an evening festival-street grill kitchen focused on individual frying timers, side-dish matching, conveyors, gates and crossing traffic. Include two frying stations, a short conveyor, alternating gate and a subordinate parade lane. Three chefs watch a frying pan, combine a grilled main with a side and receive a container. Warm lantern light and coral dusk.
```

### 第5章

```text
Chapter 5 “Combine Rice and Noodles”. Show a stable square-grid river-barge kitchen focused on rice pots, noodle pots, common bases, ingredient branching and synchronized handoffs. Include two cooking lines, rice staging, a long dock-facing handoff counter, periodic service window and compact joining platform. Three chefs portion rice, handle noodles and assemble a container. Turquoise river, pale wood deck and cream canopy.
```

### 第6章

```text
Chapter 6 “Wrap and Finish”. Show an elevated terrace kitchen with compact work platforms connected by a timed bridge and a small 90-degree rotating assembly island. Focus on frying components, wrapping or sandwiching, toppings and predicting connections. Three chefs fry a patty, fold a large wrap and carry a closed burger container. Pale stone terraces, lavender sky and distant airship.
```

### 第7章

```text
Chapter 7 “Synchronize Two Production Lines”. Show a construction-site rooftop split into upper cooking and lower dispatch platforms, synchronized by a large cargo lift and long handoff route. Combine pot and frying lines, staging, changing delivery paths and gentle wind. Three chefs tend noodles, fry a second component and wait by the lift with a closed container. Steel scaffolds, pale concrete and city skyline.
```

### 第8章

```text
Chapter 8 “Complete the Set”. Show a large winter festival kitchen on a moving snow stage; logical grid remains fixed and travel is background only. Focus on breakfast and bento sets, multiple completed components and container occupancy. Include multiple frying stations, staging, final assembly, safe teal path, amber warning floor and readable supply landing areas. Three chefs monitor, assemble and deliver. Snowy blue dusk, warm lanterns and distant fireworks.
```

### 第9章

```text
Chapter 9 “Final Large Outing”. Show a grand final cooperative kitchen on three geometric work districts around a central final assembly platform aboard an original traveling sky-and-river festival flagship. Combine only established systems: connecting bridge, short conveyor, cargo lift, long handoff counter, pot and frying lines. Four chefs chop, cook a pot, fry and deliver. Sunset city, river, clouds and celebratory lights form a low-detail outer vista.
```

## 5. 次工程

1. 9枚を章の美術基準としてレビューし、修正が必要な章だけ局所編集する。
2. 第1章の3D縦切りキットを先行制作し、画像の形状言語を実寸・ピボット・ポリゴン・UV・Colliderへ変換する。
3. 3D検証後、各章の背景モジュールと再利用ギミックの三面図・状態図を制作する。
4. ステージカード54枚はグリッドデータ確定後に生成し、キーアートの配置を流用しない。

## 6. 来歴

生成元、入力画像、出力参照、修正内容、SHA-256は`ASSET_PROVENANCE.csv`の`ART-ENV-007`〜`ART-ENV-015`を正とする。全画像は生成PNGを画素編集せずコピーした。
