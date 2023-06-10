using System.Diagnostics;

namespace Tiktoken.Utilities;

/// <summary>
/// 
/// </summary>
public static class BytePairEncoding
{
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    private static byte[] GetSlice(this ReadOnlyMemory<byte> bytes, int from, int to)
    {
        return bytes.Slice(from, to - from).ToArray();
    }
#else
    private static byte[] GetSlice(this byte[] bytes, int from, int to)
    {
        return bytes.Skip(from).Take(to - from).ToArray();
    }
#endif
    
    private static bool TryFindMinRank(IReadOnlyList<(int Index, int Rank)> parts, out int result)
    {
        result = 0;
        var minRank = int.MaxValue;
        for (var i = 0; i < parts.Count - 1; i++)
        {
            if (parts[i].Rank < minRank)
            {
                minRank = parts[i].Rank;
                result = i;
            }
        }
        
        return minRank != int.MaxValue;
    }
    
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    internal static IReadOnlyCollection<int> BytePairEncode(ReadOnlyMemory<byte> piece, IReadOnlyDictionary<byte[], int> ranks)
#else
    internal static IReadOnlyCollection<int> BytePairEncode(byte[] piece, IReadOnlyDictionary<byte[], int> ranks)
#endif
    {
        var parts = Enumerable
            .Range(0, piece.Length + 1)
            .Select(i => (Index: i, Rank: int.MaxValue))
            .ToList();
        
        int? GetRank(int startIdx, int skip = 0)
        {
            if (startIdx + skip + 2 < parts.Count)
            {
                var from = parts[startIdx].Index;
                var to = parts[startIdx + skip + 2].Index;
                var slice = piece.GetSlice(from, to);
                if (ranks.TryGetValue(slice, out var rank))
                {
                    return rank;
                }
            }
            return null;
        }
        for (int i = 0; i < parts.Count - 2; i++)
        {
            var rank = GetRank(i);
            if (rank != null)
            {
                Debug.Assert(rank.Value != int.MaxValue);
                parts[i] = (parts[i].Index, rank.Value);
            }
        }
        while (parts.Count > 1)
        {
            if (!TryFindMinRank(parts, out var i))
            {
                break;
            }

            parts[i] = (parts[i].Index, GetRank(i, 1) ?? int.MaxValue);
            if (i > 0)
            {
                parts[i - 1] = (parts[i - 1].Index, GetRank(i - 1, 1) ?? int.MaxValue);
            }
            parts.RemoveAt(i + 1);
        }
        var outList = new List<int>(parts.Count - 1);
        for (var i = 0; i < parts.Count - 1; i++)
        {
            var from = parts[i].Index;
            var to = parts[i + 1].Index;
            var slice = piece.GetSlice(from, to);
            
            outList.Add(ranks[slice]);
        }
        
        return outList;
    }
    
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    internal static int BytePairEncodeCountTokens(ReadOnlyMemory<byte> piece, IReadOnlyDictionary<byte[], int> ranks)
#else
    internal static int BytePairEncodeCountTokens(byte[] piece, IReadOnlyDictionary<byte[], int> ranks)
#endif
    {
        var parts = Enumerable
            .Range(0, piece.Length + 1)
            .Select(i => (Index: i, Rank: int.MaxValue))
            .ToList();
        
        int? GetRank(int startIdx, int skip = 0)
        {
            if (startIdx + skip + 2 < parts.Count)
            {
                var from = parts[startIdx].Index;
                var to = parts[startIdx + skip + 2].Index;
                var slice = piece.GetSlice(from, to);
                if (ranks.TryGetValue(slice, out var rank))
                {
                    return rank;
                }
            }
            return null;
        }
        for (int i = 0; i < parts.Count - 2; i++)
        {
            var rank = GetRank(i);
            if (rank != null)
            {
                Debug.Assert(rank.Value != int.MaxValue);
                parts[i] = (parts[i].Index, rank.Value);
            }
        }
        while (parts.Count > 1)
        {
            if (!TryFindMinRank(parts, out var i))
            {
                break;
            }
            
            parts[i] = (parts[i].Index, GetRank(i, 1) ?? int.MaxValue);
            if (i > 0)
            {
                parts[i - 1] = (parts[i - 1].Index, GetRank(i - 1, 1) ?? int.MaxValue);
            }
            parts.RemoveAt(i + 1);
        }
        
        return parts.Count - 1;
    }
}