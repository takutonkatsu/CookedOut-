# ADR-001: Unity 6.3 LTS とレイヤー分離

- 状態: 縦切り向けに採用
- 日付: 2026-09-11
- 対象: `stage.tutorial.1-1`

## 背景

初期対象はiPhone横持ちで、3Dの協力料理アクションを段階的に実装する。現時点では本番マルチプレイ、広告、課金、サーバーを実装せず、後から接続できる境界だけが必要である。ゲーム仕様上、料理状態、注文、得点、星判定をフレーム更新やMonoBehaviourから独立させ、同じ入力とSeedで再現できる必要がある。

## 決定

- Unity 6.3 LTSの3DプロジェクトとUniversal Render Pipelineを縦切りの基盤にする。
- iOS／Metal／横持ちを対象にし、iOSビルドはIL2CPPで生成する。
- Unity Input Systemを使う。Editorのキーボード入力と画面上の仮タッチUIは、同じ`KitchenCommandBuffer`へ変換する。
- 本番アセットは使用せず、プリミティブ、単色マテリアル、文字ラベルでグレーボックスを作る。
- 実装を次のassemblyへ分割する。

| Assembly | 責務 | Unity API依存 |
|---|---|---|
| `CookedOut.Domain` | 食材状態、レシピ、注文、営業、得点、星、外部port | なし |
| `CookedOut.Application` | 営業開始、調理、組立、提供のユースケース | なし |
| `CookedOut.Presentation` | MonoBehaviour、入力、カメラ、HUD、グレーボックス | あり |
| `CookedOut.Infrastructure` | Clock、Seed付き乱数、分析、保存、ネットワークadapter | なし（現段階） |
| `CookedOut.Tests.*` | EditMode単体テストとPlayMode統合テスト | テスト種別による |

- コンテンツは安定した文字列IDで識別し、表示ラベルやPrefab参照をDomainへ入れない。
- 厨房の地形、設備占有、スポーン、座標変換、経路検証はUnity非依存の正方形グリッドデータに置き、Presentation層がそのデータをワールド表現へ変換する。プレイヤー移動は連続座標のままとする。
- 営業状態はUnityオブジェクトを含めず、`ShiftSnapshot`へ変換できる構造にする。
- 保存、分析、ネットワークはinterface越しに接続し、縦切りではメモリ内／no-op／offline adapterを使う。
- シーン内容は本番Prefabの代わりに起動時bootstrapで生成する。これにより、アセット制作前でも厨房ループとテストを更新できる。

## 影響

- Unityを起動せずにDomainとApplicationの設計を読める一方、Unity Test Frameworkの実行にはUnity Editorが必要である。
- 起動時生成のグレーボックスは縦切り専用であり、本番ステージ制作時はPrefab／ScriptableObject等へ置き換える。
- URP assetは初回Editor import時に`Assets/_CookedOut/Settings`へ生成される。生成後はassetをGit管理する。
- Unityの別エンジンへの変更は本ADRの置換ADR、移行範囲、ユーザー確認を必要とする。
