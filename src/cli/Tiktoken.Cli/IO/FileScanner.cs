using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Enumeration;

namespace Tiktoken.Cli.IO;

internal sealed class FileScanner
{
    private readonly List<string> _includePatterns;
    private readonly List<string> _excludePatterns;
    private readonly long _maxFileSize;
    private readonly bool _noDefaultExcludes;
    private readonly bool _noGitignore;

    private static readonly FrozenSet<string> DefaultExcludedDirs =
        FrozenSet.ToFrozenSet(
            [".git", ".hg", ".svn", "node_modules", "__pycache__", "bin", "obj"],
            StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> KnownBinaryExtensions =
        FrozenSet.ToFrozenSet(
        [
            ".exe", ".dll", ".pdb", ".obj", ".bin", ".so", ".dylib",
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".svg",
            ".mp3", ".mp4", ".wav", ".avi", ".mkv", ".mov", ".flac", ".ogg",
            ".zip", ".gz", ".tar", ".7z", ".rar", ".bz2", ".xz", ".zst",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".woff", ".woff2", ".ttf", ".otf", ".eot",
            ".nupkg", ".snupkg", ".ttkb",
            ".class", ".pyc", ".o", ".a", ".lib",
        ], StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> KnownTextExtensions =
        FrozenSet.ToFrozenSet(
        [
            ".cs", ".csx", ".fs", ".fsx", ".vb",
            ".json", ".jsonl", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf",
            ".md", ".mdx", ".txt", ".text", ".log", ".csv", ".tsv",
            ".html", ".htm", ".css", ".scss", ".sass", ".less",
            ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs", ".mts", ".cts",
            ".py", ".pyi", ".rb", ".go", ".rs", ".java", ".kt", ".kts", ".scala",
            ".c", ".h", ".cpp", ".hpp", ".cc", ".hh", ".cxx", ".hxx",
            ".swift", ".m", ".mm",
            ".sh", ".bash", ".zsh", ".fish", ".ps1", ".psm1", ".bat", ".cmd",
            ".sql", ".graphql", ".gql", ".proto",
            ".r", ".R", ".jl", ".lua", ".pl", ".pm", ".php",
            ".tf", ".hcl", ".dockerfile", ".makefile",
            ".gitignore", ".gitattributes", ".editorconfig", ".prettierrc",
            ".sln", ".slnx", ".csproj", ".fsproj", ".vbproj", ".props", ".targets",
            ".razor", ".cshtml",
            ".env", ".lock",
        ], StringComparer.OrdinalIgnoreCase);

    private static readonly bool s_isWindows = OperatingSystem.IsWindows();

    public ScanStats Stats { get; private set; } = new();

    public FileScanner(
        IEnumerable<string>? includePatterns = null,
        IEnumerable<string>? excludePatterns = null,
        long maxFileSize = 50 * 1024 * 1024,
        bool noDefaultExcludes = false,
        bool noGitignore = false)
    {
        _includePatterns = includePatterns?.ToList() ?? [];
        _excludePatterns = excludePatterns?.ToList() ?? [];
        _maxFileSize = maxFileSize;
        _noDefaultExcludes = noDefaultExcludes;
        _noGitignore = noGitignore;
    }

    public IReadOnlyList<string> Scan(string rootPath)
    {
        rootPath = Path.GetFullPath(rootPath);

        // Pre-compute prefix length for fast relative path extraction
        var rootPrefixLen = rootPath.Length;
        if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
        {
            rootPrefixLen++;
        }

        // Load parent + global gitignores once upfront
        var parentIgnores = new List<(string Directory, GitignoreMatcher Matcher)>();
        if (!_noGitignore)
        {
            LoadParentGitignores(rootPath, parentIgnores);
            LoadGlobalGitignore(parentIgnores);
        }

        // Pre-compute whether any parent gitignore has file-matching rules
        var anyParentHasFileRules = false;
        foreach (var (_, matcher) in parentIgnores)
        {
            if (matcher.HasFileRules)
            {
                anyParentHasFileRules = true;
                break;
            }
        }

        var localStats = new ScanStats();
        var results = new List<string>();
        ScanDirectory(rootPath, rootPath, rootPrefixLen, parentIgnores, anyParentHasFileRules, results, localStats, depth: 0);
        results.Sort(StringComparer.OrdinalIgnoreCase);
        Stats = localStats;
        return results;
    }

    /// <summary>
    /// Single-pass directory entry — captures only path and type from readdir.
    /// Length is NOT included because on macOS/Linux it triggers an extra stat() syscall
    /// per entry. We defer the stat call to after gitignore/filter checks pass.
    /// </summary>
    private readonly record struct DirEntry(string FullPath, bool IsDirectory);

    /// <summary>
    /// Enumerates all entries (files + directories) in a single readdir pass using
    /// FileSystemEnumerable. Only extracts path and IsDirectory (from d_type on Unix),
    /// avoiding per-entry stat() calls.
    /// </summary>
    private static FileSystemEnumerable<DirEntry> EnumerateEntries(string dirPath)
    {
        return new FileSystemEnumerable<DirEntry>(
            dirPath,
            static (ref FileSystemEntry entry) => new DirEntry(
                entry.ToFullPath(),
                entry.IsDirectory),
            new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
                AttributesToSkip = 0, // Don't skip hidden files — let gitignore handle that
            });
    }

    /// <summary>
    /// Recursively scans a directory, skipping excluded directories at the directory level
    /// and loading nested .gitignore files on-the-fly.
    /// Depth 0 and 1 subdirectories are scanned in parallel for throughput.
    /// Uses thread-local results and stats to avoid contention.
    /// </summary>
    private void ScanDirectory(
        string dirPath,
        string rootPath,
        int rootPrefixLen,
        List<(string Directory, GitignoreMatcher Matcher)> gitignores,
        bool anyHasFileRules,
        List<string> results,
        ScanStats localStats,
        int depth)
    {
        // Load .gitignore from this directory if it exists
        var localMatcher = _noGitignore ? null : LoadLocalGitignore(dirPath);
        List<(string Directory, GitignoreMatcher Matcher)> effectiveIgnores;
        bool effectiveHasFileRules;

        if (localMatcher != null)
        {
            effectiveIgnores = [.. gitignores, (dirPath, localMatcher)];
            effectiveHasFileRules = anyHasFileRules || localMatcher.HasFileRules;
        }
        else
        {
            effectiveIgnores = gitignores;
            effectiveHasFileRules = anyHasFileRules;
        }

        // Guard against symlink loops and extremely deep paths
        if (depth > 64)
        {
            localStats.DirsErrored++;
            return;
        }

        List<string>? subDirs = null;

        // SINGLE PASS: enumerate files and directories together
        IEnumerable<DirEntry> entries;
        try
        {
            entries = EnumerateEntries(dirPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException)
        {
            localStats.DirsErrored++;
            return;
        }

        try
        {
            foreach (var entry in entries)
            {
                if (entry.IsDirectory)
                {
                    var dirName = Path.GetFileName(entry.FullPath);

                    if (!_noDefaultExcludes && DefaultExcludedDirs.Contains(dirName))
                    {
                        localStats.DirsDefaultExcluded++;
                        continue;
                    }

                    // Always check directory gitignore (directory-only rules are common)
                    if (effectiveIgnores.Count > 0)
                    {
                        var relDir = entry.FullPath.Length >= rootPrefixLen
                            ? entry.FullPath.Substring(rootPrefixLen)
                            : Path.GetRelativePath(rootPath, entry.FullPath);

                        if (s_isWindows)
                        {
                            relDir = relDir.Replace('\\', '/');
                        }

                        var t0 = Stopwatch.GetTimestamp();
                        var ignored = IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relDir + "/") ||
                            IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relDir);
                        localStats.TicksGitignoreMatch += Stopwatch.GetTimestamp() - t0;

                        if (ignored)
                        {
                            localStats.DirsGitignored++;
                            continue;
                        }
                    }

                    subDirs ??= [];
                    subDirs.Add(entry.FullPath);
                }
                else
                {
                    localStats.FilesExamined++;

                    var relativePath = entry.FullPath.Length >= rootPrefixLen
                        ? entry.FullPath.Substring(rootPrefixLen)
                        : Path.GetRelativePath(rootPath, entry.FullPath);

                    if (s_isWindows)
                    {
                        relativePath = relativePath.Replace('\\', '/');
                    }

                    // Directory-level cache: skip per-file gitignore check when no rules
                    // in the effective set can match files (only directory-only rules exist).
                    if (effectiveHasFileRules)
                    {
                        var t0 = Stopwatch.GetTimestamp();
                        var ignored = IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relativePath);
                        localStats.TicksGitignoreMatch += Stopwatch.GetTimestamp() - t0;
                        if (ignored)
                        {
                            localStats.FilesGitignored++;
                            continue;
                        }
                    }

                    if (_includePatterns.Count > 0 &&
                        !_includePatterns.Any(p => MatchesPattern(relativePath, p)))
                    {
                        localStats.FilesFilteredOut++;
                        continue;
                    }

                    if (_excludePatterns.Any(p => MatchesPattern(relativePath, p)))
                    {
                        localStats.FilesFilteredOut++;
                        continue;
                    }

                    // Defer stat() until after gitignore/filter checks pass.
                    var tStat = Stopwatch.GetTimestamp();
                    long fileSize;
                    try
                    {
                        fileSize = new FileInfo(entry.FullPath).Length;
                    }
                    catch (IOException)
                    {
                        localStats.TicksStatSize += Stopwatch.GetTimestamp() - tStat;
                        continue;
                    }
                    localStats.TicksStatSize += Stopwatch.GetTimestamp() - tStat;

                    if (fileSize > _maxFileSize)
                    {
                        localStats.FilesTooLarge++;
                        continue;
                    }

                    var tBin = Stopwatch.GetTimestamp();
                    var isBin = IsBinary(entry.FullPath, fileSize);
                    localStats.TicksBinaryDetect += Stopwatch.GetTimestamp() - tBin;

                    if (isBin)
                    {
                        localStats.FilesBinary++;
                        continue;
                    }

                    results.Add(entry.FullPath);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PathTooLongException)
        {
            // Enumeration can fail mid-iteration on problematic paths (symlink loops,
            // network mounts timing out, paths exceeding OS limits). Skip this directory.
            localStats.DirsErrored++;
        }

        if (subDirs is null)
        {
            return;
        }

        // Parallelize top-level and second-level subdirectories for throughput.
        // Going deeper adds thread pool overhead that outweighs the benefit.
        if (depth <= 1 && subDirs.Count > 1)
        {
            var bags = new ConcurrentBag<(List<string> Results, ScanStats Stats)>();
            Parallel.ForEach(subDirs, subDir =>
            {
                var localResults = new List<string>();
                var localSubStats = new ScanStats();
                ScanDirectory(subDir, rootPath, rootPrefixLen, effectiveIgnores, effectiveHasFileRules, localResults, localSubStats, depth + 1);
                bags.Add((localResults, localSubStats));
            });

            foreach (var (subResults, subStats) in bags)
            {
                results.AddRange(subResults);
                localStats.Merge(subStats);
            }
        }
        else
        {
            foreach (var subDir in subDirs)
            {
                ScanDirectory(subDir, rootPath, rootPrefixLen, effectiveIgnores, effectiveHasFileRules, results, localStats, depth + 1);
            }
        }
    }

    private static GitignoreMatcher? LoadLocalGitignore(string dirPath)
    {
        var gitignorePath = Path.Combine(dirPath, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            return null;
        }

        try
        {
            var lines = File.ReadAllLines(gitignorePath);
            var matcher = new GitignoreMatcher(lines);
            return matcher.IsEmpty ? null : matcher;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Walks up from scanRoot toward the repo root, loading .gitignore files.
    /// Stops at the directory containing .git or filesystem root.
    /// </summary>
    private static void LoadParentGitignores(
        string scanRoot,
        List<(string Directory, GitignoreMatcher Matcher)> result)
    {
        var dir = Directory.GetParent(scanRoot);
        while (dir != null)
        {
            var gitignorePath = Path.Combine(dir.FullName, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                try
                {
                    var matcher = new GitignoreMatcher(File.ReadAllLines(gitignorePath));
                    if (!matcher.IsEmpty)
                    {
                        result.Add((dir.FullName, matcher));
                    }
                }
                catch (IOException) { }
            }

            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                break;
            }

            dir = dir.Parent;
        }
    }

    /// <summary>
    /// Loads the global git exclude file (~/.config/git/ignore or core.excludesFile).
    /// Applied as a root-level ignore (matches against all paths).
    /// </summary>
    private static void LoadGlobalGitignore(List<(string Directory, GitignoreMatcher Matcher)> result)
    {
        var globalPath = GetGlobalGitignorePath();
        if (globalPath == null || !File.Exists(globalPath))
        {
            return;
        }

        try
        {
            var matcher = new GitignoreMatcher(File.ReadAllLines(globalPath));
            if (!matcher.IsEmpty)
            {
                // Use "/" as directory so it matches all paths via the global fallback
                result.Add(("/", matcher));
            }
        }
        catch (IOException) { }
    }

    /// <summary>
    /// Resolves the global gitignore path: first checks git config core.excludesFile,
    /// then falls back to ~/.config/git/ignore.
    /// </summary>
    private static string? GetGlobalGitignorePath()
    {
        // Try git config core.excludesFile
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "config --global core.excludesFile")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(2000);
                if (proc.ExitCode == 0 && output.Length > 0)
                {
                    // Expand ~ to home directory
                    if (output.StartsWith('~'))
                    {
                        output = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            output[2..]);
                    }

                    return output;
                }
            }
        }
        catch { }

        // Fallback: ~/.config/git/ignore (XDG default)
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config", "git", "ignore");
    }

    /// <summary>
    /// Checks if a path is ignored by any applicable .gitignore.
    /// Each .gitignore applies to paths relative to its own directory.
    /// Optimized to avoid string allocations in the hot path.
    /// </summary>
    private static bool IsIgnoredByGitignore(
        List<(string Directory, GitignoreMatcher Matcher)> gitignores,
        string rootPath,
        string fullPath,
        string relativePath)
    {
        foreach (var (dir, matcher) in gitignores)
        {
            string relToIgnore;
            if (ReferenceEquals(dir, rootPath) || dir == rootPath)
            {
                relToIgnore = relativePath;
            }
            else if (dir == "/")
            {
                // Global ignore — match against the relative path from root
                relToIgnore = relativePath;
            }
            else if (fullPath.Length > dir.Length
                && fullPath[dir.Length] == Path.DirectorySeparatorChar
                && fullPath.AsSpan(0, dir.Length).SequenceEqual(dir.AsSpan()))
            {
                // Fast substring instead of Path.GetRelativePath
                relToIgnore = fullPath.Substring(dir.Length + 1);
                if (s_isWindows)
                {
                    relToIgnore = relToIgnore.Replace('\\', '/');
                }

                // Propagate directory indicator so directory-only rules match correctly
                if (relativePath.EndsWith('/'))
                {
                    relToIgnore += "/";
                }
            }
            else
            {
                continue;
            }

            if (matcher.IsIgnored(relToIgnore))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPattern(string relativePath, string pattern)
    {
        // relativePath is already normalized to '/' on entry
        if (pattern.Contains('/') || pattern.Contains('\\'))
        {
            return FileSystemName.MatchesSimpleExpression(
                pattern.Replace('\\', '/'),
                relativePath);
        }

        var fileName = Path.GetFileName(relativePath);
        return FileSystemName.MatchesSimpleExpression(pattern, fileName);
    }

    private static bool IsBinary(string filePath, long fileSize)
    {
        var ext = Path.GetExtension(filePath);

        // Known binary extensions — skip immediately
        if (KnownBinaryExtensions.Contains(ext))
        {
            return true;
        }

        // Known text extensions — skip the file read entirely
        if (KnownTextExtensions.Contains(ext))
        {
            return false;
        }

        // Extensionless or unknown extension — check file name patterns
        if (ext.Length == 0)
        {
            var fileName = Path.GetFileName(filePath);
            // Common extensionless text files
            if (fileName is "Makefile" or "Dockerfile" or "Jenkinsfile" or "Vagrantfile"
                or "LICENSE" or "LICENCE" or "README" or "CHANGELOG" or "AUTHORS"
                or "CONTRIBUTING" or "CODEOWNERS" or ".gitignore" or ".gitattributes"
                or ".editorconfig" or ".dockerignore")
            {
                return false;
            }
        }

        if (fileSize == 0)
        {
            return false;
        }

        // Unknown extension — do a vectorized null-byte scan on first 4KB
        try
        {
            Span<byte> buffer = stackalloc byte[(int)Math.Min(4096, fileSize)];
            using var handle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytesRead = RandomAccess.Read(handle, buffer, 0);
            return buffer[..bytesRead].Contains((byte)0);
        }
        catch
        {
            return true;
        }
    }
}

internal sealed class ScanStats
{
    public long FilesExamined;
    public long FilesGitignored;
    public long FilesFilteredOut;
    public long FilesTooLarge;
    public long FilesBinary;
    public long DirsDefaultExcluded;
    public long DirsGitignored;
    public long DirsErrored;

    // Sub-phase timing (Stopwatch ticks — accumulated across parallel workers)
    public long TicksGitignoreMatch;
    public long TicksBinaryDetect;
    public long TicksStatSize;

    public TimeSpan GitignoreMatchTime => Stopwatch.GetElapsedTime(0, TicksGitignoreMatch);
    public TimeSpan BinaryDetectTime => Stopwatch.GetElapsedTime(0, TicksBinaryDetect);
    public TimeSpan StatSizeTime => Stopwatch.GetElapsedTime(0, TicksStatSize);

    /// <summary>
    /// Merges another ScanStats instance into this one (used after parallel work completes).
    /// </summary>
    public void Merge(ScanStats other)
    {
        FilesExamined += other.FilesExamined;
        FilesGitignored += other.FilesGitignored;
        FilesFilteredOut += other.FilesFilteredOut;
        FilesTooLarge += other.FilesTooLarge;
        FilesBinary += other.FilesBinary;
        DirsDefaultExcluded += other.DirsDefaultExcluded;
        DirsGitignored += other.DirsGitignored;
        DirsErrored += other.DirsErrored;
        TicksGitignoreMatch += other.TicksGitignoreMatch;
        TicksBinaryDetect += other.TicksBinaryDetect;
        TicksStatSize += other.TicksStatSize;
    }
}
