using Tiktoken.Encodings;

namespace Tiktoken.UnitTests;

[TestClass]
public partial class Tests
{
    // private static IEnumerable<object[]> TestData => ReadTestPlans(H.Resources.TestPlans_txt).Select(static x => new object[] { x });
    //
    // [TestMethod]
    // [DynamicData(nameof(TestData))]
    // public void VariousCases(Tuple<string, string, List<int>> resource)
    // {
    //     var (encodingName, textToEncode, expectedEncoded) = resource;
    //
    //     var encoding = Encoding.Get(encodingName);
    //     var encoded = encoding.Encode(textToEncode);
    //     var decodedText = encoding.Decode(encoded);
    //
    //     encoded.Should().BeEquivalentTo(expectedEncoded);
    //     decodedText.Should().Be(textToEncode);
    // }
    
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
    public Task KingLear(string encodingName)
    {
        return BaseTest(encodingName, Strings.KingLear);
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
            Strings.KingLear,
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
}