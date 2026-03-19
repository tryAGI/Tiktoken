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

    private static async Task<int> GetServerTokenCount(HttpClient httpClient, string requestJson)
    {
        var response = await httpClient.PostAsync(
            "https://api.openai.com/v1/responses/input_tokens",
            new StringContent(requestJson, Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("input_tokens").GetInt32();
    }

    private static HttpClient CreateOpenAiClient(string apiKey)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return httpClient;
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

        using var httpClient = CreateOpenAiClient(apiKey);

        var requestJson = JsonSerializer.Serialize(new
        {
            model = "gpt-4o-mini",
            input = "Tell me a joke about programming.",
        });

        var serverTokens = await GetServerTokenCount(httpClient, requestJson);

        var encoder = ModelToEncoder.For("gpt-4o-mini");
        var localTokens = encoder.CountTokens("Tell me a joke about programming.");

        // The server count includes system prompt overhead, so server >= local
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

        using var httpClient = CreateOpenAiClient(apiKey);

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

        var serverTokens = await GetServerTokenCount(httpClient, requestJson);

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

        Console.WriteLine($"Server tokens: {serverTokens}, Local tokens: {localTokens}, Diff: {serverTokens - localTokens}");

        // Allow ±20% tolerance since the formula is reverse-engineered
        var tolerance = Math.Max(serverTokens * 0.2, 5);
        localTokens.Should().BeCloseTo(serverTokens, (uint)tolerance,
            $"Local estimate ({localTokens}) should be close to server count ({serverTokens})");
    }

    [TestMethod]
    public async Task ValidateMessagesOnlyAgainstOpenAiApi()
    {
        var apiKey = GetOpenAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive("OPENAI_API_KEY not set — skipping integration test.");
            return;
        }

        using var httpClient = CreateOpenAiClient(apiKey);

        // Messages-only test (no tools) to validate message counting
        var requestJson = """
        {
            "model": "gpt-4o-mini",
            "input": [
                {"role": "developer", "content": "You are a helpful assistant."},
                {"role": "user", "content": "hello world"}
            ]
        }
        """;

        var serverTokens = await GetServerTokenCount(httpClient, requestJson);

        var encoder = ModelToEncoder.For("gpt-4o-mini");
        var messages = new List<ChatMessage>
        {
            new("developer", "You are a helpful assistant."),
            new("user", "hello world"),
        };

        var localTokens = encoder.CountMessageTokens(messages);

        Console.WriteLine($"Messages-only: Server={serverTokens}, Local={localTokens}, Diff={serverTokens - localTokens}");

        // Messages-only should be very close
        localTokens.Should().BeCloseTo(serverTokens, 3,
            $"Local ({localTokens}) should be very close to server ({serverTokens}) for messages-only");
    }

    [TestMethod]
    public async Task ValidateToolsOnlyAgainstOpenAiApi()
    {
        var apiKey = GetOpenAiApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Inconclusive("OPENAI_API_KEY not set — skipping integration test.");
            return;
        }

        using var httpClient = CreateOpenAiClient(apiKey);

        // Test no-params tool
        var requestJson = """
        {
            "model": "gpt-4o-mini",
            "input": [{"role": "user", "content": "hi"}],
            "tools": [
                {
                    "type": "function",
                    "name": "get_time",
                    "description": "Get the current time"
                }
            ]
        }
        """;

        var serverTokens = await GetServerTokenCount(httpClient, requestJson);

        var encoder = ModelToEncoder.For("gpt-4o-mini");
        var messages = new List<ChatMessage> { new("user", "hi") };
        var tools = new List<ChatFunction> { new("get_time", "Get the current time") };

        var localTokens = encoder.CountMessageTokens(messages, tools);
        var toolTokensOnly = encoder.CountToolTokens(tools);

        Console.WriteLine($"No-params tool: Server={serverTokens}, Local={localTokens}, ToolTokens={toolTokensOnly}, Diff={serverTokens - localTokens}");

        var tolerance = Math.Max(serverTokens * 0.2, 5);
        localTokens.Should().BeCloseTo(serverTokens, (uint)tolerance,
            $"Local ({localTokens}) should be close to server ({serverTokens})");
    }

}
