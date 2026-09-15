# 第1・2章 ステージ別出現料理・画像化記録

> 注意：第2章画像は旧構成。C-D196で2-5じゃがいもカレー、2-6肉カレー＋焼肉おにぎり（4箱）へ変更済み。本記録・画像は制作時の履歴として保持し、未更新の画像を現行確定表として使わない。今回は画像再生成の依頼がないため変更していない。

- 日付：2026-09-14
- 用途：料理設計の確認用。実装用アイコン・最終アート・提供容器仕様の確定ではない。
- 方法：imagegenスキルに従い内蔵 image_gen を使用。CLI/APIフォールバックは未使用。
- 第1章：[画像](chapter-1-stage-dishes-20260914-v1.png)。BASIC_TWO_AND_ADVANCED_RECIPES_PROPOSAL.mdの最新修正案を採用して図示し、画像内にも「最新修正案」と記載。旧承認表との相違は統合承認していない。
- 第2章：[画像](chapter-2-stage-dishes-20260914-v1.png)。CHAPTER_2_BASIC_TWO_ORDER_PLAN.mdの承認済み構成を図示し、「料理構成：確定」と記載。イラストの見た目は参考。
- 目視確認：各画像6面・料理イラスト10点。面番号、料理名、原料箱数を資料と照合。第1章1-4は野菜焼きそばのみ、1-6は肉焼きそば／肉じゃがいもスープ。第2章2-6はじゃがいもカレー／チーズ焼肉おにぎり。双方に単品注文・参考イメージの注記あり。
- 原料箱数：第1章1・2・2・3・3・4、第2章2・3・3・3・4・6。
- 注意：器の絵、米の粒数やカレーの塊数はゲーム材料数や器具追加を表さない。画風・盛付けは今後のアート検討で変更可能。
- 共有仕様・元タスク・コードは未変更。画像作成による料理の採否変更なし。

## 第1章・生成プロンプト（全文）

```text
Use case: infographic-diagram.
Asset type: Japanese cooking-game planning reference poster, a raster image with appetizing original illustrated dishes and beautifully legible Japanese typesetting.
Primary request: Summarize exactly six stages of COOKED OUT! with a separate labeled illustration for EVERY dish in each stage. This is a planning reference, not a gameplay screenshot.
Composition: high-resolution portrait poster, approximately 3:4 aspect ratio, two columns by three rows of equal roomy cards, read left-to-right then top-to-bottom. Title band on top. Each card has a large stage number, a small ingredient-box-count label, and one or two large dish illustrations with exact Japanese labels underneath. One dish card: center the single dish. Two dish card: separate side-by-side dishes with their own names, NOT a combined meal. Good whitespace, clean aligned margins, bold highly readable Japanese sans-serif lettering. Large food images, no paragraphs, no arrows.
Style: clean warm off-white editorial board, subtle thin card outlines, muted teal headings, charcoal text. Consistent original semi-realistic soft 3D food illustrations at a three-quarter overhead angle, appetizing but simple, gentle shadows. Minimal neutral serving dishes only for illustration, no unrelated props, people or kitchen scenery. No branding other than the requested exact game name. Show only recipe ingredients described; no decorative extra garnish.
Typography: reproduce every quoted Japanese text exactly and only in its specified card. Do not omit or repeat a stage, do not invent recipes. Do not number dishes. Do not add NEW or stars.
Top eyebrow exactly: "COOKED OUT!"
Footer exactly: "各料理は別々の単品注文です。料理の見た目は参考イメージです。"

Title exactly: "第1章｜ステージ別 出現料理"
Small subtitle exactly: "最新修正案"
Cards in exact row-major order:
TOP LEFT: stage "1-1", small label "原料箱：1". One dish labeled "グリーンサラダ": a small plate of fresh chopped leafy lettuce only, no tomato.
TOP RIGHT: stage "1-2", small label "原料箱：2". Two separate dishes: "グリーンサラダ" (same green lettuce-only salad); "トマトサラダ" (lettuce and vivid cut red tomatoes only).
MIDDLE LEFT: stage "1-3", small label "原料箱：2". Two separate dishes: "プレーンオムレツ" (plain golden folded egg omelet, no rice, sauce or side garnish); "チーズオムレツ" (golden egg omelet partly opened showing clearly visible melted yellow cheese, no rice).
MIDDLE RIGHT: stage "1-4", small label "原料箱：3". One dish labeled "野菜焼きそば": brown stir-fried noodles with chopped green cabbage and pale onion pieces, no meat, shrimp, carrots or red garnish.
BOTTOM LEFT: stage "1-5", small label "原料箱：3". Two separate dishes: "オニオンスープ" (small neutral bowl of amber broth with translucent onion pieces, no cheese/croutons); "トマトサラダ" (lettuce with cut red tomatoes).
BOTTOM RIGHT: stage "1-6", small label "原料箱：4". Two separate dishes: "肉焼きそば" (brown stir-fried noodles with visible browned sliced meat and green cabbage, no shrimp); "肉じゃがいもスープ" (small neutral bowl of broth with clearly visible meat and potato chunks, no carrot or green garnish).
Important constraints: exactly these six stage cards, exactly 10 labeled dish illustrations total. Do NOT use old chapter 1 menus; 1-4 has only vegetable yakisoba; 1-6 contains exactly meat yakisoba and meat-potato soup, not salad. The whole poster is explicitly a latest revision proposal.
```

## 第2章・生成プロンプト（全文）

```text
Use case: infographic-diagram.
Asset type: Japanese cooking-game planning reference poster, a raster image with appetizing original illustrated dishes and beautifully legible Japanese typesetting.
Primary request: Summarize exactly six stages of COOKED OUT! with a separate labeled illustration for EVERY dish in each stage. This is a planning reference, not a gameplay screenshot.
Composition: high-resolution portrait poster, approximately 3:4 aspect ratio, two columns by three rows of equal roomy cards, read left-to-right then top-to-bottom. Title band on top. Each card has a large stage number, a small ingredient-box-count label, and one or two large dish illustrations with exact Japanese labels underneath. One dish card: center the single dish. Two dish card: separate side-by-side dishes with their own names, NOT a combined meal. Good whitespace, clean aligned margins, bold highly readable Japanese sans-serif lettering. Large food images, no paragraphs, no arrows.
Style: clean warm off-white editorial board, subtle thin card outlines, muted teal headings, charcoal text. Consistent original semi-realistic soft 3D food illustrations at a three-quarter overhead angle, appetizing but simple, gentle shadows. Minimal neutral serving dishes only for illustration, no unrelated props, people or kitchen scenery. No branding other than the requested exact game name. Show only recipe ingredients described; no decorative extra garnish.
Typography: reproduce every quoted Japanese text exactly and only in its specified card. Do not omit or repeat a stage, do not invent recipes. Do not number dishes. Do not add NEW or stars.
Top eyebrow exactly: "COOKED OUT!"
Footer exactly: "各料理は別々の単品注文です。料理の見た目は参考イメージです。"

Title exactly: "第2章｜ステージ別 出現料理"
Small subtitle exactly: "料理構成：確定"
Cards in exact row-major order:
TOP LEFT: stage "2-1", small label "原料箱：2". One dish labeled "魚のにぎり": recognizable nigiri sushi, glossy pink-red raw fish slice on oval white rice, no seaweed or garnish.
TOP RIGHT: stage "2-2", small label "原料箱：3". Two separate dishes: "焼肉おにぎり" (triangular white rice ball slightly open showing browned sliced grilled meat filling, NO seaweed); "チーズ焼肉おにぎり" (matching triangular rice ball showing grilled meat and clearly visible melted yellow cheese filling, NO seaweed).
MIDDLE LEFT: stage "2-3", small label "原料箱：3". Two separate dishes: "魚のにぎり" (same fish nigiri); "えびのにぎり" (clearly recognizable curved orange-white cooked shrimp laid over oval white rice, no seaweed).
MIDDLE RIGHT: stage "2-4", small label "原料箱：3". Two separate dishes: "チーズホットサンド" (golden toasted triangular sandwich halves with melted yellow cheese visible, no meat or vegetables); "チーズトマトホットサンド" (matching toasted sandwich with clearly visible red tomato slices and yellow cheese).
BOTTOM LEFT: stage "2-5", small label "原料箱：4". One dish labeled "肉カレー": Japanese brown curry with visible meat and onion beside white rice on the same plate. No potato, carrot or garnish.
BOTTOM RIGHT: stage "2-6", small label "原料箱：6". Two separate dishes: "じゃがいもカレー" (Japanese brown curry with clearly visible potato chunks and onion beside white rice, NO meat or carrot); "チーズ焼肉おにぎり" (same triangular white rice ball with visible grilled meat and cheese filling, no seaweed).
Important constraints: exactly six stage cards, exactly 10 labeled dish illustrations total. Curry includes its rice as one recipe, but do NOT combine curry and rice-ball dishes into one set meal. Do not include plain salted rice balls, minced meat onigiri, tempura, pizza, or chapter 3 dishes.
```
