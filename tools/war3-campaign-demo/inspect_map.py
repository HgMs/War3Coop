#!/usr/bin/env python3
"""Parse enough Warcraft III map data to validate the campaign MPQ demo."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import struct
from pathlib import Path


class BinaryCursor:
    def __init__(self, data: bytes) -> None:
        self.data = data
        self.offset = 0

    def read(self, size: int) -> bytes:
        end = self.offset + size
        if end > len(self.data):
            raise ValueError(
                f"unexpected end of war3map.w3i at offset {self.offset}"
            )
        value = self.data[self.offset:end]
        self.offset = end
        return value

    def int32(self) -> int:
        return struct.unpack("<i", self.read(4))[0]

    def float32(self) -> float:
        return struct.unpack("<f", self.read(4))[0]

    def byte(self) -> int:
        return self.read(1)[0]

    def c_string(self) -> str:
        end = self.data.find(b"\0", self.offset)
        if end < 0:
            raise ValueError(
                f"unterminated string in war3map.w3i at offset {self.offset}"
            )
        raw = self.data[self.offset:end]
        self.offset = end + 1
        return decode_text(raw)[0]


def decode_text(data: bytes) -> tuple[str, str]:
    for encoding in ("utf-8-sig", "gb18030", "cp1252"):
        try:
            return data.decode(encoding), encoding
        except UnicodeDecodeError:
            continue
    raise ValueError("text could not be decoded")


def parse_w3i(path: Path) -> dict[str, object]:
    cursor = BinaryCursor(path.read_bytes())
    result: dict[str, object] = {
        "format_version": cursor.int32(),
        "save_count": cursor.int32(),
        "editor_version": cursor.int32(),
        "name_raw": cursor.c_string(),
        "author_raw": cursor.c_string(),
        "description_raw": cursor.c_string(),
        "recommended_players_raw": cursor.c_string(),
        "camera_bounds": [cursor.float32() for _ in range(8)],
        "camera_complements": [cursor.int32() for _ in range(4)],
        "width": cursor.int32(),
        "height": cursor.int32(),
        "flags": cursor.int32(),
        "tileset": chr(cursor.byte()),
    }
    return result


def parse_wts(path: Path) -> tuple[dict[int, str], str]:
    text, encoding = decode_text(path.read_bytes())
    entries: dict[int, str] = {}
    pattern = re.compile(
        r"(?ms)^\s*STRING\s+(\d+)\s*\r?\n\{\r?\n(.*?)\r?\n\}"
    )
    for match in pattern.finditer(text):
        entries[int(match.group(1))] = match.group(2)
    return entries, encoding


def resolve_trigger_string(value: str, strings: dict[int, str]) -> str:
    match = re.fullmatch(r"TRIGSTR_(\d+)", value)
    if not match:
        return value
    return strings.get(int(match.group(1)), value)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def file_summary(path: Path) -> dict[str, object]:
    return {
        "size": path.stat().st_size,
        "sha256": sha256(path),
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--map", type=Path, required=True)
    parser.add_argument("--w3i", type=Path, required=True)
    parser.add_argument("--wts", type=Path, required=True)
    parser.add_argument("--jass", type=Path, required=True)
    parser.add_argument("--units", type=Path, required=True)
    parser.add_argument("--doodads", type=Path, required=True)
    parser.add_argument("--source-archive", required=True)
    parser.add_argument("--source-entry", required=True)
    parser.add_argument("--game-version", required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    info = parse_w3i(args.w3i)
    strings, wts_encoding = parse_wts(args.wts)
    for key in ("name", "author", "description", "recommended_players"):
        raw_key = f"{key}_raw"
        info[key] = resolve_trigger_string(str(info[raw_key]), strings)

    jass, jass_encoding = decode_text(args.jass.read_bytes())
    functions = re.findall(r"(?m)^\s*function\s+\w+\s+takes\b", jass)
    globals_found = sorted(set(re.findall(r"\budg_[A-Za-z0-9_]+\b", jass)))

    identity_text = "\n".join(strings.values())
    checks = {
        "is_night_elf_chapter_7": (
            "暗夜精灵07" in identity_text
            and ("诸神之黄昏" in identity_text or "诸神黄昏" in identity_text)
        ),
        "has_archimonde_objective": (
            "阿克蒙德" in identity_text and "拖延阿克蒙德45分钟" in identity_text
        ),
        "has_45_minute_timer": bool(
            re.search(r"StartTimerBJ\s*\([^\n]*2700\.00", jass)
        ),
        "has_archimonde_script_state": "udg_Archimonde" in jass,
        "has_expected_map_format": info["format_version"] == 18,
    }

    report = {
        "demo": "Warcraft III campaign map read",
        "success": all(checks.values()),
        "source": {
            "game_version": args.game_version,
            "archive": args.source_archive,
            "entry": args.source_entry,
        },
        "map_info": info,
        "content": {
            "trigger_string_count": len(strings),
            "wts_encoding": wts_encoding,
            "jass_encoding": jass_encoding,
            "jass_function_count": len(functions),
            "jass_global_count": len(globals_found),
            "key_strings": {
                str(index): strings[index]
                for index in (0, 2, 3, 40, 86, 274)
                if index in strings
            },
        },
        "checks": checks,
        "files": {
            "map": file_summary(args.map),
            "war3map.w3i": file_summary(args.w3i),
            "war3map.wts": file_summary(args.wts),
            "war3map.j": file_summary(args.jass),
            "war3mapUnits.doo": file_summary(args.units),
            "war3map.doo": file_summary(args.doodads),
        },
    }

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0 if report["success"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
