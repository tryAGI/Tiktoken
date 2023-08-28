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
    
    private static IEnumerable<object[]> CustomTestData => ReadTestPlans(H.Resources.Custom_txt).Select(static x => new object[] { x });
    
    [TestMethod]
    [DynamicData(nameof(CustomTestData))]
    public void Custom(Tuple<string, string, List<int>> resource)
    {
        var (encodingName, textToEncode, expectedEncoded) = resource;

        var encoding = Encoding.Get(encodingName);
        var encoded = encoding.Encode(textToEncode);
        var decodedText = encoding.Decode(encoded);

        Console.WriteLine(string.Join(", ", encoded));
        encoded.Should().BeEquivalentTo(expectedEncoded);
        decodedText.Should().Be(textToEncode);
    }
    
    [TestMethod]
    public void Special()
    {
        var encoding = Encoding.Get("p50k_base");
        var tokens = encoding.EncodeWithAllAllowedSpecial(Strings.Special);
        
        tokens.Should().BeEquivalentTo(new[] { 31373, 220, 50256 });
    }
    
    [TestMethod]
    public void HelloWorld()
    {
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(Strings.HelloWorld);
        tokens.Should().BeEquivalentTo(new[] { 15339, 1917 });
        
        var text = encoding.Decode(tokens);
        text.Should().Be(Strings.HelloWorld);

        encoding.CountTokens(text).Should().Be(2);
        
        encoding.Explore(text).Should().BeEquivalentTo(new[] { "hello", " world" });
        encoding.Explore(text).Should().HaveCount(2);
    }
    
    [TestMethod]
    public void Special_Gpt4()
    {
        const string text = Strings.Special;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.EncodeWithAllAllowedSpecial(text);
        
        tokens.Should().BeEquivalentTo(new[] { 15339, 220, 100257 });
        encoding.CountTokens(text).Should().Be(7);
        encoding.Explore(text).Should().HaveCount(3);
    }
    
    [TestMethod]
    public void Chinese()
    {
        const string text = Strings.Chinese;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(135);
        encoding.CountTokens(text).Should().Be(135);
        encoding.Explore(text).Should().HaveCount(135);
    }
    
    [TestMethod]
    public void KingLear()
    {
        const string text = Strings.KingLear;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(60);
        encoding.CountTokens(text).Should().Be(60);
        encoding.Explore(text).Should().HaveCount(60);
    }
    
    [TestMethod]
    public void Bitcoin()
    {
        const string text = Strings.Bitcoin;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(4603);
        encoding.CountTokens(text).Should().Be(4603);
        encoding.Explore(text).Should().HaveCount(4603);
    }
    
    [TestMethod]
    public void ConvertChinese()
    {
        var test = Strings.Chinese.Substring(0, 1);
        var testBytes = System.Text.Encoding.UTF8.GetBytes(test);
        testBytes.Should().HaveCount(3);

        var dictionary = EncodingManager.Get("cl100k_base").MergeableRanks;
        dictionary.ContainsKey(testBytes).Should().BeTrue();
        dictionary.TryGetValue("Hello"u8.ToArray(), out var helloResult).Should().BeTrue();
        helloResult.Should().Be(9906);
        
        var dictionaryNew = EncodingManager.Get("cl100k_base").MergeableRanks
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
}