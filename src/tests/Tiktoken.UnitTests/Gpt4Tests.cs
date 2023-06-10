namespace Tiktoken.UnitTests;

[TestClass]
public class Gpt4Tests
{
    [TestMethod]
    public void HelloWorld()
    {
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(Strings.HelloWorld);
        tokens.Should().BeEquivalentTo(new[] { 15339, 1917 });
        
        var text = encoding.Decode(tokens);
        text.Should().Be(Strings.HelloWorld);

        encoding.CountTokens(text).Should().Be(2);
    }
    
    [TestMethod]
    public void Special()
    {
        const string text = Strings.Special;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.EncodeWithAllAllowedSpecial(text);
        
        tokens.Should().BeEquivalentTo(new[] { 15339, 220, 100257 });
        encoding.CountTokens(text).Should().Be(7);
    }
    
    [TestMethod]
    public void Chinese()
    {
        const string text = Strings.Chinese;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(135);
        encoding.CountTokens(text).Should().Be(135);
    }
    
    [TestMethod]
    public void KingLear()
    {
        const string text = Strings.KingLear;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(60);
        encoding.CountTokens(text).Should().Be(60);
    }
    
    [TestMethod]
    public void Bitcoin()
    {
        const string text = Strings.Bitcoin;
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(text);

        tokens.Should().HaveCount(4603);
        encoding.CountTokens(text).Should().Be(4603);
    }
}