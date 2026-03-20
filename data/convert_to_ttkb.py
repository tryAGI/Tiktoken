#!/usr/bin/env python3
"""
Convert OpenAI .tiktoken text files to .ttkb binary format.

Usage:
    python convert_to_ttkb.py                    # Convert all .tiktoken files in this directory
    python convert_to_ttkb.py cl100k_base.tiktoken  # Convert a specific file

The .tiktoken text format (from OpenAI):
    Each line: <base64-encoded token bytes> <rank as decimal integer>
    Example:   IQ==  0       (bytes [0x21], rank 0)

The .ttkb binary format (Tiktoken .NET):
    Header:
        4 bytes     magic   "TTKB" (ASCII)
        4 bytes     version uint32 LE (currently 1)
        4 bytes     count   uint32 LE (number of entries)
    Entries (repeated `count` times):
        4 bytes     rank        int32 LE
        1 byte      token_len   uint8 (max 255)
        N bytes     token_bytes raw bytes (N = token_len)

Why binary?
    - 34% smaller than text (no base64 overhead, no newlines)
    - Faster to load (BinaryReader vs line-by-line base64 decoding)
    - Pre-known entry count enables exact dictionary pre-allocation

To verify round-trip correctness:
    python convert_to_ttkb.py
    python verify_ttkb.py      # (see verify script below)
"""

import base64
import struct
import sys
from pathlib import Path


def convert(tiktoken_path: Path) -> Path:
    """Convert a .tiktoken file to .ttkb binary format."""
    entries = []
    with open(tiktoken_path, "r") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            b64_token, rank_str = line.split()
            token_bytes = base64.b64decode(b64_token)
            rank = int(rank_str)
            if len(token_bytes) > 255:
                raise ValueError(
                    f"Token at rank {rank} is {len(token_bytes)} bytes (max 255)"
                )
            entries.append((rank, token_bytes))

    output_path = tiktoken_path.with_suffix(".ttkb")
    with open(output_path, "wb") as f:
        # Header
        f.write(b"TTKB")                                    # magic
        f.write(struct.pack("<I", 1))                        # version
        f.write(struct.pack("<I", len(entries)))              # count
        # Entries
        for rank, token_bytes in entries:
            f.write(struct.pack("<i", rank))                 # rank (int32 LE)
            f.write(struct.pack("B", len(token_bytes)))      # token length (uint8)
            f.write(token_bytes)                             # raw token bytes

    print(
        f"  {tiktoken_path.name} -> {output_path.name}"
        f"  ({len(entries)} entries,"
        f" {tiktoken_path.stat().st_size:,} -> {output_path.stat().st_size:,} bytes,"
        f" {output_path.stat().st_size / tiktoken_path.stat().st_size:.0%})"
    )
    return output_path


def main():
    data_dir = Path(__file__).parent

    if len(sys.argv) > 1:
        files = [Path(arg) for arg in sys.argv[1:]]
    else:
        files = sorted(data_dir.glob("*.tiktoken"))

    if not files:
        print("No .tiktoken files found.")
        sys.exit(1)

    print(f"Converting {len(files)} file(s):")
    for f in files:
        convert(f)
    print("Done.")


if __name__ == "__main__":
    main()
