using System.Text.Json.Serialization;

namespace Tiktoken.Encodings;

[JsonSerializable(typeof(Tokenizer))]
internal partial class SourceGenerationContext : JsonSerializerContext;