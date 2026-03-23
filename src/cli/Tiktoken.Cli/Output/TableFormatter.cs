namespace Tiktoken.Cli.Output;

internal static class TableFormatter
{
    public static void WriteFileResults(
        IReadOnlyList<FileTokenResult> results,
        TextWriter writer,
        bool quiet = false)
    {
        if (results.Count == 0)
        {
            return;
        }

        if (results.Count == 1)
        {
            writer.WriteLine(results[0].Tokens.ToString("N0"));
            return;
        }

        var maxPathLen = results.Max(r => r.RelativePath.Length);
        var maxTokenLen = results.Max(r => r.Tokens.ToString("N0").Length);
        maxTokenLen = Math.Max(maxTokenLen, "tokens".Length);

        foreach (var result in results)
        {
            var tokenStr = result.Tokens.ToString("N0").PadLeft(maxTokenLen);
            writer.Write(result.RelativePath.PadRight(maxPathLen));
            writer.Write("  ");
            writer.WriteLine(tokenStr);
        }

        if (!quiet)
        {
            var total = results.Sum(r => (long)r.Tokens);
            var totalStr = total.ToString("N0").PadLeft(maxTokenLen);
            var separator = new string('\u2500', maxPathLen + 2 + maxTokenLen);
            writer.WriteLine(separator);
            writer.Write((results.Count + " files").PadRight(maxPathLen));
            writer.Write("  ");
            writer.Write(totalStr);
            writer.WriteLine(" tokens");
        }
    }

    public static void WriteGroupedResults(
        IReadOnlyList<FileTokenResult> results,
        TextWriter writer,
        bool quiet = false)
    {
        var groups = results
            .GroupBy(r => Path.GetExtension(r.RelativePath).ToLowerInvariant())
            .Select(g => new
            {
                Extension = g.Key.Length == 0 ? "(no ext)" : g.Key,
                Tokens = g.Sum(r => (long)r.Tokens),
                Count = g.Count(),
            })
            .OrderByDescending(g => g.Tokens)
            .ToList();

        var maxExtLen = groups.Max(g => g.Extension.Length);
        var maxTokenLen = groups.Max(g => g.Tokens.ToString("N0").Length);

        foreach (var group in groups)
        {
            var tokenStr = group.Tokens.ToString("N0").PadLeft(maxTokenLen);
            var countStr = "(" + group.Count + " " + (group.Count == 1 ? "file" : "files") + ")";
            writer.Write(group.Extension.PadRight(maxExtLen));
            writer.Write("  ");
            writer.Write(tokenStr);
            writer.Write("  ");
            writer.WriteLine(countStr);
        }

        if (!quiet)
        {
            var total = groups.Sum(g => g.Tokens);
            var totalStr = total.ToString("N0").PadLeft(maxTokenLen);
            var totalCountStr = "(" + results.Count + " " + (results.Count == 1 ? "file" : "files") + ")";
            var separator = new string('\u2500', maxExtLen + 2 + maxTokenLen + 2 + totalCountStr.Length);
            writer.WriteLine(separator);
            writer.Write("total".PadRight(maxExtLen));
            writer.Write("  ");
            writer.Write(totalStr);
            writer.Write("  ");
            writer.WriteLine(totalCountStr);
        }
    }

    public static void WriteContextCheck(
        long totalTokens,
        string modelName,
        int contextWindow,
        TextWriter writer)
    {
        var percent = contextWindow > 0
            ? (double)totalTokens / contextWindow * 100
            : 0;

        var status = totalTokens <= contextWindow ? "\u2713" : "\u2717";
        writer.WriteLine(
            $"Total: {totalTokens:N0} tokens ({percent:F0}% of {contextWindow:N0} context window) {status}");
    }
}

internal record FileTokenResult(string RelativePath, int Tokens);
