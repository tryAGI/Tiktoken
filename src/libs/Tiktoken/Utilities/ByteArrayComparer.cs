namespace Tiktoken.Utilities;

/// <summary>
/// 
/// </summary>
public class ByteArrayComparer : IEqualityComparer<byte[]>
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
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }
        int hash = 17;
        foreach (byte b in obj)
        {
            hash = hash * 31 + b;
        }
        return hash;
    }
}