# COOKED OUT! ビジュアル開発 第43ラウンド — 含有原料アイコンの上部拡大表示

- 更新日: 2026-09-12
- 対象: 加工済み材料、鍋・フライパン内の材料、組立中・完成済み料理の含有原料アイコン
- 状態: Unity統合・自動検証済み。固定俯瞰の実機承認待ち
- 非変更範囲: IngredientSourceCards画像、3D食材、レシピ、調理工程、当たり判定、設備座標、グリッド、カメラ、操作UI

## 1. 比較と評価

| 項目 | 第42ラウンドまで | 第43ラウンド承認候補 | 評価 |
| --- | --- | --- | --- |
| カード外形 | 0.62 mの円形カード | 0.70 mの表示面内に直径約0.60 mの白丸 | 白丸の見かけ寸法をほぼ保ち、原料だけを大きく見せる |
| 原料写真 | 元画像の余白を含めて等倍表示 | 中心基準で1.12倍へ拡大 | 白丸の縁近くまで原料が入り、小さい画面でも識別しやすい |
| 白丸からの張り出し | 円周で写真全体を切り抜き | 四隅の背景色との差から原料部分を抽出し、白丸の外側へわずかに許可 | 玩具シールのような密度を作りつつ、背景は円形を維持 |
| 料理との距離 | 全Renderer最高点から0.55 m | 全Renderer最高点から0.78 m | 食材、鍋、蒸気、透明蓋へ被らない上部位置を確保 |
| 複数原料 | 0.68 m間隔の自動グリッド | 0.76 m間隔の最大2×2自動グリッド | 拡大後も互いに重ならず、1セル内へ収まる |

評価: 白丸を単純に大きくするのではなく、原料写真の占有率を上げた。単品、鍋・フライパン、完成容器のいずれも料理本体の上へ十分離れ、IngredientSourceCardsの大きな形と色をそのまま読める。

## 2. 実装ブリーフ

```text
Keep the existing IngredientSourceCards as the sole ingredient-image source.
For processed ingredients, cookware contents, assembled food, and completed meals, place the ingredient indicators clearly above every rendered part of the item so they never cover the food, pan, pot, steam, or closed transparent lid.
Preserve a clean warm-white circular background, enlarge the ingredient photograph until it nearly fills that circle, and allow only the ingredient subject to extend slightly beyond the circle like a dense toy sticker.
Keep every card readable from the fixed elevated camera and independent from the carried item's rotation.
For multiple ingredients, retain the automatic maximum 2x2 camera-aligned layout with a visible gap and keep the group inside one logical grid cell.
Do not create new ingredient artwork and do not change recipes, gameplay timing, colliders, anchors, grid data, or equipment coordinates.
```

## 3. 実装

- `IngredientIndicatorCardSize`を0.62 mから0.70 mへ拡大
- `IngredientIndicatorVerticalClearance`を0.55 mから0.78 mへ拡大
- 表示間隔をカード寸法へ追従させ、0.76 m間隔の最大2×2配置へ更新
- `CircularIngredientCard`へ表示専用の写真ズーム、白丸半径、原料部分の張り出し設定を追加
- ワールド上の含有原料表示だけへ1.12倍ズーム、白丸半径0.43、原料張り出し有効を設定
- テクスチャ四隅のスタジオ背景色との差を使い、白丸外では原料に該当する画素だけを残す
- 原料箱上面と注文票は従来設定を維持し、加工品・料理上の表示だけを変更

## 4. 検証

- 加工済みにんじんのカード寸法、クリアランス、写真拡大率、白丸半径、張り出し設定をPlayModeで検証
- 単品カード下面が材料最高点より0.68 m超上にあることを検証
- 4原料の鍋で全カードが2×2に並び、互いに重ならず1グリッドセル内へ収まることを検証
- 鍋カード下面が鍋・食材・蒸気の最高点より0.68 m超上にあることを検証
- 完成容器カード下面が料理・透明蓋の最高点より0.68 m超上にあることを検証
- カメラ基準の表示向きとIngredientSourceCardsの参照先を維持
- EditMode: 52/52成功
- PlayMode: 54/54成功
- C#コンパイルエラー、シェーダーエラー・警告なし

## 5. ハッシュ

- WorldItems: `c07c99e6af588e331e77c3780caae51d3a6fb26a82c923f8f0c13172c631b7e1`
- CircularIngredientCard shader: `31f8612be364751893bab5dd8ae92b94dd7d2c58bd0344196bcfc73966e7c42c`
- PlayModeテスト: `86072f8718e7825c6f1486f22841b73f30ec00114279058ff4bd8a36ae7d6b34`
- EditMode結果XML: `b7a8e1f9b10042c337bc72550fd7ed8c2ac1de25fd48adb2ec24e74c10e9867d`
- PlayMode結果XML: `0f2e645fc2648010e2980eff57e1ec1b517426aca61a953268595ac6a92cf35b`

## 6. 実機確認ゲート

- 通常ズームで原料写真が白丸の縁近くまで大きく見えること
- 原料の張り出しがごく小さく、四角い背景や不自然な切り抜きに見えないこと
- 手持ち、作業台、コンロ上のいずれでも料理本体とアイコンが重ならないこと
- 4原料の2×2表示が隣のセルやプレイヤー表示を過度に隠さないこと
