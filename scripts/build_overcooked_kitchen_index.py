#!/usr/bin/env python3
"""Build the human-readable all-kitchen index from captured research data."""

from __future__ import annotations

import json
import re
import sys
import urllib.parse
from collections import Counter, defaultdict
from pathlib import Path


PACK_ORDER = [
    "Overcooked!",
    "The Lost Morsel",
    "Festive Seasoning",
    "Overcooked! 2",
    "Surf 'n' Turf",
    "Kevin's Christmas Cracker",
    "Chinese New Year",
    "Campfire Cook Off",
    "Night of the Hangry Horde",
    "Carnival of Chaos",
    "Winter Wonderland",
    "Spring Festival",
    "Sun's Out, Buns Out",
    "Moon Harvest",
    "The Ever Peckish Rises",
    "Birthday Party",
    "World Food Festival",
]


TAG_RULES = [
    (r"isolated chefs|separated chefs|separated kitchens", "担当エリア分断"),
    (r"semi[- ]separated", "半分断"),
    (r"conveyor", "コンベア"),
    (r"moving table|moving counter|moving station|moving chopping|moving oven", "可動カウンター／設備"),
    (r"moving platform|shifting platform|sinking platform|raising platform", "可動・沈降足場"),
    (r"moving stair", "可動階段"),
    (r"moving bridge|shifting bridge", "可動橋"),
    (r"bottleneck|narrow|1 block|walked around", "狭路・ボトルネック"),
    (r"portal", "ポータル"),
    (r"pressure plate|button|lever|control switch|control stick|switches", "ボタン／レバー式地形"),
    (r"unlockable gate|opening doors", "開閉ゲート"),
    (r"cannon", "大砲輸送／火球"),
    (r"fireworks|falling meteors", "飛来物・爆発"),
    (r"changing fires|\bfire\b", "炎・火災床"),
    (r"moving truck|cars|pedestrian|conga|dancing dragon|moving obstacles", "横断する移動障害"),
    (r"slippery|\bice\b", "滑る床"),
    (r"wind|gust", "風で押し流す"),
    (r"rats|mouse", "食材泥棒"),
    (r"darkness", "視界制限"),
    (r"earthquake", "地震"),
    (r"rotating", "回転地形"),
    (r"elevator", "エレベーター"),
    (r"lily pads", "蓮の葉足場"),
    (r"backpack", "背負い式食材供給"),
    (r"bellows", "ふいご加熱"),
    (r"water gun", "放水洗浄・消火"),
    (r"guillotine", "ギロチン式自動裁断"),
    (r"furnace", "薪投入かまど"),
    (r"condiment", "後付け調味料"),
    (r"drinks", "ドリンク複合注文"),
    (r"limited ingredients|randomly", "限定・ランダム食材供給"),
    (r"two plates|2 plates", "皿不足"),
    (r"moving arena", "厨房全体の移動"),
]


OVERLAY_TAGS = {
    "guillotine chopping station": "ギロチン式自動裁断",
    "compact moving-ship layout": "船上厨房",
    "central bottleneck": "中央ボトルネック",
    "portal": "ポータル",
    "long transfer counter": "長い受け渡し台",
    "lava-separated work zones": "溶岩によるエリア分断",
    "cannon transport": "大砲輸送",
    "lava-surrounded arena": "溶岩包囲",
    "split upper work zones": "担当エリア分断",
    "central gap": "中央分断",
    "multiple separated rafts": "複数いかだ分断",
    "Switcheroo cards": "Switcherooカード",
    "forced chef teleport": "強制テレポート",
    "portals": "ポータル",
    "central work island": "中央作業島",
    "long parallel counters": "平行カウンター",
    "open central arena": "開放型中央アリーナ",
    "maze-like counters": "迷路状カウンター",
    "pedestrian crowd": "歩行者群",
    "delivery bag request": "デリバリーバッグ依頼",
    "open market paths": "市場の横断動線",
    "central recessed work zone": "中央くぼみ型作業区",
    "narrow circulation loop": "狭い周回動線",
    "water-separated work zones": "水路によるエリア分断",
    "narrow dock bridges": "狭い桟橋",
    "traffic lanes": "車道横断",
    "split kitchens across road": "道路による厨房分断",
    "pedestrian crossings": "横断歩道",
    "divided prep and cook lanes": "下処理・加熱レーン分断",
    "central moving-counter candidate": "中央設備変化（目視推定）",
    "narrow bridges": "狭い橋",
    "moving pedestrian column": "移動する歩行者列",
    "split left-right kitchens": "左右厨房分断",
    "open flame hazards": "炎床",
    "central restricted work zone": "中央作業区制限",
    "moving crowd lanes": "移動する群衆レーン",
    "split work zones": "担当エリア分断",
    "tight transfer counters": "狭い受け渡し台",
    "water-separated inner ring": "水路で囲まれた内周",
    "restricted crossings": "限定横断路",
}


THEME_TRANSLATIONS = {
    "Pirate ship": "海賊船",
    "Lava temple": "溶岩寺院",
    "Broken rafts at sea": "海上の壊れたいかだ",
    "Birthday hall": "誕生日ホール",
    "Birthday garden": "誕生日庭園",
    "Baked Bazaar": "ベイクド・バザール",
    "Baked Bazaar docks": "ベイクド・バザール桟橋",
    "Metro Mash": "メトロ・マッシュ",
    "Pepper Plaza": "ペッパー・プラザ",
}


def tags_for_wiki(row: dict) -> list[str]:
    raw = row.get("obstacles", "")
    haystack = raw.lower()
    tags: list[str] = []
    for pattern, label in TAG_RULES:
        if re.search(pattern, haystack) and label not in tags:
            tags.append(label)
    if row["title"].startswith("Horde ") or "waves of unbread" in row.get("overview", "").lower():
        tags.append("アンブレッド防衛・バリケード修理")
    if "plates = yes" in haystack:
        raw = ""
    if not tags:
        tags.append("主要環境ギミックなし／固定動線")
    return tags


def short_level_title(title: str) -> str:
    if " (" in title:
        return title.split(" (", 1)[0]
    return title


def main() -> int:
    if len(sys.argv) != 4:
        print("usage: build_overcooked_kitchen_index.py EXTRACT.json OVERLAY.json OUTPUT.md", file=sys.stderr)
        return 2
    extract_path, overlay_path, output_path = map(Path, sys.argv[1:])
    wiki_rows = json.loads(extract_path.read_text())["levels"]
    overlay_rows = json.loads(overlay_path.read_text())["levels"]

    by_pack: dict[str, list[dict]] = defaultdict(list)
    tag_counts: Counter[str] = Counter()
    wiki_count = 0
    for row in wiki_rows:
        if row["title"] == "1-1 Horde DLC":  # Empty duplicate stub in the community wiki.
            continue
        tags = tags_for_wiki(row)
        tag_counts.update(tags)
        by_pack[row["pack"]].append(
            {
                "level": short_level_title(row["title"]),
                "url": row["url"],
                "theme": row.get("theme") or "—",
                "gimmicks": "、".join(tags),
                "dynamic": "あり" if row.get("dynamic", "").lower().startswith("yes") else "—",
                "confidence": "高",
            }
        )
        wiki_count += 1

    slug = {
        "The Ever Peckish Rises": "the-ever-peckish-rises",
        "Birthday Party": "birthday-party",
        "World Food Festival": "world-food-festival",
    }
    for row in overlay_rows:
        tags = [OVERLAY_TAGS[item] for item in row["gimmicks"]]
        tag_counts.update(tags)
        url = f"https://overcooked.greeny.dev/ayce/{slug[row['pack']]}/{row['level']}"
        by_pack[row["pack"]].append(
            {
                "level": row["level"],
                "url": url,
                "theme": THEME_TRANSLATIONS.get(row["theme"], row["theme"]),
                "gimmicks": "、".join(tags),
                "dynamic": "あり" if any(k in " ".join(row["gimmicks"]).lower() for k in ["moving", "forced", "cannon"]) else "—",
                "confidence": "高" if row["confidence"] == "high" else "中",
            }
        )

    total = sum(len(rows) for rows in by_pack.values())
    lines = [
        "# Overcooked 全厨房インデックス",
        "",
        "## 調査範囲と読み方",
        "",
        f"収録厨房 {total} 件を、再収録版の重複を除いて整理した。内訳はコミュニティWiki詳細ページ {wiki_count} 件と、All You Can Eat固有厨房 {len(overlay_rows)} 件である。対戦専用の左右反転・人数差レイアウトは別厨房として重複計上していない。",
        "",
        "- `主要ギミック`: レベル固有の空間・妨害・設備メカニクス。レシピそのものは原則として除外。",
        "- `動的`: Wiki側でDynamicと明記されたもの、または固有資料で明確に時間変化が確認できたもの。実際には局所的に動く設備を含むレベルもある。",
        "- `確度`: 高は個別Wiki記述または公式記述と一致、中は公式プレビューとプレイ映像からの目視分類。",
        "- 「主要環境ギミックなし」は、レシピ、注文、皿洗いなどの通常負荷がないという意味ではない。",
        "",
        "## 収録数",
        "",
        "| コンテンツ | 厨房数 |",
        "|---|---:|",
    ]
    for pack in PACK_ORDER:
        if pack in by_pack:
            lines.append(f"| {pack} | {len(by_pack[pack])} |")
    lines.extend([f"| **合計** | **{total}** |", "", "## 全厨房一覧", ""])

    for pack in PACK_ORDER:
        rows = by_pack.get(pack, [])
        if not rows:
            continue
        lines.extend(
            [
                f"### {pack}（{len(rows)}厨房）",
                "",
                "| 厨房 | テーマ | 主要ギミック | 動的 | 確度 |",
                "|---|---|---|:---:|:---:|",
            ]
        )
        for row in rows:
            level = f"[{row['level']}]({row['url']})"
            theme = str(row["theme"]).replace("|", "/")
            gimmicks = row["gimmicks"].replace("|", "/")
            lines.append(f"| {level} | {theme} | {gimmicks} | {row['dynamic']} | {row['confidence']} |")
        lines.append("")

    lines.extend(
        [
            "## データ上の注意",
            "",
            "- All You Can Eatは旧作・DLCを再収録しているため、『200以上』という公式総数と、独自レベル数は同じ意味ではない。",
            "- コミュニティWikiの空ページ `1-1 Horde DLC` は、内容を持たない重複スタブとして集計から除外した。",
            "- Wikiの `Dynamic` 欄は厳密に統一されていない。したがって、選定では動的フラグより主要ギミック列を優先する。",
            "- All You Can Eat固有22厨房は個別文章資料が少ないため、公式が提供したレベルプレビューとプレイ映像を併用した。中確度項目は今後、実機確認時に更新する。",
            "",
            "## Sources",
            "",
            "1. Team17. [Overcooked! All You Can Eat: Updated FAQs](https://www.team17.com/news/overcooked-all-you-can-eat-updated-faqs). 2023.",
            "2. Team17. [Overcooked! 2: DLCs](https://team17.helpshift.com/hc/en/4-overcooked-franchise/faq/462-overcooked-2-dlcs/).",
            "3. Overcooked Wiki. [Levels](https://overcooked.fandom.com/wiki/Levels). Community-maintained; retrieved 2026-09-10.",
            "4. greeny & Overtyr. [Overcooked Leaderboards](https://overcooked.greeny.dev/). Level previews supplied by Team17; retrieved 2026-09-10.",
            "5. Team17. [World Food Festival Update](https://www.team17.com/news/overcooked-all-you-can-eat-world-food-festival-update-out-now). 2022.",
            "6. Digital Dimensions. [The Ever Peckish Rises 4 Star Walkthrough](https://www.youtube.com/watch?v=LP6OSbhyhVU). 2024.",
            "7. Levas. [Overcooked: Extra Garnish Full Walkthrough](https://www.youtube.com/watch?v=uV1dUj16KXM). 2022.",
            "",
        ]
    )
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text("\n".join(lines), encoding="utf-8")
    print(f"wrote {total} kitchen rows to {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
