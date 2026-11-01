#!/usr/bin/env python3
"""Download Roslyn and BepInEx into tools/deps.

Both archives are third-party software and are not part of this repository:
  Roslyn (Microsoft.Net.Compilers.Toolset 4.11.0) - MIT
  BepInEx 5.4.23.3 x64                           - LGPL-2.1

Usage:
    python tools/fetch_deps.py
    python tools/fetch_deps.py --force
"""
from __future__ import annotations

import argparse
import os
import shutil
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEPS = ROOT / "tools" / "deps"

ROSLYN_URL = "https://api.nuget.org/v3-flatcontainer/microsoft.net.compilers.toolset/4.11.0/microsoft.net.compilers.toolset.4.11.0.nupkg"
BEPINEX_URL = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip"


def download(url: str, destination: Path) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_suffix(destination.suffix + ".part")
    print("Downloading", url)
    request = urllib.request.Request(url, headers={"User-Agent": "HeartlessLullabyRU-build"})
    with urllib.request.urlopen(request, timeout=180) as response, temporary.open("wb") as output:
        shutil.copyfileobj(response, output)
    os.replace(temporary, destination)
    print("  saved", destination, destination.stat().st_size, "bytes")


def extract(archive: Path, destination: Path) -> None:
    destination.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive) as zipped:
        zipped.extractall(destination)
    print("  extracted to", destination)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--force", action="store_true", help="Download and extract again.")
    args = parser.parse_args()

    roslyn_archive = DEPS / "roslyn.nupkg"
    roslyn_dir = DEPS / "roslyn"
    bepinex_archive = DEPS / "bepinex.zip"
    bepinex_dir = DEPS / "bepinex"

    try:
        if args.force or not (roslyn_dir / "tasks" / "net472" / "csc.exe").is_file():
            if args.force and roslyn_archive.exists():
                roslyn_archive.unlink()
            if not roslyn_archive.is_file():
                download(ROSLYN_URL, roslyn_archive)
            extract(roslyn_archive, roslyn_dir)
        else:
            print("Roslyn already present:", roslyn_dir)

        if args.force or not (bepinex_dir / "BepInEx" / "core" / "BepInEx.dll").is_file():
            if args.force and bepinex_archive.exists():
                bepinex_archive.unlink()
            if not bepinex_archive.is_file():
                download(BEPINEX_URL, bepinex_archive)
            extract(bepinex_archive, bepinex_dir)
        else:
            print("BepInEx already present:", bepinex_dir)
    except Exception as error:
        print("Download failed:", error)
        print("Check the network connection or install the dependencies manually:")
        print("  Roslyn:", ROSLYN_URL)
        print("  BepInEx:", BEPINEX_URL)
        sys.exit(1)

    if not (roslyn_dir / "tasks" / "net472" / "csc.exe").is_file():
        print("Roslyn extraction is incomplete: csc.exe was not found.")
        sys.exit(1)
    if not (bepinex_dir / "BepInEx" / "core" / "BepInEx.dll").is_file():
        print("BepInEx extraction is incomplete: BepInEx.dll was not found.")
        sys.exit(1)

    print("Dependencies are ready.")
    print("Next: python tools/build_plugin.py --game \"C:\\path\\to\\game\"")


if __name__ == "__main__":
    main()
