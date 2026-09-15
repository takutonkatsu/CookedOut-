#!/usr/bin/env python3
"""Collect the base Overcooked! 2 stage and score reference into JSON.

The level metadata is merged from the repository's preserved Fandom extract. Score
thresholds are parsed from coo1man's Steam community guide, which lists the PC
values for 1-4 players and 1-4 stars.
"""

from __future__ import annotations

import html as html_lib
from html.parser import HTMLParser
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]
LEVELS_PATH = ROOT / "docs/research/data/overcooked_fandom_level_extract.json"
OUTPUT_PATH = ROOT / "docs/research/data/overcooked2_base_reference.json"
SCORE_URL = "https://steamcommunity.com/sharedfiles/filedetails/?id=1832823209"


class GuideParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.stack: list[tuple[str, set[str]]] = []
        self.capture: tuple[str, int] | None = None
        self.buffer: list[str] = []
        self.current_section = ""
        self.current_stage = ""
        self.current_row: list[str] | None = None
        self.tables: list[dict] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        classes = set(dict(attrs).get("class", "").split())
        self.stack.append((tag, classes))
        depth = len(self.stack)
        if tag == "div" and "subSectionTitle" in classes:
            self.capture = ("section", depth)
            self.buffer = []
        elif tag == "div" and "bb_h1" in classes:
            self.capture = ("heading", depth)
            self.buffer = []
        elif tag == "div" and "bb_table_tr" in classes:
            self.current_row = []
        elif tag == "div" and ("bb_table_td" in classes or "bb_table_th" in classes):
            self.capture = ("cell", depth)
            self.buffer = []

    def handle_data(self, data: str) -> None:
        if self.capture:
            self.buffer.append(data)

    def handle_endtag(self, tag: str) -> None:
        depth = len(self.stack)
        if self.capture and self.capture[1] == depth:
            kind = self.capture[0]
            value = " ".join("".join(self.buffer).split())
            if kind == "section":
                self.current_section = value
            elif kind == "heading":
                self.current_stage = value
            elif kind == "cell" and self.current_row is not None:
                self.current_row.append(value)
            self.capture = None
            self.buffer = []

        if self.stack:
            _, classes = self.stack[-1]
            if tag == "div" and "bb_table_tr" in classes and self.current_row is not None:
                if self.current_row:
                    if self.current_row[0] in {"1 Player", "2 Players", "3 Players", "4 Players"}:
                        self.tables.append(
                            {
                                "section": self.current_section,
                                "stage": self.current_stage,
                                "row": self.current_row,
                            }
                        )
                self.current_row = None
            self.stack.pop()


def fetch(url: str) -> str:
    result = subprocess.run(
        ["curl", "-L", "-sS", url],
        check=True,
        stdout=subprocess.PIPE,
        text=True,
    )
    return result.stdout


def parse_scores(page: str) -> dict[str, dict[str, list[int]]]:
    parser = GuideParser()
    parser.feed(page)
    scores: dict[str, dict[str, list[int]]] = {}
    world = ""
    for item in parser.tables:
        section = item["section"]
        if section == "Overcooked 2":
            key = "Tutorial"
        elif re.fullmatch(r"- World [1-6]", section):
            world = section.rsplit(" ", 1)[-1]
            key = f"{world}-{item['stage'].split()[-1].split('-')[-1]}"
        elif section == "- Kevins":
            key = item["stage"].replace("Level ", "")
        else:
            continue
        row = item["row"]
        if len(row) != 5:
            raise ValueError(f"Unexpected score row for {key}: {row}")
        scores.setdefault(key, {})[row[0]] = [int(x) for x in row[1:]]
    return scores


def canonical_stage(title: str) -> str:
    return re.sub(r"\s*\(Overcooked! 2\)$", "", title)


def sort_key(stage: str) -> tuple[int, int, int]:
    if stage == "Tutorial":
        return (0, 0, 0)
    if stage.startswith("Kevin "):
        return (2, int(stage.split()[-1]), 0)
    world, level = stage.split("-")
    return (1, int(world), int(level))


def main() -> int:
    preserved = json.loads(LEVELS_PATH.read_text(encoding="utf-8"))
    levels = [x for x in preserved["levels"] if x.get("pack") == "Overcooked! 2"]
    scores = parse_scores(fetch(SCORE_URL))

    records = []
    for level in levels:
        stage = canonical_stage(level["title"])
        if stage not in scores:
            raise KeyError(f"No score table found for {stage}")
        records.append(
            {
                "stage": stage,
                "url": level["url"],
                "theme": level.get("theme", ""),
                "recipes": level.get("recipes", ""),
                "time": level.get("time", ""),
                "obstacles": level.get("obstacles", ""),
                "dynamic": level.get("dynamic", ""),
                "plates": level.get("plates", ""),
                "overview": html_lib.unescape(re.sub(r"<br\s*/?>", "\n", level.get("overview", ""))),
                "scores": scores[stage],
            }
        )

    records.sort(key=lambda x: sort_key(x["stage"]))
    if len(records) != 45:
        raise ValueError(f"Expected 45 base-game kitchens, got {len(records)}")
    for record in records:
        if set(record["scores"]) != {"1 Player", "2 Players", "3 Players", "4 Players"}:
            raise ValueError(f"Incomplete player rows: {record['stage']}")

    payload = {
        "scope": "Overcooked! 2 base campaign (Tutorial + 36 story + 8 Kevin)",
        "retrieved": "2026-09-12",
        "platform": "PC/Steam score thresholds",
        "sources": {
            "scores": SCORE_URL,
            "layouts": preserved.get("source", "https://overcooked.fandom.com/wiki/Levels"),
        },
        "record_count": len(records),
        "records": records,
    }
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(OUTPUT_PATH)
    return 0


if __name__ == "__main__":
    sys.exit(main())
