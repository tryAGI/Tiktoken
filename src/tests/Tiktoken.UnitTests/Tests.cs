using Tiktoken.Services;

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
        var encoding = Encoding.Get(encodingName);
        var encoded = special
            ? encoding.EncodeWithAllAllowedSpecial(text)
            : encoding.Encode(text);
        var decodedText = encoding.Decode(encoded);
        var words = encoding.Explore(text);

        if (!special)
        {
            encoding.CountTokens(text).Should().Be(encoded.Count);
        }
        words.Count.Should().Be(encoded.Count);
        decodedText.Should().Be(text);

        return Verify((words, encoded))
            .UseDirectory("Snapshots")
            //.AutoVerify()
            .UseTextForParameters(encodingName);
    }
    
    [TestMethod]
    [DataRow(Encodings.Cl100KBase)]
    [DataRow(Encodings.P50KBase)]
    [DataRow(Encodings.P50KEdit)]
    [DataRow(Encodings.R50KBase)]
    public Task HelloWorld(string encodingName)
    {
        return BaseTest(encodingName, Strings.HelloWorld);
    }
    
    [TestMethod]
    [DataRow(Encodings.Cl100KBase)]
    [DataRow(Encodings.P50KBase)]
    [DataRow(Encodings.P50KEdit)]
    [DataRow(Encodings.R50KBase)]
    public Task Special(string encodingName)
    {
        return BaseTest(encodingName, Strings.Special, special: true);
    }
    
    [TestMethod]
    [DataRow(Encodings.Cl100KBase)]
    [DataRow(Encodings.P50KBase)]
    [DataRow(Encodings.P50KEdit)]
    [DataRow(Encodings.R50KBase)]
    public Task Chinese(string encodingName)
    {
        return BaseTest(encodingName, Strings.Chinese);
    }
    
    [TestMethod]
    [DataRow(Encodings.Cl100KBase)]
    [DataRow(Encodings.P50KBase)]
    [DataRow(Encodings.P50KEdit)]
    [DataRow(Encodings.R50KBase)]
    public Task KingLear(string encodingName)
    {
        return BaseTest(encodingName, Strings.KingLear);
    }
    
    [TestMethod]
    [DataRow(Encodings.Cl100KBase)]
    [DataRow(Encodings.P50KBase)]
    [DataRow(Encodings.P50KEdit)]
    [DataRow(Encodings.R50KBase)]
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

        var dictionary = EncodingManager.Get(Encodings.Cl100KBase).MergeableRanks;
        dictionary.ContainsKey(testBytes).Should().BeTrue();
        dictionary.TryGetValue("Hello"u8.ToArray(), out var helloResult).Should().BeTrue();
        helloResult.Should().Be(9906);
        
        var dictionaryNew = EncodingManager.Get(Encodings.Cl100KBase).MergeableRanks
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
        IReadOnlyCollection<string> tokens = Encoding.ForModel("gpt-4").Explore(text);
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
        IReadOnlyCollection<UtfToken> tokens = Encoding.ForModel("gpt-4").ExploreUtfSafe(text);
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
        IReadOnlyCollection<UtfToken> tokens = Encoding.ForModel("gpt-4").ExploreUtfSafe(text);
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
        IReadOnlyCollection<UtfToken> tokens = Encoding.ForModel("gpt-4").ExploreUtfSafe(text);
        List<string> expected = new List<string> { "\ud83e\udd1a", "ř" };
        int i = 0;

        tokens.Count.Should().Be(expected.Count);
        
        foreach (UtfToken token in tokens)
        {
            token.Token.Should().Be(expected[i]);
            i++;
        }
    }
}