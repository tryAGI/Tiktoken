using System.Globalization;
using System.Reflection;
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using System.Buffers.Binary;
#endif

namespace Tiktoken.Encodings;

/// <summary>
///
/// </summary>
public static class EncodingLoader
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="FormatException"></exception>
    public static IReadOnlyDictionary<byte[], int> LoadEncodingFromManifestResource(
        this Assembly assembly,
        string name)
    {
        assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        name = name ?? throw new ArgumentNullException(nameof(name));

        var resourceNames = assembly.GetManifestResourceNames();

        // Prefer binary format (.ttkb) for faster loading
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        var binaryName = name.Replace(".tiktoken", ".ttkb", StringComparison.Ordinal);
#else
        var binaryName = name.Replace(".tiktoken", ".ttkb");
#endif
        var binaryResourcePath = resourceNames
            .FirstOrDefault(x => x.EndsWith(binaryName, StringComparison.OrdinalIgnoreCase));
        if (binaryResourcePath != null)
        {
            using var binaryStream =
                assembly.GetManifestResourceStream(binaryResourcePath) ??
                throw new InvalidOperationException("Resource not found.");
            return LoadEncodingFromBinaryStream(binaryStream);
        }

        // Fall back to text format (.tiktoken)
        var resourcePath = resourceNames
            .Single(x => x.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(resourcePath) ??
            throw new InvalidOperationException("Resource not found.");
        using var reader = new StreamReader(stream);

        // Pre-allocate: ~17 bytes per line on average for .tiktoken files
        var estimatedCapacity = stream.CanSeek ? (int)(stream.Length / 17) : 16384;
        var dictionary = new Dictionary<byte[], int>(estimatedCapacity, new ByteArrayComparer());

#if NET8_0_OR_GREATER
        Span<Range> ranges = stackalloc Range[3];
        Span<byte> bytes = stackalloc byte[256];
#endif

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

#if NET8_0_OR_GREATER
            var splitCount = line.AsSpan().Split(ranges, ' ');
            if (splitCount != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }
            Convert.TryFromBase64Chars(line.AsSpan(ranges[0]), bytes, out var bytesWritten);
            var tokenBytes = bytes.Slice(0, bytesWritten).ToArray();
            var rank = int.Parse(line.AsSpan(ranges[1]), CultureInfo.InvariantCulture);
#else
            var tokens = line.Split(' ');
            if (tokens.Length != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }
            var tokenBytes = Convert.FromBase64String(tokens[0]);
            var rank = int.Parse(tokens[1], CultureInfo.InvariantCulture);
#endif
            dictionary[tokenBytes] = rank;
        }

        return dictionary;
    }

    /// <summary>
    /// Loads encoding from a binary .ttkb stream.
    /// Format: header (28B) + buckets + ranks + offsets + lengths + key blob.
    /// </summary>
    public static IReadOnlyDictionary<byte[], int> LoadEncodingFromBinaryStream(Stream stream)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        // Read entire stream into buffer for span-based parsing (one I/O call)
        byte[] buffer;
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            // Zero-copy for MemoryStream (common for manifest resources)
#if NET8_0_OR_GREATER
            return ParseBinaryEncodingToArrays(segment.AsSpan());
#else
            return ParseBinaryEncoding(segment.AsSpan());
#endif
        }

        if (stream.CanSeek)
        {
            var remaining = (int)(stream.Length - stream.Position);
            buffer = new byte[remaining];
            var totalRead = 0;
            while (totalRead < remaining)
            {
                var read = stream.Read(buffer, totalRead, remaining - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
        }
        else
        {
            using var tempMs = new MemoryStream();
            stream.CopyTo(tempMs);
            buffer = tempMs.ToArray();
        }
#if NET8_0_OR_GREATER
        return ParseBinaryEncodingToArrays(buffer.AsSpan());
#else
        return ParseBinaryEncoding(buffer.AsSpan());
#endif
#else
        using var reader = new BinaryReader(stream);

        // Read and validate header (28 bytes)
        var magic = reader.ReadBytes(4);
        if (magic.Length < 4 || magic[0] != (byte)'T' || magic[1] != (byte)'T' ||
            magic[2] != (byte)'K' || magic[3] != (byte)'B')
        {
            throw new FormatException("Invalid binary encoding file: bad magic.");
        }

        var version = reader.ReadUInt32();
        if (version != 1)
        {
            throw new FormatException($"Unsupported binary encoding version: {version}");
        }

        var count = (int)reader.ReadUInt32();
        var tableSize = (int)reader.ReadUInt32();
        reader.ReadUInt32(); // mask (unused on pre-NET8)
        var keyBlobSize = (int)reader.ReadUInt32();
        reader.ReadUInt32(); // flags

        // Skip buckets (pre-NET8 doesn't use them)
        reader.ReadBytes(tableSize * 4);

        // Read ranks
        var ranks = new int[count];
        for (var i = 0; i < count; i++)
            ranks[i] = reader.ReadInt32();

        // Read key offsets
        var keyOffsets = new int[count];
        for (var i = 0; i < count; i++)
            keyOffsets[i] = reader.ReadInt32();

        // Read key lengths
        var keyLengths = reader.ReadBytes(count);

        // Read key blob
        var keyBlob = reader.ReadBytes(keyBlobSize);

        // Build dictionary
        var dictionary = new Dictionary<byte[], int>(count, new ByteArrayComparer());
        for (var i = 0; i < count; i++)
        {
            var tokenBytes = new byte[keyLengths[i]];
            Array.Copy(keyBlob, keyOffsets[i], tokenBytes, 0, keyLengths[i]);
            dictionary[tokenBytes] = ranks[i];
        }

        return dictionary;
#endif
    }

    /// <summary>
    /// Loads encoding from a binary .ttkb byte array.
    /// </summary>
    /// <param name="data">Binary .ttkb data.</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static IReadOnlyDictionary<byte[], int> LoadEncodingFromBinaryData(byte[] data)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));

#if NET8_0_OR_GREATER
        return ParseBinaryEncodingToArrays(data.AsSpan());
#elif NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return ParseBinaryEncoding(data.AsSpan());
#else
        using var stream = new MemoryStream(data, writable: false);
        return LoadEncodingFromBinaryStream(stream);
#endif
    }

#if (NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER) && !NET8_0_OR_GREATER
    private static Dictionary<byte[], int> ParseBinaryEncoding(ReadOnlySpan<byte> span)
    {
        // Validate header (28 bytes)
        if (span.Length < 28 ||
            span[0] != (byte)'T' || span[1] != (byte)'T' ||
            span[2] != (byte)'K' || span[3] != (byte)'B')
        {
            throw new FormatException("Invalid binary encoding file: bad magic.");
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4));
        if (version != 1)
        {
            throw new FormatException($"Unsupported binary encoding version: {version}");
        }

        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8));
        var tableSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12));
        var keyBlobSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20));

        // Skip header + buckets
        var off = 28 + tableSize * 4;

        // Read ranks
        var ranks = new int[count];
        for (var i = 0; i < count; i++)
        {
            ranks[i] = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(off));
            off += 4;
        }

        // Read key offsets
        var keyOffsets = new int[count];
        for (var i = 0; i < count; i++)
        {
            keyOffsets[i] = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(off));
            off += 4;
        }

        // Key lengths and blob
        var keyLengths = span.Slice(off, count);
        off += count;
        var keyBlob = span.Slice(off, keyBlobSize);

        // Build dictionary
        var dictionary = new Dictionary<byte[], int>(count, new ByteArrayComparer());
        for (var i = 0; i < count; i++)
        {
            dictionary[keyBlob.Slice(keyOffsets[i], keyLengths[i]).ToArray()] = ranks[i];
        }

        return dictionary;
    }
#endif

#if NET8_0_OR_GREATER
    private static EncodingData ParseBinaryEncodingToArrays(ReadOnlySpan<byte> span)
    {
        // Validate header (28 bytes)
        if (span.Length < 28 ||
            span[0] != (byte)'T' || span[1] != (byte)'T' ||
            span[2] != (byte)'K' || span[3] != (byte)'B')
        {
            throw new FormatException("Invalid binary encoding file: bad magic.");
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4));
        if (version != 1)
        {
            throw new FormatException($"Unsupported binary encoding version: {version}");
        }

        var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8));
        var tableSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(12));
        var mask = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(16));
        var keyBlobSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20));

        var off = 28;

        // Bulk-copy pre-computed hash table buckets
        var buckets = System.Runtime.InteropServices.MemoryMarshal
            .Cast<byte, int>(span.Slice(off, tableSize * 4)).ToArray();
        off += tableSize * 4;

        // Bulk-copy ranks
        var ranks = System.Runtime.InteropServices.MemoryMarshal
            .Cast<byte, int>(span.Slice(off, count * 4)).ToArray();
        off += count * 4;

        // Bulk-copy key offsets
        var offsets = new int[count];
        System.Runtime.InteropServices.MemoryMarshal
            .Cast<byte, int>(span.Slice(off, count * 4)).CopyTo(offsets);
        off += count * 4;

        // Bulk-copy key lengths
        var tokenLengths = span.Slice(off, count).ToArray();
        off += count;

        // Bulk-copy key blob
        var data = span.Slice(off, keyBlobSize).ToArray();

        return new EncodingData(data, offsets, tokenLengths, ranks, buckets, mask);
    }
#endif

    /// <summary>
    /// Writes encoding data in binary .ttkb format to a stream.
    /// Can be used to convert custom .tiktoken text data to the faster binary format.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    /// <param name="encoder">The encoding dictionary (token bytes to rank).</param>
    /// <exception cref="ArgumentException">Thrown when any token exceeds 255 bytes.</exception>
    public static void WriteEncodingToBinaryStream(
        Stream stream,
        IReadOnlyDictionary<byte[], int> encoder)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));
        encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));

#if NET8_0_OR_GREATER
        // Fast path: if input is EncodingData, use its flat arrays directly (zero enumeration)
        if (encoder is EncodingData encodingData)
        {
            WriteFromFlatArrays(
                stream,
                encodingData._data, encodingData._offsets, encodingData._tokenLengths,
                encodingData._ranks, encodingData.Count);
            return;
        }
#endif

        var count = encoder.Count;

        // Single pass: validate, compute total size, and copy data
        // Use List<byte> for key blob since we don't know total size upfront
        var totalKeyBytes = 0;
        foreach (var kvp in encoder)
        {
            totalKeyBytes += kvp.Key.Length;
        }

        var keyBlobArray = new byte[totalKeyBytes];
        var keyOffsets = new int[count];
        var keyLengths = new byte[count];
        var ranks = new int[count];

        var idx = 0;
        var blobOffset = 0;
        foreach (var kvp in encoder)
        {
            if (kvp.Key.Length > 255)
            {
                throw new ArgumentException(
                    $"Token at rank {kvp.Value} is {kvp.Key.Length} bytes (max 255).");
            }

            keyOffsets[idx] = blobOffset;
            keyLengths[idx] = (byte)kvp.Key.Length;
            ranks[idx] = kvp.Value;
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            kvp.Key.AsSpan().CopyTo(keyBlobArray.AsSpan(blobOffset));
#else
            Array.Copy(kvp.Key, 0, keyBlobArray, blobOffset, kvp.Key.Length);
#endif
            blobOffset += kvp.Key.Length;
            idx++;
        }

        WriteFlatLayoutToStream(stream, keyBlobArray, keyOffsets, keyLengths, ranks, count);
    }

#if NET8_0_OR_GREATER
    private static void WriteFromFlatArrays(
        Stream stream,
        byte[] data, int[] offsets, byte[] tokenLengths, int[] ranks, int count)
    {
        // Validate token lengths
        for (var i = 0; i < count; i++)
        {
            if (tokenLengths[i] > 255)
            {
                throw new ArgumentException(
                    $"Token at rank {ranks[i]} is {tokenLengths[i]} bytes (max 255).");
            }
        }

        WriteFlatLayoutToStream(stream, data, offsets, tokenLengths, ranks, count);
    }
#endif

    private static void WriteFlatLayoutToStream(
        Stream stream,
        byte[] keyBlobArray, int[] keyOffsets, byte[] keyLengths, int[] ranks, int count)
    {
        // Build hash table (FNV-1a + triangular probing)
        var tableSize = RoundUpPowerOf2((uint)(count * 3 / 2));
        if (tableSize < 16) tableSize = 16;
        var mask = (int)(tableSize - 1);
        var buckets = new int[tableSize];
        for (var i = 0; i < buckets.Length; i++) buckets[i] = -1;

        for (var i = 0; i < count; i++)
        {
            var bucket = (int)(FnvHash(keyBlobArray, keyOffsets[i], keyLengths[i]) & (uint)mask);
            var step = 1;
            while (buckets[bucket] != -1)
            {
                bucket = (bucket + step) & mask;
                step++;
            }
            buckets[bucket] = i;
        }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        // Header (28 bytes)
        Span<byte> header = stackalloc byte[28];
        header[0] = (byte)'T'; header[1] = (byte)'T'; header[2] = (byte)'K'; header[3] = (byte)'B';
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4), 1);                         // version
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(8), (uint)count);               // entryCount
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(12), (uint)tableSize);          // tableSize
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(16), (uint)mask);               // mask
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(20), (uint)keyBlobArray.Length); // keyBlobSize
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(24), 0);                        // flags
        stream.Write(header);

        // Bulk-write int arrays as raw bytes
        stream.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(buckets.AsSpan()));
        stream.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(ranks.AsSpan()));
        stream.Write(System.Runtime.InteropServices.MemoryMarshal.AsBytes(keyOffsets.AsSpan()));
        stream.Write(keyLengths);
        stream.Write(keyBlobArray);
#else
        using var writer = new BinaryWriter(stream);

        // Header (28 bytes)
        writer.Write((byte)'T'); writer.Write((byte)'T'); writer.Write((byte)'K'); writer.Write((byte)'B');
        writer.Write((uint)1);                   // version
        writer.Write((uint)count);               // entryCount
        writer.Write((uint)tableSize);           // tableSize
        writer.Write((uint)mask);                // mask
        writer.Write((uint)keyBlobArray.Length);  // keyBlobSize
        writer.Write((uint)0);                   // flags

        // Per-element write (no MemoryMarshal on older targets)
        foreach (var b in buckets) writer.Write(b);
        foreach (var r in ranks) writer.Write(r);
        foreach (var o in keyOffsets) writer.Write(o);
        writer.Write(keyLengths);
        writer.Write(keyBlobArray);
#endif
    }

    // FNV-1a hash — matches C# TokenEncoder.FnvHash and Python convert_to_ttkb.py
    private static uint FnvHash(byte[] data, int offset, int length)
    {
        var hash = 2166136261u;
        for (var i = 0; i < length; i++)
        {
            hash ^= data[offset + i];
            hash *= 16777619u;
        }
        return hash;
    }

    private static uint RoundUpPowerOf2(uint value)
    {
        if (value <= 1) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    /// Loads encoding from a file path, auto-detecting format by extension.
    /// Files ending in .ttkb are loaded as binary; all others are loaded as .tiktoken text.
    /// </summary>
    /// <param name="path">Path to the encoding file (.ttkb or .tiktoken).</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static IReadOnlyDictionary<byte[], int> LoadEncodingFromFile(string path)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

        if (path.EndsWith(".ttkb", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = File.OpenRead(path);
            return LoadEncodingFromBinaryStream(stream);
        }

        var lines = File.ReadAllLines(path);
        return LoadEncodingFromLines(lines, Path.GetFileName(path));
    }

    /// <summary>
    /// Asynchronously loads encoding from a file path, auto-detecting format by extension.
    /// Files ending in .ttkb are loaded as binary; all others are loaded as .tiktoken text.
    /// </summary>
    /// <param name="path">Path to the encoding file (.ttkb or .tiktoken).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static async Task<IReadOnlyDictionary<byte[], int>> LoadEncodingFromFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

        if (path.EndsWith(".ttkb", StringComparison.OrdinalIgnoreCase))
        {
            return await LoadEncodingFromBinaryFileAsync(path, cancellationToken)
                .ConfigureAwait(false);
        }

        return await LoadEncodingFromTextFileAsync(path, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously loads encoding from a binary .ttkb file.
    /// </summary>
    /// <param name="path">Path to the .ttkb file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static async Task<IReadOnlyDictionary<byte[], int>> LoadEncodingFromBinaryFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

#if NET6_0_OR_GREATER
        var data = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
#else
        var data = await Task.Run(() => File.ReadAllBytes(path), cancellationToken).ConfigureAwait(false);
#endif
        return LoadEncodingFromBinaryData(data);
    }

    /// <summary>
    /// Asynchronously loads encoding from a .tiktoken text file.
    /// </summary>
    /// <param name="path">Path to the .tiktoken file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static async Task<IReadOnlyDictionary<byte[], int>> LoadEncodingFromTextFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        path = path ?? throw new ArgumentNullException(nameof(path));

#if NET6_0_OR_GREATER
        var lines = await File.ReadAllLinesAsync(path, cancellationToken).ConfigureAwait(false);
#else
        var lines = await Task.Run(() => File.ReadAllLines(path), cancellationToken).ConfigureAwait(false);
#endif
        return LoadEncodingFromLines(lines, Path.GetFileName(path));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="FormatException"></exception>
    public static IReadOnlyDictionary<byte[], int> LoadEncodingFromLines(
        this IReadOnlyList<string> lines,
        string name)
    {
        lines = lines ?? throw new ArgumentNullException(nameof(lines));

#if NET8_0_OR_GREATER
        Span<Range> tokens = stackalloc Range[3];
        Span<byte> bytes = stackalloc byte[256];
#endif
        var dictionary = new Dictionary<byte[], int>(lines.Count, new ByteArrayComparer());
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

#if NET8_0_OR_GREATER
            var splitCount = line.AsSpan().Split(tokens, ' ');
            if (splitCount != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }
#else
            var tokens = line.Split(' ');
            if (tokens.Length != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }
#endif

#if NET8_0_OR_GREATER
            Convert.TryFromBase64Chars(line.AsSpan(tokens[0]), bytes, out var bytesWritten);
            var tokenBytes = bytes.Slice(0, bytesWritten).ToArray();
            var rank = int.Parse(line.AsSpan(tokens[1]), CultureInfo.InvariantCulture);
#else
            var tokenBytes = Convert.FromBase64String(tokens[0]);
            var rank = int.Parse(tokens[1], CultureInfo.InvariantCulture);
#endif
            dictionary[tokenBytes] = rank;
        }

        return dictionary;
    }
}
