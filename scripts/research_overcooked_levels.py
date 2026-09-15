#!/usr/bin/env python3
"""Fetch a reproducible Overcooked level inventory from the community MediaWiki API.

This is research support code. The generated JSON is an evidence cache; the
curated Japanese research report remains the authoritative project artifact.
"""

from __future__ import annotations

import json
import re
import subprocess
import sys
import time
import urllib.parse
from pathlib import Path


API = "https://overcooked.fandom.com/api.php"
USER_AGENT = "CookedOutDesignResearch/0.1 (level-gimmick inventory)"


def api_get(params: dict[str, str]) -> dict:
    url = API + "?" + urllib.parse.urlencode(params)
    completed = subprocess.run(
        ["curl", "-L", "--fail", "--max-time", "30", "-sS", "-A", USER_AGENT, url],
        check=True,
        capture_output=True,
        text=True,
    )
    return json.loads(completed.stdout)


def category_titles() -> list[str]:
    result: list[str] = []
    continuation: str | None = None
    while True:
        params = {
            "action": "query",
            "list": "categorymembers",
            "cmtitle": "Category:Levels",
            "cmlimit": "500",
            "format": "json",
        }
        if continuation:
            params["cmcontinue"] = continuation
        data = api_get(params)
        result.extend(item["title"] for item in data["query"]["categorymembers"])
        continuation = data.get("continue", {}).get("cmcontinue")
        if not continuation:
            return result


def page_wikitext(title: str) -> str:
    data = api_get(
        {
            "action": "parse",
            "page": title,
            "prop": "wikitext",
            "format": "json",
            "formatversion": "2",
        }
    )
    return data.get("parse", {}).get("wikitext", "")


def field(text: str, name: str) -> str:
    match = re.search(rf"^\|\s*{re.escape(name)}\s*=\s*(.*)$", text, re.I | re.M)
    if not match:
        return ""
    return clean_markup(match.group(1).strip())


def clean_markup(text: str) -> str:
    text = re.sub(r"<!--.*?-->", "", text, flags=re.S)
    text = re.sub(r"\[\[(?:[^\]|]+\|)?([^\]]+)\]\]", r"\1", text)
    text = re.sub(r"\{\{O\|([^}]+)\}\}", r"\1", text)
    text = re.sub(r"\{\{([^{}|]+)(?:\|[^{}]*)?\}\}", r"\1", text)
    text = re.sub(r"'{2,}", "", text)
    return re.sub(r"\s+", " ", text).strip()


def section(text: str, heading: str) -> str:
    match = re.search(
        rf"^==\s*{re.escape(heading)}\s*==\s*(.*?)(?=^==[^=]|\Z)",
        text,
        re.I | re.M | re.S,
    )
    return clean_markup(match.group(1)) if match else ""


def pack_from_title(title: str, game: str) -> str:
    ordered = [
        "Campfire Cook Off",
        "Carnival of Chaos",
        "Chinese New Year",
        "Kevin's Christmas Cracker",
        "Moon Harvest",
        "Night of the Hangry Horde",
        "Spring Festival",
        "Sun's Out, Buns Out",
        "Surf 'n' Turf",
        "Winter Wonderland",
        "Festive Seasoning",
        "The Lost Morsel",
    ]
    for pack in ordered:
        if pack.lower() in title.lower() or pack.lower() in game.lower():
            return pack
    if title.startswith("Horde ") or "Horde DLC" in title:
        return "Night of the Hangry Horde"
    if "Overcooked! 2" in title or title.startswith("Kevin ") or title == "Tutorial":
        return "Overcooked! 2"
    if "Overcooked!" in title or title in {"Intro Apocalypse", "The Peckening"}:
        return "Overcooked!"
    return game or "Unclassified"


def should_include(title: str) -> bool:
    return title not in {"Levels", "Overcooked! Levels"}


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: research_overcooked_levels.py OUTPUT.json", file=sys.stderr)
        return 2
    output = Path(sys.argv[1]).resolve()
    rows: list[dict[str, object]] = []
    titles = [title for title in category_titles() if should_include(title)]
    for index, title in enumerate(titles, start=1):
        try:
            text = page_wikitext(title)
        except Exception as exc:  # Keep an auditable missing-page record.
            rows.append({"title": title, "error": str(exc)})
            continue
        game = field(text, "game")
        rows.append(
            {
                "title": title,
                "url": "https://overcooked.fandom.com/wiki/"
                + urllib.parse.quote(title.replace(" ", "_"), safe="!'()_-"),
                "pack": pack_from_title(title, game),
                "game_field": game,
                "theme": field(text, "theme"),
                "recipes": field(text, "recipes"),
                "time": field(text, "time"),
                "obstacles": field(text, "obstacles"),
                "dynamic": field(text, "dynamic"),
                "plates": field(text, "plates"),
                "overview": section(text, "Overview"),
                "source_wikitext_length": len(text),
            }
        )
        if index % 25 == 0:
            print(f"fetched {index}/{len(titles)}", file=sys.stderr)
        time.sleep(0.04)

    payload = {
        "source": API,
        "category": "Category:Levels",
        "retrieved_level_count": len(rows),
        "levels": rows,
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n")
    print(f"wrote {len(rows)} records to {output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
