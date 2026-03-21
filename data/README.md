# Encoding Data Files

This directory contains the **source-of-truth** encoding data for all built-in tokenizers.

## Provenance

The `.tiktoken` text files are the original encoding data from **OpenAI's [tiktoken](https://github.com/openai/tiktoken)** library. Each file defines a byte-pair encoding (BPE) vocabulary — a mapping from token byte sequences to integer ranks.

| File | Encoding | Vocab size | Used by |
|------|----------|-----------|---------|
| `o200k_base.tiktoken` | o200k_base | 199,998 | GPT-4o, GPT-4o-mini |
| `cl100k_base.tiktoken` | cl100k_base | 100,256 | GPT-3.5-turbo, GPT-4, text-embedding-ada-002 |
| `p50k_base.tiktoken` | p50k_base | 50,280 | Codex models, text-davinci-002/003 |
| `r50k_base.tiktoken` | r50k_base | 50,256 | GPT-3 (davinci, curie, babbage, ada) |

### Text format (`.tiktoken`)

Each line contains a base64-encoded token and its integer rank, separated by a space:

```
IQ== 0         # bytes [0x21] = "!", rank 0
Ig== 1         # bytes [0x22] = '"', rank 1
Iw== 2         # bytes [0x23] = "#", rank 2
...
```

## Binary format (`.ttkb`)

The `.ttkb` files in the encoding project directories (`src/libs/Tiktoken.Encodings.*/`) are the binary equivalents, embedded as assembly resources at build time. They are **mechanically derived** from the `.tiktoken` text files — no hand-editing, no data changes.

### Why binary?

| | `.tiktoken` (text) | `.ttkb` (binary) |
|---|---|---|
| **Construction** | ~100ms | **<1ms** (pre-computed hash table) |
| **Loading** | Line-by-line + base64 decode | `MemoryMarshal.Cast` bulk copy |
| **Allocation** | String + byte[] per line | 5 bulk array copies only |

### Binary format specification

```
Header (28 bytes):
  [0..4]    "TTKB" magic (0x54 0x54 0x4B 0x42)
  [4..8]    version = 1 (uint32 LE)
  [8..12]   entryCount (uint32 LE)
  [12..16]  tableSize (uint32 LE, power of 2)
  [16..20]  mask (uint32 LE, tableSize - 1)
  [20..24]  keyBlobSize (uint32 LE)
  [24..28]  flags (uint32 LE, reserved = 0)

Sections (contiguous, bulk-copyable):
  Buckets:    int32[tableSize]     — pre-computed FNV-1a hash table (-1 = empty)
  Ranks:      int32[entryCount]    — token ranks
  KeyOffsets: int32[entryCount]    — byte offset of each key in KeyBlob (uint32)
  KeyLengths: uint8[entryCount]    — byte length of each key
  KeyBlob:    byte[keyBlobSize]    — concatenated raw token bytes
```

The hash table uses FNV-1a (32-bit) hashing with triangular number probing. The Python converter (`convert_to_ttkb.py`) is bit-exact with the C# `TokenEncoder` implementation.

## How to regenerate `.ttkb` files

```bash
# From this directory — converts all .tiktoken files, copies to encoding dirs, verifies:
make

# Or step by step:
make convert   # .tiktoken -> .ttkb
make copy      # Copy to src/libs/Tiktoken.Encodings.*/
make verify    # Verify entries + hash table correctness
```

## How to verify

```bash
# Verify all .ttkb files in this directory against .tiktoken sources:
python verify_ttkb.py
```

This confirms every rank and token byte sequence matches, and that the pre-computed hash table is correct.
