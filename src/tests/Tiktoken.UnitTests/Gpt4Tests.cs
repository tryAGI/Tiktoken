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
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.EncodeWithAllAllowedSpecial(Strings.Special);
        
        tokens.Should().BeEquivalentTo(new[] { 15339, 220, 100257 });
    }
    
    [TestMethod]
    public void Chinese()
    {
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(Strings.Chinese);

        tokens.Should().HaveCount(135);
    }
    
    [TestMethod]
    public void KingLear()
    {
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(Strings.KingLear);

        tokens.Should().HaveCount(60);
    }
    
    [TestMethod]
    public void Bitcoin()
    {
        var encoding = Encoding.Get("cl100k_base");
        var tokens = encoding.Encode(Strings.Bitcoin);

        tokens.Should().HaveCount(4603);
    }
}