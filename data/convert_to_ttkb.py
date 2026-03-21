#!/usr/bin/env python3
"""
Convert OpenAI .tiktoken text files to .ttkb binary format (v2).

Usage:
    python convert_to_ttkb.py                       # Convert all .tiktoken files
    python convert_to_ttkb.py cl100k_base.tiktoken   # Convert a specific file

The .tiktoken text format (from OpenAI):
    Each line: <base64-encoded token bytes> <rank as decimal integer>
    Example:   IQ==  0       (bytes [0x21], rank 0)

TTKB v2 binary format:
    Header (28 bytes):
        [0..4]    "TTKB" magic
        [4..8]    version = 2 (uint32 LE)
        [8..12]   entryCount (uint32 LE)
        [12..16]  tableSize (uint32 LE, power of 2)
        [16..20]  mask (uint32 LE, tableSize - 1)
        [20..24]  keyBlobSize (uint32 LE)
        [24..28]  flags (uint32 LE, reserved = 0)

    Sections (contiguous, bulk-copyable):
        Buckets:    int32[tableSize]     — pre-computed FNV-1a hash table (-1 = empty)
        Ranks:      int32[entryCount]    — token ranks
        KeyOffsets: int32[entryCount]    — byte offset of each key in KeyBlob
        KeyLengths: uint8[entryCount]    — byte length of each key
        KeyBlob:    byte[keyBlobSize]    — concatenated raw token bytes

    The hash table uses FNV-1a hashing with triangular number probing,
    bit-exact with the C# TokenEncoder implementation.

To verify: python verify_ttkb.py
"""

import base64
import struct
import sys
from pathlib import Path


# FNV-1a constants (32-bit) — must match C# TokenEncoder.FnvHash exactly
FNV_OFFSET_BASIS = 2166136261
FNV_PRIME = 16777619
MASK_32 = 0xFFFFFFFF


def fnv_hash(key: bytes) -> int:
    """Compute 32-bit FNV-1a hash. Bit-exact with C# TokenEncoder.FnvHash."""
    h = FNV_OFFSET_BASIS
    for b in key:
        h = ((h ^ b) * FNV_PRIME) & MASK_32
    return h


def round_up_power_of_2(value: int) -> int:
    """Smallest power of 2 >= value. Matches C# TokenEncoder.RoundUpPowerOf2."""
    if value <= 1:
        return 1
    return 1 << (value - 1).bit_length()


def convert(tiktoken_path: Path) -> Path:
    """Convert a .tiktoken file to .ttkb v2 binary format."""
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

    count = len(entries)

    # Build flat memory layout: key blob + offsets + lengths + ranks
    key_blob = bytearray()
    offsets = []
    lengths = []
    ranks = []
    for rank, token_bytes in entries:
        offsets.append(len(key_blob))
        lengths.append(len(token_bytes))
        ranks.append(rank)
        key_blob.extend(token_bytes)

    key_blob_size = len(key_blob)

    # Build hash table (FNV-1a + triangular probing) — must match C# TokenEncoder.From
    target = count * 3 // 2
    table_size = round_up_power_of_2(target)
    if table_size < 16:
        table_size = 16
    mask = table_size - 1
    buckets = [-1] * table_size

    for i in range(count):
        offset = offsets[i]
        length = lengths[i]
        token_key = bytes(key_blob[offset : offset + length])
        bucket = fnv_hash(token_key) & mask
        step = 1
        while buckets[bucket] != -1:
            bucket = (bucket + step) & mask
            step += 1
        buckets[bucket] = i

    # Write v2 binary format
    output_path = tiktoken_path.with_suffix(".ttkb")
    with open(output_path, "wb") as f:
        # Header (28 bytes)
        f.write(b"TTKB")                                  # magic
        f.write(struct.pack("<I", 1))                      # version
        f.write(struct.pack("<I", count))                  # entryCount
        f.write(struct.pack("<I", table_size))             # tableSize
        f.write(struct.pack("<I", mask))                   # mask
        f.write(struct.pack("<I", key_blob_size))          # keyBlobSize
        f.write(struct.pack("<I", 0))                      # flags (reserved)

        # Sections (contiguous arrays)
        f.write(struct.pack(f"<{table_size}i", *buckets))  # Buckets
        f.write(struct.pack(f"<{count}i", *ranks))         # Ranks
        f.write(struct.pack(f"<{count}I", *offsets))       # KeyOffsets (uint32)
        f.write(struct.pack(f"{count}B", *lengths))        # KeyLengths
        f.write(bytes(key_blob))                           # KeyBlob

    v1_size_est = 12 + count * 5 + key_blob_size
    v2_size = output_path.stat().st_size
    print(
        f"  {tiktoken_path.name} -> {output_path.name}"
        f"  ({count} entries, table={table_size},"
        f" v1≈{v1_size_est:,} -> v2={v2_size:,} bytes,"
        f" +{(v2_size - v1_size_est) / v1_size_est:.0%})"
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

    print(f"Converting {len(files)} file(s) to TTKB v2:")
    for f in files:
        convert(f)
    print("Done.")


if __name__ == "__main__":
    main()
