#!/usr/bin/env python3
"""Build HeartlessLullabyRU.dll.

The build needs the game's managed assemblies and a Roslyn csc.exe.
It never launches the game.

Usage:
    python tools/build_plugin.py --game "C:\\path\\to\\VTM Heartless Lullaby"
    python tools/build_plugin.py --game "..." --csc "C:\\path\\to\\csc.exe"

Environment variables (used when an argument is not passed):
    HL_GAME_DIR      game folder with VTM Heartless Lullaby.exe
    HL_BEPINEX_CORE  folder with BepInEx.dll and 0Harmony.dll
    HL_CSC           path to csc.exe
"""
from __future__ import annotations

import argparse
import hashlib
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "HeartlessLullabyRU"
ASSETS = ROOT / "assets"
OUT_DIR = ROOT / "build" / "out"
DEPS = ROOT / "tools" / "deps"

SOURCES = [
    "Plugin.cs",
    "TranslationData.cs",
    "TextTranslator.cs",
    "DialoguePatcher.cs",
    "FontManager.cs",
    "SpriteReplacer.cs",
]

FONTS = [
    "OswaldSemiBold.ttf",
    "PTMono-Regular.ttf",
    "OldStandard-Regular.ttf",
    "RubikWetPaint-Regular.ttf",
    "RussoOne-Regular.ttf",
]

FRAMEWORK_REFERENCES = [
    "mscorlib.dll",
    "System.dll",
    "System.Core.dll",
    "System.Xml.dll",
]

MANAGED_REFERENCES = [
    "netstandard.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.TextRenderingModule.dll",
    "UnityEngine.TextCoreModule.dll",
    "UnityEngine.ImageConversionModule.dll",
    "UnityEngine.UIModule.dll",
    "Unity.TextMeshPro.dll",
    "Assembly-CSharp-firstpass.dll",
    "Newtonsoft.Json.dll",
]


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Build the Russian localization plugin.")
    parser.add_argument("--game", help="Game folder (contains VTM Heartless Lullaby_Data).")
    parser.add_argument("--csc", help="Path to Roslyn csc.exe.")
    parser.add_argument("--bepinex-core", help="Folder with BepInEx.dll and 0Harmony.dll.")
    return parser.parse_args()


def find_game(arg: str | None) -> Path:
    candidates: list[Path] = []
    if arg:
        candidates.append(Path(arg))
    if os.environ.get("HL_GAME_DIR"):
        candidates.append(Path(os.environ["HL_GAME_DIR"]))

    program_files_x86 = Path(os.environ.get("ProgramFiles(x86)", r"C:\Program Files (x86)"))
    program_files = Path(os.environ.get("ProgramFiles", r"C:\Program Files"))
    for base in (program_files_x86, program_files):
        candidates.append(base / "Steam" / "steamapps" / "common" / "Vampire The Masquerade - Heartless Lullaby")
        candidates.append(base / "GOG Galaxy" / "Games" / "Vampire The Masquerade - Heartless Lullaby")

    for candidate in candidates:
        managed = candidate / "VTM Heartless Lullaby_Data" / "Managed"
        if managed.is_dir():
            return candidate

    print("Game folder was not found.")
    print("Pass it explicitly: --game \"C:\\path\\to\\Vampire The Masquerade - Heartless Lullaby\"")
    print("or set HL_GAME_DIR.")
    sys.exit(2)


def find_bepinex_core(arg: str | None, game: Path) -> Path:
    candidates: list[Path] = []
    if arg:
        candidates.append(Path(arg))
    if os.environ.get("HL_BEPINEX_CORE"):
        candidates.append(Path(os.environ["HL_BEPINEX_CORE"]))
    candidates.append(game / "BepInEx" / "core")
    candidates.append(DEPS / "bepinex" / "BepInEx" / "core")

    for candidate in candidates:
        if (candidate / "BepInEx.dll").is_file() and (candidate / "0Harmony.dll").is_file():
            return candidate

    print("BepInEx core was not found.")
    print("Install BepInEx 5.4.23.3 x64 into the game or run: python tools/fetch_deps.py")
    sys.exit(2)


def find_csc(arg: str | None) -> Path:
    candidates: list[Path] = []
    if arg:
        candidates.append(Path(arg))
    if os.environ.get("HL_CSC"):
        candidates.append(Path(os.environ["HL_CSC"]))
    if os.environ.get("CSC_PATH"):
        candidates.append(Path(os.environ["CSC_PATH"]))
    candidates.append(DEPS / "roslyn" / "tasks" / "net472" / "csc.exe")

    for candidate in candidates:
        if candidate.is_file():
            return candidate

    print("csc.exe (Roslyn) was not found.")
    print("Run: python tools/fetch_deps.py")
    print("or pass --csc \"C:\\path\\to\\csc.exe\" / set HL_CSC.")
    sys.exit(2)


def require_file(path: Path, what: str) -> None:
    if not path.is_file():
        print(f"Missing {what}: {path}")
        sys.exit(2)


def build_resources() -> list[tuple[Path, str]]:
    resources: list[tuple[Path, str]] = []

    for name in ("ru_dialogue.json", "ru_ui.json", "ru_sprites.json"):
        path = ASSETS / name
        require_file(path, "translation data")
        resources.append((path, "HeartlessLullabyRU.Resources." + name))

    for name in FONTS:
        path = ASSETS / "fonts" / name
        require_file(path, "bundled font")
        resources.append((path, "HeartlessLullabyRU.fonts." + name))

    sprite_dir = ASSETS / "sprites_ru"
    if sprite_dir.is_dir():
        for path in sorted(sprite_dir.iterdir()):
            if path.suffix.lower() in (".png", ".jpg", ".jpeg"):
                resources.append((path, "HeartlessLullabyRU.sprites." + path.name))
    else:
        print("Warning: assets/sprites_ru is missing; the build will not replace baked-in images.")

    return resources


def main() -> None:
    args = parse_args()
    game = find_game(args.game)
    managed = game / "VTM Heartless Lullaby_Data" / "Managed"
    bepinex_core = find_bepinex_core(args.bepinex_core, game)
    csc = find_csc(args.csc)

    OUT_DIR.mkdir(parents=True, exist_ok=True)

    framework = Path(os.environ.get("WINDIR", r"C:\Windows")) / "Microsoft.NET" / "Framework64" / "v4.0.30319"
    refs: list[Path] = [framework / name for name in FRAMEWORK_REFERENCES]
    refs += [managed / name for name in MANAGED_REFERENCES]
    refs.append(bepinex_core / "0Harmony.dll")
    refs.append(bepinex_core / "BepInEx.dll")
    for ref in refs:
        require_file(ref, "reference assembly")

    sources: list[Path] = []
    for name in SOURCES:
        path = SRC / name
        require_file(path, "source file")
        sources.append(path)

    resources = build_resources()
    out_dll = OUT_DIR / "HeartlessLullabyRU.dll"

    command = [
        str(csc),
        "/nologo",
        "/target:library",
        "/langversion:latest",
        "/optimize+",
        "/platform:anycpu",
        "/out:" + str(out_dll),
    ]
    command += ["/reference:" + str(ref) for ref in refs]
    command += [str(source) for source in sources]
    command += ["/resource:" + str(path) + "," + logical for path, logical in resources]

    print("Game:      ", game)
    print("BepInEx:   ", bepinex_core)
    print("Compiler:  ", csc)
    print("Resources: ", len(resources))
    print("Compiling...")
    result = subprocess.run(command, capture_output=True, text=True)
    if result.stdout.strip():
        print(result.stdout)
    if result.stderr.strip():
        print(result.stderr)
    if result.returncode != 0:
        sys.exit(result.returncode)

    data = out_dll.read_bytes()
    print("Built:     ", out_dll, len(data), "bytes")
    print("SHA-256:   ", hashlib.sha256(data).hexdigest())


if __name__ == "__main__":
    main()
