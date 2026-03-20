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
    public static Dictionary<byte[], int> LoadEncodingFromManifestResource(
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

#if NET7_0_OR_GREATER
        Span<Range> ranges = stackalloc Range[3];
        Span<byte> bytes = stackalloc byte[256];
#endif

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

#if NET7_0_OR_GREATER
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
    /// Loads encoding from a binary .ttkb stream (compact format: no base64 decoding overhead).
    /// Format: [TTKB magic 4B][version uint32 LE][count uint32 LE][entries: rank int32 LE + len byte + token bytes]
    /// </summary>
    public static Dictionary<byte[], int> LoadEncodingFromBinaryStream(Stream stream)
    {
        stream = stream ?? throw new ArgumentNullException(nameof(stream));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        // Read entire stream into buffer for span-based parsing (one I/O call)
        byte[] buffer;
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            // Zero-copy for MemoryStream (common for manifest resources)
            return ParseBinaryEncoding(segment.AsSpan());
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
        return ParseBinaryEncoding(buffer.AsSpan());
#else
        using var reader = new BinaryReader(stream);

        // Read and validate header
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
        var dictionary = new Dictionary<byte[], int>(count, new ByteArrayComparer());

        for (var i = 0; i < count; i++)
        {
            var rank = reader.ReadInt32();
            var tokenLength = reader.ReadByte();
            var tokenBytes = reader.ReadBytes(tokenLength);
            dictionary[tokenBytes] = rank;
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
    public static Dictionary<byte[], int> LoadEncodingFromBinaryData(byte[] data)
    {
        data = data ?? throw new ArgumentNullException(nameof(data));

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        return ParseBinaryEncoding(data.AsSpan());
#else
        using var stream = new MemoryStream(data, writable: false);
        return LoadEncodingFromBinaryStream(stream);
#endif
    }

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
    private static Dictionary<byte[], int> ParseBinaryEncoding(ReadOnlySpan<byte> span)
    {
        // Validate header
        if (span.Length < 12 ||
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
        var dictionary = new Dictionary<byte[], int>(count, new ByteArrayComparer());

        var offset = 12;
        for (var i = 0; i < count; i++)
        {
            var rank = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset));
            offset += 4;
            var tokenLength = span[offset];
            offset += 1;
            var tokenBytes = span.Slice(offset, tokenLength).ToArray();
            offset += tokenLength;
            dictionary[tokenBytes] = rank;
        }

        return dictionary;
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

        using var writer = new BinaryWriter(stream);

        // Header
        writer.Write((byte)'T');
        writer.Write((byte)'T');
        writer.Write((byte)'K');
        writer.Write((byte)'B');
        writer.Write((uint)1);             // version
        writer.Write((uint)encoder.Count);  // count

        // Entries
        foreach (var kvp in encoder)
        {
            if (kvp.Key.Length > 255)
            {
                throw new ArgumentException(
                    $"Token at rank {kvp.Value} is {kvp.Key.Length} bytes (max 255).");
            }

            writer.Write(kvp.Value);            // rank (int32 LE)
            writer.Write((byte)kvp.Key.Length);  // token length (uint8)
            writer.Write(kvp.Key);               // raw token bytes
        }
    }

    /// <summary>
    /// Loads encoding from a file path, auto-detecting format by extension.
    /// Files ending in .ttkb are loaded as binary; all others are loaded as .tiktoken text.
    /// </summary>
    /// <param name="path">Path to the encoding file (.ttkb or .tiktoken).</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static Dictionary<byte[], int> LoadEncodingFromFile(string path)
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
    public static async Task<Dictionary<byte[], int>> LoadEncodingFromFileAsync(
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
    public static async Task<Dictionary<byte[], int>> LoadEncodingFromBinaryFileAsync(
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
    public static async Task<Dictionary<byte[], int>> LoadEncodingFromTextFileAsync(
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
    public static Dictionary<byte[], int> LoadEncodingFromLines(
        this IReadOnlyList<string> lines,
        string name)
    {
        lines = lines ?? throw new ArgumentNullException(nameof(lines));

#if NET7_0_OR_GREATER
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

#if NET7_0_OR_GREATER
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

#if NET7_0_OR_GREATER
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