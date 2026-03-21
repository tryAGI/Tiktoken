```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M4 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Categories    | Mean            | Ratio | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|------------------------------- |-------------- |----------------:|------:|---------:|---------:|---------:|-----------:|------------:|
| AsciiLookup_Encode             | AsciiEncode   |       113.13 ns |  1.00 |   0.0086 |        - |        - |       72 B |        1.00 |
| NonAsciiLookup_Encode          | AsciiEncode   |       276.12 ns |  2.44 |   0.0086 |        - |        - |       72 B |        1.00 |
|                                |               |                 |       |          |          |          |            |             |
| AsciiLookup_CountTokens        | AsciiLookup   |        89.70 ns |  1.00 |        - |        - |        - |          - |          NA |
| NonAsciiLookup_CountTokens     | AsciiLookup   |       249.18 ns |  2.78 |        - |        - |        - |          - |          NA |
|                                |               |                 |       |          |          |          |            |             |
| Tiktoken_Construction_o200k    | Construction  | 1,035,410.74 ns |  1.01 | 230.4688 | 230.4688 | 230.4688 | 10594948 B |        1.00 |
| Tiktoken_Construction_cl100k   | Construction  |   564,023.20 ns |  0.55 | 126.9531 | 126.9531 | 126.9531 |  5195072 B |        0.49 |
|                                |               |                 |       |          |          |          |            |             |
| Tiktoken_FirstCall_CountTokens | FirstCall     |   951,575.17 ns |  1.00 | 281.2500 | 281.2500 | 281.2500 | 10594895 B |        1.00 |
| Tiktoken_FirstCall_Encode      | FirstCall     |   918,556.36 ns |  0.97 | 269.5313 | 269.5313 | 269.5313 | 10599356 B |        1.00 |
|                                |               |                 |       |          |          |          |            |             |
| Tiktoken_WriteToBinary_o200k   | WriteToBinary | 3,409,896.08 ns |  1.00 | 964.8438 | 964.8438 | 964.8438 | 16778836 B |        1.00 |
| Tiktoken_WriteToBinary_cl100k  | WriteToBinary | 1,669,422.56 ns |  0.49 | 220.7031 | 220.7031 | 220.7031 |  8390307 B |        0.50 |

### Changes (vs v3-beta_construction_flat_memory.md)

- **Eliminated FastEncoder**: Removed the lazy `FrozenDictionary<string, int>` (~31MB for o200k) that was built on the first Encode/CountTokens call. Replaced with direct ASCII lookup on the existing `TokenEncoder` hash table via new `TryGetValueAscii(ReadOnlySpan<char>)` and `ContainsKeyAscii(ReadOnlySpan<char>)` methods.
- **Direct ASCII lookup**: For ASCII-only regex matches, `(byte)char == byte`, so we compute FNV-1a hash directly from `ReadOnlySpan<char>` and compare against the pre-computed hash table — no intermediate dictionary needed.
- **Removed dead NET7 branches**: Since no `net7.0` TFM exists, all `#if NET7_0_OR_GREATER` guards were equivalent to `NET8_0_OR_GREATER`. Renamed globally across all files.

### Cumulative Results (vs 2.2.0.0_construction.md original baseline)

| Metric | Original | After | Improvement |
|--------|----------|-------|:-----------:|
| **o200k construction** | 31.56 ms | 1.04 ms | **30x faster** |
| **cl100k construction** | 14.08 ms | 0.56 ms | **25x faster** |
| **o200k memory** | 43.93 MB | 10.1 MB | **77% reduction** |
| **FirstCall (o200k)** | ~31 ms | ~0.95 ms | **33x faster** |
| **FirstCall memory** | 38.15 MB | 10.1 MB | **74% reduction** |

### Step-by-step Results

| Metric | Original | + Lazy + Quadratic | + Flat Memory | + No FastEncoder |
|--------|----------|-------------------|---------------|-----------------|
| **o200k construction** | 31.56 ms | 12.39 ms (2.5x) | 3.81 ms (8.3x) | 1.04 ms (**30x**) |
| **cl100k construction** | 14.08 ms | 2.56 ms (5.5x) | 1.90 ms (7.4x) | 0.56 ms (**25x**) |
| **o200k memory** | 43.93 MB | 13.12 MB (70%) | 7.34 MB (83%) | 10.1 MB (**77%**) |
| **FirstCall** | ~31 ms | ~23 ms | ~23 ms | ~0.95 ms (**33x**) |

### ASCII Fast-Path Micro-Benchmarks

| Input | CountTokens | Encode | Alloc |
|-------|------------|--------|-------|
| ASCII ("Hello, World!") | 89.7 ns | 113.1 ns | 72 B (encode only) |
| Non-ASCII ("Привет мир!") | 249.2 ns (2.78x) | 276.1 ns (2.44x) | 72 B (encode only) |

### Key Optimizations Applied

1. **Lazy FastEncoder** — Deferred `FrozenDictionary<string, int>` (~30MB for o200k) from construction to first Encode/CountTokens call
2. **Triangular number probing** — Replaced linear probing in TokenEncoder hash table (better cache behavior at low load factors)
3. **Flat memory layout** — Single `byte[]` buffer for all token data eliminates ~200K individual array allocations during binary parsing
4. **Direct ASCII lookup on TokenEncoder** — Eliminated the lazy FastEncoder entirely by adding `TryGetValueAscii`/`ContainsKeyAscii` methods that hash directly from `ReadOnlySpan<char>` (treating each char as a byte for ASCII). First-call drops from ~23ms to ~0.95ms. Memory drops from ~38MB to ~10MB.

### Steady-State Encode Performance (no regression)

Verified via separate encode benchmarks — zero regression on all input types (ASCII, code, multilingual, CJK, long documents). Cache and no-cache paths both unchanged.
