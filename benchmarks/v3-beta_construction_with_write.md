```

BenchmarkDotNet v0.15.8, macOS Tahoe 26.3.1 (25D2128) [Darwin 25.3.0]
Apple M4 Max, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a
  DefaultJob : .NET 10.0.5 (10.0.5, 10.0.526.15411), Arm64 RyuJIT armv8.0-a


```
| Method                         | Categories    | Mean        | Ratio | Gen0      | Gen1      | Gen2     | Allocated | Alloc Ratio |
|------------------------------- |-------------- |------------:|------:|----------:|----------:|---------:|----------:|------------:|
| Tiktoken_Construction_o200k    | Construction  |    784.1 μs |  1.00 |  261.7188 |  261.7188 | 261.7188 |   10.1 MB |        1.00 |
| Tiktoken_Construction_cl100k   | Construction  |    455.1 μs |  0.58 |  164.5508 |  164.0625 | 164.0625 |   4.95 MB |        0.49 |
|                                |               |             |       |           |           |          |           |             |
| Tiktoken_FirstCall_CountTokens | FirstCall     | 20,759.8 μs |  1.00 | 1531.2500 | 1093.7500 | 875.0000 |  40.91 MB |        1.00 |
| Tiktoken_FirstCall_Encode      | FirstCall     | 20,680.5 μs |  1.00 | 1531.2500 | 1093.7500 | 875.0000 |  40.91 MB |        1.00 |
|                                |               |             |       |           |           |          |           |             |
| Tiktoken_WriteToBinary_o200k   | WriteToBinary |  3,450.8 μs |  1.00 |  500.0000 |  500.0000 | 500.0000 |     16 MB |        1.00 |
| Tiktoken_WriteToBinary_cl100k  | WriteToBinary |  1,671.1 μs |  0.48 |  888.6719 |  888.6719 | 888.6719 |      8 MB |        0.50 |
