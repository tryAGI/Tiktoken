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
        
        var resourcePath = assembly
            .GetManifestResourceNames()
            .Single(x => x.EndsWith(name, StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(resourcePath) ??
            throw new InvalidOperationException("Resource not found.");
        using var reader = new StreamReader(stream);

        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return LoadEncodingFromLines(lines, name);
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
        var dictionary = new Dictionary<byte[], int>(new ByteArrayComparer());
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