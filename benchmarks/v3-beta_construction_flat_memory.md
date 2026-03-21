```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M4 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Categories   | Mean      | Ratio | Gen0      | Gen1      | Gen2      | Allocated | Alloc Ratio |
|------------------------------- |------------- |----------:|------:|----------:|----------:|----------:|----------:|------------:|
| Tiktoken_Construction_o200k    | Construction |  3.811 ms |  1.00 |  996.0938 |  996.0938 |  996.0938 |   7.34 MB |        1.00 |
| Tiktoken_Construction_cl100k   | Construction |  1.895 ms |  0.50 |  996.0938 |  996.0938 |  996.0938 |   3.57 MB |        0.49 |
|                                |              |           |       |           |           |           |           |             |
| Tiktoken_FirstCall_CountTokens | FirstCall    | 23.332 ms |  1.00 | 1843.7500 | 1500.0000 | 1187.5000 |  38.15 MB |        1.00 |
| Tiktoken_FirstCall_Encode      | FirstCall    | 23.226 ms |  1.00 | 1718.7500 | 1343.7500 | 1062.5000 |  38.15 MB |        1.00 |

### Changes (vs 2.2.0.0_construction_optimized.md)

- **Flat memory layout:** Replaced ~200K individual `byte[]` allocations with a single contiguous `byte[]` buffer + offset/length arrays in `EncodingData` and `TokenEncoder`
- **Zero-copy pipeline:** `.ttkb` binary parser (`ParseBinaryEncodingToArrays`) now writes tokens into a single flat buffer, which flows directly into `TokenEncoder.From()` and `BuildFastEncoder()` without any intermediate `byte[][]`

### Cumulative Results (vs 2.2.0.0_construction.md original baseline)

| Metric | Original | After | Improvement |
|--------|----------|-------|:-----------:|
| **o200k construction** | 31.56 ms | 3.81 ms | **8.3x faster** |
| **cl100k construction** | 14.08 ms | 1.90 ms | **7.4x faster** |
| **o200k memory** | 43.93 MB | 7.34 MB | **83% reduction** |
| **cl100k memory** | 21.47 MB | 3.57 MB | **83% reduction** |
| **FirstCall** | ~31 ms | ~23 ms | **26% faster** |

### Step-by-step Results

| Metric | Original | + Lazy + Quadratic | + Flat Memory |
|--------|----------|-------------------|---------------|
| **o200k construction** | 31.56 ms | 12.39 ms (2.5x) | 3.81 ms (**8.3x**) |
| **cl100k construction** | 14.08 ms | 2.56 ms (5.5x) | 1.90 ms (**7.4x**) |
| **o200k memory** | 43.93 MB | 13.12 MB (70%) | 7.34 MB (**83%**) |
| **cl100k memory** | 21.47 MB | 6.48 MB (70%) | 3.57 MB (**83%**) |

### Key Optimizations Applied

1. **Lazy FastEncoder** — Deferred `FrozenDictionary<string, int>` (~30MB for o200k) from construction to first Encode/CountTokens call
2. **Triangular number probing** — Replaced linear probing in TokenEncoder hash table (better cache behavior at low load factors)
3. **Flat memory layout** — Single `byte[]` buffer for all token data eliminates ~200K individual array allocations during binary parsing
