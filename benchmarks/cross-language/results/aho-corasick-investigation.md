# Aho-Corasick Tokenizer Investigation

## Background

GitHub's [`bpe-openai`](https://github.com/github/rust-gems) Rust crate uses an Aho-Corasick automaton for BPE tokenization, achieving O(n) worst-case complexity. This document investigates whether a similar approach could benefit the .NET Tiktoken implementation, particularly for cold-path (no cache) performance on multilingual/CJK text.

## Current Architecture: Regex Split + BPE Merge

```
Input text
  → Regex pre-tokenization (split by Unicode properties)
  → For each piece: BPE merge loop (find min rank, merge, repeat)
  → Token IDs
```

- **Regex split** uses patterns like `\p{L}+`, `\p{N}+`, `[^\s\p{L}\p{N}]+` to split text into "pieces"
- **BPE merge** iteratively finds the lowest-rank adjacent pair and merges, until no more merges exist
- **Complexity**: O(n log n) per piece with the new min-heap optimization (was O(n²)), where n = piece byte length
- **Memory**: ~8-12 MB per encoding (hash table for 200K vocab)

## bpe-openai Architecture: Aho-Corasick Automaton

```
Input text (UTF-8 bytes)
  → Aho-Corasick automaton finds all vocabulary matches at each position
  → Suffix matching + compatibility checking selects non-overlapping tokens
  → Token IDs
```

- **No regex pre-tokenization** — the automaton directly matches vocabulary entries
- **Aho-Corasick automaton** built from the full 200K vocabulary (all byte sequences)
- **O(n) worst case** — single pass through the input, no merge loop
- **Memory**: ~20-50 MB per encoding (automaton state tables are large)

### Key Differences

| Aspect | Regex + BPE (Tiktoken) | Aho-Corasick (bpe-openai) |
|--------|----------------------|---------------------------|
| Pre-tokenization | Regex (Unicode-aware) | None (built into automaton) |
| Algorithm | Iterative merge | Single-pass matching |
| Complexity | O(n log n) per piece | O(n) total |
| Memory | ~8-12 MB | ~20-50 MB |
| Construction | Fast (~0.5-0.8 ms) | Slow (several seconds) |
| Best for | Short/cached text | Long/cold text |
| Worst case | Long multilingual pieces | High memory pressure |

## Performance Comparison (Measured)

From cross-language benchmarks on Apple M4 Max, o200k_base:

| Input | .NET Tiktoken (no cache) | .NET Tiktoken (cached) | Rust bpe-openai |
|-------|-------------------------|----------------------|-----------------|
| hello_world (13 B) | 108 ns / 115 MiB/s | 105 ns / 119 MiB/s | 426 ns / 29 MiB/s |
| multilingual (382 B) | 7.6 us / 48 MiB/s | 1.2 us / 308 MiB/s | 5.7 us / 64 MiB/s |
| cjk_heavy (1,676 B) | 58 us / 28 MiB/s | 2.8 us / 577 MiB/s | 24 us / 66 MiB/s |
| code (879 B) | 6.8 us / 123 MiB/s | 6.6 us / 127 MiB/s | 20.7 us / 41 MiB/s |
| multilingual_long (4,313 B) | 145 us / 28 MiB/s | 9.7 us / 423 MiB/s | 77 us / 53 MiB/s |
| bitcoin (19,884 B) | 189 us / 100 MiB/s | 133 us / 143 MiB/s | 397 us / 48 MiB/s |

**Key observation**: bpe-openai's O(n) algorithm beats .NET Tiktoken's cold path on CJK-heavy and multilingual-long inputs (66 vs 28 MiB/s), but .NET wins decisively on ASCII-heavy text and with cache warm.

## Available .NET Libraries

| Library | Notes |
|---------|-------|
| `NReco.Text.AhoCorasickDoubleArrayTrie` | Best option — compact double-array trie, good .NET perf |
| `Sdcb.DashScope.AhoCorasickDoubleArrayTrie` | Fork of NReco with minor changes |
| Hand-rolled | Custom implementation could be tuned for byte-level matching |

## Feasibility Assessment

### Challenges

1. **Major architectural change**: Cannot be a drop-in optimization. Would require an entirely separate tokenizer path, bypassing the regex pre-tokenizer.

2. **Correctness**: BPE merge ordering is critical for correct tokenization. The Aho-Corasick approach must produce identical token sequences to the standard BPE algorithm. bpe-openai achieves this via suffix-based compatibility checking, which is non-trivial to implement.

3. **Memory cost**: 3-5x more memory per encoding. For server scenarios with multiple encodings loaded, this adds up.

4. **Construction time**: Building the Aho-Corasick automaton from 200K vocabulary entries takes several seconds (vs <1 ms for current approach). Would need lazy initialization.

5. **Diminishing returns**: The min-heap optimization (Task 1) already improves cold-path from O(n²) to O(n log n). The token cache already provides 6-21x speedup for repeated patterns. The remaining gap to O(n) may not justify the complexity.

### Where It Would Help

- First-call performance on long multilingual/CJK text (the one scenario where cold-path .NET lags Rust bpe-openai)
- Server scenarios processing diverse, non-repeating text where the cache provides less benefit

### Where It Would Not Help

- Short text (current approach is already faster)
- ASCII-dominated text (regex + BPE is very fast on ASCII)
- Repeated text patterns (cache already handles this)

## Recommendation

**Not recommended for immediate implementation.** The min-heap optimization (O(n²) → O(n log n)) combined with the existing token cache addresses the most impactful performance gaps. The Aho-Corasick approach would be a major architectural investment with unclear net benefit given the memory trade-off.

**Potential next step**: If cold-path multilingual performance remains a priority after the min-heap optimization is measured, build a proof-of-concept Aho-Corasick tokenizer in a separate benchmark project to measure actual throughput and memory impact on .NET before committing to a full implementation.
