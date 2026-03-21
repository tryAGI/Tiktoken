```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M4 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Categories   | Mean        | Ratio | Gen0      | Gen1      | Gen2      | Allocated | Alloc Ratio |
|------------------------------- |------------- |------------:|------:|----------:|----------:|----------:|----------:|------------:|
| Tiktoken_Construction_o200k    | Construction |    995.6 us |  1.01 |  482.4219 |  482.4219 |  482.4219 |   10.1 MB |        1.00 |
| Tiktoken_Construction_cl100k   | Construction |    306.7 us |  0.31 |  447.2656 |  446.2891 |  446.2891 |   4.96 MB |        0.49 |
|                                |              |             |       |           |           |           |           |             |
| Tiktoken_FirstCall_CountTokens | FirstCall    | 20,916.1 us |  1.00 | 1718.7500 | 1281.2500 | 1000.0000 |  40.91 MB |        1.00 |
| Tiktoken_FirstCall_Encode      | FirstCall    | 21,239.9 us |  1.02 | 1625.0000 | 1187.5000 |  906.2500 |  40.91 MB |        1.00 |

### Changes (vs 2.2.0.0_construction_flat_memory.md)

- **TTKB v2 binary format:** Pre-computed FNV-1a hash table stored directly in `.ttkb` file
- **Bulk-copy parsing:** `MemoryMarshal.Cast<byte, int>` for buckets/ranks/offsets arrays — pure memcpy, no per-entry loop
- **No Task.Run:** Hash table is pre-built, so TokenEncoder construction is a trivial array assignment

### TTKB v2 Format

```
Header (28 bytes):
  [0..4]    "TTKB" magic
  [4..8]    version = 2 (uint32 LE)
  [8..12]   entryCount (uint32 LE)
  [12..16]  tableSize (uint32 LE, power of 2)
  [16..20]  mask (uint32 LE, tableSize - 1)
  [20..24]  keyBlobSize (uint32 LE)
  [24..28]  flags (uint32 LE, reserved = 0)

Sections (contiguous, bulk-copyable):
  Buckets:    int32[tableSize]     — pre-computed hash table (-1 = empty)
  Ranks:      int32[entryCount]    — token ranks
  KeyOffsets: int32[entryCount]    — byte offset of each key in blob
  KeyLengths: uint8[entryCount]    — byte length of each key
  KeyBlob:    byte[keyBlobSize]    — concatenated raw token bytes
```

### Full Optimization History

| Step | o200k Time | o200k Memory | cl100k Time | cl100k Memory |
|------|-----------|-------------|-------------|---------------|
| **Original baseline** | 31,560 μs | 43.93 MB | 14,080 μs | 21.47 MB |
| + Lazy FastEncoder + Triangular probing | 12,390 μs | 13.12 MB | 2,560 μs | 6.48 MB |
| + Flat memory layout | 3,811 μs | 7.34 MB | 1,895 μs | 3.57 MB |
| **+ TTKB v2 pre-computed hash table** | **996 μs** | **10.1 MB** | **307 μs** | **4.96 MB** |
| **Total improvement** | **31.7x faster** | **4.4x less**¹ | **45.9x faster** | **4.3x less**¹ |

¹ Construction memory is higher than flat-memory-only (v1) due to storing pre-computed buckets, but still 77% less than original.

### File Size Impact

| Encoding | v1 size | v2 size | Delta |
|----------|---------|---------|-------|
| o200k_base | 2.4 MB | 5.3 MB | +2.9 MB |
| cl100k_base | 1.1 MB | 2.6 MB | +1.5 MB |
| p50k_base | 0.6 MB | 1.3 MB | +0.7 MB |
| r50k_base | 0.6 MB | 1.3 MB | +0.7 MB |
| **Total** | **4.7 MB** | **10.5 MB** | **+5.8 MB** |

The +5.8 MB NuGet package increase is justified by 32-46x faster construction.
