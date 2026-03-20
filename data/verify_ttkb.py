#!/usr/bin/env python3
"""
Verify that .ttkb binary files are byte-for-byte equivalent to their .tiktoken source files.

Usage:
    python verify_ttkb.py                         # Verify all pairs in this directory
    python verify_ttkb.py cl100k_base.ttkb        # Verify a specific file

Exit code 0 = all files verified, 1 = mismatch found.
"""

import base64
import struct
import sys
from pathlib import Path


def verify(ttkb_path: Path) -> bool:
    """Verify a .ttkb file against its .tiktoken source. Returns True if matched."""
    tiktoken_path = ttkb_path.with_suffix(".tiktoken")
    if not tiktoken_path.exists():
        print(f"  SKIP {ttkb_path.name}: no matching {tiktoken_path.name}")
        return True

    # Load text entries
    text_entries = {}
    with open(tiktoken_path, "r") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            b64_token, rank_str = line.split()
            token_bytes = base64.b64decode(b64_token)
            rank = int(rank_str)
            text_entries[rank] = token_bytes

    # Load binary entries
    binary_entries = {}
    with open(ttkb_path, "rb") as f:
        magic = f.read(4)
        if magic != b"TTKB":
            print(f"  FAIL {ttkb_path.name}: bad magic {magic!r}")
            return False
        (version,) = struct.unpack("<I", f.read(4))
        if version != 1:
            print(f"  FAIL {ttkb_path.name}: unsupported version {version}")
            return False
        (count,) = struct.unpack("<I", f.read(4))
        for _ in range(count):
            (rank,) = struct.unpack("<i", f.read(4))
            (token_len,) = struct.unpack("B", f.read(1))
            token_bytes = f.read(token_len)
            binary_entries[rank] = token_bytes
        # Ensure we consumed the entire file
        trailing = f.read()
        if trailing:
            print(f"  FAIL {ttkb_path.name}: {len(trailing)} trailing bytes")
            return False

    # Compare
    if len(text_entries) != len(binary_entries):
        print(
            f"  FAIL {ttkb_path.name}: count mismatch"
            f" (text={len(text_entries)}, binary={len(binary_entries)})"
        )
        return False

    for rank, text_bytes in text_entries.items():
        if rank not in binary_entries:
            print(f"  FAIL {ttkb_path.name}: rank {rank} missing in binary")
            return False
        if binary_entries[rank] != text_bytes:
            print(f"  FAIL {ttkb_path.name}: rank {rank} bytes differ")
            return False

    print(f"  OK   {ttkb_path.name} ({len(binary_entries)} entries match)")
    return True


def main():
    data_dir = Path(__file__).parent

    if len(sys.argv) > 1:
        files = [Path(arg) for arg in sys.argv[1:]]
    else:
        files = sorted(data_dir.glob("*.ttkb"))

    if not files:
        print("No .ttkb files found.")
        sys.exit(1)

    print(f"Verifying {len(files)} file(s):")
    all_ok = all(verify(f) for f in files)
    if all_ok:
        print("All files verified.")
    else:
        print("VERIFICATION FAILED.")
        sys.exit(1)


if __name__ == "__main__":
    main()
