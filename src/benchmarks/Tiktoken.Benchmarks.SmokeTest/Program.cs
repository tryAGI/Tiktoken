using System.Diagnostics;
using Tiktoken;
using Tiktoken.Benchmarks;

// Local performance smoke test — verifies cache speedup ratios are healthy.
// Ratios are machine-independent (test code path behavior, not absolute speed).
// Run: dotnet run -c Release --project src/benchmarks/Tiktoken.Benchmarks.SmokeTest/

var encoder = ModelToEncoder.For("gpt-4o");

var tests = new (string Name, string Text, double MinCacheSpeedup)[]
{
    ("Hello (ASCII)",       Strings.HelloWorld,      0.0), // no cache effect expected
    ("Multilingual",        Strings.Multilingual,    3.0), // expect >= 3x (typically ~5x)
    ("CJK-heavy",           Strings.CjkHeavy,        6.0), // expect >= 6x (typically ~13x)
    ("Python code",         Strings.Code,            0.0), // no cache effect expected
    ("Multilingual long",   Strings.MultilingualLong, 4.0), // expect >= 4x (typically ~8x)
    ("Bitcoin whitepaper",  Strings.Bitcoin,          0.0), // minimal cache effect
};

Console.WriteLine("Tiktoken Performance Smoke Test");
Console.WriteLine("===============================");
Console.WriteLine();

var failures = 0;

foreach (var (name, text, minSpeedup) in tests)
{
    // Warmup with cache enabled (fills cache)
    var cachedEncoder = ModelToEncoder.For("gpt-4o");
    for (var i = 0; i < 200; i++)
        cachedEncoder.CountTokens(text);

    // Measure cached
    var cachedBest = long.MaxValue;
    for (var run = 0; run < 5; run++)
    {
        var sw = Stopwatch.StartNew();
        const int iters = 10000;
        for (var i = 0; i < iters; i++)
            cachedEncoder.CountTokens(text);
        sw.Stop();
        if (sw.ElapsedTicks < cachedBest)
            cachedBest = sw.ElapsedTicks;
    }
    var cachedNs = (double)cachedBest / Stopwatch.Frequency * 1e9 / 10000;

    // Measure no-cache (fresh encoder with cache disabled)
    var noCacheEncoder = new Encoder(new Tiktoken.Encodings.O200KBase()) { EnableCache = false };
    // Warmup no-cache
    for (var i = 0; i < 50; i++)
        noCacheEncoder.CountTokens(text);

    var noCacheBest = long.MaxValue;
    for (var run = 0; run < 5; run++)
    {
        var sw = Stopwatch.StartNew();
        const int iters = 5000;
        for (var i = 0; i < iters; i++)
            noCacheEncoder.CountTokens(text);
        sw.Stop();
        if (sw.ElapsedTicks < noCacheBest)
            noCacheBest = sw.ElapsedTicks;
    }
    var noCacheNs = (double)noCacheBest / Stopwatch.Frequency * 1e9 / 5000;

    var ratio = noCacheNs / cachedNs;
    var status = minSpeedup > 0 ? (ratio >= minSpeedup ? "PASS" : "FAIL") : "----";

    if (status == "FAIL")
        failures++;

    var ratioStr = ratio > 1.3 ? $"{ratio:F1}x" : "~1.0x";
    var thresholdStr = minSpeedup > 0 ? $">= {minSpeedup:F0}x" : "n/a";

    Console.WriteLine($"  {status}  {name,-22} cached: {FormatNs(cachedNs),10}  no-cache: {FormatNs(noCacheNs),10}  ratio: {ratioStr,6}  (threshold: {thresholdStr})");
}

Console.WriteLine();
if (failures > 0)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"FAILED: {failures} test(s) below threshold — cache fast path may be broken");
    Console.ResetColor();
    return 1;
}
else
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("ALL PASSED: Cache speedup ratios are healthy");
    Console.ResetColor();
    return 0;
}

static string FormatNs(double ns)
{
    if (ns >= 1_000_000) return $"{ns / 1_000_000:F1} ms";
    if (ns >= 1_000) return $"{ns / 1_000:F1} us";
    return $"{ns:F0} ns";
}
