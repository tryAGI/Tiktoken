using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tiktoken.UnitTests;

public partial class Tests
{
    private static string? GetOpenAiApiKey()
    {
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    [TestMethod]
    public async Task ValidateMessageTokensAgainstOpenAiApi()
    {
        var apiKey = GetOpenAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive("OPENAI_API_KEY not set — skipping integration test.");
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // Test: simple text input
        var requestJson = JsonSerializer.Serialize(new
        {
            model = "gpt-4o-mini",
            input = "Tell me a joke about programming.",
        });

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/responses/input_tokens",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var serverTokens = doc.RootElement.GetProperty("input_tokens").GetInt32();

        // Local count for the same text
        var encoder = ModelToEncoder.For("gpt-4o-mini");
        var localTokens = encoder.CountTokens("Tell me a joke about programming.");

        // The server count includes system prompt overhead, so server >= local
        // We just verify our local count is reasonable (within the server count)
        localTokens.Should().BeGreaterThan(0);
        serverTokens.Should().BeGreaterThanOrEqualTo(localTokens,
            $"Server returned {serverTokens} tokens, local counted {localTokens}");
    }

    [TestMethod]
    public async Task ValidateMessageTokensWithToolsAgainstOpenAiApi()
    {
        var apiKey = GetOpenAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive("OPENAI_API_KEY not set — skipping integration test.");
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestJson = """
        {
            "model": "gpt-4o-mini",
            "input": [
                {"role": "user", "content": "What is the weather in Paris?"}
            ],
            "tools": [
                {
                    "type": "function",
                    "name": "get_weather",
                    "description": "Get the current weather for a location",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "location": {
                                "type": "string",
                                "description": "The city and state"
                            },
                            "unit": {
                                "type": "string",
                                "enum": ["celsius", "fahrenheit"]
                            }
                        },
                        "required": ["location"]
                    }
                }
            ]
        }
        """;

        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/responses/input_tokens",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        var serverTokens = doc.RootElement.GetProperty("input_tokens").GetInt32();

        // Local count
        var encoder = ModelToEncoder.For("gpt-4o-mini");
        var messages = new List<ChatMessage>
        {
            new("user", "What is the weather in Paris?"),
        };
        var tools = new List<ChatFunction>
        {
            new("get_weather", "Get the current weather for a location", new List<FunctionParameter>
            {
                new("location", "string", "The city and state", isRequired: true),
                new("unit", "string", "", enumValues: new[] { "celsius", "fahrenheit" }),
            }),
        };

        var localTokens = encoder.CountMessageTokens(messages, tools);

        // Log both values for debugging
        Console.WriteLine($"Server tokens: {serverTokens}, Local tokens: {localTokens}, Diff: {serverTokens - localTokens}");

        // Allow reasonable tolerance (±20%) since the formula is reverse-engineered
        var tolerance = Math.Max(serverTokens * 0.2, 5);
        localTokens.Should().BeCloseTo(serverTokens, (uint)tolerance,
            $"Local estimate ({localTokens}) should be close to server count ({serverTokens})");
    }
}
