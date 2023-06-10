namespace Tiktoken.UnitTests;

[TestClass]
public class Gpt35Tests
{
    [TestMethod]
    public void HelloWorld()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.Encode(Strings.HelloWorld);
        tokens.Should().BeEquivalentTo(new[] { 15339, 1917 });
        
        var text = encoding.Decode(tokens);
        text.Should().Be(Strings.HelloWorld);
    }
    
    [TestMethod]
    public void Special()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.EncodeWithAllAllowedSpecial(Strings.Special);
        
        tokens.Should().BeEquivalentTo(new[] { 15339, 220, 100257 });
    }
    
    [TestMethod]
    public void Chinese()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.Encode(Strings.Chinese);

        tokens.Should().HaveCount(135);
    }
}