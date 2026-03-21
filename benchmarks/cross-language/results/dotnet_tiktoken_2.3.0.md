# .NET Tiktoken v2.3.0 — Apple M4 Max

BenchmarkDotNet v0.15.8 | .NET 10.0.5 | Arm64 RyuJIT | Encoding: o200k_base

Data extracted from `benchmarks/2.3.0.0_encode.md` (full benchmark suite).

## CountTokens (cached, zero-allocation)

| Input | Median | Throughput | Allocated |
|-------|--------|:----------:|-----------|
| hello_world (13 B) | 104.6 ns | 118.5 MiB/s | 0 B |
| multilingual (382 B) | 1,181.6 ns | 308.3 MiB/s | 0 B |
| cjk_heavy (1,676 B) | 2,768.3 ns | 577.4 MiB/s | 0 B |
| code (879 B) | 6,612.1 ns | 126.8 MiB/s | 0 B |
| multilingual_long (4,313 B) | 9,716.7 ns | 423.3 MiB/s | 0 B |
| bitcoin (19,866 B) | 132,554.9 ns | 142.9 MiB/s | 0 B |

## Encode (returns token IDs, cached)

| Input | Median | Throughput | Allocated |
|-------|--------|:----------:|-----------|
| hello_world (13 B) | 127.9 ns | 96.9 MiB/s | 72 B |
| multilingual (382 B) | 1,392.1 ns | 261.7 MiB/s | 1,184 B |
| cjk_heavy (1,676 B) | 3,424.0 ns | 466.9 MiB/s | 4,448 B |
| code (879 B) | 7,030.5 ns | 119.3 MiB/s | 4,304 B |
| multilingual_long (4,313 B) | 12,505.3 ns | 328.9 MiB/s | 8,424 B |
| bitcoin (19,866 B) | 142,354.5 ns | 133.1 MiB/s | 65,840 B |

## CountTokens (no cache)

| Input | Median | Throughput | Allocated |
|-------|--------|:----------:|-----------|
| hello_world (13 B) | 107.8 ns | 114.9 MiB/s | 0 B |
| multilingual (382 B) | 7,628.6 ns | 47.7 MiB/s | 0 B |
| cjk_heavy (1,676 B) | 58,121.9 ns | 27.5 MiB/s | 0 B |
| code (879 B) | 6,816.6 ns | 123.0 MiB/s | 0 B |
| multilingual_long (4,313 B) | 144,942.2 ns | 28.4 MiB/s | 0 B |
| bitcoin (19,866 B) | 188,671.9 ns | 100.4 MiB/s | 0 B |

Note: The "cached" columns show performance after the token cache is warm.
For short/ASCII text (hello_world, code, bitcoin) cache provides minimal benefit.
For multilingual/CJK text the cache delivers 6-21x speedup.
The "no cache" numbers represent cold/first-call performance and are the
fairest comparison to Rust/Python which have no caching layer.
