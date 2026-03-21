#!/usr/bin/env python3
"""
Verify that .ttkb binary files match their .tiktoken source files.
Validates entries and hash table correctness.

Usage:
    python verify_ttkb.py                         # Verify all pairs in this directory
    python verify_ttkb.py cl100k_base.ttkb        # Verify a specific file

Exit code 0 = all files verified, 1 = mismatch found.
"""

import base64
import struct
import sys
from pathlib import Path

# FNV-1a constants (must match convert_to_ttkb.py and C# TokenEncoder)
FNV_OFFSET_BASIS = 2166136261
FNV_PRIME = 16777619
MASK_32 = 0xFFFFFFFF


def fnv_hash(key: bytes) -> int:
    h = FNV_OFFSET_BASIS
    for b in key:
        h = ((h ^ b) * FNV_PRIME) & MASK_32
    return h


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

    # Load binary
    with open(ttkb_path, "rb") as f:
        data = f.read()

    if len(data) < 28:
        print(f"  FAIL {ttkb_path.name}: file too small ({len(data)} bytes)")
        return False

    magic = data[:4]
    if magic != b"TTKB":
        print(f"  FAIL {ttkb_path.name}: bad magic {magic!r}")
        return False

    (version,) = struct.unpack_from("<I", data, 4)
    if version != 1:
        print(f"  FAIL {ttkb_path.name}: unsupported version {version}")
        return False

    count, table_size, mask, key_blob_size, flags = struct.unpack_from("<5I", data, 8)

    if mask != table_size - 1:
        print(f"  FAIL {ttkb_path.name}: mask mismatch ({mask} != {table_size - 1})")
        return False

    # Parse sections
    off = 28
    buckets = list(struct.unpack_from(f"<{table_size}i", data, off))
    off += table_size * 4
    ranks = list(struct.unpack_from(f"<{count}i", data, off))
    off += count * 4
    key_offsets = list(struct.unpack_from(f"<{count}I", data, off))
    off += count * 4
    key_lengths = list(struct.unpack_from(f"{count}B", data, off))
    off += count
    key_blob = data[off : off + key_blob_size]
    off += key_blob_size

    if off != len(data):
        print(f"  FAIL {ttkb_path.name}: {len(data) - off} trailing bytes")
        return False

    # Reconstruct entries and compare
    binary_entries = {}
    for i in range(count):
        token_bytes = key_blob[key_offsets[i] : key_offsets[i] + key_lengths[i]]
        binary_entries[ranks[i]] = token_bytes

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

    # Verify hash table correctness
    for i in range(count):
        token_bytes = key_blob[key_offsets[i] : key_offsets[i] + key_lengths[i]]
        bucket = fnv_hash(token_bytes) & mask
        step = 1
        found = False
        while True:
            idx = buckets[bucket]
            if idx == -1:
                break
            if idx == i:
                found = True
                break
            bucket = (bucket + step) & mask
            step += 1
        if not found:
            print(
                f"  FAIL {ttkb_path.name}: entry {i} (rank {ranks[i]})"
                f" not found via hash table lookup"
            )
            return False

    print(
        f"  OK   {ttkb_path.name} ({count} entries, table={table_size},"
        f" hash table verified)"
    )
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
