namespace Tiktoken.Encodings;

/// <summary>
///
/// </summary>
public class ByteArrayComparer : IEqualityComparer<byte[]>
#if NET9_0_OR_GREATER
    , IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>
#endif
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x == null || y == null)
        {
            return x == y;
        }
        if (x.Length != y.Length)
        {
            return false;
        }
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int GetHashCode(byte[] obj)
    {
        obj = obj ?? throw new ArgumentNullException(nameof(obj));

#if NET8_0_OR_GREATER
        var hash = new HashCode();
        hash.AddBytes(obj.AsSpan());
        return hash.ToHashCode();
#else
        var hash = 17;
        for (var i = 0; i < obj.Length; i++)
        {
            hash = hash * 31 + obj[i];
        }
        return hash;
#endif
    }

#if NET9_0_OR_GREATER
    /// <inheritdoc />
    bool IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>.Equals(ReadOnlySpan<byte> alternate, byte[] other)
    {
        return alternate.SequenceEqual(other);
    }

    /// <inheritdoc />
    int IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>.GetHashCode(ReadOnlySpan<byte> alternate)
    {
        var hash = new HashCode();
        hash.AddBytes(alternate);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    byte[] IAlternateEqualityComparer<ReadOnlySpan<byte>, byte[]>.Create(ReadOnlySpan<byte> alternate)
    {
        return alternate.ToArray();
    }
#endif
}
