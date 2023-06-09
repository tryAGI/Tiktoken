namespace Tiktoken.UnitTests;

[TestClass]
public class TextDavinciTests
{
    [TestMethod]
    public void HelloWorld()
    {
        var tikToken = Encoding.ForModel("text-davinci-003");
        var tokens = tikToken.Encode("hello world");
        tokens.Should().BeEquivalentTo(new[] { 31373, 995 });
        
        var text = tikToken.Decode(tokens);
        text.Should().Be("hello world");
    }
    
    [TestMethod]
    public void Special()
    {
        var tikToken = Encoding.ForModel("text-davinci-003");
        var tokens = tikToken.Encode("hello <|endoftext|>", allowedSpecial: "all");
        
        tokens.Should().BeEquivalentTo(new[] { 31373, 220, 50256 });
    }
    
    [TestMethod]
    public void Chinese()
    {
        var tikToken = Encoding.ForModel("text-davinci-003");
        var tokens = tikToken.Encode("我很抱歉，我不能提供任何非法或不道德的建议。快速赚钱是不容易的，需要耐心、刻苦努力和经验。如果您想增加收入，请考虑增加工作时间、寻找其他业务机会、学习新技能或提高自己的价值等方法。请记住，通过合法而道德的方式来获得收入，才是长期稳定的解决方案。");

        tokens.Should().HaveCount(257);
    }
}