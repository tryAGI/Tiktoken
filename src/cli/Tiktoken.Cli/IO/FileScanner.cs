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
    private readonly bool _followSymlinks;
    private readonly bool _trackStats;
    private long _dirsVisited;

    private static readonly FrozenSet<string> DefaultExcludedDirs =
        FrozenSet.ToFrozenSet(
        [
            // Version control
            ".git", ".hg", ".svn",
            // Build output
            "bin", "obj", "node_modules", "__pycache__",
            // Package manager caches
            ".npm", ".nuget", ".cargo", ".rustup", ".gradle", ".m2",
            ".pnpm-store", "bower_components",
            ".bun", ".deno", ".gem", ".cocoapods", ".pub-cache",
            // Language runtimes / version managers (multi-GB, never contain source)
            ".nvm", ".dotnet", ".local", ".conda", ".virtualenvs", ".venvs",
            ".android", ".sdkman", ".jabba", ".swiftly",
            // Python virtual environments / caches
            "venv", ".venv", ".tox", ".mypy_cache", ".pytest_cache", ".ruff_cache",
            // IDE / editor state
            ".idea", ".vs", ".fleet",
            ".vscode", ".vscode-insiders", ".cursor", ".windsurf",
            // AI / ML tool caches (model weights, multi-GB)
            ".ollama", ".lmstudio", ".keras", ".matplotlib",
            ".claude", ".codex", ".cline", ".aider", ".copilot",
            // Container / cloud / infra
            ".docker", ".minikube", ".kube",
            ".terraform", ".pulumi",
            // Misc caches and generated dirs
            ".cache", ".Trash", ".Trashes",
            ".next", ".turbo", ".angular", ".parcel-cache",
            "coverage", ".nyc_output",
            // macOS / filesystem metadata
            ".Spotlight-V100", ".fseventsd", ".TemporaryItems",
            // App bundles with recursive symlink structures
            "Steam.AppBundle",
        ], StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Directories excluded only on macOS — system/app directories that never contain
    /// user source code but can have tens of thousands of subdirectories.
    /// </summary>
    private static readonly FrozenSet<string> MacOsExcludedDirs = OperatingSystem.IsMacOS()
        ? FrozenSet.ToFrozenSet(
            [
                "Library", "Applications", "Movies", "Music", "Pictures",
                // Inside ~/Library — excluded as a second line of defense if Library itself
                // is not excluded (e.g. --no-default-excludes, or scanning inside ~/Library)
                "Developer", "Application Support", "Containers", "Group Containers",
                "Caches", "Logs", "Saved Application State", "WebKit",
            ],
            StringComparer.OrdinalIgnoreCase)
        : FrozenSet<string>.Empty;

    private static readonly FrozenSet<string> KnownBinaryExtensions =
        FrozenSet.ToFrozenSet(
        [
            // Executables / libraries
            ".exe", ".dll", ".pdb", ".obj", ".bin", ".so", ".dylib", ".framework",
            // Images
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp", ".svg",
            ".tiff", ".tif", ".heic", ".heif", ".avif", ".raw", ".cr2", ".nef",
            // Audio
            ".mp3", ".wav", ".flac", ".ogg", ".aac", ".wma", ".m4a", ".opus",
            // Video
            ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts",
            // Archives
            ".zip", ".gz", ".tar", ".7z", ".rar", ".bz2", ".xz", ".zst", ".lz4",
            ".cab", ".dmg", ".iso", ".img", ".pkg", ".deb", ".rpm",
            // Documents (binary formats)
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".odt", ".ods", ".odp", ".pages", ".numbers", ".keynote",
            // Fonts
            ".woff", ".woff2", ".ttf", ".otf", ".eot",
            // .NET / Java
            ".nupkg", ".snupkg", ".ttkb", ".class", ".jar", ".war", ".ear",
            // Compiled objects
            ".pyc", ".pyo", ".o", ".a", ".lib", ".ko",
            // Databases
            ".db", ".sqlite", ".sqlite3", ".mdb", ".ldb",
            // Misc binary
            ".dat", ".DS_Store", ".localized",
        ], StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenSet<string> KnownTextExtensions =
        FrozenSet.ToFrozenSet(
        [
            // .NET
            ".cs", ".csx", ".fs", ".fsx", ".vb",
            ".sln", ".slnx", ".csproj", ".fsproj", ".vbproj", ".props", ".targets",
            ".razor", ".cshtml",
            // Data / Config
            ".json", ".jsonl", ".jsonc", ".xml", ".yaml", ".yml", ".toml",
            ".ini", ".cfg", ".conf", ".config", ".properties",
            ".env", ".lock", ".plist",
            // Markup / Documentation
            ".md", ".mdx", ".txt", ".text", ".log", ".csv", ".tsv",
            ".rst", ".adoc", ".tex", ".latex",
            // Web
            ".html", ".htm", ".css", ".scss", ".sass", ".less", ".styl",
            ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs", ".mts", ".cts",
            ".vue", ".svelte", ".astro",
            // Languages
            ".py", ".pyi", ".rb", ".go", ".rs", ".java", ".kt", ".kts", ".scala",
            ".c", ".h", ".cpp", ".hpp", ".cc", ".hh", ".cxx", ".hxx",
            ".swift", ".m", ".mm",
            ".sh", ".bash", ".zsh", ".fish", ".ps1", ".psm1", ".bat", ".cmd",
            ".sql", ".graphql", ".gql", ".proto",
            ".r", ".R", ".jl", ".lua", ".pl", ".pm", ".php",
            ".tf", ".hcl", ".dockerfile", ".makefile",
            ".zig", ".nim", ".dart", ".ex", ".exs", ".erl", ".hrl",
            ".clj", ".cljs", ".cljc", ".edn",
            ".hs", ".lhs", ".elm", ".ml", ".mli", ".f90", ".f95",
            // DevOps / Config files
            ".gitignore", ".gitattributes", ".editorconfig", ".prettierrc",
            ".eslintrc", ".babelrc", ".npmrc",
            ".dockerignore", ".helmignore",
        ], StringComparer.OrdinalIgnoreCase);

    private static readonly bool s_isWindows = OperatingSystem.IsWindows();

    public ScanStats Stats { get; private set; } = new();

    /// <summary>
    /// Approximate number of directories visited so far (thread-safe, for progress reporting).
    /// </summary>
    public long DirsVisited => Interlocked.Read(ref _dirsVisited);

    public FileScanner(
        IEnumerable<string>? includePatterns = null,
        IEnumerable<string>? excludePatterns = null,
        long maxFileSize = 50 * 1024 * 1024,
        bool noDefaultExcludes = false,
        bool noGitignore = false,
        bool followSymlinks = false,
        bool trackStats = false)
    {
        _includePatterns = includePatterns?.ToList() ?? [];
        _excludePatterns = excludePatterns?.ToList() ?? [];
        _maxFileSize = maxFileSize;
        _noDefaultExcludes = noDefaultExcludes;
        _noGitignore = noGitignore;
        _followSymlinks = followSymlinks;
        _trackStats = trackStats;
    }

    public IReadOnlyList<string> Scan(string rootPath, CancellationToken cancellationToken = default)
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
        ScanDirectory(rootPath, rootPath, rootPrefixLen, parentIgnores, anyParentHasFileRules, results, localStats, depth: 0, cancellationToken);
        results.Sort(StringComparer.OrdinalIgnoreCase);
        Stats = localStats;
        return results;
    }

    /// <summary>
    /// Single-pass directory entry — captures only path and type from readdir.
    /// Length is NOT included because on macOS/Linux it triggers an extra stat() syscall
    /// per entry. We defer the stat call to after gitignore/filter checks pass.
    /// </summary>
    private readonly record struct DirEntry(string FullPath, bool IsDirectory, bool IsSymlink);

    /// <summary>
    /// Enumerates all entries (files + directories) in a single readdir pass using
    /// FileSystemEnumerable. Only extracts path and IsDirectory (from d_type on Unix),
    /// avoiding per-entry stat() calls.
    /// </summary>
    private static FileSystemEnumerable<DirEntry> EnumerateEntries(string dirPath)
    {
        return new FileSystemEnumerable<DirEntry>(
            dirPath,
            static (ref FileSystemEntry entry) =>
            {
                var isDir = entry.IsDirectory; // Free on Unix (uses d_type from readdir)
                // Only access entry.Attributes for directories — this triggers fstatat()
                // on Unix. For files we don't need symlink info, so skip the extra syscall.
                var isSymlink = isDir && (entry.Attributes & FileAttributes.ReparsePoint) != 0;
                return new DirEntry(entry.ToFullPath(), isDir, isSymlink);
            },
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
        int depth,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _dirsVisited);

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

        // Guard against symlink loops, recursive .app bundles, and extremely deep paths
        // that would cause PathTooLongException. 40 levels is far beyond any real source tree.
        if (depth > 40)
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
                    // Skip symlink directories to avoid traversing into virtual/mounted
                    // filesystems, symlink loops, and paths like Steam.app recursive bundles
                    if (!_followSymlinks && entry.IsSymlink)
                    {
                        continue;
                    }

                    var dirName = Path.GetFileName(entry.FullPath);

                    if (!_noDefaultExcludes && (DefaultExcludedDirs.Contains(dirName) || MacOsExcludedDirs.Contains(dirName)))
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

                        var t0 = _trackStats ? Stopwatch.GetTimestamp() : 0;
                        var ignored = IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relDir + "/") ||
                            IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relDir);
                        if (_trackStats) localStats.TicksGitignoreMatch += Stopwatch.GetTimestamp() - t0;

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
                        var t0 = _trackStats ? Stopwatch.GetTimestamp() : 0;
                        var ignored = IsIgnoredByGitignore(effectiveIgnores, rootPath, entry.FullPath, relativePath);
                        if (_trackStats) localStats.TicksGitignoreMatch += Stopwatch.GetTimestamp() - t0;
                        if (ignored)
                        {
                            localStats.FilesGitignored++;
                            continue;
                        }
                    }

                    if (_includePatterns.Count > 0 && !MatchesAnyPattern(relativePath, _includePatterns))
                    {
                        localStats.FilesFilteredOut++;
                        continue;
                    }

                    if (_excludePatterns.Count > 0 && MatchesAnyPattern(relativePath, _excludePatterns))
                    {
                        localStats.FilesFilteredOut++;
                        continue;
                    }

                    // Defer stat() until after gitignore/filter checks pass.
                    var tStat = _trackStats ? Stopwatch.GetTimestamp() : 0;
                    long fileSize;
                    try
                    {
                        fileSize = new FileInfo(entry.FullPath).Length;
                    }
                    catch (IOException)
                    {
                        if (_trackStats) localStats.TicksStatSize += Stopwatch.GetTimestamp() - tStat;
                        continue;
                    }
                    if (_trackStats) localStats.TicksStatSize += Stopwatch.GetTimestamp() - tStat;

                    if (fileSize > _maxFileSize)
                    {
                        localStats.FilesTooLarge++;
                        continue;
                    }

                    var tBin = _trackStats ? Stopwatch.GetTimestamp() : 0;
                    var isBin = IsBinary(entry.FullPath, fileSize);
                    if (_trackStats) localStats.TicksBinaryDetect += Stopwatch.GetTimestamp() - tBin;

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

        // Parallelize shallow subdirectories for throughput on large trees.
        // depth <= 3 ensures heavy subtrees (e.g. ~/GitHub/<repo>/) are also parallelized.
        if (depth <= 3 && subDirs.Count > 1)
        {
            var bags = new ConcurrentBag<(List<string> Results, ScanStats Stats)>();
            try
            {
                Parallel.ForEach(subDirs, new ParallelOptions { CancellationToken = cancellationToken }, subDir =>
                {
                    var localResults = new List<string>();
                    var localSubStats = new ScanStats();
                    try
                    {
                        ScanDirectory(subDir, rootPath, rootPrefixLen, effectiveIgnores, effectiveHasFileRules, localResults, localSubStats, depth + 1, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Let cancellation propagate to Parallel.ForEach
                    }
                    catch (Exception)
                    {
                        // Catch ALL exceptions including AggregateException from nested
                        // Parallel.ForEach, IOException, PathTooLongException, etc.
                        localSubStats.DirsErrored++;
                    }
                    bags.Add((localResults, localSubStats));
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // AggregateException or any other exception from Parallel.ForEach
                localStats.DirsErrored++;
            }

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
                try
                {
                    ScanDirectory(subDir, rootPath, rootPrefixLen, effectiveIgnores, effectiveHasFileRules, results, localStats, depth + 1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    localStats.DirsErrored++;
                }
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

    private static bool MatchesAnyPattern(string relativePath, List<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (MatchesPattern(relativePath, pattern))
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
