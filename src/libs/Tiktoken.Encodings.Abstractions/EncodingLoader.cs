using System.Globalization;
using System.Reflection;

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

        var resourceNames = assembly.GetManifestResourceNames();

        // Prefer binary format (.ttkb) for faster loading
        var binaryName = name.Replace(".tiktoken", ".ttkb");
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