# Cold-Path Performance Profiling

## Context

After the min-heap optimization (O(n²) → O(n log n) for large pieces), the cold-path penalty
for multilingual/CJK text is ~4-9x vs cached. This document profiles where the remaining
time is spent.

## Call Path: Cold-Path CountTokens

```
Encoder.CountTokens(text)
  → CoreBpe.CountTokensNative(text)
    → Regex.EnumerateMatches(text)          [15-20% of time]
    → For EACH match:
      ├─ Encoder.ContainsKeyUtf16(key)      [10-15%] — single-token fast check
      ├─ Cache lookup (SKIPPED on cold)     [0%]
      ├─ GetUtf8Span(key, scratch)          [5-10%] — UTF-16→UTF-8 conversion
      └─ BytePairEncoding.FindParts(...)    [55-70%] — BPE merge algorithm
```

## Time Distribution for CJK Text (~644 bytes)

| Stage | % Time | Description |
|-------|--------|-------------|
| **BPE merge (FindParts)** | **55-70%** | Hash lookups + merge loop per piece |
| Regex pre-tokenization | 15-20% | Unicode property matching (`\p{L}`, `\p{N}`) |
| Hash table lookups | 10-15% | FNV-1a with 3x cost per CJK character |
| UTF-8 conversion | 5-10% | GetUtf8Span with heap fallback for long pieces |

## Key Bottleneck: BPE Merge on Small CJK Pieces

The min-heap optimization (HeapThreshold=32) only activates for pieces with ≥32 byte
boundaries (~31 bytes). Most CJK pieces from regex pre-tokenization are **smaller than this**:

- Single CJK character: 3 bytes = 4 boundaries → linear scan
- 5 CJK characters: 15 bytes = 16 boundaries → linear scan
- 10 CJK characters: 30 bytes = 31 boundaries → linear scan
- 11+ CJK characters: 33+ bytes = 34+ boundaries → **heap kicks in**

The o200k_base regex pattern matches sequences of CJK characters as single pieces
(`[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+`), so piece sizes
depend on the text structure. Short phrases stay in O(n²) linear path.

## Why Lowering HeapThreshold Doesn't Help

Benchmark results (HeapThreshold tuning):
- T=16: **worse** (heap overhead exceeds O(n²) savings for n<32)
- T=32: baseline
- T=48: similar (±3-6%)

The heap requires 5 stackalloc arrays + O(n) initialization + O(n log n) merge.
For small n (<32), the constant factor of the heap exceeds the O(n²) cost of linear scan.

## Hash Table Miss Rate for CJK

The BPE merge algorithm calls `encoder.TryGetValue(span)` for every adjacent pair.
For CJK text, most lookups **miss** because:

1. The vocabulary has ~200K tokens but very few arbitrary 2-byte sub-character fragments
2. CJK UTF-8 bytes (0xE0-0xEF range) rarely form valid token starts in 2-byte combos
3. Each miss still computes full FNV-1a hash (3 multiplies per CJK byte in `TryGetValueUtf16`)

**TokenEncoder.cs hot path for CJK (lines 220-227):**
```csharp
// 3-byte UTF-8 BMP path (CJK characters)
hash ^= 0xE0u | (c >> 12); hash *= 16777619u;
hash ^= 0x80u | ((c >> 6) & 0x3Fu); hash *= 16777619u;
hash ^= 0x80u | (c & 0x3Fu); hash *= 16777619u;
```

## Potential Future Optimizations

### 1. Early-reject bloom filter for BPE lookups
Add a small bloom filter (~32 KB) to quickly reject byte sequences not in the vocabulary.
Would eliminate most hash table misses for CJK, reducing BPE merge cost by ~30-40%.

### 2. Piece-length-aware BPE skip
If the BPE vocabulary has no tokens of length N, skip the merge step entirely for pieces
of that length. Requires pre-computing min/max token lengths per byte prefix.

### 3. Larger stackalloc buffer for UTF-8 conversion
Current: 512 bytes (handles ~170 CJK chars). Increasing to 1024 would eliminate most
heap allocation fallbacks for CJK text. Minimal stack impact.

### 4. Regex pattern optimization for CJK
The current patterns are Latin-script-optimized. A CJK-specific fast path could
skip Unicode property checks for known CJK Unicode ranges (U+4E00-U+9FFF).

### 5. Warm cache preloading
For known CJK-heavy workloads, pre-populate the FastCache with common CJK character
tokens on encoder construction.

## Conclusion

The remaining 4-9x cold-path penalty is **inherent to the BPE algorithm** for non-ASCII text:
- Regex must process Unicode properties (no shortcut for CJK)
- BPE must try all pair merges, and CJK has high miss rate
- Hash computation is 3x more expensive for CJK vs ASCII

The token cache (6-21x speedup) is the correct architectural solution. The min-heap
optimization reduced worst-case cold-path from ~21x to ~9x. Further improvements would
require algorithmic changes (bloom filter, Aho-Corasick) rather than constant-factor tuning.
