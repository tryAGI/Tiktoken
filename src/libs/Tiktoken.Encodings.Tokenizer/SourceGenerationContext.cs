using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

[JsonSerializable(typeof(TokenizerJson))]
internal partial class SourceGenerationContext : JsonSerializerContext;
