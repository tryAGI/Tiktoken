#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using Bytes = System.ReadOnlyMemory<byte>;
#else
using Bytes = System.Collections.Generic.IReadOnlyCollection<byte>;
#endif
#if NET9_0_OR_GREATER
using System.Collections.Frozen;
#endif
#if NET8_0_OR_GREATER
using System.Buffers;
using Tiktoken.Core;
#endif

namespace Tiktoken.Core;

/// <summary>
///
/// </summary>
public static class BytePairEncoding
{
    // Maximum number of int elements to stackalloc (2 arrays × this size × 4 bytes each).
    // 512 elements = 4KB per array = 8KB total on stack, well within safe limits.
    private const int MaxStackAllocLength = 512;

    private static byte[] GetSlice(this Bytes bytes, int from, int to)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return bytes[from..to].ToArray();
#else
        return bytes.Skip(from).Take(to - from).ToArray();
#endif
    }

    private static int GetLength(this Bytes bytes)
    {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return bytes.Length;
#else
        return bytes.Count;
#endif
    }

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

    private static unsafe int GetRank(
        int startIdx,
        int* partsIndexes,
        int count,
        Bytes piece,
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

    private static unsafe int FindParts(
        Bytes piece,
        int* partsIndexes,
        int* partsRanks,
        int partsLength,
        IReadOnlyDictionary<byte[], int> ranks)
    {
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

#if NET8_0_OR_GREATER
    // Threshold: pieces with fewer parts use linear scan (lower overhead).
    // Pieces with more parts use heap-based merge (O(n log n) vs O(n²)).
    private const int HeapThreshold = 32;

    private static unsafe int GetRank(
        int startIdx,
        int* partsIndexes,
        int count,
        ReadOnlySpan<byte> pieceSpan,
        TokenEncoder encoder,
        int length)
    {
        if (startIdx + length < count)
        {
            var from = partsIndexes[startIdx];
            var to = partsIndexes[startIdx + length];
            var span = pieceSpan.Slice(from, to - from);
            if (encoder.TryGetValue(span, out var rank))
            {
                return rank;
            }
        }

        return int.MaxValue;
    }

    private static unsafe int FindParts(
        ReadOnlySpan<byte> pieceSpan,
        int* partsIndexes,
        int* partsRanks,
        int partsLength,
        TokenEncoder encoder)
    {
        if (partsLength >= HeapThreshold)
        {
            return FindPartsHeap(pieceSpan, partsIndexes, partsLength, encoder);
        }

        for (var i = 0; i < partsLength; i++)
        {
            partsIndexes[i] = i;
            partsRanks[i] = int.MaxValue;
        }
        for (var i = 0; i < partsLength - 2; i++)
        {
            partsRanks[i] = GetRank(i, partsIndexes, partsLength, pieceSpan, encoder, length: 2);
        }

        var count = partsLength - 1;
        while (true)
        {
            if (!TryFindMinRank(partsRanks, count, out var i))
            {
                break;
            }

            partsRanks[i] = GetRank(i, partsIndexes, count + 1, pieceSpan, encoder, length: 3);
            if (i > 0)
            {
                partsRanks[i - 1] = GetRank(i - 1, partsIndexes, count + 1, pieceSpan, encoder, length: 3);
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

    /// <summary>
    /// Heap-based BPE merge: O(n log n) instead of O(n²).
    /// Uses a linked list to avoid O(n) array shifts per merge,
    /// and a binary min-heap for O(log n) minimum rank extraction.
    /// </summary>
    private static unsafe int FindPartsHeap(
        ReadOnlySpan<byte> pieceSpan,
        int* resultIndexes,
        int partsLength,
        TokenEncoder encoder)
    {
        var n = partsLength;

        // For large pieces, rent from ArrayPool to avoid stack overflow on thread pool
        // threads and reduce GC pressure on repeated large-file tokenizations.
        // 5 arrays × n × 4 bytes each — for n > 512 this exceeds safe stackalloc limits.
        if (n > MaxStackAllocLength)
        {
            var pool = ArrayPool<int>.Shared;
            var nextArr = pool.Rent(n);
            var prevArr = pool.Rent(n);
            var ranksArr = pool.Rent(n);
            var heapArr = pool.Rent(n);
            var heapPosArr = pool.Rent(n);
            try
            {
                fixed (int* next = nextArr)
                fixed (int* prev = prevArr)
                fixed (int* ranks = ranksArr)
                fixed (int* heap = heapArr)
                fixed (int* heapPos = heapPosArr)
                {
                    return FindPartsHeapCore(pieceSpan, resultIndexes, n, encoder, next, prev, ranks, heap, heapPos);
                }
            }
            finally
            {
                pool.Return(heapPosArr);
                pool.Return(heapArr);
                pool.Return(ranksArr);
                pool.Return(prevArr);
                pool.Return(nextArr);
            }
        }
        else
        {
            var next = stackalloc int[n];
            var prev = stackalloc int[n];
            var ranks = stackalloc int[n];
            var heap = stackalloc int[n];
            var heapPos = stackalloc int[n];
            return FindPartsHeapCore(pieceSpan, resultIndexes, n, encoder, next, prev, ranks, heap, heapPos);
        }
    }

    private static unsafe int FindPartsHeapCore(
        ReadOnlySpan<byte> pieceSpan,
        int* resultIndexes,
        int n,
        TokenEncoder encoder,
        int* next,
        int* prev,
        int* ranks,
        int* heap,
        int* heapPos)
    {
        // Initialize linked list: 0 → 1 → 2 → ... → n-1
        for (var i = 0; i < n; i++)
        {
            next[i] = i + 1;
            prev[i] = i - 1;
            ranks[i] = int.MaxValue;
            heapPos[i] = -1;
        }
        next[n - 1] = n; // sentinel (past-end)

        // Compute initial ranks for all adjacent pairs and build heap
        var heapSize = 0;
        for (var i = 0; i < n - 2; i++)
        {
            var j = next[i]; // i+1
            var k = next[j]; // i+2
            var span = pieceSpan.Slice(i, k - i);
            if (encoder.TryGetValue(span, out var r))
            {
                ranks[i] = r;
            }
            heap[heapSize] = i;
            heapPos[i] = heapSize;
            heapSize++;
        }

        // Heapify (build heap in O(n))
        for (var i = heapSize / 2 - 1; i >= 0; i--)
        {
            HeapSiftDown(heap, heapPos, ranks, heapSize, i);
        }

        var count = n - 1; // number of parts

        while (heapSize > 0)
        {
            var minBoundary = heap[0];
            if (ranks[minBoundary] == int.MaxValue)
            {
                break;
            }

            // The boundary to remove is the one after minBoundary
            var removed = next[minBoundary];
            if (removed >= n)
            {
                break;
            }

            // Remove 'removed' from linked list
            var afterRemoved = next[removed];
            next[minBoundary] = afterRemoved;
            if (afterRemoved < n)
            {
                prev[afterRemoved] = minBoundary;
            }

            // Remove 'removed' from heap (if present)
            if (heapPos[removed] >= 0)
            {
                HeapRemove(heap, heapPos, ranks, ref heapSize, removed);
            }

            // Recompute rank for minBoundary: span from minBoundary to 2 boundaries ahead
            ranks[minBoundary] = int.MaxValue;
            var nn = next[minBoundary];
            if (nn < n)
            {
                var nnn = next[nn];
                if (nnn < n) // nnn must be a valid boundary (not the past-end sentinel)
                {
                    var span = pieceSpan.Slice(minBoundary, nnn - minBoundary);
                    if (encoder.TryGetValue(span, out var r))
                    {
                        ranks[minBoundary] = r;
                    }
                }
            }
            HeapUpdate(heap, heapPos, ranks, heapSize, minBoundary);

            // Recompute rank for left neighbor
            var p = prev[minBoundary];
            if (p >= 0)
            {
                ranks[p] = int.MaxValue;
                var pNext = next[p]; // = minBoundary
                if (pNext < n)
                {
                    var pNextNext = next[pNext];
                    if (pNextNext < n)
                    {
                        var span = pieceSpan.Slice(p, pNextNext - p);
                        if (encoder.TryGetValue(span, out var r))
                        {
                            ranks[p] = r;
                        }
                    }
                }
                HeapUpdate(heap, heapPos, ranks, heapSize, p);
            }

            count--;
        }

        // Traverse linked list to produce output (remaining boundary byte offsets)
        var idx = 0;
        var cur = 0;
        while (cur < n)
        {
            resultIndexes[idx++] = cur;
            cur = next[cur];
        }

        return count;
    }

    // --- Min-heap helpers (inline for performance) ---

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static unsafe void HeapSwap(int* heap, int* heapPos, int a, int b)
    {
        var tmp = heap[a];
        heap[a] = heap[b];
        heap[b] = tmp;
        heapPos[heap[a]] = a;
        heapPos[heap[b]] = b;
    }

    private static unsafe void HeapSiftUp(int* heap, int* heapPos, int* ranks, int idx)
    {
        while (idx > 0)
        {
            var parent = (idx - 1) >> 1;
            if (ranks[heap[idx]] < ranks[heap[parent]] ||
                (ranks[heap[idx]] == ranks[heap[parent]] && heap[idx] < heap[parent]))
            {
                HeapSwap(heap, heapPos, idx, parent);
                idx = parent;
            }
            else
            {
                break;
            }
        }
    }

    private static unsafe void HeapSiftDown(int* heap, int* heapPos, int* ranks, int heapSize, int idx)
    {
        while (true)
        {
            var smallest = idx;
            var left = 2 * idx + 1;
            var right = 2 * idx + 2;

            if (left < heapSize &&
                (ranks[heap[left]] < ranks[heap[smallest]] ||
                 (ranks[heap[left]] == ranks[heap[smallest]] && heap[left] < heap[smallest])))
            {
                smallest = left;
            }
            if (right < heapSize &&
                (ranks[heap[right]] < ranks[heap[smallest]] ||
                 (ranks[heap[right]] == ranks[heap[smallest]] && heap[right] < heap[smallest])))
            {
                smallest = right;
            }

            if (smallest != idx)
            {
                HeapSwap(heap, heapPos, idx, smallest);
                idx = smallest;
            }
            else
            {
                break;
            }
        }
    }

    private static unsafe void HeapRemove(int* heap, int* heapPos, int* ranks, ref int heapSize, int boundary)
    {
        var idx = heapPos[boundary];
        if (idx < 0 || idx >= heapSize)
        {
            return;
        }

        heapSize--;
        if (idx == heapSize)
        {
            heapPos[boundary] = -1;
            return;
        }

        heap[idx] = heap[heapSize];
        heapPos[heap[idx]] = idx;
        heapPos[boundary] = -1;

        HeapSiftDown(heap, heapPos, ranks, heapSize, idx);
        HeapSiftUp(heap, heapPos, ranks, idx);
    }

    private static unsafe void HeapUpdate(int* heap, int* heapPos, int* ranks, int heapSize, int boundary)
    {
        var idx = heapPos[boundary];
        if (idx < 0 || idx >= heapSize)
        {
            return;
        }

        HeapSiftDown(heap, heapPos, ranks, heapSize, idx);
        HeapSiftUp(heap, heapPos, ranks, idx);
    }
#endif

    internal static unsafe void BytePairEncode(Bytes piece, IReadOnlyDictionary<byte[], int> ranks, List<int> outList)
    {
        var partsLength = piece.GetLength() + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

            for (var i = 0; i < count; i++)
            {
                outList.Add(ranks[piece.GetSlice(partsIndexes[i], partsIndexes[i + 1])]);
            }
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

                for (var i = 0; i < count; i++)
                {
                    outList.Add(ranks[piece.GetSlice(partsIndexes[i], partsIndexes[i + 1])]);
                }
            }
        }
    }

#if NET8_0_OR_GREATER
    internal static unsafe void BytePairEncode(
        ReadOnlySpan<byte> pieceSpan,
        TokenEncoder encoder,
        List<int> outList)
    {
        var partsLength = pieceSpan.Length + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

            for (var i = 0; i < count; i++)
            {
                outList.Add(encoder[pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i])]);
            }
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

                for (var i = 0; i < count; i++)
                {
                    outList.Add(encoder[pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i])]);
                }
            }
        }
    }
#endif

    internal static unsafe int[] BytePairEncodeToArray(Bytes piece, IReadOnlyDictionary<byte[], int> ranks)
    {
        var partsLength = piece.GetLength() + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

            var result = new int[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = ranks[piece.GetSlice(partsIndexes[i], partsIndexes[i + 1])];
            }
            return result;
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

                var result = new int[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = ranks[piece.GetSlice(partsIndexes[i], partsIndexes[i + 1])];
                }
                return result;
            }
        }
    }

#if NET8_0_OR_GREATER
    internal static unsafe int[] BytePairEncodeToArray(
        ReadOnlySpan<byte> pieceSpan,
        TokenEncoder encoder)
    {
        var partsLength = pieceSpan.Length + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

            var result = new int[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = encoder[pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i])];
            }
            return result;
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

                var result = new int[count];
                for (var i = 0; i < count; i++)
                {
                    result[i] = encoder[pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i])];
                }
                return result;
            }
        }
    }
#endif

    internal static unsafe List<byte[]> BytePairExplore(Bytes piece, IReadOnlyDictionary<byte[], int> ranks)
    {
        var partsLength = piece.GetLength() + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

            var outList = new List<byte[]>(count);
            for (var i = 0; i < count; i++)
            {
                outList.Add(piece.GetSlice(partsIndexes[i], partsIndexes[i + 1]));
            }
            return outList;
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);

                var outList = new List<byte[]>(count);
                for (var i = 0; i < count; i++)
                {
                    outList.Add(piece.GetSlice(partsIndexes[i], partsIndexes[i + 1]));
                }
                return outList;
            }
        }
    }

#if NET8_0_OR_GREATER
    internal static unsafe List<byte[]> BytePairExplore(
        ReadOnlySpan<byte> pieceSpan,
        TokenEncoder encoder)
    {
        var partsLength = pieceSpan.Length + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

            var outList = new List<byte[]>(count);
            for (var i = 0; i < count; i++)
            {
                outList.Add(pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i]).ToArray());
            }
            return outList;
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                var count = FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);

                var outList = new List<byte[]>(count);
                for (var i = 0; i < count; i++)
                {
                    outList.Add(pieceSpan.Slice(partsIndexes[i], partsIndexes[i + 1] - partsIndexes[i]).ToArray());
                }
                return outList;
            }
        }
    }
#endif

    internal static unsafe int BytePairEncodeCountTokens(Bytes piece, IReadOnlyDictionary<byte[], int> ranks)
    {
        var partsLength = piece.GetLength() + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            return FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                return FindParts(piece, partsIndexes, partsRanks, partsLength, ranks);
            }
        }
    }

#if NET8_0_OR_GREATER
    internal static unsafe int BytePairEncodeCountTokens(
        ReadOnlySpan<byte> pieceSpan,
        TokenEncoder encoder)
    {
        var partsLength = pieceSpan.Length + 1;

        if (partsLength <= MaxStackAllocLength)
        {
            var partsIndexes = stackalloc int[partsLength];
            var partsRanks = stackalloc int[partsLength];
            return FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);
        }
        else
        {
            var heapIndexes = new int[partsLength];
            var heapRanks = new int[partsLength];
            fixed (int* partsIndexes = heapIndexes)
            fixed (int* partsRanks = heapRanks)
            {
                return FindParts(pieceSpan, partsIndexes, partsRanks, partsLength, encoder);
            }
        }
    }
#endif
}
