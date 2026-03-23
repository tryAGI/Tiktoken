using System.Text.Json;

namespace Tiktoken.Cli.Output;

internal static class JsonFormatter
{
    public static void WriteFileResults(
        IReadOnlyList<FileTokenResult> results,
        TextWriter writer)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteStartArray("files");
            foreach (var result in results)
            {
                json.WriteStartObject();
                json.WriteString("path", result.RelativePath);
                json.WriteNumber("tokens", result.Tokens);
                json.WriteEndObject();
            }
            json.WriteEndArray();
            json.WriteNumber("total", results.Sum(r => (long)r.Tokens));
            json.WriteEndObject();
        }

        writer.WriteLine(System.Text.Encoding.UTF8.GetString(stream.ToArray()));
    }
}
