namespace Tiktoken.UnitTests;

[TestClass]
public class TextDavinciTests
{
    [TestMethod]
    public void HelloWorld()
    {
        var encoding = Encoding.ForModel("text-davinci-003");
        var tokens = encoding.Encode(Strings.HelloWorld);
        tokens.Should().BeEquivalentTo(new[] { 31373, 995 });
        
        var text = encoding.Decode(tokens);
        text.Should().Be(Strings.HelloWorld);
    }
    
    [TestMethod]
    public void Special()
    {
        var encoding = Encoding.ForModel("text-davinci-003");
        var tokens = encoding.Encode(Strings.Special, allowedSpecial: "all");
        
        tokens.Should().BeEquivalentTo(new[] { 31373, 220, 50256 });
    }
    
    [TestMethod]
    public void Chinese()
    {
        var encoding = Encoding.ForModel("text-davinci-003");
        var tokens = encoding.Encode(Strings.Chinese);

        tokens.Should().HaveCount(257);
    }
}