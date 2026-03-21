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
