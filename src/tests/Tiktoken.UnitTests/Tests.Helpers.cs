using System.Globalization;
using System.Text.RegularExpressions;
using Tiktoken.Encodings;

namespace Tiktoken.UnitTests;

public partial class Tests : VerifyBase
{
    private static readonly string[] EncodingNames =
        ["r50k_base", "p50k_base", "p50k_edit", "cl100k_base", "o200k_base"];

    private static readonly string[] TestSamples =
    [
        "",
        "a",
        "1",
        "a a",
        "hello",
        "Hello, World! How are you today? \U0001f30d",
        "\u3053\u3093\u306b\u3061\u306f\u3001\u4e16\u754c\uff01\u304a\u5143\u6c17\u3067\u3059\u304b\uff1f",
        "Hola, mundo! \u00bfC\u00f3mo est\u00e1s hoy? \U0001f1ea\U0001f1f8",
        "\u041f\u0440\u0438\u0432\u0435\u0442, \u043c\u0438\u0440! \u041a\u0430\u043a \u0434\u0435\u043b\u0430?",
        "\uc548\ub155\ud558\uc138\uc694, \uc138\uc0c1! \uc624\ub298 \uae30\ubd84\uc774 \uc5b4\ub5a8\uc694? \U0001f1f0\U0001f1f7",
        "Bonjour, le monde ! Comment \u00e7a va aujourd'hui ? \U0001f1eb\U0001f1f7",
        "The quick brown fox jumps over 13 lazy dogs. \U0001f63a",
        "1234567890!@#$%^&*()-=_+[]{};:'\",.<>?/|`~ \U0001f389",
        "C# is a great programming language for building apps.",
        "El \u00e1rea de un tri\u00e1ngulo es (base * altura) / 2.",
        "\u0417\u0434\u0440\u0430\u0432\u0441\u0442\u0432\u0443\u0439\u0442\u0435, \u044d\u0442\u043e \u043c\u043e\u0439 \u043f\u0435\u0440\u0432\u044b\u0439 \u0440\u0430\u0437 \u0437\u0434\u0435\u0441\u044c. \u0427\u0442\u043e \u043c\u043d\u0435 \u0434\u0435\u043b\u0430\u0442\u044c?",
        "\u0ab9\u0ac7\u0ab2\u0acb, \u0ab5\u0abf\u0ab6\u0acd\u0ab5! \u0aa4\u0aae\u0ac7 \u0a86\u0a9c\u0ac7 \u0a95\u0ac7\u0aae \u0a9b\u0acb? \U0001f1ee\U0001f1f3",
        "\u0e04\u0e27\u0e32\u0e21\u0e23\u0e31\u0e01\u0e41\u0e25\u0e30\u0e01\u0e32\u0e23\u0e40\u0e1b\u0e47\u0e19\u0e01\u0e31\u0e19\u0e40\u0e2d\u0e07\u0e40\u0e1b\u0e47\u0e19\u0e2a\u0e34\u0e48\u0e07\u0e2a\u0e33\u0e04\u0e31\u0e0d\u0e17\u0e35\u0e48\u0e2a\u0e38\u0e14\u0e43\u0e19\u0e42\u0e25\u0e01 \U0001f1f9\U0001f1ed",
        "Python vs Java: Which programming language should you learn first?",
        "A journey of a thousand miles begins with a single step. - Lao Tzu",
        "Die Grenzen meiner Sprache bedeuten die Grenzen meiner Welt. \U0001f1e9\U0001f1ea",
        "\u05d9\u05e9 \u05dc\u05d9 \u05db\u05de\u05d4 \u05e9\u05d0\u05dc\u05d5\u05ea \u05d1\u05e0\u05d5\u05d2\u05e2 \u05dc\u05e4\u05e8\u05d5\u05d9\u05e7\u05d8 \u05d4\u05d7\u05d3\u05e9 \u05e9\u05dc\u05da. \U0001f1ee\U0001f1f1",
        "Det \u00e4r en vacker dag i Sverige. \U0001f1f8\U0001f1ea",
        "A \u2200 x (P(x) \u2192 Q(x)) \u2227 (\u2203x P(x)) \u2192 \u2203x Q(x)",
        "O Brasil \u00e9 o maior pa\u00eds da Am\u00e9rica do Sul. \U0001f1e7\U0001f1f7",
        "L'amore \u00e8 una forza potente che unisce le persone. \U0001f1ee\U0001f1f9",
        "\u0395\u03af\u03bd\u03b1\u03b9 \u03bc\u03b9\u03b1 \u03b7\u03bb\u03b9\u03cc\u03bb\u03bf\u03c5\u03c3\u03c4\u03b7 \u03b7\u03bc\u03ad\u03c1\u03b1 \u03c3\u03c4\u03b7\u03bd \u0395\u03bb\u03bb\u03ac\u03b4\u03b1. \U0001f1ec\U0001f1f7",
        "Teslim tarihi yakla\u015f\u0131yor, projeyi zaman\u0131nda bitirmemiz gerekiyor. \U0001f1f9\U0001f1f7",
        "Det finnes ingen bedre tid enn n\u00e5 for \u00e5 starte noe nytt. \U0001f1f3\U0001f1f4",
        "Aanvaard de uitdagingen van het leven met moed en vastberadenheid. \U0001f1f3\U0001f1f1",
        "Ch\u00e0o m\u1eebng b\u1ea1n \u0111\u1ebfn v\u1edbi th\u1ebf gi\u1edbi c\u1ee7a l\u1eadp tr\u00ecnh. \U0001f1fb\U0001f1f3",
        "Dlaczego warto uczy\u0107 si\u0119 j\u0119zyk\u00f3w obcych? \U0001f1f5\U0001f1f1",
        "E = mc\u00b2, uma equa\u00e7\u00e3o famosa na f\u00edsica. \U0001f1f5\U0001f1f9",
        "\u4f60\u4eca\u5929\u9047\u5230\u4ec0\u4e48\u6709\u8da3\u7684\u4e8b\u60c5\u4e86\u5417\uff1f\U0001f1e8\U0001f1f3",
        "N\u00e5 er det tid for \u00e5 feire med familie og venner. \U0001f1f3\U0001f1f4",
        "\u00deetta er g\u00f3\u00f0ur dagur til a\u00f0 l\u00e6ra eitthva\u00f0 n\u00fdtt. \U0001f1ee\U0001f1f8",
        "\u10d2\u10d0\u10db\u10d0\u10e0\u10ef\u10dd\u10d1\u10d0! \u10e0\u10dd\u10d2\u10dd\u10e0 \u10ee\u10d0\u10e0\u10d7 \u10d3\u10e6\u10d4\u10e1? \U0001f1ec\U0001f1ea",
        "M\u0101 te whakawhiti k\u014drero e whai hua ai t\u0101tou. \U0001f1f3\U0001f1ff",
        "\u042d\u0442\u043e \u0431\u044b\u043b \u043d\u0435\u0437\u0430\u0431\u044b\u0432\u0430\u0435\u043c\u044b\u0439 \u043e\u043f\u044b\u0442, \u043a\u043e\u0442\u043e\u0440\u044b\u0439 \u044f \u0431\u0443\u0434\u0443 \u043f\u043e\u043c\u043d\u0438\u0442\u044c \u0432\u0441\u0435\u0433\u0434\u0430.",
        "\u0394\u03b9\u03b1\u03b2\u03ac\u03b6\u03bf\u03bd\u03c4\u03b1\u03c2 \u03b2\u03b9\u03b2\u03bb\u03af\u03b1, \u03b5\u03bc\u03c0\u03bb\u03bf\u03c5\u03c4\u03af\u03b6\u03bf\u03c5\u03bc\u03b5 \u03c4\u03bf\u03bd \u03b5\u03b1\u03c5\u03c4\u03cc \u03bc\u03b1\u03c2 \u03bc\u03b5 \u03b3\u03bd\u03ce\u03c3\u03b5\u03b9\u03c2.",
        "A sz\u00e1m\u00edt\u00e1stechnika vil\u00e1ga tele van izgalmas lehet\u0151s\u00e9gekkel. \U0001f1ed\U0001f1fa",
        "V\u017edy je dobr\u00e9 m\u00edt pl\u00e1n B, pokud n\u011bco nevyjde. \U0001f1e8\U0001f1ff",
        "Dragostea e un sentiment minunat care ne une\u0219te pe to\u021bi. \U0001f1f7\U0001f1f4",
        "\u062f\u06cc\u06a9\u06be\u0648\u060c \u0622\u0633\u0645\u0627\u0646 \u0645\u06cc\u06ba \u06a9\u062a\u0646\u06cc \u062a\u0627\u0631\u06d2 \u06c1\u06cc\u06ba! \U0001f1f5\U0001f1f0",
        "Nenda polepole na ujifunze kila siku. \U0001f1f9\U0001f1ff",
        "\u041a\u0430\u043a\u0432\u0430 \u0435 \u0442\u0432\u043e\u044f\u0442\u0430 \u043b\u044e\u0431\u0438\u043c\u0430 \u0445\u0440\u0430\u043d\u0430? \U0001f1e7\U0001f1ec",
        "Str\u00e4va alltid efter att bli en b\u00e4ttre version av dig sj\u00e4lv.",
        "\u0424\u0456\u043b\u043e\u0441\u043e\u0444\u0456\u044f - \u0446\u0435 \u043d\u0430\u0443\u043a\u0430 \u043f\u0440\u043e \u0437\u043d\u0430\u043d\u043d\u044f. \U0001f1fa\U0001f1e6",
        "\u03a4\u03bf \u03c0\u03c1\u03cc\u03b3\u03c1\u03b1\u03bc\u03bc\u03b1 \u03b1\u03c5\u03c4\u03cc \u03b5\u03af\u03bd\u03b1\u03b9 \u03c0\u03bf\u03bb\u03cd \u03b5\u03bd\u03b4\u03b9\u03b1\u03c6\u03ad\u03c1\u03bf\u03bd. \U0001f1ec\U0001f1f7",
        "^$%#*@!&)(_+=}{|:;\"?><,~`'-./][",
        "4gH@!0sT*#(9^%$[x{}j+|Yz6;Q]~8",
        "wNb)I<>#:i^P]*cR8ytUx1Q`6O@z/",
        "\u00c4\u00dc\u00f6\u00bf\u00a1\u00a2\u00a3\u00a4\u00a5\u00a6\u00a7\u00a8\u00a9\u00aa\u00ab\u00ac\u00ae\u00af\u00b0\u00b1\u00b2\u00b3\u00b4\u00b5\u00b6\u00b7\u00b8\u00b9\u00ba\u00bb\u00bc\u00bd\u00be\u00bf",
        "\u0192\u0161\u0160\u0152\u017d\u0192\u0161\u0160\u0152\u017d\u0192\u0161\u0160\u0152\u017d\u0192\u0161\u0160\u0152\u017d\u0192\u0161\u0160\u0152\u017d\u0192\u0161\u0160\u0152\u017d",
        "5\u0127\u00c5\u0178\u0113\u00fd\u00ef\u016b\u0113$%#^*()_+{[\u00f6&!@#?>|,.<>",
        "1B4t#%&*()_+dF5g^hJk7LmN0pQrS<>?",
        "\u00ac\u00a7\u00b1\u00b2\u00b3\u00b5\u00b6\u00b7\u00b9\u00ba\u00aa\u00ab\u00bb\u00a6\u00a9\u00af\u00b0\u00b1!@#$%^&*()\x5f+",
        "8mR5*w7^a$!F(0%#J9@X6vZ1)nU3]_Y/",
        "\U0001f60a\U0001f600\U0001f601\U0001f602\U0001f923\U0001f603\U0001f604\U0001f605\U0001f606\U0001f609\U0001f60a\U0001f60b\U0001f60e\U0001f60d\U0001f618\U0001f617\U0001f619\U0001f61a\u263a\ufe0f\U0001f642\U0001f917\U0001f914",
        "\U0001f928\U0001f610\U0001f611\U0001f636\U0001f644\U0001f60f\U0001f623\U0001f625\U0001f62e\U0001f910\U0001f62f\U0001f62a\U0001f62b\U0001f634\U0001f60c\U0001f913\U0001f61b\U0001f61c\U0001f61d\U0001f924",
        "\U0001f612\U0001f613\U0001f614\U0001f615\U0001f643\U0001f911\U0001f632\U0001f637\U0001f912\U0001f915\U0001f922\U0001f927\U0001f608\U0001f47f\U0001f479\U0001f47a\U0001f480\u2620\ufe0f",
        "\U0001f63e\U0001f63f\U0001f640\U0001f63d\U0001f63c\U0001f63b\U0001f648\U0001f649\U0001f64a\U0001f476\U0001f466\U0001f467\U0001f468\U0001f469\U0001f474\U0001f475\U0001f468\u200d\u2695\ufe0f\U0001f469\u200d\u2695\ufe0f",
        "\U0001f31e\U0001f31d\U0001f31a\U0001f31b\U0001f31c\U0001f319\u2b50\ufe0f\U0001f31f\U0001f4ab\u2728\U0001f525\U0001f4a5\u2604\ufe0f\U0001f308\u2600\ufe0f\U0001f324\ufe0f\u26c5\ufe0f\U0001f325\ufe0f",
        "\U0001f34f\U0001f34e\U0001f350\U0001f34a\U0001f34b\U0001f34c\U0001f349\U0001f347\U0001f353\U0001f348\U0001f352\U0001f351",
        "\u0412 \u0446\u0435\u043f\u043e\u0447\u043a\u0430\u0445 \u043f\u043e\u0441\u0442\u0430\u0432\u043e\u043a \u043a\u0435\u0439\u0441-\u0441\u0442\u0430\u0434\u0438\u0438, \u043a\u043e\u0433\u0434\u0430 \u043d\u0430\u0437\u044b\u0432\u0430\u044e\u0442\u0441\u044f \u043e\u0434\u043d\u0430 \u0438\u043b\u0438 \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e \u0441\u0442\u043e\u0440\u043e\u043d, \u0441\u0442\u0440\u0430\u0434\u0430\u044e\u0442 \u043e\u0442 \u0441\u0435\u0440\u044c\u0435\u0437\u043d\u044b\u0445 \u043a\u043e\u043d\u0444\u043b\u0438\u043a\u0442\u043e\u0432 \u0438\u043d\u0442\u0435\u0440\u0435\u0441\u043e\u0432. \u041a\u043e\u043c\u043f\u0430\u043d\u0438\u0438 \u0438 \u0438\u0445 \u043f\u043e\u0434\u0434\u0435\u0440\u0436\u0438\u0432\u0430\u044e\u0449\u0438\u0435 \u043f\u043e\u0441\u0442\u0430\u0432\u0449\u0438\u043a\u0438 (\u043f\u0440\u043e\u0433\u0440\u0430\u043c\u043c\u043d\u043e\u0435 \u043e\u0431\u0435\u0441\u043f\u0435\u0447\u0435\u043d\u0438\u0435, \u043a\u043e\u043d\u0441\u0430\u043b\u0442\u0438\u043d\u0433) \u0438\u043c\u0435\u044e\u0442 \u0437\u0430\u0438\u043d\u0442\u0435\u0440\u0435\u0441\u043e\u0432\u0430\u043d\u043d\u043e\u0441\u0442\u044c \u0432 \u043f\u0440\u0435\u0434\u0441\u0442\u0430\u0432\u043b\u0435\u043d\u0438\u0438 \u0440\u0435\u0437\u0443\u043b\u044c\u0442\u0430\u0442\u0430 \u0432 \u043f\u043e\u043b\u043e\u0436\u0438\u0442\u0435\u043b\u044c\u043d\u043e\u043c \u0441\u0432\u0435\u0442\u0435. \u041a\u0440\u043e\u043c\u0435 \u0442\u043e\u0433\u043e, \u0444\u0430\u043a\u0442\u0438\u0447\u0435\u0441\u043a\u0438\u0435 \u0446\u0435\u043f\u043e\u0447\u043a\u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u043e\u043a \u043e\u0431\u044b\u0447\u043d\u043e \u043f\u043e\u043b\u0443\u0447\u0430\u044e\u0442 \u043f\u043e\u043b\u044c\u0437\u0443 \u0438\u043b\u0438 \u043f\u043e\u0441\u0442\u0440\u0430\u0434\u0430\u044e\u0442 \u043e\u0442 \u0441\u043b\u0443\u0447\u0430\u0439\u043d\u044b\u0445 \u0443\u0441\u043b\u043e\u0432\u0438\u0439, \u043a\u043e\u0442\u043e\u0440\u044b\u0435 \u043d\u0438\u043a\u0430\u043a \u043d\u0435 \u0441\u0432\u044f\u0437\u0430\u043d\u044b \u0441 \u043a\u0430\u0447\u0435\u0441\u0442\u0432\u043e\u043c \u0438\u0445 \u0438\u0441\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u044f. \u041f\u0435\u0440\u0441\u043e\u043d\u0430\u0436\u0438 \u0446\u0435\u043f\u043e\u0447\u043a\u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u043e\u043a - \u044d\u0442\u043e \u043c\u0435\u0442\u043e\u0434\u043e\u043b\u043e\u0433\u0438\u0447\u0435\u0441\u043a\u0438\u0439 \u043e\u0442\u0432\u0435\u0442 \u043d\u0430 \u044d\u0442\u0438 \u043f\u0440\u043e\u0431\u043b\u0435\u043c\u044b.",
    ];

    internal static IEnumerable<Tuple<string, string, List<int>>> ReadTestPlans(H.Resource resource)
    {
        using var stream = resource.AsStream();
        using var reader = new StreamReader(stream);
        return ReadTestPlansFromReader(reader);
    }

    internal static List<Tuple<string, string, List<int>>> ReadTestPlansFromString(string content)
    {
        using var reader = new StringReader(content);
        return ReadTestPlansFromReader(reader);
    }

    private static List<Tuple<string, string, List<int>>> ReadTestPlansFromReader(TextReader reader)
    {
        var testPlans = new List<Tuple<string, string, List<int>>>();

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("EncodingName: "))
            {
                var encodingName = line["EncodingName: ".Length..];
                var sample = reader.ReadLine()!["Sample: ".Length..];
                var encodedStr = reader.ReadLine()!["Encoded: ".Length..];

                var encoded = Regex.Matches(encodedStr, @"\d+")
                    .Select(m => int.Parse(m.Value, CultureInfo.InvariantCulture))
                    .ToList();

                testPlans.Add(Tuple.Create(encodingName, sample, encoded));
            }
        }

        return testPlans;
    }

    /// <summary>
    /// Regenerates TestPlans.txt from the .NET encoder for all samples and encodings.
    /// Run manually when samples or encodings change:
    ///   dotnet test --filter GenerateTestPlansFromDotNet
    /// Then commit the updated TestPlans.txt.
    /// </summary>
    [TestMethod]
    [Ignore("Run manually to regenerate TestPlans.txt from .NET encoder")]
    public void GenerateTestPlansFromDotNet()
    {
        // Walk up from test output to find Resources directory
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "Tiktoken.UnitTests.csproj")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        dir ??= ".";
        var outputPath = Path.Combine(dir, "Resources", "TestPlans.txt");

        using var writer = new StreamWriter(outputPath, append: false, encoding: System.Text.Encoding.UTF8);

        foreach (var encodingName in EncodingNames)
        {
            var encoding = ModelToEncoding.ForEncoding(encodingName);
            var encoder = new Encoder(encoding);

            foreach (var sample in TestSamples)
            {
                var tokens = encoder.Encode(sample);
                var tokenStr = "[" + string.Join(", ", tokens) + "]";
                writer.WriteLine($"EncodingName: {encodingName}");
                writer.WriteLine($"Sample: {sample}");
                writer.WriteLine($"Encoded: {tokenStr}");
                writer.WriteLine();
            }
        }
    }
}