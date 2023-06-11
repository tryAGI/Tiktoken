#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using Bytes = System.ReadOnlyMemory<byte>;
#endif

namespace Tiktoken.Utilities;

/// <summary>
/// 
/// </summary>
public static class BytePairEncoding
{
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    private static byte[] GetSlice(this Bytes bytes, int from, int to)
    {
        return bytes[from..to].ToArray();
    }
#else
    private static byte[] GetSlice(this byte[] bytes, int from, int to)
    {
        return bytes.Skip(from).Take(to - from).ToArray();
    }
#endif
    
    private static unsafe bool TryFindMinRank(int* partsRanks, int count, out int result)
    {
        result = 0;
        var minRank = int.MaxValue;
        for (var i = 0; i < count; i++)
        {
            if (partsRanks[i] < minRank)
            {
                minRank = partsRanks[i];
                result = i;
            }
        }
        
        return minRank != int.MaxValue;
    }
    
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    internal static unsafe IReadOnlyCollection<int> BytePairEncode(Bytes piece, IReadOnlyDictionary<byte[], int> ranks)
#else
    internal static unsafe IReadOnlyCollection<int> BytePairEncode(byte[] piece, IReadOnlyDictionary<byte[], int> ranks)
#endif
    {
        var partsLength = piece.Length + 1;
        var partsIndexes = stackalloc int [partsLength];
        var partsRanks = stackalloc int [partsLength];
        for (var i = 0; i < partsLength; i++)
        {
            partsIndexes[i] = i;
            partsRanks[i] = int.MaxValue;
        }
        for (var i = 0; i < partsLength - 2; i++)
        {
            partsRanks[i] = GetRank(i, partsIndexes, partsLength, piece, ranks, length: 2);
        }

        var count = partsLength - 1;
        while (true)
        {
            if (!TryFindMinRank(partsRanks, count, out var i))
            {
                break;
            }

            partsRanks[i] = GetRank(i, partsIndexes, count + 1, piece, ranks, length: 3);
            if (i > 0)
            {
                partsRanks[i - 1] = GetRank(i - 1, partsIndexes, count + 1, piece, ranks, length: 3);
            }
            for (var j = i + 1; j < count; j++)
            {
                partsIndexes[j] = partsIndexes[j + 1];
                partsRanks[j] = partsRanks[j + 1];
            }
            count--;
        }
        var outList = new List<int>(count);
        for (var i = 0; i < count; i++)
        {
            var from = partsIndexes[i];
            var to = partsIndexes[i + 1];
            var slice = piece.GetSlice(from, to);
            
            outList.Add(ranks[slice]);
        }
        
        return outList;
    }
    
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    internal static unsafe int BytePairEncodeCountTokens(Bytes piece, IReadOnlyDictionary<byte[], int> ranks)
#else
    internal static unsafe int BytePairEncodeCountTokens(byte[] piece, IReadOnlyDictionary<byte[], int> ranks)
#endif
    {
        var partsLength = piece.Length + 1;
        var partsIndexes = stackalloc int [partsLength];
        var partsRanks = stackalloc int [partsLength];
        for (var i = 0; i < partsLength; i++)
        {
            partsIndexes[i] = i;
            partsRanks[i] = int.MaxValue;
        }
        for (var i = 0; i < partsLength - 2; i++)
        {
            partsRanks[i] = GetRank(i, partsIndexes, partsLength, piece, ranks, length: 2);
        }
        
        var count = partsLength - 1;
        while (true)
        {
            if (!TryFindMinRank(partsRanks, count, out var i))
            {
                break;
            }
            
            partsRanks[i] = GetRank(i, partsIndexes, count + 1, piece, ranks, length: 3);
            if (i > 0)
            {
                partsRanks[i - 1] = GetRank(i - 1, partsIndexes, count + 1, piece, ranks, length: 3);
            }
            for (var j = i + 1; j < count; j++)
            {
                partsIndexes[j] = partsIndexes[j + 1];
                partsRanks[j] = partsRanks[j + 1];
            }
            count--;
        }
        
        return count;
    }

    private static unsafe int GetRank(
        int startIdx,
        int* partsIndexes,
        int count,
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        Bytes piece,
#else
        byte[] piece,
#endif
        IReadOnlyDictionary<byte[], int> ranks,
        int length)
    {
        if (startIdx + length < count)
        {
            var from = partsIndexes[startIdx];
            var to = partsIndexes[startIdx + length];
            var slice = piece.GetSlice(from, to);
            if (ranks.TryGetValue(slice, out var rank))
            {
                return rank;
            }
        }
        
        return int.MaxValue;
    }
}