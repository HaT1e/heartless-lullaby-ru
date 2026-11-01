#!/usr/bin/env python3
"""Static checks for the translation data and repository assets.

Exit code 0 means there are no blocking issues. Warnings do not fail the check.

Usage:
    python tools/validate_translation.py
"""
from __future__ import annotations

import json
import re
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"

EXPECTED_LINES = 1086
EXPECTED_ACTORS = 24
EXPECTED_ITEMS = 8
EXPECTED_UI = 96
EXPECTED_SPRITES = 96
EXPECTED_FONTS = 5

TAG = re.compile(r"<(/?)(color|b|i|u|size)(?:=[^>]*)?>")
WORD = re.compile(r"[^\s.,;:!?()\"«»\-–—/]+")
LATIN_WORD = re.compile(r"[A-Za-z][A-Za-z.'-]*")

# Intentional Latin: club name, song titles, French lines, graffiti, brands.
ALLOWED_LATIN = {
    "Heartless", "HEARTLESS", "Armani", "TAB", "A.C.A.B",
    "All", "Cops", "Are", "Bastards",
    "No", "Easy", "Way", "Out", "Love", "Will", "Tear", "Us", "Apart",
    "Have", "a", "Drink", "on", "Me", "Dust", "in", "the", "Wind",
    "Pas", "d'access", "mon", "coeur", "ma", "petite", "fleur",
    "Cathedralis", "Tenebrae", "A", "B", "f",
}

errors: list[str] = []
warnings: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def warn(message: str) -> None:
    warnings.append(message)


def load_json(name: str) -> dict:
    path = ASSETS / name
    if not path.is_file():
        fail(f"missing file: {path}")
        return {}
    try:
        with path.open(encoding="utf-8") as handle:
            return json.load(handle)
    except Exception as error:
        fail(f"invalid JSON in {name}: {error}")
        return {}


def check_tags(key: str, text: str) -> None:
    stack: list[str] = []
    for match in TAG.finditer(text):
        closing, name = match.group(1), match.group(2)
        if not closing:
            stack.append(name)
        else:
            if not stack or stack[-1] != name:
                fail(f"{key}: unexpected closing tag {match.group(0)}")
                return
            stack.pop()
    if stack:
        fail(f"{key}: unclosed tag(s): {', '.join(stack)}")


def check_texts(prefix: str, texts: list[tuple[str, str]]) -> None:
    mixed: list[tuple[str, str]] = []
    latin: Counter[str] = Counter()
    for key, text in texts:
        if not isinstance(text, str) or not text.strip():
            fail(f"{prefix}/{key}: empty text")
            continue
        check_tags(f"{prefix}/{key}", text)
        clean = re.sub(r"<[^>]*>", "", text)
        for word in WORD.findall(clean):
            if re.search(r"[\u0400-\u04FF]", word) and re.search(r"[A-Za-z]", word):
                mixed.append((key, word))
        for word in LATIN_WORD.findall(clean):
            word = word.rstrip(".,;:!?")
            if word and word not in ALLOWED_LATIN:
                latin[word] += 1
    if mixed:
        fail(f"{prefix}: mixed Cyrillic/Latin words: {mixed[:10]}")
    if latin:
        warn(f"{prefix}: unexpected Latin words: {dict(latin.most_common(20))}")
    if "VIP" in "\n".join(text for _, text in texts):
        warn(f"{prefix}: Latin 'VIP' found; the project uses 'ВИП'")


def main() -> None:
    dialogue_doc = load_json("ru_dialogue.json")
    ui = load_json("ru_ui.json")
    sprites_doc = load_json("ru_sprites.json")

    lines = dialogue_doc.get("lines", {})
    if not isinstance(lines, dict):
        fail("ru_dialogue.json: 'lines' must be an object")
        lines = {}
    if len(lines) != EXPECTED_LINES:
        fail(f"dialogue: expected {EXPECTED_LINES} entries, found {len(lines)}")

    dialogue_texts: list[tuple[str, str]] = []
    for key, value in lines.items():
        if not isinstance(value, dict) or "text" not in value:
            fail(f"dialogue/{key}: missing 'text'")
            continue
        dialogue_texts.append((key, value.get("text", "")))
        menu = value.get("menu")
        if menu is not None and (not isinstance(menu, str) or not menu.strip()):
            fail(f"dialogue/{key}: empty 'menu'")
    check_texts("dialogue", dialogue_texts)

    sections = {}
    for section in ("uis", "tmp", "questJournal"):
        data = ui.get(section, {})
        if not isinstance(data, dict):
            fail(f"ru_ui.json: '{section}' must be an object")
            continue
        sections[section] = data

    all_ui: dict[str, str] = {}
    for section, data in sections.items():
        for key, value in data.items():
            if not isinstance(value, str) or not value.strip():
                fail(f"ui/{section}/{key}: empty text")
                continue
            if key in all_ui and all_ui[key] != value:
                fail(f"ui: conflicting translations for '{key}'")
            all_ui[key] = value
    if len(all_ui) != EXPECTED_UI:
        fail(f"ui: expected {EXPECTED_UI} unique strings, found {len(all_ui)}")
    check_texts("ui", list(all_ui.items()))

    actors = ui.get("actors", {})
    items = ui.get("items", {})
    time_map = ui.get("time", {})
    if len(actors) != EXPECTED_ACTORS:
        fail(f"actors: expected {EXPECTED_ACTORS}, found {len(actors)}")
    if len(items) != EXPECTED_ITEMS:
        fail(f"items: expected {EXPECTED_ITEMS}, found {len(items)}")
    check_texts("actors", [(str(k), str(v)) for k, v in actors.items()])
    check_texts("items", [(str(k), str(v)) for k, v in items.items()])
    check_texts("time", [(str(k), str(v)) for k, v in time_map.items()])

    sprite_map = sprites_doc.get("map", {})
    if not isinstance(sprite_map, dict):
        fail("ru_sprites.json: 'map' must be an object")
        sprite_map = {}
    if len(sprite_map) != EXPECTED_SPRITES:
        fail(f"sprites: expected {EXPECTED_SPRITES} replacements, found {len(sprite_map)}")
    sprite_dir = ASSETS / "sprites_ru"
    for source, target in sprite_map.items():
        if not isinstance(target, str) or not target:
            fail(f"sprite/{source}: empty target")
            continue
        if not (sprite_dir / target).is_file():
            fail(f"sprite/{source}: missing file {target}")

    fonts = sorted((ASSETS / "fonts").glob("*.ttf"))
    if len(fonts) != EXPECTED_FONTS:
        fail(f"fonts: expected {EXPECTED_FONTS} TTF files, found {len(fonts)}")
    licenses = sorted((ASSETS / "fonts_licenses").glob("*.txt"))
    if len(licenses) < EXPECTED_FONTS:
        fail(f"font licenses: expected at least {EXPECTED_FONTS}, found {len(licenses)}")

    plugin = ROOT / "src" / "HeartlessLullabyRU" / "Plugin.cs"
    readme = ROOT / "docs" / "README_RU.txt"
    if not plugin.is_file():
        fail("missing src/HeartlessLullabyRU/Plugin.cs")
    if not readme.is_file():
        fail("missing docs/README_RU.txt")
    if plugin.is_file() and readme.is_file():
        plugin_version = re.search(
            r'\[BepInPlugin\(Guid,\s*"[^"]+",\s*"([^"]+)"\)\]',
            plugin.read_text(encoding="utf-8"),
        )
        readme_version = re.search(r"перевод \(v([^)]+)\)", readme.read_text(encoding="utf-8"))
        if not plugin_version:
            fail("Plugin.cs: BepInPlugin version not found")
        if not readme_version:
            fail("docs/README_RU.txt: version not found")
        if plugin_version and readme_version and plugin_version.group(1) != readme_version.group(1):
            fail(
                "version mismatch: Plugin.cs "
                f"{plugin_version.group(1)} vs README {readme_version.group(1)}"
            )

    print(f"Dialogue lines: {len(lines)}")
    print(f"Unique UI strings: {len(all_ui)}")
    print(f"Actors: {len(actors)}, items: {len(items)}")
    print(f"Sprite replacements: {len(sprite_map)}")
    print(f"Fonts: {len(fonts)}, licenses: {len(licenses)}")

    for message in warnings:
        print("WARNING:", message)
    if errors:
        for message in errors:
            print("ERROR:", message)
        print(f"FAILED: {len(errors)} error(s)")
        sys.exit(1)
    print("OK: no blocking issues")


if __name__ == "__main__":
    main()
