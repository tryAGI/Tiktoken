using System.Text.RegularExpressions;
using BenchmarkDotNet.Running;
using Tiktoken.Benchmarks;

var summary = BenchmarkRunner.Run<Benchmarks>();
var markdownPath = Directory.EnumerateFiles(summary.ResultsDirectoryPath, "*.md").First();
var markdown = File.ReadAllText(markdownPath);
var repositoryFolder = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "..");
var readmePath = Path.Combine(repositoryFolder, "README.md");
var readme = File.ReadAllText(readmePath);
var newReadme = BenchmarksRegex().Replace(readme, $@"<!--BENCHMARKS_START-->
{markdown}
<!--BENCHMARKS_END-->");
File.WriteAllText(readmePath, newReadme);

var version = typeof(Tiktoken.Encoding).Assembly.GetName().Version;
var benchmarkFolder = Path.Combine(repositoryFolder, "benchmarks");
Directory.CreateDirectory(benchmarkFolder);
var benchmarkPath = Path.Combine(benchmarkFolder, $"{version}_encode.md");
File.WriteAllText(benchmarkPath, markdown);

internal partial class Program
{
    [GeneratedRegex(@"<!--BENCHMARKS_START-->[\s\S]*<!--BENCHMARKS_END-->", RegexOptions.Multiline)]
    private static partial Regex BenchmarksRegex();
}