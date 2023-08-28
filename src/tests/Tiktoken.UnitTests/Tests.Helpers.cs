using System.Globalization;
using System.Text.RegularExpressions;

namespace Tiktoken.UnitTests;

public partial class Tests : VerifyBase
{
    internal static IEnumerable<Tuple<string, string, List<int>>> ReadTestPlans(H.Resource resource)
    {
        var testPlans = new List<Tuple<string, string, List<int>>>();

        using var stream = resource.AsStream();
        using var reader = new StreamReader(stream);

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
}