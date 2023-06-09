namespace Tiktoken.UnitTests;

[TestClass]
public class Gpt35Tests
{
    [TestMethod]
    public void HelloWorld()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.Encode("hello world");
        tokens.Should().BeEquivalentTo(new[] { 15339, 1917 });
        
        var text = encoding.Decode(tokens);
        text.Should().Be("hello world");
    }
    
    [TestMethod]
    public void Special()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.Encode("hello <|endoftext|>", allowedSpecial: "all");
        
        tokens.Should().BeEquivalentTo(new[] { 15339, 220, 100257 });
    }
    
    [TestMethod]
    public void Chinese()
    {
        var encoding = Encoding.ForModel("gpt-3.5-turbo");
        var tokens = encoding.Encode("我很抱歉，我不能提供任何非法或不道德的建议。快速赚钱是不容易的，需要耐心、刻苦努力和经验。如果您想增加收入，请考虑增加工作时间、寻找其他业务机会、学习新技能或提高自己的价值等方法。请记住，通过合法而道德的方式来获得收入，才是长期稳定的解决方案。");

        tokens.Should().HaveCount(135);
    }
}