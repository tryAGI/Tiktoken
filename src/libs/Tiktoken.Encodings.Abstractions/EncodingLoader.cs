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
        
        var dictionary = new Dictionary<byte[], int>(new ByteArrayComparer());
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var tokens = line.Split(' ');
            if (tokens.Length != 2)
            {
                throw new FormatException($"Invalid file format: {name}");
            }

            var tokenBytes = Convert.FromBase64String(tokens[0]);
            var rank = int.Parse(tokens[1], CultureInfo.InvariantCulture);
            dictionary[tokenBytes] = rank;
        }

        return dictionary;
    }
}