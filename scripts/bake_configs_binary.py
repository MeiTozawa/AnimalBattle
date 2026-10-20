#!/usr/bin/env python3
"""
bake_configs_binary.py

Config/*.toml および Resource/Platform/sheet.toml をスキャン・構文解析し、
Release ビルド用の単一バイナリ設定ファイル (Resource/baked_configs.bin) を生成します。

フォーマット:
  - Header:
      Magic (4 bytes): "ABCF" (AnimalBattle Config Format)
      Version (uint32, LE): 1
      UncompressedSize (uint32, LE)
      CompressedSize (uint32, LE)
  - Payload:
      ZLib 圧縮された UTF-8 JSON ペイロード (Level 9)

使用方法:
    python scripts/bake_configs_binary.py
"""

import json
import struct
import sys
import zlib
from pathlib import Path

# Python 3.11+ では tomllib が標準搭載
if sys.version_info >= (3, 11):
    import tomllib
else:
    try:
        import tomli as tomllib  # type: ignore[import]
    except ImportError:
        print("[ConfigBaker エラー] Python 3.11 以上、または 'tomli' パッケージが必要です。")
        sys.exit(1)

_SCRIPT_DIR = Path(__file__).resolve().parent
_PROJECT_ROOT = _SCRIPT_DIR.parent
_CONFIG_DIR = _PROJECT_ROOT / "Config"
_RESOURCE_DIR = _PROJECT_ROOT / "Resource"
_OUTPUT_BIN = _RESOURCE_DIR / "baked_configs.bin"

_CONFIG_MAP = {
    "Cat": _CONFIG_DIR / "cat.toml",
    "Bunny": _CONFIG_DIR / "bunny.toml",
    "Player": _CONFIG_DIR / "cat.toml",
    "Boss1": _CONFIG_DIR / "boss1.toml",
    "Boss2": _CONFIG_DIR / "boss2.toml",
    "Boss1Pattern": _CONFIG_DIR / "boss1_patterns.toml",
    "Boss2Pattern": _CONFIG_DIR / "boss2_patterns.toml",
    "SpecialBullet": _CONFIG_DIR / "special_bullets.toml",
    "Arena": _CONFIG_DIR / "arena.toml",
    "Render": _CONFIG_DIR / "render.toml",
    "Camera": _CONFIG_DIR / "camera.toml",
    "TileSet": _RESOURCE_DIR / "Platform" / "sheet.toml",
    "Audio": _CONFIG_DIR / "audio.toml",
    "CombatPrefab": _CONFIG_DIR / "combat.prefab.toml",
    "CatPrefab": _CONFIG_DIR / "cat.prefab.toml",
    "BunnyPrefab": _CONFIG_DIR / "bunny.prefab.toml",
    "Boss1Prefab": _CONFIG_DIR / "boss1.prefab.toml",
    "Boss2Prefab": _CONFIG_DIR / "boss2.prefab.toml",
    "StartScene": _CONFIG_DIR / "start_scene.toml",
}

def load_toml(path: Path) -> dict:
    if not path.exists():
        print(f"[ConfigBaker エラー] 設定ファイルが見つかりません: {path}")
        sys.exit(1)
    with open(path, "rb") as f:
        return tomllib.load(f)


def main() -> None:
    bundle = {}
    for key, path in _CONFIG_MAP.items():
        bundle[key] = load_toml(path)

    json_str = json.dumps(bundle, ensure_ascii=False, separators=(",", ":"))
    raw_bytes = json_str.encode("utf-8")
    compressed_payload = zlib.compress(raw_bytes, level=9)

    magic = b"ABCF"
    version = 1
    uncompressed_len = len(raw_bytes)
    compressed_len = len(compressed_payload)

    header = struct.pack("<4sIII", magic, version, uncompressed_len, compressed_len)

    _OUTPUT_BIN.parent.mkdir(parents=True, exist_ok=True)
    with open(_OUTPUT_BIN, "wb") as f:
        f.write(header)
        f.write(compressed_payload)

    print(
        f"[ConfigBaker] {len(bundle)} 件の設定ファイルをバイナリに正常にベイクしました: "
        f"{_OUTPUT_BIN} (非圧縮: {uncompressed_len} bytes -> 圧縮後: {len(header) + compressed_len} bytes)"
    )


if __name__ == "__main__":
    main()
