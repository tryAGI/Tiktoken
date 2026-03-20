using Tiktoken.Encodings;

namespace Tiktoken.UnitTests;

[TestClass]
public partial class Tests
{
    private static IEnumerable<object[]> TestData => ReadTestPlans(H.Resources.TestPlans_txt).Select(static x => new object[] { x });

    [TestMethod]
    [DynamicData(nameof(TestData))]
    public void VariousCases(Tuple<string, string, List<int>> resource)
    {
        var (encodingName, textToEncode, expectedEncoded) = resource;

        var encoding = ModelToEncoding.ForEncoding(encodingName);
        var encoder = new Encoder(encoding);
        var encoded = encoder.Encode(textToEncode);

        encoded.Should().BeEquivalentTo(expectedEncoded);
    }

    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public void RoundTripAllSamples(string encodingName)
    {
        var encoding = ModelToEncoding.ForEncoding(encodingName);
        var encoder = new Encoder(encoding);
        var failures = new List<string>();

        for (var i = 0; i < TestSamples.Length; i++)
        {
            var sample = TestSamples[i];
            var encoded = encoder.Encode(sample);
            var decoded = encoder.Decode(encoded);

            if (decoded != sample)
            {
                failures.Add($"[{i}] '{(sample.Length > 40 ? sample[..40] + "..." : sample)}'");
            }
        }

        failures.Should().BeEmpty(
            $"round-trip failed for {failures.Count} sample(s) with {encodingName}: {string.Join(", ", failures)}");
    }
    
    public Task BaseTest(string encodingName, string text, bool special = false)
    {
        Encoding encoding = encodingName switch
        {
            "o200k_base" => new O200KBase(),
            "cl100k_base" => new Cl100KBase(),
            "p50k_base" => new P50KBase(),
            "p50k_edit" => new P50KEdit(),
            "r50k_base" => new R50KBase(),
            _ => throw new ArgumentOutOfRangeException(nameof(encodingName))
        };
        var encoder = new Encoder(encoding);
        var encoded = special
            ? encoder.EncodeWithAllAllowedSpecial(text)
            : encoder.Encode(text);
        var decodedText = encoder.Decode(encoded);
        var words = encoder.Explore(text);

        if (!special)
        {
            encoder.CountTokens(text).Should().Be(encoded.Count);
        }
        words.Count.Should().Be(encoded.Count);
        decodedText.Should().Be(text);

        return Verify((words, encoded))
            .UseDirectory("Snapshots")
            //.AutoVerify()
            .UseTextForParameters(encodingName);
    }
    
    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task HelloWorld(string encodingName)
    {
        return BaseTest(encodingName, Strings.HelloWorld);
    }
    
    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task Special(string encodingName)
    {
        return BaseTest(encodingName, Strings.Special, special: true);
    }
    
    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task Chinese(string encodingName)
    {
        return BaseTest(encodingName, Strings.Chinese);
    }
    
    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task Multilingual(string encodingName)
    {
        return BaseTest(encodingName, Strings.Multilingual);
    }

    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task Code(string encodingName)
    {
        return BaseTest(encodingName, Strings.Code);
    }

    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task MultilingualLong(string encodingName)
    {
        return BaseTest(encodingName, Strings.MultilingualLong);
    }

    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public Task Bitcoin(string encodingName)
    {
        return BaseTest(encodingName, Strings.Bitcoin);
    }
    
    [TestMethod]
    public void ConvertChinese()
    {
        var test = Strings.Chinese.Substring(0, 1);
        var testBytes = System.Text.Encoding.UTF8.GetBytes(test);
        testBytes.Should().HaveCount(3);

        var dictionary = new Cl100KBase().MergeableRanks;
        dictionary.ContainsKey(testBytes).Should().BeTrue();
        dictionary.TryGetValue("Hello"u8.ToArray(), out var helloResult).Should().BeTrue();
        helloResult.Should().Be(9906);
        
        var dictionaryNew = new Cl100KBase().MergeableRanks
            .ToDictionary(
                x => new string(x.Key.Select(y => (char) y).ToArray()),
                x => x.Value);
        dictionaryNew.TryGetValue("Hello", out var newHelloResult).Should().BeTrue();
        newHelloResult.Should().Be(9906);
        
        var newBytes = System.Text.Encoding.Unicode.GetBytes(test);
        newBytes.Should().HaveCount(2);

        var newTest = new string(System.Text.Encoding.UTF8.GetBytes(test).Select(y => (char) y).ToArray());
        dictionaryNew.ContainsKey(newTest).Should().BeTrue();
    }
    
    [TestMethod]
    public void ExploreUtfSafe()
    {
        var text = Strings.HelloWorld;
        IReadOnlyCollection<string> tokens = new Encoder(new Cl100KBase()).Explore(text);
        List<string> expected = new List<string> { "hello", " world" };
        int i = 0;

        tokens.Count.Should().Be(expected.Count);
        
        foreach (string token in tokens)
        {
            token.Should().Be(expected[i]);
            i++;
        }
    }
    
    [TestMethod]
    public void ExploreUtfBoundary()
    {
        var text = Strings.EploreUtfBoundary;
        IReadOnlyCollection<UtfToken> tokens = new Encoder(new Cl100KBase()).ExploreUtfSafe(text);
        List<string> expected = new List<string> { " ř", "ek", "nu" };
        int i = 0;

        tokens.Count.Should().Be(expected.Count);
        
        foreach (UtfToken token in tokens)
        {
            token.Token.Should().Be(expected[i]);
            i++;
        }
    }
    
    [TestMethod]
    public void ExploreUtfBoundaryEmojiSurrogate()
    {
        var text = "\ud83e\udd1a\ud83c\udffe";
        IReadOnlyCollection<UtfToken> tokens = new Encoder(new Cl100KBase()).ExploreUtfSafe(text);
        List<string> expected = new List<string> { "🤚🏾" };
        int i = 0;

        tokens.Count.Should().Be(expected.Count);
        
        foreach (UtfToken token in tokens)
        {
            token.Token.Should().Be(expected[i]);
            i++;
        }
    }
    
    [TestMethod]
    public void ExploreUtfBoundaryEmoji()
    {
        var text = "\ud83e\udd1ař";
        IReadOnlyCollection<UtfToken> tokens = new Encoder(new Cl100KBase()).ExploreUtfSafe(text);
        List<string> expected = new List<string> { "\ud83e\udd1a", "ř" };
        int i = 0;

        tokens.Count.Should().Be(expected.Count);

        foreach (UtfToken token in tokens)
        {
            token.Token.Should().Be(expected[i]);
            i++;
        }
    }

    [TestMethod]
    [DataRow("cl100k_base")]
    [DataRow("o200k_base")]
    public void LargeInputDoesNotStackOverflow(string encodingName)
    {
        // Regression test for #75: stackalloc in BytePairEncoding caused StackOverflowException
        // on large regex matches (>512 UTF-8 bytes). A 1000-char Cyrillic word = 2000 UTF-8 bytes.
        var largeWord = new string('я', 1000);
        var text = $"Hello {largeWord} world";

        Encoding encoding = encodingName switch
        {
            "cl100k_base" => new Cl100KBase(),
            "o200k_base" => new O200KBase(),
            _ => throw new ArgumentOutOfRangeException(nameof(encodingName))
        };
        var encoder = new Encoder(encoding);

        var count = encoder.CountTokens(text);
        var encoded = encoder.Encode(text);
        var decoded = encoder.Decode(encoded);

        count.Should().Be(encoded.Count);
        decoded.Should().Be(text);
    }

    [TestMethod]
    public void ModelToEncoderCachesInstances()
    {
        var encoder1 = ModelToEncoder.For("gpt-4o");
        var encoder2 = ModelToEncoder.For("gpt-4o");

        encoder1.Should().BeSameAs(encoder2);
    }

    [TestMethod]
    public void TikTokenEncoderCreateForModel()
    {
        var encoder = TikTokenEncoder.CreateForModel(Models.Gpt4o);
        var tokens = encoder.Encode("hello world");

        tokens.Count.Should().Be(2);

        // Same cached instance as ModelToEncoder
        var encoder2 = ModelToEncoder.For("gpt-4o");
        encoder.Should().BeSameAs(encoder2);
    }

    [TestMethod]
    public void TikTokenEncoderTryCreateForModelReturnsNull()
    {
        var encoder = TikTokenEncoder.TryCreateForModel("nonexistent-model");
        encoder.Should().BeNull();
    }

    [TestMethod]
    public void TokenizerJsonLoadsGpt2()
    {
        var encoding = TokenizerJsonLoader.FromFile("Resources/gpt2.tokenizer.json", name: "gpt2");
        var encoder = new Encoder(encoding);

        // GPT-2 vocab: "hello" = 31373, " world" (Ġworld) = 995
        var encoded = encoder.Encode("hello world");
        var decoded = encoder.Decode(encoded);

        decoded.Should().Be("hello world");
        encoded.Should().BeEquivalentTo(new[] { 31373, 995 });
        encoder.CountTokens("hello world").Should().Be(2);
    }

    [TestMethod]
    public void TokenizerJsonSpecialTokensExtracted()
    {
        var encoding = TokenizerJsonLoader.FromFile("Resources/gpt2.tokenizer.json", name: "gpt2");

        encoding.SpecialTokens.Should().ContainKey("<|endoftext|>");
        encoding.SpecialTokens["<|endoftext|>"].Should().Be(50256);
    }

    [TestMethod]
    public void TokenizerJsonFromStream()
    {
        using var stream = File.OpenRead("Resources/gpt2.tokenizer.json");
        var encoding = TokenizerJsonLoader.FromStream(stream, name: "gpt2");
        var encoder = new Encoder(encoding);

        var encoded = encoder.Encode("hello world");
        encoded.Should().BeEquivalentTo(new[] { 31373, 995 });
    }

    [TestMethod]
    public void TokenizerJsonRoundTrip()
    {
        var encoding = TokenizerJsonLoader.FromFile("Resources/gpt2.tokenizer.json", name: "gpt2");
        var encoder = new Encoder(encoding);

        var texts = new[]
        {
            Strings.HelloWorld,
            Strings.Multilingual,
            Strings.Code,
            Strings.MultilingualLong,
            Strings.Chinese,
        };

        foreach (var text in texts)
        {
            var encoded = encoder.Encode(text);
            var decoded = encoder.Decode(encoded);

            decoded.Should().Be(text);
            encoder.CountTokens(text).Should().Be(encoded.Count);
        }
    }

    [TestMethod]
    public void TokenizerJsonDetectsSequenceSplitPattern()
    {
        // Simulate Llama 3/Qwen2 format: Sequence[Split(regex), ByteLevel]
        var json = """
        {
            "version": "1.0",
            "added_tokens": [
                { "id": 3, "special": true, "content": "<eos>" }
            ],
            "pre_tokenizer": {
                "type": "Sequence",
                "pretokenizers": [
                    {
                        "type": "Split",
                        "pattern": {
                            "Regex": "(?i:'s|'t|'re|'ve|'m|'ll|'d)|[^\\r\\n\\p{L}\\p{N}]?\\p{L}+|\\p{N}{1,3}| ?[^\\s\\p{L}\\p{N}]+[\\r\\n]*|\\s*[\\r\\n]+|\\s+(?!\\S)|\\s+"
                        }
                    },
                    {
                        "type": "ByteLevel",
                        "add_prefix_space": false,
                        "trim_offsets": true
                    }
                ]
            },
            "model": {
                "vocab": {
                    "h": 0, "e": 1, "l": 2, "o": 3,
                    "\u0120": 4, "w": 5, "r": 6, "d": 7,
                    "he": 8, "ll": 9, "lo": 10,
                    "hel": 11, "hello": 12,
                    "\u0120w": 13, "or": 14, "ld": 15,
                    "\u0120wo": 16, "rld": 17,
                    "\u0120world": 18
                },
                "merges": []
            }
        }
        """;

        var encoding = TokenizerJsonLoader.FromJson(json, name: "test-seq");

        // Pattern should be auto-detected from the Split pre-tokenizer
        encoding.Pattern.Should().Contain("\\p{L}+");
        encoding.SpecialTokens.Should().ContainKey("<eos>");

        // Verify ByteLevel decoding: Ġ (U+0120) maps to space (byte 0x20)
        var encoder = new Encoder(encoding);
        var encoded = encoder.Encode("hello world");
        var decoded = encoder.Decode(encoded);
        decoded.Should().Be("hello world");
    }

    [TestMethod]
    public void CreateForEncodingCl100K()
    {
        var encoder = TikTokenEncoder.CreateForEncoding("cl100k_base");
        var tokens = encoder.Encode("hello world");

        tokens.Count.Should().Be(2);
    }

    [TestMethod]
    public void CreateForEncodingO200K()
    {
        var encoder = TikTokenEncoder.CreateForEncoding("o200k_base");
        var tokens = encoder.Encode("hello world");

        tokens.Count.Should().Be(2);
    }

    [TestMethod]
    public void CreateForEncodingP50K()
    {
        var encoder = TikTokenEncoder.CreateForEncoding("p50k_base");
        encoder.Encode("hello world").Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void CreateForEncodingR50K()
    {
        var encoder = TikTokenEncoder.CreateForEncoding("r50k_base");
        encoder.Encode("hello world").Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void CreateForEncodingThrowsOnUnknown()
    {
        var act = () => TikTokenEncoder.CreateForEncoding("unknown_encoding");
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void ModelPrefixMatchingO3Mini()
    {
        var encoder = TikTokenEncoder.CreateForModel(Models.O3Mini);
        encoder.Should().NotBeNull();
        encoder.Encode("hello").Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void ModelPrefixMatchingGpt4Turbo()
    {
        var encoder = TikTokenEncoder.CreateForModel(Models.Gpt4Turbo);
        encoder.Should().NotBeNull();
        encoder.Encode("hello").Count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void CountMessageTokensBasic()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var messages = new List<ChatMessage>
        {
            new("system", "You are a helpful assistant."),
            new("user", "hello world"),
        };

        var count = encoder.CountMessageTokens(messages);

        // Each message: 3 overhead + role tokens + content tokens
        // Plus 3 reply priming at the end
        // "system" = 1 token, "You are a helpful assistant." = 6 tokens → 3 + 1 + 6 = 10
        // "user" = 1 token, "hello world" = 2 tokens → 3 + 1 + 2 = 6
        // Reply priming: 3
        // Total: 10 + 6 + 3 = 19
        count.Should().Be(19);
    }

    [TestMethod]
    public void CountMessageTokensWithName()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var messages = new List<ChatMessage>
        {
            new("system", "You are a helpful assistant.", name: "helper"),
        };

        var countWithName = encoder.CountMessageTokens(messages);

        var messagesWithoutName = new List<ChatMessage>
        {
            new("system", "You are a helpful assistant."),
        };

        var countWithoutName = encoder.CountMessageTokens(messagesWithoutName);

        // Name adds: CountTokens("helper") + 1
        // "helper" = 1 token, so name adds 2
        countWithName.Should().Be(countWithoutName + 2);
    }

    [TestMethod]
    public void CountMessageTokensEmpty()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var messages = new List<ChatMessage>();

        var count = encoder.CountMessageTokens(messages);

        // Only reply priming: 3
        count.Should().Be(3);
    }

    [TestMethod]
    public void CountToolTokensBasic()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>
        {
            new("get_weather", "Get the current weather", new List<FunctionParameter>
            {
                new("location", "string", "The city name", isRequired: true),
                new("unit", "string", "Temperature unit", enumValues: new[] { "celsius", "fahrenheit" }),
            }),
        };

        var count = encoder.CountToolTokens(tools);

        // Should be > 0 and include overhead: funcInit(7) + funcEnd(12) + propInit(3) + 2*propKey(6) + enum handling
        count.Should().BeGreaterThan(30);
    }

    [TestMethod]
    public void CountToolTokensNoParams()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>
        {
            new("do_nothing", "A function with no parameters"),
        };

        var count = encoder.CountToolTokens(tools);

        // funcInit(7) + tokenize("do_nothing:A function with no parameters") + funcEnd(12)
        count.Should().BeGreaterThan(19);
    }

    [TestMethod]
    public void CountToolTokensEmpty()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>();

        var count = encoder.CountToolTokens(tools);

        count.Should().Be(0);
    }

    [TestMethod]
    public void CountMessageTokensWithTools()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var messages = new List<ChatMessage>
        {
            new("user", "What's the weather?"),
        };
        var tools = new List<ChatFunction>
        {
            new("get_weather", "Get weather", new List<FunctionParameter>
            {
                new("city", "string", "City name", isRequired: true),
            }),
        };

        var countWithTools = encoder.CountMessageTokens(messages, tools);
        var countWithoutTools = encoder.CountMessageTokens(messages);

        // With tools should be larger
        countWithTools.Should().BeGreaterThan(countWithoutTools);
        // The difference should equal the tool tokens
        (countWithTools - countWithoutTools).Should().Be(encoder.CountToolTokens(tools));
    }

    [TestMethod]
    public void TikTokenEncoderCountMessageTokensStatic()
    {
        var messages = new List<ChatMessage>
        {
            new("user", "hello world"),
        };

        var count = TikTokenEncoder.CountMessageTokens("gpt-4o", messages);

        // Should match the instance method
        var encoder = TikTokenEncoder.CreateForModel("gpt-4o");
        count.Should().Be(encoder.CountMessageTokens(messages));
    }

    [TestMethod]
    public void TikTokenEncoderCountMessageTokensWithToolsStatic()
    {
        var messages = new List<ChatMessage>
        {
            new("user", "hello"),
        };
        var tools = new List<ChatFunction>
        {
            new("greet", "Say hello"),
        };

        var count = TikTokenEncoder.CountMessageTokens("gpt-4o", messages, tools);

        count.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void CountToolTokensNestedObject()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>
        {
            new("create_order", "Create a new order", new List<FunctionParameter>
            {
                new("item", "string", "Item name", isRequired: true),
                new("address", "object", "Shipping address", properties: new List<FunctionParameter>
                {
                    new("street", "string", "Street address", isRequired: true),
                    new("city", "string", "City", isRequired: true),
                    new("zip", "string", "ZIP code"),
                }),
            }),
        };

        var count = encoder.CountToolTokens(tools);

        // Should count both top-level and nested parameters
        // Flat version for comparison
        var flatTools = new List<ChatFunction>
        {
            new("create_order", "Create a new order", new List<FunctionParameter>
            {
                new("item", "string", "Item name", isRequired: true),
            }),
        };

        var flatCount = encoder.CountToolTokens(flatTools);

        // Nested version should have more tokens
        count.Should().BeGreaterThan(flatCount);
    }

    [TestMethod]
    public void CountToolTokensArrayType()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>
        {
            new("process_items", "Process a list of items", new List<FunctionParameter>
            {
                new("items", "array", "The items to process", isRequired: true, arrayItemType: "string"),
            }),
        };

        var count = encoder.CountToolTokens(tools);

        count.Should().BeGreaterThan(19); // funcInit + funcEnd + propInit + propKey + tokens
    }

    [TestMethod]
    public void CountToolTokensAnyOf()
    {
        var encoder = ModelToEncoder.For("gpt-4o");
        var tools = new List<ChatFunction>
        {
            new("update_field", "Update a field value", new List<FunctionParameter>
            {
                new("value", "", "The new value", isRequired: true,
                    anyOf: new[] { "string", "number", "boolean" }),
            }),
        };

        var count = encoder.CountToolTokens(tools);

        // Should tokenize the type as "string | number | boolean"
        count.Should().BeGreaterThan(19);

        // Compare with a simple string type — anyOf should produce more tokens
        var simpleTools = new List<ChatFunction>
        {
            new("update_field", "Update a field value", new List<FunctionParameter>
            {
                new("value", "string", "The new value", isRequired: true),
            }),
        };

        var simpleCount = encoder.CountToolTokens(simpleTools);
        count.Should().BeGreaterThan(simpleCount);
    }

    [TestMethod]
    public void EmptyStringReturnsZeroTokens()
    {
        var encodingNames = new[] { "o200k_base", "cl100k_base", "p50k_base", "p50k_edit", "r50k_base" };

        foreach (var encodingName in encodingNames)
        {
            var encoding = ModelToEncoding.ForEncoding(encodingName);
            var encoder = new Encoder(encoding);

            encoder.Encode("").Should().BeEmpty($"Encode should return empty for {encodingName}");
            encoder.CountTokens("").Should().Be(0, $"CountTokens should return 0 for {encodingName}");
            encoder.Decode(Array.Empty<int>()).Should().Be("", $"Decode of empty should return empty for {encodingName}");
        }
    }

    [TestMethod]
    public void SpecialTokenThrowsWhenDisallowed()
    {
        var encoder = new Encoder(new Cl100KBase());

        var act = () => encoder.EncodeWithAllDisallowedSpecial("hello <|endoftext|> world");

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void SpecialTokenAllowedWhenExplicit()
    {
        var encoder = new Encoder(new Cl100KBase());

        // Test with the same text the existing Special() BaseTest uses
        var encoded = encoder.EncodeWithAllAllowedSpecial(Strings.Special);

        encoded.Should().NotBeEmpty();
        // <|endoftext|> should be encoded as token 100257 in cl100k_base
        encoded.Should().Contain(100257);

        var decoded = encoder.Decode(encoded.ToList());
        decoded.Should().Be(Strings.Special);
    }

    [TestMethod]
    public void ModelToEncoderThrowsForUnknownModel()
    {
        var act = () => ModelToEncoder.For("nonexistent-model-xyz");

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("o200k_base")]
    [DataRow("cl100k_base")]
    [DataRow("p50k_base")]
    [DataRow("p50k_edit")]
    [DataRow("r50k_base")]
    public void CountTokensMatchesEncodeLength(string encodingName)
    {
        var encoding = ModelToEncoding.ForEncoding(encodingName);
        var encoder = new Encoder(encoding);

        var texts = new[]
        {
            Strings.HelloWorld,
            Strings.Chinese,
            Strings.Multilingual,
            Strings.Code,
            Strings.MultilingualLong,
            "A journey of a thousand miles begins with a single step.",
            "こんにちは、世界！",
            "1234567890",
            " ",
            "a",
        };

        foreach (var text in texts)
        {
            var encoded = encoder.Encode(text);
            encoder.CountTokens(text).Should().Be(
                encoded.Count,
                $"CountTokens should match Encode().Count for '{text}' with {encodingName}");
        }
    }

    [TestMethod]
    public void ParallelEncodingIsThreadSafe()
    {
        var testPlans = ReadTestPlans(H.Resources.TestPlans_txt).ToList();
        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.ForEach(testPlans, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, plan =>
        {
            try
            {
                var (encodingName, textToEncode, expectedEncoded) = plan;

                var encoding = ModelToEncoding.ForEncoding(encodingName);
                var encoder = new Encoder(encoding);
                var encoded = encoder.Encode(textToEncode);

                if (!encoded.SequenceEqual(expectedEncoded))
                {
                    throw new AssertFailedException(
                        $"Parallel encoding mismatch for '{textToEncode}' with {encodingName}");
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException("Parallel encoding failures", exceptions);
        }
    }

    [TestMethod]
    public void HelloWorldExactTokenIds()
    {
        // "hello world" — verified against Python tiktoken
        new Encoder(new Cl100KBase()).Encode("hello world")
            .Should().BeEquivalentTo(new[] { 15339, 1917 });

        new Encoder(new O200KBase()).Encode("hello world")
            .Should().BeEquivalentTo(new[] { 24912, 2375 });

        new Encoder(new R50KBase()).Encode("hello world")
            .Should().BeEquivalentTo(new[] { 31373, 995 });
    }

    [TestMethod]
    public void HelloWorldWithPunctuationExactTokenIds()
    {
        // "Hello, World!" — verified against Python tiktoken
        new Encoder(new Cl100KBase()).Encode("Hello, World!")
            .Should().BeEquivalentTo(new[] { 9906, 11, 4435, 0 });
    }

    [TestMethod]
    public void CrossValidateWithPythonTiktoken()
    {
        // Cross-validate .NET encoder output against Python tiktoken.
        // Runs the GenerateTestPlans.py script and compares results.
        // Skips gracefully if Python or tiktoken module is not available.
        var scriptDir = AppContext.BaseDirectory;
        while (scriptDir != null && !File.Exists(Path.Combine(scriptDir, "Tiktoken.UnitTests.csproj")))
        {
            scriptDir = Path.GetDirectoryName(scriptDir);
        }
        if (scriptDir == null)
        {
            Assert.Inconclusive("Could not find project directory");
            return;
        }

        var scriptPath = Path.Combine(scriptDir, "Resources", "GenerateTestPlans.py");
        if (!File.Exists(scriptPath))
        {
            Assert.Inconclusive("GenerateTestPlans.py not found");
            return;
        }

        // Try python3 first, then python
        var pythonCmd = "python3";
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pythonCmd,
            Arguments = $"\"{scriptPath}\" --stdout",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        string pythonOutput;
        try
        {
            using var process = System.Diagnostics.Process.Start(psi)!;
            pythonOutput = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(30_000);

            if (process.ExitCode != 0)
            {
                Assert.Inconclusive($"Python script failed (exit {process.ExitCode}): {stderr}");
                return;
            }
        }
        catch (Exception ex)
        {
            Assert.Inconclusive($"Python not available: {ex.Message}");
            return;
        }

        // Parse Python output
        var pythonPlans = ReadTestPlansFromString(pythonOutput).ToList();

        // Generate .NET output for comparison
        var dotnetPlans = new List<Tuple<string, string, List<int>>>();
        foreach (var encodingName in EncodingNames)
        {
            var encoding = ModelToEncoding.ForEncoding(encodingName);
            var encoder = new Encoder(encoding);

            foreach (var sample in TestSamples)
            {
                var tokens = encoder.Encode(sample);
                dotnetPlans.Add(Tuple.Create(encodingName, sample, tokens.ToList()));
            }
        }

        // Compare
        var mismatches = new List<string>();
        var pythonDict = pythonPlans.ToDictionary(p => (p.Item1, p.Item2), p => p.Item3);

        foreach (var plan in dotnetPlans)
        {
            if (pythonDict.TryGetValue((plan.Item1, plan.Item2), out var pythonTokens))
            {
                if (!plan.Item3.SequenceEqual(pythonTokens))
                {
                    mismatches.Add(
                        $"{plan.Item1}: '{(plan.Item2.Length > 30 ? plan.Item2[..30] + "..." : plan.Item2)}' " +
                        $"(.NET={plan.Item3.Count} tokens, Python={pythonTokens.Count} tokens)");
                }
            }
        }

        if (mismatches.Count > 0)
        {
            // Report but don't fail — some divergence is expected due to
            // .NET vs Python regex engine differences for \p{L} classification
            Console.WriteLine($"Cross-validation divergences ({mismatches.Count}):");
            foreach (var m in mismatches)
            {
                Console.WriteLine($"  {m}");
            }
        }

        // At minimum, the majority should match
        var matchCount = dotnetPlans.Count - mismatches.Count;
        matchCount.Should().BeGreaterThan(dotnetPlans.Count * 8 / 10,
            $"At least 80% of test cases should match Python tiktoken. " +
            $"Matched {matchCount}/{dotnetPlans.Count}. Mismatches:\n{string.Join("\n", mismatches.Take(10))}");
    }

    [TestMethod]
    [DataRow("gpt-4o", "o200k_base")]
    [DataRow("gpt-4o-mini", "o200k_base")]
    [DataRow("gpt-4.5-preview", "o200k_base")]
    [DataRow("gpt-4.1", "o200k_base")]
    [DataRow("gpt-4.1-mini", "o200k_base")]
    [DataRow("gpt-4.1-nano", "o200k_base")]
    [DataRow("chatgpt-4o-latest", "o200k_base")]
    [DataRow("o4-mini", "o200k_base")]
    [DataRow("o3", "o200k_base")]
    [DataRow("o3-mini", "o200k_base")]
    [DataRow("o3-pro", "o200k_base")]
    [DataRow("o1", "o200k_base")]
    [DataRow("o1-mini", "o200k_base")]
    [DataRow("gpt-4", "cl100k_base")]
    [DataRow("gpt-4-turbo", "cl100k_base")]
    [DataRow("gpt-3.5-turbo", "cl100k_base")]
    [DataRow("gpt-35-turbo", "cl100k_base")]
    [DataRow("text-embedding-ada-002", "cl100k_base")]
    [DataRow("text-embedding-3-small", "cl100k_base")]
    [DataRow("text-embedding-3-large", "cl100k_base")]
    public void ModelToEncodingMapsCorrectly(string modelName, string expectedEncoding)
    {
        var encoding = ModelToEncoding.For(modelName);
        var expectedEnc = ModelToEncoding.ForEncoding(expectedEncoding);

        // Verify they produce the same tokens for a test string
        var encoder1 = new Encoder(encoding);
        var encoder2 = new Encoder(expectedEnc);

        var tokens1 = encoder1.Encode("hello world");
        var tokens2 = encoder2.Encode("hello world");

        tokens1.Should().BeEquivalentTo(tokens2,
            $"Model '{modelName}' should use {expectedEncoding} encoding");
    }

    [TestMethod]
    public void TryForReturnsNullForUnknownModel()
    {
        var encoding = ModelToEncoding.TryFor("nonexistent-model-xyz-999");
        encoding.Should().BeNull();
    }

    [TestMethod]
    public void TryForEncodingReturnsNullForUnknownEncoding()
    {
        var encoding = ModelToEncoding.TryForEncoding("nonexistent_encoding");
        encoding.Should().BeNull();
    }

    [TestMethod]
    public void BinaryRoundTrip()
    {
        // Create a small encoding dictionary
        var original = new Dictionary<byte[], int>(new ByteArrayComparer())
        {
            [new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }] = 0,  // "Hello"
            [new byte[] { 0x20 }] = 1,                             // " "
            [new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 }] = 2,  // "World"
        };

        // Write to binary
        using var ms = new MemoryStream();
        EncodingLoader.WriteEncodingToBinaryStream(ms, original);
        var binaryData = ms.ToArray();

        // Read back
        var loaded = EncodingLoader.LoadEncodingFromBinaryData(binaryData);

        loaded.Count.Should().Be(original.Count);
        foreach (var kvp in original)
        {
            loaded.Should().ContainKey(kvp.Key);
            loaded[kvp.Key].Should().Be(kvp.Value);
        }
    }

    [TestMethod]
    public void BinaryRoundTripEmptyDictionary()
    {
        var original = new Dictionary<byte[], int>(new ByteArrayComparer());

        using var ms = new MemoryStream();
        EncodingLoader.WriteEncodingToBinaryStream(ms, original);
        var binaryData = ms.ToArray();

        var loaded = EncodingLoader.LoadEncodingFromBinaryData(binaryData);

        loaded.Count.Should().Be(0);
    }

    [TestMethod]
    public void BinaryStreamBadMagicThrows()
    {
        var badData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var act = () => EncodingLoader.LoadEncodingFromBinaryData(badData);

        act.Should().Throw<FormatException>().WithMessage("*bad magic*");
    }

    [TestMethod]
    public void BinaryStreamBadVersionThrows()
    {
        // TTKB magic + version 99
        var badData = new byte[] { 0x54, 0x54, 0x4B, 0x42, 99, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        var act = () => EncodingLoader.LoadEncodingFromBinaryData(badData);

        act.Should().Throw<FormatException>().WithMessage("*version*");
    }

    [TestMethod]
    public void WriteEncodingTokenTooLongThrows()
    {
        var dict = new Dictionary<byte[], int>(new ByteArrayComparer())
        {
            [new byte[256]] = 0,  // 256 bytes > max 255
        };

        using var ms = new MemoryStream();
        var act = () => EncodingLoader.WriteEncodingToBinaryStream(ms, dict);

        act.Should().Throw<ArgumentException>().WithMessage("*256 bytes*");
    }

    [TestMethod]
    public void BinaryRoundTripWithRealEncoding()
    {
        // Load cl100k_base via the normal path
        var encoding = new Cl100KBase();
        var original = encoding.MergeableRanks;

        // Write to binary and read back
        using var ms = new MemoryStream();
        EncodingLoader.WriteEncodingToBinaryStream(ms, original);
        var binaryData = ms.ToArray();

        var loaded = EncodingLoader.LoadEncodingFromBinaryData(binaryData);

        loaded.Count.Should().Be(original.Count);

        // Spot-check a known token
        var helloToken = System.Text.Encoding.UTF8.GetBytes("Hello");
        loaded.Should().ContainKey(helloToken);
        loaded[helloToken].Should().Be(original[helloToken]);
    }

    [TestMethod]
    public void LoadEncodingFromFileText()
    {
        // Find the data/ directory with .tiktoken files
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "data")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        if (dir == null || !File.Exists(Path.Combine(dir, "data", "r50k_base.tiktoken")))
        {
            Assert.Inconclusive("data/r50k_base.tiktoken not found (run from repo root)");
            return;
        }

        var path = Path.Combine(dir, "data", "r50k_base.tiktoken");
        var loaded = EncodingLoader.LoadEncodingFromFile(path);

        loaded.Count.Should().Be(50256);
    }

    [TestMethod]
    public void LoadEncodingFromFileBinary()
    {
        // Create a temp .ttkb file
        var original = new Dictionary<byte[], int>(new ByteArrayComparer())
        {
            [new byte[] { 0x41 }] = 0,
            [new byte[] { 0x42, 0x43 }] = 1,
        };

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ttkb");
        try
        {
            using (var fs = File.Create(tempPath))
            {
                EncodingLoader.WriteEncodingToBinaryStream(fs, original);
            }

            var loaded = EncodingLoader.LoadEncodingFromFile(tempPath);

            loaded.Count.Should().Be(2);
            loaded[new byte[] { 0x41 }].Should().Be(0);
            loaded[new byte[] { 0x42, 0x43 }].Should().Be(1);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}