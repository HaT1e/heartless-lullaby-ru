#!/usr/bin/env python3
"""Assemble a ready-to-install release folder and zip.

Prerequisites:
    python tools/fetch_deps.py
    python tools/build_plugin.py --game "..."
    docs/README_RU.txt must exist.

Usage:
    python tools/package_release.py
    python tools/package_release.py --out build/test-dist
"""
from __future__ import annotations

import argparse
import hashlib
import os
import re
import shutil
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PLUGIN_SOURCE = ROOT / "src" / "HeartlessLullabyRU" / "Plugin.cs"
PLUGIN_DLL = ROOT / "build" / "out" / "HeartlessLullabyRU.dll"
README = ROOT / "docs" / "README_RU.txt"
TRANSLATION_LICENSE = ROOT / "LICENSE.txt"
FONT_LICENSES = ROOT / "assets" / "fonts_licenses"


def read_version() -> str:
    text = PLUGIN_SOURCE.read_text(encoding="utf-8")
    match = re.search(r'\[BepInPlugin\(Guid,\s*"[^"]+",\s*"([^"]+)"\)\]', text)
    if not match:
        print("Could not read the version from Plugin.cs")
        sys.exit(2)
    return match.group(1)


def require(path: Path, what: str) -> None:
    if not path.exists():
        print(f"Missing {what}: {path}")
        sys.exit(2)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=str(ROOT / "dist"), help="Output folder for the release.")
    parser.add_argument(
        "--bepinex",
        default=os.environ.get("HL_BEPINEX_DIST", str(ROOT / "tools" / "deps" / "bepinex")),
        help="Extracted BepInEx 5.4.23.3 x64 folder.",
    )
    parser.add_argument("--no-zip", action="store_true", help="Only create the folder, not the zip.")
    args = parser.parse_args()

    version = read_version()
    output_root = Path(args.out)
    release_dir = output_root / "HeartlessLullabyRU"
    zip_path = output_root / f"HeartlessLullabyRU_v{version}.zip"
    bepinex = Path(args.bepinex)

    require(PLUGIN_DLL, "built plugin (run tools/build_plugin.py first)")
    require(bepinex / "winhttp.dll", "BepInEx loader (run tools/fetch_deps.py)")
    require(bepinex / "BepInEx" / "core" / "BepInEx.dll", "BepInEx core (run tools/fetch_deps.py)")
    require(README, "docs/README_RU.txt")
    require(TRANSLATION_LICENSE, "LICENSE.txt")
    require(FONT_LICENSES, "font licenses")

    if release_dir.exists():
        shutil.rmtree(release_dir)
    release_dir.mkdir(parents=True)

    for name in (".doorstop_version", "doorstop_config.ini", "winhttp.dll"):
        source = bepinex / name
        require(source, "BepInEx file")
        shutil.copy2(source, release_dir / name)

    shutil.copytree(bepinex / "BepInEx" / "core", release_dir / "BepInEx" / "core")
    (release_dir / "BepInEx" / "plugins").mkdir(parents=True)
    shutil.copy2(PLUGIN_DLL, release_dir / "BepInEx" / "plugins" / "HeartlessLullabyRU.dll")

    shutil.copy2(README, release_dir / "README_RU.txt")
    shutil.copy2(TRANSLATION_LICENSE, release_dir / "LICENSE.txt")
    shutil.copytree(FONT_LICENSES, release_dir / "fonts_licenses")

    if not args.no_zip:
        if zip_path.exists():
            zip_path.unlink()
        with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as archive:
            for path in sorted(release_dir.rglob("*")):
                if path.is_file():
                    archive.write(path, path.relative_to(release_dir))
        data = zip_path.read_bytes()
        print("Archive:", zip_path)
        print("Entries:", len(zipfile.ZipFile(zip_path).namelist()))
        print("Size:   ", len(data), "bytes")
        print("SHA-256:", hashlib.sha256(data).hexdigest())

    print("Folder: ", release_dir)


if __name__ == "__main__":
    main()
