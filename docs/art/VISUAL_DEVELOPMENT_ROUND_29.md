# Visual Development Round 29 — 厨房連結面・原料箱・配達境界

## 1. 反映内容

- 調理台のキャビネットを1.58 m、天板を1.62 mへ揃え、1.60 mグリッドで隣接する設備間に背景が見える隙間を残さない
- 原料箱を床面まで0.40 m下げ、上面表示を`IngredientSourceCards`の円形透過表示へ統一。写真面を0.72 mから1.34 mへ拡大し、独立した四角い台紙を廃止
- ゴミ箱を平坦な前面を持つ低背ボディへ変更。魚骨を前面へわずかに埋め込み、尻尾を上下2枚の三角形で作り直す
- バイク積載台を撤去し、厨房北端と街路の境界にバイク本体を配置。積載と乗車はバイク位置の論理ターゲットへ直接行う
- 場外落下物用の回収作業台をステージから撤去。落下物は厨房内で最後に通過した安全地点へ戻る
- 注文票の原料円を約2倍へ拡大し、下部のひし形タブを削除。材料背景を下へ延長し、票全体を70%へ縮小

## 2. Blender再生成

`tools/blender/generate_kitchen_assets.py`を正としてBlender編集正本、20 FBX、20 Unity Resources Prefabを再生成した。ゲームプレイ用ColliderとItem AnchorはUnity側の論理オブジェクトに残し、表示モデルへColliderは含めていない。

![連結厨房・ゴミ箱・バイク境界マーカーの比較プレビュー](concepts/kitchen_hotel_stainless_sv1_blender_preview_v4.png)

## 3. 検証

- EditMode: 41/41成功
- PlayMode: 46/46成功
- 確認対象: 連続天板、原料箱の接地、円形透過シェーダー、注文票寸法、バイク境界侵入防止、直接積載、回収台不在、場外落下物の安全地点復帰

## 4. ハッシュ

- Blenderプレビュー: `4f34af946c2b818ab35a236f4d54d9a248af10d606c24b22304b22b8552d963a`
- Blender編集正本: `1accb5c85cc056c95b93d2c03e4ae73dc456d27e930818469aaa004c0c913139`
- Blender生成スクリプト: `f8bc44721dcff875fd33788f9ec20dbab11e89b2773d200d43affa439d5de714`
- 20 FBX一覧の集約SHA-256: `6e8159de2058f7e4f6e33d9dfcd20530ffaf13df4f2997a2a7c8b125665b374c`
