# COOKED OUT! ビジュアル開発 第41ラウンド — IngredientSourceCards準拠の3D食材

- 更新日: 2026-09-12
- 対象: 生食材、加工済み食材、完成料理内の食材表現
- 状態: Blender／Unity統合・自動検証済み。横持ちiPhone実機承認待ち
- 非変更範囲: レシピ、調理工程、切断・加熱時間、当たり判定、設備座標、グリッド、カメラ、操作UI

## 1. 比較と評価

| 対象 | 第40ラウンドまで | 第41ラウンド | IngredientSourceCardsとの一致点 |
| --- | --- | --- | --- |
| 生レタス | 緑色の丸い葉塊 | 外葉、淡色の芯、太い葉脈を持つ球状の重なり | 明るい黄緑の中心と濃淡のある外葉 |
| 切断レタス | 平たい緑の楕円 | 5枚の大きな葉片と淡色の中央葉脈 | カードの葉らしい面と白緑の軸 |
| 生にんじん | 円錐と3枚の葉 | 丸い肩、先細り胴、4本の濃色成長線、5枚の葉 | オレンジの大形状、横筋、濃緑の葉束 |
| 切断にんじん | 単色の角形4個 | 丸い4切れと各切れの濃色筋 | カードの丸い玩具的切断面と横筋 |
| 生玉ねぎ | 単純な橙色球 | 球根、8本の縦筋、3分割の茎、根粒 | 金色の皮、縦方向の層、上下の端部 |
| 切断玉ねぎ | 淡色の楕円4個 | 金色の皮を残した淡色の扇形4個 | カードの皮付き白色断面 |
| 生／成形牛肉 | 少数の滑らかな赤い塊 | 粒状の縁と表面、淡色の脂身ライン | カードの粗挽き感、赤と脂身の二色構成 |
| 加熱牛肉 | 滑らかな茶色楕円 | 粒状の縁と太い焼き目 | 生状態との連続性と加熱済みの即時判別 |
| 完成料理 | 料理ごとに別の簡易球を生成 | 上記と同じ生成済み食材Prefabを縮小再利用 | 原料箱、加工中、料理内で同じ形・色・特徴を維持 |

評価: 4原料すべてでカードの識別要素を固定俯瞰から読める大きな形へ移植できた。写実的な細部は増やさず、葉脈、成長線、皮筋、脂身という原料ごとの主要な特徴だけを追加したため、既存の玩具的3D表現とも同じ作品に見える。

## 2. 3D制作ブリーフ

```text
Use the approved IngredientSourceCards as the shape and color authority for the corresponding 3D food.
Preserve a chunky toy-like mobile-game silhouette and use only a few large material regions.
Lettuce: layered round head, bright inner leaves, readable pale ribs.
Carrot: tapered orange body, rounded shoulder, four broad growth grooves, compact dark-green leaf fan.
Onion: golden round bulb, broad vertical skin ribs, short clustered stem and roots; cut wedges retain a golden skin edge.
Beef: compact round ground-beef mass or patty, coarse perimeter lobes and three broad pale fat streaks; cooked state turns brown and gains three grill marks.
Do not add text, faces, human characters, packaging brands, tiny garnish, photoreal textures, or unrelated ingredients.
All details must remain readable from the fixed elevated kitchen camera and stay within the existing mobile renderer and triangle budgets.
```

## 3. 実装

- Blender生成処理へ曲線チューブ部品を追加し、葉脈、玉ねぎの皮筋、牛肉の脂身を大きな造形として作成
- `MAT_LettuceLight`、`MAT_CarrotGroove`、`MAT_OnionHighlight`を追加し、IngredientSourceCardsの明暗関係を共有
- 生・切断状態のレタス、にんじん、玉ねぎ、牛肉と、加熱済み牛肉のFBXを再生成
- 完成サラダは`LettuceChopped`、スープは`CarrotChopped`と`OnionChopped`、ハンバーグは`BeefCooked`と`LettuceChopped`を容器内で縮小再利用
- 組み立て途中の料理も、対応する生成済み食材Prefabを使用
- 透明蓋の閉状態、料理の高さ、既存IngredientSourceCardsの浮遊表示、原料構成は維持
- 設備全体の再出力に伴い、鍋設備の操作つまみと目盛りは既存材質を再利用して6 Renderer以内を維持

## 4. 検証

- Blender 5.2.1 LTSで30 FBXと編集正本を再生成
- Unity Resources Prefab: 30/30再生成・検証成功
- 原料Prefab: 6 Renderer以下、1資産12,000三角形以下
- 完成サラダ、スープ、ハンバーグ内に対応する同一食材Prefabが含まれることをPlayModeで検証
- 完成料理が閉じた透明蓋を突き抜けないことを検証
- EditMode: 52/52成功
- PlayMode: 54/54成功

## 5. ハッシュ

- Blenderプレビュー: `3dcfc768b9594855ec32e1893888e432f6b84b5ab70c73fdae2cfd52951ca654`
- Blender正本: `ce8da05e9c797da3417dee81c51348bf93c2cbf0646167a7903044bf2a3e0345`
- Blender生成処理: `8f4d5b26dcbd91911b45070212b9a97c7d1649ca11d8656e874a56ad1579de36`
- 9食材FBX集約: `3ea6d56444d4c37e2299b014a03d0aa925a4d513908f3244d8c47f0952276229`
- 30 FBX集約: `cdc8d0b654170711502f5d16096aa10847b6cb831748916bbe65bbc17257476a`
- 30 Resources Prefab集約: `fe036d608d17ad37b879405283fe4ebd878a3452fc06c91a790e01685387b48b`
- WorldItems: `d3a7567b1c5999b6ce5c3eb1db41ad1b5c8b142d17e640cd950573e773df81ac`
- KitchenArt: `20fbbd6897900a909f378a8b1b31d027188e264e7db5abf21396906658d8706f`
- PrefabBuilder: `2ba523dd4f3ecf8eb0ed4ea510706a58e535eb5e61148ffd606975df9560ff82`
- PlayModeテスト: `6d6e489b05d7a36eb266a1e13c1520b48c54dd43a72733558a6a62d8252bf960`
- EditMode結果XML: `f7cfa59c0140b93d24917acb304289af34dfe4f3cf40a600af7e7be1d58f10c6`
- PlayMode結果XML: `4ba87da4677878e6ff18de142d93f251130b94ab48201d1e1a34df8c5f329d56`

## 6. 実機確認ゲート

- 固定俯瞰の通常ズームで、カードを見なくても4原料を色と外形で区別できること
- 切断前後で同じ原料に見え、かつ状態差も読めること
- 完成料理内の縮小モデルが透明蓋越しに潰れず、IngredientSourceCardsと競合しないこと
- 4人分の料理が近接した場合も、原料カードと料理本体が過密に見えないこと
