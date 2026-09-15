# COOKED OUT! キャラクター3D本番制作仕様

- 更新日: 2026-09-11
- 対象: `style_version 1` 初期12動物
- 最初の本番モデル: カピバラ料理人
- 造形正本: [第13ラウンド](VISUAL_DEVELOPMENT_ROUND_13.md)の種・衣装定義、[第15ラウンド](VISUAL_DEVELOPMENT_ROUND_15.md)のポップ改修比率、[第16ラウンド](VISUAL_DEVELOPMENT_ROUND_16.md)の閉じ口通常表情

## 1. Unity納品物

カピバラの最終Prefabを次の固定パスへ配置する。

```text
Assets/_CookedOut/Art/Resources/Characters/CapybaraChef.prefab
```

ゲームはこのPrefabを起動時に検証して読み込む。欠落または検証失敗時はgrayboxへ戻り、不完全な本番モデルを使用しない。

## 2. 座標と外形

- Unity 1 unit = 1 m
- +Yを上、+Zを正面、原点を両足の中央にする
- 帽子を含む全高は1.80〜1.90 m
- CharacterControllerは高さ1.55 m、半径0.40 mを共通とし、種ごとに変更しない
- 約2頭身。顔と帽子で全高のおよそ半分を使う
- 手持ち基準はローカル座標 `(0, 0.72, 0.68)` とし、顔を隠さない腰前の位置へ置く
- 腕、口吻、帽子が遠距離カメラでも輪郭として読めること

## 3. 必須Prefab階層とバインド

`AnimalChefVisual`へ以下をすべて登録する。

```text
CapybaraChef
├── Player Visual Root / AnimalChefVisual
│   ├── Character Motion Root
│   │   ├── Common Cook Body
│   │   ├── Capybara Limbs
│   │   │   ├── Left Arm Pivot
│   │   │   ├── Right Arm Pivot
│   │   │   ├── Left Foot Pivot
│   │   │   └── Right Foot Pivot
│   │   └── Capybara Head
│   │       ├── Left Brow
│   │       ├── Right Brow
│   │       └── Common Wedge Chef Cap
│   └── Held Item Anchor
└── LODGroup
```

`Held Item Anchor`を`Character Motion Root`の子にしてはいけない。見た目の上下動や傾きを料理の論理位置へ伝播させないためである。

## 4. メッシュとリグ

- LOD0: 20,000 triangles以下
- LOD1: 10,000 triangles以下
- LOD2: 3,000 triangles以下
- SkinnedMeshRendererは可能なら体＋制服の1個、最大2個
- 使用マテリアルは最大2個
- 骨は64本以下、1頂点あたりの影響は最大4本
- 共通骨格名と腕脚ピボット名は12動物で維持する
- 頭、耳、口吻、角、くちばし、外鰓、尻尾だけを種固有追加骨とする
- 帽子、口吻、耳を含めRenderer Boundsを確認する
- T-poseではなく、短い腕が胴体へ埋まらない緩いA-poseで書き出す

`AnimalChefMotion`は現在、登録された腕脚ピボット、頭、眉、帽子を直接動かす。FBX骨を各ピボットへ登録すれば、grayboxと同じゲーム状態で本番Skinned Meshを駆動できる。

## 5. 材質とテクスチャ

- URP Litを使用し、Shader Graph固有機能へ依存しない
- 毛、布、口吻は色面と粗さで分離し、微細な毛カードを使用しない
- 基本色、法線、マスクを各2048×2048以下の共有アトラスへまとめる
- iOSはASTC 6×6を基準にし、顔の劣化が大きい場合だけ4×4を検討する
- 透明材質、ディザ、リアルタイム反射は使用しない
- クリーム、黄土、濃プラムの共通制服色を全12体で同じ値にする
- 毛色と種固有部位だけを個体差として変更する

## 6. LODとPrefab検査

推奨LOD遷移はLOD0 `0.55`、LOD1 `0.25`、LOD2 `0.08`、Culled `0.02`。実際の固定厨房カメラとiPhone実機でポッピングを確認して確定する。

Unityメニューの次を実行する。

```text
COOKED OUT! > Validate Production Character Assets
```

最低条件は以下。

- `AnimalChefVisual`の全バインドが存在する
- `speciesId`が`capybara`
- `productionAsset`が有効
- `SkinnedMeshRenderer`が1個以上
- `LODGroup`が1個以上
- 手持ち基準がMotion Root外にある

## 7. 受け入れ条件

- 正面・側面・背面が第13ラウンドの造形資料と一致する
- 人間の手、白手袋、既存作品固有の衣装や配色を含まない
- 待機、歩行、運搬、作業、乗車で腕脚や帽子が胴体へ大きく貫通しない
- 閉じた容器を持っても顔と容器内容が固定カメラから読める
- 30 fps目標のiPhone実機で4体同時表示を確認する
- Unity自動テスト、Prefab検査、実機目視のすべてに合格する

## 8. 現在の制作状況

[第14ラウンド](VISUAL_DEVELOPMENT_ROUND_14.md)で本番3D候補をUnityへ統合し、[第15ラウンド](VISUAL_DEVELOPMENT_ROUND_15.md)でポップ改修v2、[第16ラウンド](VISUAL_DEVELOPMENT_ROUND_16.md)で歯・舌・口内のない閉じ口v3へ更新した。次の停止条件はiPhone実機での描画負荷・LOD・操作中の輪郭確認と、出荷用の毛・布表現に対する美術承認である。
