using System.CommandLine;
using System.CommandLine.Parsing;
using Tiktoken;
using Tiktoken.Cli.IO;
using Tiktoken.Cli.Output;

var pathsArg = new Argument<string[]>("paths")
{
    Description = "Files or directories to count tokens for. Reads from stdin if omitted.",
    Arity = ArgumentArity.ZeroOrMore,
};

var modelOption = new Option<string>("--model", ["-m"])
{
    Description = "Model name for tokenizer selection.",
    DefaultValueFactory = _ => "gpt-4o",
};

var encodingOption = new Option<string?>("--encoding", ["-e"])
{
    Description = "Encoding name (e.g. cl100k_base, o200k_base). Overrides --model.",
};

var encodeOption = new Option<bool>("--encode")
{
    Description = "Output token IDs instead of count.",
};

var decodeOption = new Option<bool>("--decode")
{
    Description = "Decode token IDs from input back to text.",
};

var exploreOption = new Option<bool>("--explore")
{
    Description = "Show token boundaries with | delimiters.",
};

var truncateOption = new Option<int?>("--truncate", ["-t"])
{
    Description = "Truncate input to N tokens and output the result.",
};

var includeOption = new Option<string[]>("--include")
{
    Description = "Include only files matching these patterns (e.g. *.cs).",
    Arity = ArgumentArity.ZeroOrMore,
};

var excludeOption = new Option<string[]>("--exclude")
{
    Description = "Exclude files matching these patterns (e.g. **/bin/**).",
    Arity = ArgumentArity.ZeroOrMore,
};

var maxFileSizeOption = new Option<string>("--max-file-size")
{
    Description = "Skip files larger than this size (e.g. 10mb, 500kb).",
    DefaultValueFactory = _ => "50mb",
};

var formatOption = new Option<string>("--format", ["-f"])
{
    Description = "Output format: table or json.",
    DefaultValueFactory = _ => "table",
};

var maxTokensOption = new Option<int?>("--max-tokens")
{
    Description = "Exit with code 2 if total tokens exceed this limit.",
};

var contextCheckOption = new Option<bool>("--context-check")
{
    Description = "Show token count as percentage of model's context window.",
};

var sortOption = new Option<string?>("--sort")
{
    Description = "Sort results: tokens (largest first) or name.",
};

var groupByOption = new Option<string?>("--group-by")
{
    Description = "Group results by: ext.",
};

var topOption = new Option<int?>("--top")
{
    Description = "Show only the top N files by token count.",
};

var noDefaultExcludesOption = new Option<bool>("--no-default-excludes")
{
    Description = "Disable default directory exclusions (bin, obj, node_modules, etc.).",
};

var noGitignoreOption = new Option<bool>("--no-gitignore")
{
    Description = "Disable .gitignore processing (include all non-binary files).",
};

var quietOption = new Option<bool>("--quiet", ["-q"])
{
    Description = "Suppress the summary footer (separator + total line).",
};

var progressOption = new Option<bool>("--progress")
{
    Description = "Show progress to stderr during directory scans.",
};

var statsOption = new Option<bool>("--stats")
{
    Description = "Show scan statistics (files examined, skipped, timing) to stderr.",
};

var rootCommand = new RootCommand("Count, encode, decode, and explore BPE tokens — powered by the fastest .NET tokenizer.")
{
    pathsArg,
    modelOption,
    encodingOption,
    encodeOption,
    decodeOption,
    exploreOption,
    truncateOption,
    includeOption,
    excludeOption,
    maxFileSizeOption,
    formatOption,
    maxTokensOption,
    contextCheckOption,
    sortOption,
    groupByOption,
    topOption,
    noDefaultExcludesOption,
    noGitignoreOption,
    quietOption,
    progressOption,
    statsOption,
};

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var paths = parseResult.GetValue(pathsArg) ?? [];
    var model = parseResult.GetValue(modelOption) ?? "gpt-4o";
    var encodingName = parseResult.GetValue(encodingOption);
    var encode = parseResult.GetValue(encodeOption);
    var decode = parseResult.GetValue(decodeOption);
    var explore = parseResult.GetValue(exploreOption);
    var truncate = parseResult.GetValue(truncateOption);
    var include = parseResult.GetValue(includeOption) ?? [];
    var exclude = parseResult.GetValue(excludeOption) ?? [];
    var maxFileSizeStr = parseResult.GetValue(maxFileSizeOption) ?? "50mb";
    var format = parseResult.GetValue(formatOption) ?? "table";
    var maxTokens = parseResult.GetValue(maxTokensOption);
    var contextCheck = parseResult.GetValue(contextCheckOption);
    var sort = parseResult.GetValue(sortOption);
    var groupBy = parseResult.GetValue(groupByOption);
    var top = parseResult.GetValue(topOption);
    var noDefaultExcludes = parseResult.GetValue(noDefaultExcludesOption);
    var noGitignore = parseResult.GetValue(noGitignoreOption);
    var quiet = parseResult.GetValue(quietOption);
    var progress = parseResult.GetValue(progressOption);
    var stats = parseResult.GetValue(statsOption);

    // Resolve encoder — use ModelToEncoder cache for both paths to avoid
    // recreating the encoder (and its internal BPE cache) on every invocation.
    Encoder encoder;
    try
    {
        if (encodingName != null)
        {
            // Wrap encoding name as a pseudo-model so we go through the same cache
            var encoding = ModelToEncoding.ForEncoding(encodingName);
            encoder = new Encoder(encoding);
        }
        else
        {
            encoder = ModelToEncoder.For(model);
        }
    }
    catch (ArgumentException ex)
    {
        await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
        return 1;
    }

    // Warm up the encoder's internal regex + BPE cache with a trivial call
    // so the first real file doesn't pay the cold-start cost.
    encoder.CountTokens("warmup");

    var maxFileSize = ParseSize(maxFileSizeStr);
    var writer = Console.Out;

    // No paths: read from stdin
    if (paths.Length == 0)
    {
        if (Console.IsInputRedirected)
        {
            var input = await Console.In.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            HandleStdinMode(input, encoder, encode, decode, explore, truncate, writer);
            return 0;
        }

        await Console.Error.WriteLineAsync("Error: No input. Pipe text via stdin or provide file/directory paths.").ConfigureAwait(false);
        return 1;
    }

    // Collect files from all paths
    var allFiles = new List<(string FullPath, string RelativePath)>();
    var scanStopwatch = System.Diagnostics.Stopwatch.StartNew();
    FileScanner? lastScanner = null;

    foreach (var path in paths)
    {
        if (Directory.Exists(path))
        {
            var scanner = new FileScanner(include, exclude, maxFileSize, noDefaultExcludes, noGitignore);
            var files = scanner.Scan(path);
            lastScanner = scanner;
            var root = Path.GetFullPath(path);
            foreach (var file in files)
            {
                allFiles.Add((file, Path.GetRelativePath(root, file)));
            }
        }
        else if (File.Exists(path))
        {
            allFiles.Add((Path.GetFullPath(path), path));
        }
        else
        {
            await Console.Error.WriteLineAsync($"Error: '{path}' not found.").ConfigureAwait(false);
            return 1;
        }
    }

    var scanElapsed = scanStopwatch.Elapsed;

    // Single file + special modes
    if (allFiles.Count == 1 && (encode || explore || truncate.HasValue))
    {
        var text = await File.ReadAllTextAsync(allFiles[0].FullPath, cancellationToken).ConfigureAwait(false);
        HandleStdinMode(text, encoder, encode, false, explore, truncate, writer);
        return 0;
    }

    // Count tokens for all files in parallel
    var tokenizeStopwatch = System.Diagnostics.Stopwatch.StartNew();
    var results = new FileTokenResult[allFiles.Count];
    var processed = 0;
    Parallel.For(0, allFiles.Count, i =>
    {
        string text;
        try
        {
            text = File.ReadAllText(allFiles[i].FullPath);
        }
        catch (IOException)
        {
            results[i] = new FileTokenResult(allFiles[i].RelativePath, 0);
            return; // File vanished between scan and read
        }

        var tokens = encoder.CountTokens(text);
        results[i] = new FileTokenResult(allFiles[i].RelativePath, tokens);

        if (progress)
        {
            var count = Interlocked.Increment(ref processed);
            Console.Error.Write($"\r  Counting tokens... {count}/{allFiles.Count}");
        }
    });
    var tokenizeElapsed = tokenizeStopwatch.Elapsed;

    if (progress)
    {
        Console.Error.WriteLine();
    }

    var resultsList = results.ToList();

    // Apply sorting
    if (sort == "tokens" || (top.HasValue && sort == null))
    {
        resultsList.Sort((a, b) => b.Tokens.CompareTo(a.Tokens));
    }
    else if (sort == "name")
    {
        resultsList.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
    }

    // Compute total before --top truncation (so total reflects all files)
    var totalAllFiles = resultsList.Sum(r => (long)r.Tokens);
    var totalFileCount = resultsList.Count;

    // Apply --top
    if (top.HasValue && top.Value < resultsList.Count)
    {
        resultsList = resultsList.Take(top.Value).ToList();
    }

    // Output
    if (format == "json")
    {
        JsonFormatter.WriteFileResults(resultsList, writer);
    }
    else if (groupBy == "ext")
    {
        TableFormatter.WriteGroupedResults(resultsList, writer, quiet);
    }
    else
    {
        TableFormatter.WriteFileResults(resultsList, writer, quiet);
    }

    // Context check
    if (contextCheck)
    {
        var contextWindow = GetContextWindow(model);
        TableFormatter.WriteContextCheck(totalAllFiles, model, contextWindow, writer);
    }

    // Stats output
    if (stats && lastScanner != null)
    {
        var s = lastScanner.Stats;
        var totalElapsed = scanElapsed + tokenizeElapsed;
        await Console.Error.WriteLineAsync($"""

            Scan statistics:
              Files examined:      {s.FilesExamined:N0}
              Files included:      {allFiles.Count:N0}
              Files gitignored:    {s.FilesGitignored:N0}
              Files filtered out:  {s.FilesFilteredOut:N0}
              Files too large:     {s.FilesTooLarge:N0}
              Files binary:        {s.FilesBinary:N0}
              Dirs default-excl:   {s.DirsDefaultExcluded:N0}
              Dirs gitignored:     {s.DirsGitignored:N0}
            Timing:
              Scan:                {scanElapsed.TotalSeconds:F2}s
                Gitignore match:   {s.GitignoreMatchTime.TotalSeconds:F3}s (CPU)
                File stat():       {s.StatSizeTime.TotalSeconds:F3}s (CPU)
                Binary detect:     {s.BinaryDetectTime.TotalSeconds:F3}s (CPU)
              Tokenize:            {tokenizeElapsed.TotalSeconds:F2}s
              Total:               {totalElapsed.TotalSeconds:F2}s
            """).ConfigureAwait(false);
    }

    // Max tokens check
    if (maxTokens.HasValue)
    {
        if (totalAllFiles > maxTokens.Value)
        {
            await Console.Error.WriteLineAsync(
                $"Token limit exceeded: {totalAllFiles:N0} > {maxTokens.Value:N0}").ConfigureAwait(false);
            return 2;
        }
    }

    return 0;
});

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args).ConfigureAwait(false);

static void HandleStdinMode(
    string input,
    Encoder encoder,
    bool encode,
    bool decode,
    bool explore,
    int? truncate,
    TextWriter writer)
{
    if (decode)
    {
        var ids = input.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();
        writer.WriteLine(encoder.Decode(ids));
        return;
    }

    if (truncate.HasValue)
    {
        var tokens = encoder.Encode(input);
        var truncated = tokens.Take(truncate.Value).ToArray();
        writer.Write(encoder.Decode(truncated));
        return;
    }

    if (encode)
    {
        var tokens = encoder.Encode(input);
        writer.WriteLine(string.Join(" ", tokens));
        return;
    }

    if (explore)
    {
        var parts = encoder.Explore(input);
        writer.Write("|");
        foreach (var part in parts)
        {
            writer.Write(part);
            writer.Write("|");
        }
        writer.WriteLine();
        return;
    }

    // Default: count tokens
    writer.WriteLine(encoder.CountTokens(input));
}

static long ParseSize(string sizeStr)
{
    sizeStr = sizeStr.Trim().ToLowerInvariant();

    if (long.TryParse(sizeStr, out var bytes))
    {
        return bytes;
    }

    if (sizeStr.EndsWith("gb"))
    {
        return (long)(double.Parse(sizeStr[..^2]) * 1024 * 1024 * 1024);
    }
    if (sizeStr.EndsWith("mb"))
    {
        return (long)(double.Parse(sizeStr[..^2]) * 1024 * 1024);
    }
    if (sizeStr.EndsWith("kb"))
    {
        return (long)(double.Parse(sizeStr[..^2]) * 1024);
    }

    return 50 * 1024 * 1024; // default 50MB
}

static int GetContextWindow(string model) => model switch
{
    "gpt-4o" or "gpt-4o-mini" or "chatgpt-4o-latest" => 128_000,
    "gpt-4.5-preview" => 128_000,
    "gpt-4.1" or "gpt-4.1-mini" or "gpt-4.1-nano" => 1_047_576,
    "gpt-4-turbo" => 128_000,
    "gpt-4" => 8_192,
    "gpt-3.5-turbo" or "gpt-35-turbo" => 16_385,
    "o1" or "o1-mini" => 128_000,
    "o3" or "o3-mini" or "o3-pro" => 200_000,
    "o4-mini" => 200_000,
    _ => 128_000, // sensible default
};
