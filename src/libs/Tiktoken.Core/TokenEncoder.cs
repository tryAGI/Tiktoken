#if NET8_0_OR_GREATER

using System.Collections;
using System.Runtime.CompilerServices;

namespace Tiktoken.Core;

/// <summary>
/// High-performance token encoder using FNV-1a hashing and open-addressing hash table.
/// Uses flat memory layout (single byte buffer) to eliminate per-token heap allocations.
/// Triangular number probing reduces clustering at low load factors.
/// </summary>
internal sealed class TokenEncoder : IReadOnlyDictionary<byte[], int>
{
    private readonly byte[] _data;           // All token bytes concatenated
    private readonly int[] _offsets;          // Start offset of each token in _data
    private readonly byte[] _tokenLengths;   // Length of each token (max 255)
    private readonly int[] _ranks;
    private readonly int[] _buckets;          // Hash table: entry index or -1 for empty
    private readonly int _mask;               // _buckets.Length - 1 (power of 2)

    private TokenEncoder(byte[] data, int[] offsets, byte[] tokenLengths, int[] ranks, int[] buckets, int mask)
    {
        _data = data;
        _offsets = offsets;
        _tokenLengths = tokenLengths;
        _ranks = ranks;
        _buckets = buckets;
        _mask = mask;
    }

    /// <summary>
    /// Creates a TokenEncoder from pre-computed hash table (v2 binary format).
    /// Zero hash computation — just assigns the arrays.
    /// </summary>
    public static TokenEncoder FromPrecomputed(
        byte[] data, int[] offsets, byte[] tokenLengths, int[] ranks,
        int[] buckets, int mask)
    {
        return new TokenEncoder(data, offsets, tokenLengths, ranks, buckets, mask);
    }

    /// <summary>
    /// Creates a TokenEncoder from flat memory layout (zero-copy from EncodingData).
    /// </summary>
    public static TokenEncoder From(byte[] data, int[] offsets, byte[] tokenLengths, int[] ranks)
    {
        var count = ranks.Length;

        // Size hash table to ~67% load factor, rounded up to power of 2
        var tableSize = RoundUpPowerOf2((uint)(count * 3 / 2));
        if (tableSize < 16) tableSize = 16;
        var mask = (int)(tableSize - 1);
        var buckets = new int[tableSize];
        Array.Fill(buckets, -1);

        for (var i = 0; i < count; i++)
        {
            var bucket = (int)(FnvHash(data.AsSpan(offsets[i], tokenLengths[i])) & (uint)mask);
            var step = 1;
            while (buckets[bucket] != -1)
            {
                bucket = (bucket + step) & mask;
                step++;
            }
            buckets[bucket] = i;
        }

        return new TokenEncoder(data, offsets, tokenLengths, ranks, buckets, mask);
    }

    /// <summary>
    /// Creates a TokenEncoder from byte[][] keys (fallback for non-EncodingData sources).
    /// Flattens into single buffer internally.
    /// </summary>
    public static TokenEncoder From(byte[][] sourceKeys, int[] sourceRanks)
    {
        var count = sourceKeys.Length;

        // Flatten keys into single buffer
        var totalSize = 0;
        var lengths = new byte[count];
        for (var i = 0; i < count; i++)
        {
            lengths[i] = (byte)sourceKeys[i].Length;
            totalSize += sourceKeys[i].Length;
        }

        var data = new byte[totalSize];
        var offsets = new int[count];
        var writeOffset = 0;
        for (var i = 0; i < count; i++)
        {
            offsets[i] = writeOffset;
            sourceKeys[i].CopyTo(data.AsSpan(writeOffset));
            writeOffset += sourceKeys[i].Length;
        }

        return From(data, offsets, lengths, sourceRanks);
    }

    /// <summary>
    /// Creates a TokenEncoder from a dictionary (fallback for non-array sources).
    /// </summary>
    public static TokenEncoder From(IReadOnlyDictionary<byte[], int> source)
    {
        var count = source.Count;
        var keys = new byte[count][];
        var ranks = new int[count];

        var index = 0;
        foreach (var kvp in source)
        {
            keys[index] = kvp.Key;
            ranks[index] = kvp.Value;
            index++;
        }

        return From(keys, ranks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint FnvHash(ReadOnlySpan<byte> key)
    {
        var hash = 2166136261u;
        for (var i = 0; i < key.Length; i++)
        {
            hash ^= key[i];
            hash *= 16777619u;
        }
        return hash;
    }

    private static uint RoundUpPowerOf2(uint value)
    {
        return (uint)1 << (32 - int.LeadingZeroCount((int)(value - 1)));
    }

    // Span-based lookups with triangular number probing

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<byte> key, out int rank)
    {
        var bucket = (int)(FnvHash(key) & (uint)_mask);
        var step = 1;
        while (true)
        {
            var idx = _buckets[bucket];
            if (idx == -1)
            {
                rank = 0;
                return false;
            }
            if (key.SequenceEqual(_data.AsSpan(_offsets[idx], _tokenLengths[idx])))
            {
                rank = _ranks[idx];
                return true;
            }
            bucket = (bucket + step) & _mask;
            step++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(ReadOnlySpan<byte> key)
    {
        var bucket = (int)(FnvHash(key) & (uint)_mask);
        var step = 1;
        while (true)
        {
            var idx = _buckets[bucket];
            if (idx == -1)
            {
                return false;
            }
            if (key.SequenceEqual(_data.AsSpan(_offsets[idx], _tokenLengths[idx])))
            {
                return true;
            }
            bucket = (bucket + step) & _mask;
            step++;
        }
    }

    // UTF-16 span lookups — inline UTF-16→UTF-8 encoding + FNV-1a hash in a single pass.
    // Avoids intermediate UTF-8 buffer allocation for single-token non-ASCII lookups.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValueUtf16(ReadOnlySpan<char> key, out int rank)
    {
        // Phase 1: Compute FNV-1a hash and UTF-8 byte count in single pass
        var hash = 2166136261u;
        var utf8Len = 0;
        for (var i = 0; i < key.Length; i++)
        {
            var c = (uint)key[i];
            if (c < 0x80)
            {
                hash ^= c;
                hash *= 16777619u;
                utf8Len++;
            }
            else if (c < 0x800)
            {
                hash ^= 0xC0u | (c >> 6); hash *= 16777619u;
                hash ^= 0x80u | (c & 0x3Fu); hash *= 16777619u;
                utf8Len += 2;
            }
            else if (c >= 0xD800 && c <= 0xDBFF && i + 1 < key.Length)
            {
                // Surrogate pair → 4 UTF-8 bytes
                var low = (uint)key[++i];
                var cp = 0x10000u + ((c - 0xD800u) << 10) + (low - 0xDC00u);
                hash ^= 0xF0u | (cp >> 18); hash *= 16777619u;
                hash ^= 0x80u | ((cp >> 12) & 0x3Fu); hash *= 16777619u;
                hash ^= 0x80u | ((cp >> 6) & 0x3Fu); hash *= 16777619u;
                hash ^= 0x80u | (cp & 0x3Fu); hash *= 16777619u;
                utf8Len += 4;
            }
            else
            {
                // BMP non-ASCII → 3 UTF-8 bytes
                hash ^= 0xE0u | (c >> 12); hash *= 16777619u;
                hash ^= 0x80u | ((c >> 6) & 0x3Fu); hash *= 16777619u;
                hash ^= 0x80u | (c & 0x3Fu); hash *= 16777619u;
                utf8Len += 3;
            }
        }

        // Phase 2: Probe hash table
        var bucket = (int)(hash & (uint)_mask);
        var step = 1;
        while (true)
        {
            var idx = _buckets[bucket];
            if (idx == -1) { rank = 0; return false; }
            if (_tokenLengths[idx] == utf8Len)
            {
                // Phase 3: Compare by re-walking UTF-16 chars against stored UTF-8 bytes
                if (CompareUtf16ToUtf8(key, _data.AsSpan(_offsets[idx], _tokenLengths[idx])))
                {
                    rank = _ranks[idx];
                    return true;
                }
            }
            bucket = (bucket + step) & _mask;
            step++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKeyUtf16(ReadOnlySpan<char> key)
        => TryGetValueUtf16(key, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CompareUtf16ToUtf8(ReadOnlySpan<char> chars, ReadOnlySpan<byte> utf8)
    {
        var bi = 0;
        for (var ci = 0; ci < chars.Length; ci++)
        {
            var c = (uint)chars[ci];
            if (c < 0x80)
            {
                if (bi >= utf8.Length || utf8[bi++] != (byte)c) return false;
            }
            else if (c < 0x800)
            {
                if (bi + 1 >= utf8.Length) return false;
                if (utf8[bi++] != (byte)(0xC0 | (c >> 6))) return false;
                if (utf8[bi++] != (byte)(0x80 | (c & 0x3F))) return false;
            }
            else if (c >= 0xD800 && c <= 0xDBFF && ci + 1 < chars.Length)
            {
                var low = (uint)chars[++ci];
                var cp = 0x10000u + ((c - 0xD800u) << 10) + (low - 0xDC00u);
                if (bi + 3 >= utf8.Length) return false;
                if (utf8[bi++] != (byte)(0xF0 | (cp >> 18))) return false;
                if (utf8[bi++] != (byte)(0x80 | ((cp >> 12) & 0x3F))) return false;
                if (utf8[bi++] != (byte)(0x80 | ((cp >> 6) & 0x3F))) return false;
                if (utf8[bi++] != (byte)(0x80 | (cp & 0x3F))) return false;
            }
            else
            {
                if (bi + 2 >= utf8.Length) return false;
                if (utf8[bi++] != (byte)(0xE0 | (c >> 12))) return false;
                if (utf8[bi++] != (byte)(0x80 | ((c >> 6) & 0x3F))) return false;
                if (utf8[bi++] != (byte)(0x80 | (c & 0x3F))) return false;
            }
        }
        return bi == utf8.Length;
    }

    public int this[ReadOnlySpan<byte> key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (TryGetValue(key, out var rank))
            {
                return rank;
            }
            throw new KeyNotFoundException();
        }
    }

    // IReadOnlyDictionary<byte[], int> implementation

    public int Count => _ranks.Length;

    public IEnumerable<byte[]> Keys
    {
        get
        {
            for (var i = 0; i < _ranks.Length; i++)
            {
                yield return _data.AsSpan(_offsets[i], _tokenLengths[i]).ToArray();
            }
        }
    }

    public IEnumerable<int> Values => _ranks;

    public int this[byte[] key]
    {
        get
        {
            if (TryGetValue(key.AsSpan(), out var rank))
            {
                return rank;
            }
            throw new KeyNotFoundException();
        }
    }

    public bool ContainsKey(byte[] key) => ContainsKey(key.AsSpan());

    public bool TryGetValue(byte[] key, out int value) => TryGetValue(key.AsSpan(), out value);

    public IEnumerator<KeyValuePair<byte[], int>> GetEnumerator()
    {
        for (var i = 0; i < _ranks.Length; i++)
        {
            yield return new KeyValuePair<byte[], int>(
                _data.AsSpan(_offsets[i], _tokenLengths[i]).ToArray(),
                _ranks[i]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

#endif
