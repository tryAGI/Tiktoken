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
| **Size** | 3.4 MB (o200k) | 2.3 MB (o200k) — **34% smaller** |
| **Loading** | Line-by-line + base64 decode | BinaryReader sequential read |
| **Allocation** | String + byte[] per line | byte[] per entry only |
| **Pre-allocation** | Estimated from stream length | Exact count in header |

### Binary format specification (version 1)

```
Offset  Size    Type        Description
──────  ──────  ──────────  ───────────────────────────
0       4       byte[4]     Magic: "TTKB" (0x54 0x54 0x4B 0x42)
4       4       uint32 LE   Version (1)
8       4       uint32 LE   Entry count (N)
12      ...     entries     N entries, each:
                              4 bytes   int32 LE    rank
                              1 byte    uint8       token byte length (max 255)
                              L bytes   byte[L]     raw token bytes
```

## How to regenerate `.ttkb` files

```bash
# From this directory:
python convert_to_ttkb.py

# Copy to encoding project directories:
cp o200k_base.ttkb ../src/libs/Tiktoken.Encodings.o200k/
cp cl100k_base.ttkb ../src/libs/Tiktoken.Encodings.cl100k/
cp r50k_base.ttkb ../src/libs/Tiktoken.Encodings.r50k/
cp p50k_base.ttkb ../src/libs/Tiktoken.Encodings.p50k/
```

## How to verify

```bash
# Copy .ttkb files here for verification:
cp ../src/libs/Tiktoken.Encodings.o200k/o200k_base.ttkb .
cp ../src/libs/Tiktoken.Encodings.cl100k/cl100k_base.ttkb .
cp ../src/libs/Tiktoken.Encodings.r50k/r50k_base.ttkb .
cp ../src/libs/Tiktoken.Encodings.p50k/p50k_base.ttkb .

# Verify binary files match text sources:
python verify_ttkb.py
```

This confirms every rank and token byte sequence in the binary file matches the original text file exactly.
