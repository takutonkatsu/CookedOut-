# Overcookedシリーズ・ギミック総覧（本家の参照資料）

- 調査・整理日: 2026-09-14
- 目的: COOKED OUT!の採用判断の前に、本家に存在する仕組みを把握する。
- 状態: 調査整理。COOKED OUT!への採用・面への割当・共通仕様変更を決定する文書ではない。
- 分類: 配置上の仕掛け7、動的厨房11、搬送・操作9、環境障害13、特殊調理・提供14、特殊ルール5＝**整理上59項目**。公式が定めた「59種類」ではない。同一機構の挙動違いを分けた項目と、その組合せを含む。

## 1. 範囲と確度

対象はOvercooked!、Overcooked! 2、そのDLC・季節更新、All You Can Eat（AYCE）専用追加面。個々の献立、背景だけの変化、シェフスキン、ワールドマップ車両、対戦の鏡像配置、アクセシビリティ設定、バグ利用はギミック数へ含めない。基本調理・操作は別節へ分離する。

確認対象のパック:

| 作品 | 内容 |
|---|---|
| OC1 | 本編、The Lost Morsel、Festive Seasoning |
| OC2 | 本編、Surf 'n' Turf、Campfire Cook Off、Night of the Hangry Horde、Carnival of Chaos |
| OC2季節更新 | Kevin's Christmas Cracker、Chinese New Year、Winter Wonderland、Spring Festival、Sun's Out, Buns Out、Moon Harvest |
| AYCE固有 | The Ever Peckish Rises、Birthday Party、World Food Festival |

AYCEが旧2作品とDLCを収録し、専用追加が上記3コンテンツであることは[Team17公式FAQ](https://www.team17.com/news/overcooked-all-you-can-eat-updated-faqs)で確認した。既存の[全厨房インデックス](../research/03_overcooked_all_kitchens_index.md)は206件を整理しているが、今回全206面を実機再検証した意味ではない。

公式記事で特殊機構を再確認し、個別厨房の配置は既存の[Wiki抽出データ](../research/data/overcooked_fandom_level_extract.json)の概要本文と突合した。以下の「Wiki」リンクはその保存元。今回ライブ再取得できないWikiページもあるため、配置の詳細は保存済み記述に基づく。AYCEの旧オーバーレイにある中確度の面別タグは、新たな確定根拠にはしない。

網羅を目指した参照一覧であり、版ごとの細部・全人数での挙動・全周期秒数まで検証済みとはしない。代表面は登場例であって初出・全出現面の断定ではない。

## 2. 配置だけで協力を変える仕掛け（7）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| A01 | 完全分断厨房 | シェフが別区画に分かれ、食材・器具・皿を受け渡して工程をつなぐ。人数で分断形が変わる面もある | OC2 Kevin 1、OC1 6-2。[Wiki](https://overcooked.fandom.com/wiki/6-2_(Overcooked!)) |
| A02 | 半分断・大回り | 直接往来できる場所が限られ、隣に見える設備へ行くにも回り込む | OC1船厨房など。[Wiki](https://overcooked.fandom.com/wiki/6-1_(Overcooked!)) |
| A03 | 狭路・一人幅通路 | すれ違えない橋・廊下が調理と運搬の共用動線になる | OC2 2-4、Festive Seasoning 1-6。[Wiki](https://overcooked.fandom.com/wiki/1-6_(Overcooked!_Festive_Seasoning)) |
| A04 | 共有設備・狭い作業域 | 複数人が同じまな板・火口・置き場所へ集中する。独立した機械ではなく配置条件 | OC1 6-4の中央厨房。[Wiki](https://overcooked.fandom.com/wiki/6-4_(Overcooked!)) |
| A05 | 受け渡し台・投げの遮蔽物 | 人は通れず物は置いて渡せる境界。高い壁や中央障害で投げ越しも制限される例がある | OC2 6-1、Chinese New Year 1-3。[Wiki](https://overcooked.fandom.com/wiki/1-3_(Overcooked!_2_Chinese_New_Year)) |
| A06 | 少数の皿・容器循環 | 皿が少なく、調理だけ進めても洗浄・返却が間に合わない | OC2 4-5等の皿2枚構成。[Wiki](https://overcooked.fandom.com/wiki/4-5_(Overcooked!_2)) |
| A07 | 段差による片道移動 | 高所から下へ降りられるが、戻るには別の階段・ポータルなどを使う | OC2 5-6の最終配置。[Wiki](https://overcooked.fandom.com/wiki/5-6_(Overcooked!_2)) |

## 3. 厨房や接続が変化する仕掛け（11）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| B01 | 滑動・往復する作業台 | まな板・火口・原料箱などの台が移動する。提供口への接近が変わる例も含む | OC1 1-3、OC2 Kevin 5。[Wiki](https://overcooked.fandom.com/wiki/Kevin_5) |
| B02 | 回転する作業台 | 台の回転で各シェフが使える器具・原料の側が入れ替わる | OC2 2-6、Horde 6。[Wiki](https://overcooked.fandom.com/wiki/Horde_6) |
| B03 | 動く壁・像・仕切り | 設備そのものではなく、その手前を塞ぐ障害物が移動する | Chinese New Year 1-1、Spring Festival 1-2。[Wiki](https://overcooked.fandom.com/wiki/1-1_(Overcooked!_2_Chinese_New_Year)) |
| B04 | 移動足場・接岸と離別 | いかだ・氷塊・トラックなどの位置関係が変わり、渡れる時間が限られる | OC1 3-3、6-3、OC2川面。[Wiki](https://overcooked.fandom.com/wiki/6-3_(Overcooked!)) |
| B05 | 周期的な沈下・浮上 | 通路や作業区が沈み、再浮上すると通れる形・使える設備が変わる | OC2 6-2、6-5。[Wiki](https://overcooked.fandom.com/wiki/6-2_(Overcooked!_2)) |
| B06 | 移動・切替橋 | 橋が上下・左右の別の接続位置へ移る | Horde 3、Kevin's Christmas Cracker 1-2。[Wiki](https://overcooked.fandom.com/wiki/Horde_3) |
| B07 | 動く階段 | 上下階をつなぐ階段の位置が変わり、通行ルートが変わる | OC2 3-2、5-4。[Wiki](https://overcooked.fandom.com/wiki/3-2_(Overcooked!_2)) |
| B08 | エレベーター | 高低差のある作業域の間を昇降する足場で移動する | OC2 Kevin 3。[Wiki](https://overcooked.fandom.com/wiki/Kevin_3) |
| B09 | 部屋全体の回転 | 外周の部屋が回り、中央厨房から入れる部屋・原料が切り替わる | OC1 6-4。[Wiki](https://overcooked.fandom.com/wiki/6-4_(Overcooked!)) |
| B10 | 操縦する足場・航行厨房 | 操縦装置で足場を動かす。岸の原料箱・提供口に厨房を寄せる形や、対岸の足場を遠隔操縦する形がある | OC2 5-5、Surf 'n' Turf 2-2・Kevin 1。[Wiki](https://overcooked.fandom.com/wiki/2-2_(Overcooked!_2_Surf_'n'_Turf)) |
| B11 | 地震・墜落・段階的地形変化 | 途中で床が隆起したり、気球が別厨房へ墜落したりして盤面が大きく変わる | OC1 1-6、OC2 1-6・5-6。[Wiki](https://overcooked.fandom.com/wiki/1-6_(Overcooked!_2)) |

## 4. 搬送・スイッチ・特殊移動（9）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| C01 | コンベア・動く床 | 食材や皿を流す。シェフの歩行に影響する帯もある。循環型と片道型、終点がゴミ箱の型を含む | OC1・OC2各所、Chinese New Year 1-5。[Wiki](https://overcooked.fandom.com/wiki/1-5_(Overcooked!_2_Chinese_New_Year)) |
| C02 | コンベアの逆転 | 一定周期またはボタン操作で流れる方向が逆になる | OC2 4-3、4-5。[Wiki](https://overcooked.fandom.com/wiki/4-3_(Overcooked!_2)) |
| C03 | コンベアの分岐・時限ゲート | ボタンで搬送先を切り替える。数秒だけ回収路が開き、戻ると物がゴミ箱へ流れる例もある | OC2 6-3、Moon Harvest 1-3。[Wiki](https://overcooked.fandom.com/wiki/1-3_(Overcooked!_2_Moon_Harvest)) |
| C04 | 自動・ランダム原料供給 | 固定箱から好きな時に取らず、排出機やベルトで来る原料を拾う | Lost Morsel 1-3、Campfire Kevin 3。[Wiki](https://overcooked.fandom.com/wiki/Kevin_3_(Campfire_Cook_Off)) |
| C05 | ボタン・レバーによる遠隔切替 | 扉、台、橋などを別地点から動かす。これは操作方法であり、動く対象と重複する分類 | OC2 6-6、Spring Festival 1-2。[Wiki](https://overcooked.fandom.com/wiki/1-2_(Overcooked!_2_Spring_Festival)) |
| C06 | 床スイッチ・感圧板 | 踏んでいる間だけ扉を開く型など。調味料変更と花火発射が同時に起こる面もある | OC1 5-3・5-6、Lost Morsel 1-5、Sun's Out 1-4。[Wiki](https://overcooked.fandom.com/wiki/5-6_(Overcooked!)) |
| C07 | ポータル | 離れた地点へ瞬間移動する。出入口自体が移動し、出口の足場確認が必要な例もある | OC2 3-2・5-4など。[Wiki](https://overcooked.fandom.com/wiki/5-4_(Overcooked!_2)) |
| C08 | シェフを飛ばす大砲 | シェフが入り、発射操作で別区画へ飛ぶ。照準先を操作する面もある | Carnival of Chaos、Ever Peckish Rises。[公式](https://www.team17.com/news/overcooked-2-carnival-of-chaos-dlc-out-now)・[照準の例](https://overcooked.fandom.com/wiki/2-3_(Overcooked!_2_Carnival_of_Chaos)) |
| C09 | Switcheroo | カードを使った転送の仕掛けで、予定外の位置へ移される | AYCE Birthday Party。[公式](https://www.team17.com/news/overcooked-all-you-can-eat-updated-faqs) |

## 5. 移動・作業を妨げる環境障害（13）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| D01 | 滑る氷床 | 慣性のある移動で止まりにくく、狭い道や水際で運搬を失敗しやすい | OC1 3-1・6-3。[Wiki](https://overcooked.fandom.com/wiki/6-3_(Overcooked!)) |
| D02 | 風・突風 | シェフが押される。設備台の移動と同時に吹く例もある | OC2 2-5、Kevin 5。[Wiki](https://overcooked.fandom.com/wiki/Kevin_5) |
| D03 | 落下・水没・溶岩 | 足場外へ落ちると復帰待ちになり、運搬物も失う危険がある | OC1氷面・トラック、OC2川・魔法学校など。[Wiki](https://overcooked.fandom.com/wiki/3-3_(Overcooked!)) |
| D04 | 歩行者 | 横断する人々がシェフの移動を妨げる | OC1 1-2、OC2街面。[Wiki](https://overcooked.fandom.com/wiki/1-2_(Overcooked!)) |
| D05 | コンガの行列 | 長い人の列が通路や設備前をまとめて塞ぐ | Surf 'n' Turf 1-2・3-1。[Wiki](https://overcooked.fandom.com/wiki/3-1_(Overcooked!_2_Surf_'n'_Turf)) |
| D06 | 龍の行列 | 周回・横断する龍がシェフを押し、設備やボタンへの接近を遮る | Chinese New Year、Spring Festival。[Wiki](https://overcooked.fandom.com/wiki/1-3_(Overcooked!_2_Chinese_New_Year)) |
| D07 | 走行車両 | 厨房間の道路を車が走り、横断に衝突の危険がある | OC2 4-1、Kevin 5、AYCE WFF。[Wiki](https://overcooked.fandom.com/wiki/4-1_(Overcooked!_2)) |
| D08 | 火球・環境火災 | 大砲・火元から飛ぶ火球、自然発生の炎が通路を塞ぐ。料理の焦げ由来の火災とは区別 | OC1 5-2、OC2 1-6・Kevin 4、Surf 3-2。[Wiki](https://overcooked.fandom.com/wiki/Kevin_4) |
| D09 | 隕石・花火 | 飛来して作業を妨害する。花火が床に残る火になる例や、スイッチ連動の発射もある | OC1 The Peckening、Sun's Out, Buns Out。[Wiki](https://overcooked.fandom.com/wiki/1-4_(Overcooked!_2_Sun's_Out%2C_Buns_Out)) |
| D10 | 食材を盗むネズミ | 置いた原料を持ち去ろうとする。切った原料も対象になる例がある | OC1 2-2、AYCE WFF。[Wiki](https://overcooked.fandom.com/wiki/2-2_(Overcooked!)) |
| D11 | 暗闇・視界制限 | 厨房が暗く、設備位置と移動経路を把握しにくい | OC1 4-2。[Wiki](https://overcooked.fandom.com/wiki/4-2_(Overcooked!)) |
| D12 | 踏むと沈む蓮の葉 | 乗り続けられない一時足場。周期で勝手に沈む床とは発生条件が違う | Moon Harvest。[公式配信告知](https://store.steampowered.com/news/posts/?enddate=1601557245&feed=steam_community_announcements)・[沈下挙動の報道](https://www.gamespot.com/articles/overcooked-2-gets-new-kitchens-and-recipes-in-free-moon-harvest-festival-dlc/1100-6482836/) |
| D13 | 波で洗い流される浜辺 | 波に巻き込まれるとシェフ・物が流され、同時に厨房配置も次段階へ変わる | Surf 'n' Turf 3-4。[Wiki](https://overcooked.fandom.com/wiki/3-4_(Overcooked!_2_Surf_'n'_Turf)) |

## 6. 特殊な調理設備・供給・提供（14）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| E01 | 食材入りバックパック | シェフが背負い、別のシェフがそこから原料を取り出す | Campfire Cook Off。[公式](https://www.team17.com/news/overcooked-2-campfire-cook-off-dlc-season-pass-available-now) |
| E02 | ふいご・BBQ火力補助 | 送風で焼く速度を上げる。強い火は焦げるまでの余裕も減らす | Surf 'n' Turf。[発売時の機構説明](https://www.nintendolife.com/news/2018/10/overcooked_2s_mysterious_update_revealed_brand_new_surf_n_turf_dlc_available_right_now) |
| E03 | 水鉄砲 | 離れた皿を洗い、火を消す。シェフへの水かけにも使える | Surf 'n' Turf。[公式](https://www.team17.com/news/overcooked-2-surf-n-turf-dlc-available-now)・[洗浄の説明](https://www.nintendolife.com/news/2018/10/overcooked_2s_mysterious_update_revealed_brand_new_surf_n_turf_dlc_available_right_now) |
| E04 | 薪割り・焚き火への補給 | 木を切って燃料を用意し、焚き火へ補給して調理する | Campfire Cook Off。[公式](https://www.team17.com/news/overcooked-2-campfire-cook-off-dlc-season-pass-available-now) |
| E05 | 石炭・炉によるオーブン加速 | バケツで石炭を運び、炉へ入れてオーブンの調理を速める | Night of the Hangry Horde。[公式](https://www.team17.com/news/overcooked-2-introducing-night-of-the-hangry-horde)・[バケツと加速](https://overcooked.fandom.com/wiki/1-2_(Overcooked!_2_Night_of_the_Hangry_Horde)) |
| E06 | ギロチン式裁断 | 原料を裁断台へ置き、起動してカットする。投入・起動・回収を分担できる | Night of the Hangry Horde、Ever Peckish Rises。[公式](https://www.team17.com/news/overcooked-2-introducing-night-of-the-hangry-horde) |
| E07 | 火炎放射器で調理 | オーブンに入れる代わりに、持った火炎放射器で食品を焼く | OC1 Festive Seasoning。[販売元による説明](https://www.xbox.com/en-US/games/store/the-festive-seasoning/C3NJ6L07PJ2T) |
| E08 | 大型中華鍋の移動 | ホットポット用の大鍋を別の火元へ移動する。鍋の位置で原料箱への接近も変わる | Chinese New Year、Spring Festival。[Wiki](https://overcooked.fandom.com/wiki/1-1_(Overcooked!_2_Spring_Festival)) |
| E09 | 点火位置の交替 | 複数火口のうち燃えている位置が切り替わり、鍋と火元を合わせ直す | Chinese New Year 1-2など。[Wiki](https://overcooked.fandom.com/wiki/1-2_(Overcooked!_2_Chinese_New_Year)) |
| E10 | 後付け調味料・ソース切替 | 指定ソースを完成料理へ追加する。機械の選択ボタンが別区画にある構成もある | Carnival of Chaos、Sun's Out, Buns Out。[公式](https://www.team17.com/news/overcooked-2-carnival-of-chaos-dlc-out-now) |
| E11 | ドリンクディスペンサー | 注文に応じた飲み物を機械で用意する | Carnival of Chaos。[公式](https://www.team17.com/news/overcooked-2-carnival-of-chaos-dlc-out-now) |
| E12 | コンボトレー | 食べ物と飲み物をひとつのトレーへそろえて提供する | Carnival of Chaos。[公式](https://www.team17.com/news/overcooked-2-carnival-of-chaos-dlc-out-now) |
| E13 | 箱作り・箱詰め | 専用ステーションで箱を作り、皿の代わりに料理を入れて提供する | AYCE World Food Festival。[公式FAQ](https://www.team17.com/news/overcooked-all-you-can-eat-updated-faqs) |
| E14 | デリバリーバッグによる提供口封鎖 | バッグが提供口を塞ぐ。要求された生原料を入れると配達員が回収し、提供口が使えるようになる | AYCE World Food Festival。[公式](https://www.team17.com/news/overcooked-all-you-can-eat-world-food-festival-update-out-now) |

E08の小さいパンは、失敗したホットポットの中身を取り出して捨てるための復旧用具として説明されている。通常の盛付けに毎回使う追加工程とは記載しない。[利用者の説明](https://www.reddit.com/r/OvercookedGame/comments/103j7oe)

E08について「小型鍋のように持ち上げて自由な位置へ置ける」「営業前に配置を編集できる」「COOKED OUT!の可搬設備と同じ」とは解釈しない。具体入力・移動可能範囲は別途実機確認が必要。

## 7. 目的・進行条件を変える仕掛け（5）

| ID | ギミック | 実際の挙動・主な違い | 代表例・根拠 |
|---|---|---|---|
| F01 | Horde防衛戦 | 敵の波へ料理を提供して厨房・城を守る。通常の制限時間内売上競争とは目的が違う | Night of the Hangry Horde、Winter Wonderlandの一部。[公式](https://www.team17.com/news/overcooked-2-introducing-night-of-the-hangry-horde) |
| F02 | 敵種別による提供優先度 | 通常のパン、攻撃頻度の高い唐辛子、強い攻撃と複数注文を持つリンゴなどで対処優先度が変わる | Horde。[敵種別の攻略資料](https://www.playstationtrophies.org/game/overcooked-2/trophy/265244-you-shallot-pass-.html) |
| F03 | 防衛バリケードの修理 | 提供で得たコインを使い、傷んだ防衛箇所を修理する | Horde。[公式](https://www.team17.com/news/overcooked-2-introducing-night-of-the-hangry-horde) |
| F04 | コインでゲート開放 | 稼ぎを使って近道や追加作業区への扉を開ける | Horde 5・7・8。[公式](https://www.team17.com/news/overcooked-2-introducing-night-of-the-hangry-horde)・[追加調理区](https://overcooked.fandom.com/wiki/Horde_7) |
| F05 | 注文達成で進む最終試験 | 必要料理を提供すると次の工程・盤面段階へ進み、最後の達成を目指す | OC2 6-6、OC1最終戦。地形変化そのものはB11。[Wiki](https://overcooked.fandom.com/wiki/6-6_(Overcooked!_2)) |

## 8. ギミックと混同しやすい基本操作・工程（別枠）

- 取る／置く／運ぶ、ダッシュと接触、カウンター越しの受け渡し、ソロ時のシェフ切替。
- 食材投げ。OC2系の機能で、**AYCEでもOC1収録面には投げを追加していない**。[公式FAQ](https://www.team17.com/news/overcooked-all-you-can-eat-updated-faqs)
- 切る、混ぜる、煮る・茹でる、焼く、揚げる、蒸す、ブレンダーで飲み物を作る等のレシピ工程。まな板、鍋・パン、オーブン、フライヤー、ミキサー、蒸し器などの通常設備。料理ごとにギミックを水増ししない。
- 盛付け、皿・カップ等の容器確保、汚れた皿の回収と洗浄。洗浄不要の面もあり、全厨房共通の必須工程ではない。
- 加熱待ち、焦げ、火災、消火器、誤料理の廃棄と作り直し。D08の環境発火とは発生原因が違う。
- 注文時間切れ、提供順とチップ／コンボ、制限時間、星基準。C09の強制転送やF01防衛のようなステージ固有機構とは分ける。
- Survival、Practice、Assist、Versus等のモード・設定は、本一覧のステージギミック数へ入れない。

## 9. 旧45項目の読み替え・訂正

| 旧資料の項目・記述 | 今回の整理 |
|---|---|
| 「大砲輸送／火球」 | シェフ輸送C08と攻撃・環境火球D08を分離。通常の大砲を汎用貨物射出機とは断定しない |
| 「薪投入かまど」 | 木を切って焚き火へ入れるE04と、石炭で炉・オーブンを加速するE05を分離 |
| 「炎・火災床」のChanging fires | 通路の炎D08とは別。ホットポットの加熱位置切替E09へ分類 |
| 「Switcheroo」「強制役割交換」 | C09へ統合。全シェフの相互位置交換が必ず起こるとは断定しない |
| 「デリバリーバッグ＝外部調達・補給」 | E14へ訂正。要求された生原料を渡して提供口を空ける。原料を仕入れる機構ではない |
| 「蓮の葉＝流れる足場」 | 踏むと沈むD12を中心に記載。流れる氷塊B04とは別 |
| 抜けていた特殊調理・提供 | 火炎放射器E07、大型鍋と火口切替E08・E09、箱作りE13を明記 |
| 背景としてしか捉えていなかった波 | 洗い流しと配置変更があるD13を明記 |
| 延べ登場数 | 旧機械抽出タグは混同を含むため転載しない。再集計なしに新分類の出現数を出さない |
| COOKED OUT!採用・不採用の文章 | 今回の本家一覧から除外。旧調査の採用文を現行決定として引き継がない |

## 10. 次回相談への使い方

この一覧は選択肢を把握するための資料。59項目すべてをCOOKED OUT!へ移す計画ではない。採用検討へ進む際に、興味のある仕組みについて「起動条件／対象／変化予告／操作／復旧／ソロと2〜4人」を順に詰める。新料理・新設備の追加や、章・面への割当は未決定のまま保つ。
