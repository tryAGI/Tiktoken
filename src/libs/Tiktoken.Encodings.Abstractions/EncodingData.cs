#if NET8_0_OR_GREATER

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Tiktoken.Encodings;

/// <summary>
/// Flat-memory encoding data using a single byte buffer for all tokens.
/// Eliminates ~200K individual byte[] allocations during binary parsing.
/// </summary>
internal sealed class EncodingData : IReadOnlyDictionary<byte[], int>
{
    /// <summary>All token bytes concatenated into a single buffer.</summary>
    internal readonly byte[] _data;
    /// <summary>Start offset of each token in _data.</summary>
    internal readonly int[] _offsets;
    /// <summary>Length of each token (max 255 bytes).</summary>
    internal readonly byte[] _tokenLengths;
    /// <summary>Rank (token ID) for each entry.</summary>
    internal readonly int[] _ranks;
    /// <summary>Pre-computed FNV-1a hash table buckets (-1 = empty).</summary>
    internal readonly int[] _buckets;
    /// <summary>Hash table mask (buckets.Length - 1).</summary>
    internal readonly int _mask;

    internal EncodingData(byte[] data, int[] offsets, byte[] tokenLengths, int[] ranks, int[] buckets, int mask)
    {
        _data = data;
        _offsets = offsets;
        _tokenLengths = tokenLengths;
        _ranks = ranks;
        _buckets = buckets;
        _mask = mask;
    }

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
            if (TryGetValue(key, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException();
        }
    }

    public bool ContainsKey(byte[] key)
    {
        for (var i = 0; i < _ranks.Length; i++)
        {
            if (_data.AsSpan(_offsets[i], _tokenLengths[i]).SequenceEqual(key))
            {
                return true;
            }
        }
        return false;
    }

    public bool TryGetValue(byte[] key, [MaybeNullWhen(false)] out int value)
    {
        for (var i = 0; i < _ranks.Length; i++)
        {
            if (_data.AsSpan(_offsets[i], _tokenLengths[i]).SequenceEqual(key))
            {
                value = _ranks[i];
                return true;
            }
        }
        value = default;
        return false;
    }

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
