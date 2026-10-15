using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using Tiktoken.Encodings;

namespace Tiktoken.UnitTests;

[TestClass]
public class HarmonyTests
{
    private static readonly JsonElement Fixture = JsonDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Resources", "HarmonyParity.json"))).RootElement;
    private static readonly Encoder Harmony = TikTokenEncoder.CreateForEncoding("o200k_harmony");

    [TestMethod]
    public void CompleteSpecialMapAndAliasMatchUpstream()
    {
        var encoding = ModelToEncoding.ForEncoding("o200k_harmony");
        var expected = Fixture.GetProperty("specialTokens").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetInt32());
        encoding.SpecialTokens.Should().BeEquivalentTo(expected);
        encoding.SpecialTokens.Values.Max().Should().Be(201087);
        encoding.SpecialTokens.Should().NotContainKey("<|reserved_201088|>");
        foreach (var token in expected)
        {
            Harmony.EncodeWithAllAllowedSpecial(token.Key).Should().Equal(token.Value);
            if (token.Value != 200018)
            {
                Harmony.Decode((IReadOnlyCollection<int>)new[] { token.Value }).Should().Be(token.Key);
            }
            Action disallowed = () => Harmony.Encode(token.Key);
            disallowed.Should().Throw<InvalidOperationException>();
            Action selectiveDisallowed = () => Harmony.EncodeWithDisallowedSpecial(token.Key, [token.Key]);
            selectiveDisallowed.Should().Throw<InvalidOperationException>();
        }
        // Match the canonical spelling observed in the pinned upstream fixture.
        var alias = Harmony.Decode((IReadOnlyCollection<int>)new[] { 200018 });
        alias.Should().Be(Fixture.GetProperty("aliasDecode").GetString());
        Harmony.EncodeWithAllAllowedSpecial(alias).Should().Equal(200018);
    }

    [TestMethod]
    public void OrdinaryAndSpecialSegmentsMatchUpstream()
    {
        var baseEncoder = TikTokenEncoder.CreateForEncoding("o200k_base");
        foreach (var item in Fixture.GetProperty("cases").EnumerateArray())
        {
            var text = item.GetProperty("text").GetString()!;
            var expected = item.GetProperty("allowed").EnumerateArray().Select(x => x.GetInt32()).ToArray();
            foreach (var cache in new[] { false, true })
            {
                Harmony.EnableCache = cache;
                Harmony.EncodeWithAllAllowedSpecial(text).Should().Equal(expected);
                Harmony.EncodeWithAllowedSpecial(text, ModelToEncoding.ForEncoding("o200k_harmony").SpecialTokens.Keys.ToArray()).Should().Equal(expected);
                Harmony.EncodeWithDisallowedSpecial(text, []).Should().Equal(expected);
            }
            Harmony.CountTokens(text).Should().Be(item.GetProperty("literal").GetArrayLength());
            Harmony.CountTokens(text).Should().Be(baseEncoder.CountTokens(text));
            if (!text.Contains("<|", StringComparison.Ordinal))
            {
                Harmony.Encode(text).Should().Equal(item.GetProperty("literal").EnumerateArray().Select(x => x.GetInt32()));
                Harmony.Encode(text).Should().Equal(baseEncoder.Encode(text));
            }
        }
    }

    [TestMethod]
    public void SelectivePoliciesRejectOnlyTheDisallowedInput()
    {
        Harmony.EncodeWithAllowedSpecial("A<|start|>B", ["<|start|>"]).Should().Equal(32, 200006, 33);
        Action unlisted = () => Harmony.EncodeWithAllowedSpecial("<|start|>B<|end|>", ["<|start|>"]);
        unlisted.Should().Throw<InvalidOperationException>();
        Harmony.EncodeWithDisallowedSpecial("A<|start|>B", ["<|end|>"]).Should().Equal(32, 200006, 33);
        Action forbidden = () => Harmony.EncodeWithDisallowedSpecial("A<|start|>B<|end|>", ["<|end|>"]);
        forbidden.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void ModelFactoriesMatchUpstream()
    {
        foreach (var model in Fixture.GetProperty("models").EnumerateObject())
        {
            ModelToEncoding.For(model.Name).Name.Should().Be(model.Value.GetString());
            TikTokenEncoder.CreateForModel(model.Name).Should().BeSameAs(ModelToEncoder.For(model.Name));
        }
        ModelToEncoding.TryFor("gpt-6").Should().BeNull();
        ModelToEncoding.TryFor("gpt-oss").Should().BeNull();
        ModelToEncoding.TryForEncoding("o200k_harmony").Should().BeOfType<O200KHarmony>();
    }

    [TestMethod]
    [Timeout(120000)]
    public void LongPiecesMatchUpstream()
    {
        Harmony.EnableCache = false;
        foreach (var item in Fixture.GetProperty("longCases").EnumerateArray())
        {
            var text = string.Concat(Enumerable.Repeat(item.GetProperty("piece").GetString(), item.GetProperty("repeats").GetInt32()));
            // Legacy BPE uses a quadratic scan. Bound that lane; modern paths also test one million x.
            if (text.Length > 100000 && typeof(Encoder).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()!
                    .FrameworkName.StartsWith(".NETStandard", StringComparison.Ordinal))
            {
                continue;
            }
            var stopwatch = Stopwatch.StartNew();
            var ids = Harmony.Encode(text);
            stopwatch.Stop();
            ids.Count.Should().Be(item.GetProperty("count").GetInt32());
            var bytes = System.Text.Encoding.UTF8.GetBytes(string.Join(",", ids.Select(x => x.ToString(CultureInfo.InvariantCulture))));
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant().Should().Be(item.GetProperty("sha256").GetString());
            Harmony.CountTokens(text).Should().Be(ids.Count);
            Console.WriteLine($"{text.Length} chars: .NET encode {stopwatch.Elapsed.TotalSeconds:F6}s; upstream {item.GetProperty("seconds").GetDouble():F6}s");
        }
    }

    [TestMethod]
    [DataRow("cl100k_base")]
    [DataRow("o200k_base")]
    [DataRow("o200k_harmony")]
    public void PostSpecialOffsetsAreCorrectInAllViews(string name)
    {
        var encoder = TikTokenEncoder.CreateForEncoding(name);
        const string text = "A<|endoftext|>B<|endoftext|>C";
        encoder.Decode(encoder.EncodeWithAllAllowedSpecial(text)).Should().Be(text);
        string.Concat(encoder.Explore(text)).Should().Be(text);
        string.Concat(encoder.ExploreUtfSafe(text).Select(x => x.Token)).Should().Be(text);
    }
}
