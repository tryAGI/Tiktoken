namespace Tiktoken.Cli.IO;

/// <summary>
/// Fast gitignore pattern matcher. Replaces the Ignore NuGet package with
/// direct glob matching for significant speedup on large repos.
///
/// Implements the gitignore specification:
/// https://git-scm.com/docs/gitignore#_pattern_format
/// </summary>
internal sealed class GitignoreMatcher
{
    private readonly GitignoreRule[] _rules;

    public GitignoreMatcher(string[] lines)
    {
        var rules = new List<GitignoreRule>();
        foreach (var line in lines)
        {
            if (TryParseRule(line, out var rule))
            {
                rules.Add(rule);
            }
        }

        _rules = [.. rules];
    }

    /// <summary>
    /// Returns true if no rules exist (so callers can skip matching entirely).
    /// </summary>
    public bool IsEmpty => _rules.Length == 0;

    /// <summary>
    /// Returns true if the given relative path (using '/' separators) is ignored.
    /// For directories, the path should end with '/'.
    /// Applies all rules in order — last match wins (supports negation).
    /// </summary>
    public bool IsIgnored(string relativePath)
    {
        if (_rules.Length == 0)
        {
            return false;
        }

        var isDir = relativePath.EndsWith('/');
        var path = isDir ? relativePath.AsSpan(0, relativePath.Length - 1) : relativePath.AsSpan();

        // Pre-compute filename for filename-only patterns
        var lastSlash = path.LastIndexOf('/');
        var fileName = lastSlash >= 0 ? path[(lastSlash + 1)..] : path;

        var result = false;

        foreach (ref readonly var rule in _rules.AsSpan())
        {
            if (rule.DirectoryOnly && !isDir)
            {
                continue;
            }

            bool matches;
            if (rule.FileNameOnly)
            {
                // Pattern has no slash — match against filename only
                matches = GlobMatch(rule.Pattern.AsSpan(), fileName);
            }
            else
            {
                // Pattern has slash — match against full relative path
                matches = GlobMatch(rule.Pattern.AsSpan(), path);
            }

            if (matches)
            {
                result = !rule.Negated;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns true if this matcher has any rules that can match files (not just directories).
    /// Used for directory-level caching — if only directory rules exist, skip per-file checks.
    /// </summary>
    public bool HasFileRules
    {
        get
        {
            foreach (ref readonly var rule in _rules.AsSpan())
            {
                if (!rule.DirectoryOnly)
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static bool TryParseRule(string line, out GitignoreRule rule)
    {
        rule = default;

        // Trim trailing whitespace (unless escaped with \)
        var span = line.AsSpan();
        while (span.Length > 0 && span[^1] is ' ' or '\t')
        {
            if (span.Length >= 2 && span[^2] == '\\')
            {
                break; // escaped trailing space
            }

            span = span[..^1];
        }

        // Skip empty lines and comments
        if (span.Length == 0 || span[0] == '#')
        {
            return false;
        }

        // Handle negation
        var negated = false;
        if (span[0] == '!')
        {
            negated = true;
            span = span[1..];
            if (span.Length == 0)
            {
                return false;
            }
        }

        // Handle leading \# or \! (escape)
        if (span.Length >= 2 && span[0] == '\\' && span[1] is '#' or '!')
        {
            span = span[1..];
        }

        // Handle trailing / (directory-only)
        var directoryOnly = false;
        if (span.Length > 0 && span[^1] == '/')
        {
            directoryOnly = true;
            span = span[..^1];
            if (span.Length == 0)
            {
                return false;
            }
        }

        // Handle leading / (rooted pattern — strip it, pattern is already path-based)
        var hasLeadingSlash = false;
        if (span[0] == '/')
        {
            hasLeadingSlash = true;
            span = span[1..];
            if (span.Length == 0)
            {
                return false;
            }
        }

        var pattern = span.ToString();

        // Determine if this is a filename-only pattern:
        // If the pattern has no '/' (after stripping leading/trailing), it matches
        // against just the filename component. If it has a '/' or had a leading '/',
        // it matches against the full path.
        var fileNameOnly = !hasLeadingSlash && !pattern.Contains('/');

        rule = new GitignoreRule
        {
            Pattern = pattern,
            Negated = negated,
            DirectoryOnly = directoryOnly,
            FileNameOnly = fileNameOnly,
        };
        return true;
    }

    /// <summary>
    /// Matches a gitignore glob pattern against text.
    ///
    /// Supports:
    ///   *    — zero or more characters except '/'
    ///   **   — zero or more path segments (only at segment boundaries: start, end, or between /.../)
    ///   ?    — exactly one character except '/'
    ///   [..] — character class (e.g. [abc], [a-z], [!abc])
    ///   \x   — escaped literal character
    /// </summary>
    internal static bool GlobMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        return GlobMatchCore(pattern, text);
    }

    private static bool GlobMatchCore(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        var pi = 0;
        var ti = 0;

        while (pi < pattern.Length || ti < text.Length)
        {
            if (pi < pattern.Length)
            {
                // Check for ** at segment boundary
                if (IsDoubleStar(pattern, pi))
                {
                    return MatchDoubleStar(pattern, pi, text, ti);
                }

                // Single * — match zero or more non-/ chars
                if (pattern[pi] == '*')
                {
                    return MatchStar(pattern, pi, text, ti);
                }

                if (ti < text.Length)
                {
                    // ? matches one non-/ char
                    if (pattern[pi] == '?' && text[ti] != '/')
                    {
                        pi++;
                        ti++;
                        continue;
                    }

                    // [...] character class
                    if (pattern[pi] == '[')
                    {
                        var classLen = FindCharClassEnd(pattern, pi);
                        if (classLen > 0 && text[ti] != '/')
                        {
                            if (MatchCharClass(pattern[(pi + 1)..(pi + classLen)], text[ti]))
                            {
                                pi = pi + classLen + 1;
                                ti++;
                                continue;
                            }
                        }

                        // Class didn't match
                        return false;
                    }

                    // Backslash escape
                    if (pattern[pi] == '\\' && pi + 1 < pattern.Length)
                    {
                        pi++;

                        // Fall through to literal match
                    }

                    // Literal character match
                    if (pattern[pi] == text[ti])
                    {
                        pi++;
                        ti++;
                        continue;
                    }
                }
            }

            // Mismatch
            return false;
        }

        return true; // both exhausted
    }

    /// <summary>
    /// Checks if position pi in pattern is a special ** (at segment boundary).
    /// ** is special only when:
    ///   - preceded by '/' or at start of pattern
    ///   - followed by '/' or at end of pattern
    /// </summary>
    private static bool IsDoubleStar(ReadOnlySpan<char> pattern, int pi)
    {
        if (pi + 1 >= pattern.Length || pattern[pi] != '*' || pattern[pi + 1] != '*')
        {
            return false;
        }

        var atStart = pi == 0 || pattern[pi - 1] == '/';
        var atEnd = pi + 2 >= pattern.Length || pattern[pi + 2] == '/';
        return atStart && atEnd;
    }

    /// <summary>
    /// Handles ** matching: tries the remaining pattern at every '/' boundary
    /// and at the current position. ** matches zero or more complete path segments.
    /// </summary>
    private static bool MatchDoubleStar(ReadOnlySpan<char> pattern, int pi, ReadOnlySpan<char> text, int ti)
    {
        // Skip ** and optional trailing /
        var nextPi = pi + 2;
        if (nextPi < pattern.Length && pattern[nextPi] == '/')
        {
            nextPi++;
        }

        // ** at end of pattern matches everything
        if (nextPi >= pattern.Length)
        {
            return true;
        }

        var rest = pattern[nextPi..];

        // Try matching at current position
        if (GlobMatchCore(rest, text[ti..]))
        {
            return true;
        }

        // Try matching after every / boundary
        for (var i = ti; i < text.Length; i++)
        {
            if (text[i] == '/' && i + 1 <= text.Length)
            {
                if (GlobMatchCore(rest, text[(i + 1)..]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Handles * matching: tries matching rest of pattern after consuming
    /// 0..N non-'/' characters from text.
    /// </summary>
    private static bool MatchStar(ReadOnlySpan<char> pattern, int pi, ReadOnlySpan<char> text, int ti)
    {
        // Skip consecutive * (that aren't special **)
        while (pi < pattern.Length && pattern[pi] == '*' && !IsDoubleStar(pattern, pi))
        {
            pi++;
        }

        // * at end of pattern — matches if no more / in text
        if (pi >= pattern.Length)
        {
            return text[ti..].IndexOf('/') < 0;
        }

        var rest = pattern[pi..];

        // Try consuming 0..N non-/ chars from text
        for (var i = ti; i <= text.Length; i++)
        {
            if (i > ti && text[i - 1] == '/')
            {
                break; // * can't cross /
            }

            if (GlobMatchCore(rest, text[i..]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the closing ']' of a character class starting at pattern[startPi] == '['.
    /// Returns the offset of ']' relative to startPi, or -1 if not found.
    /// Handles leading ']' and '!' correctly.
    /// </summary>
    private static int FindCharClassEnd(ReadOnlySpan<char> pattern, int startPi)
    {
        var i = startPi + 1;

        // Skip leading ! or ^
        if (i < pattern.Length && pattern[i] is '!' or '^')
        {
            i++;
        }

        // A leading ']' is treated as a literal in the class
        if (i < pattern.Length && pattern[i] == ']')
        {
            i++;
        }

        while (i < pattern.Length)
        {
            if (pattern[i] == ']')
            {
                return i - startPi; // offset from startPi to ']'
            }

            i++;
        }

        return -1; // unclosed class
    }

    /// <summary>
    /// Matches a single character against a character class (contents between [ and ]).
    /// Supports ranges (a-z), negation (! or ^), and literal characters.
    /// </summary>
    private static bool MatchCharClass(ReadOnlySpan<char> classContent, char c)
    {
        var negated = false;
        var i = 0;

        if (classContent.Length > 0 && classContent[0] is '!' or '^')
        {
            negated = true;
            i = 1;
        }

        var matched = false;

        while (i < classContent.Length)
        {
            if (i + 2 < classContent.Length && classContent[i + 1] == '-')
            {
                // Range: a-z
                if (c >= classContent[i] && c <= classContent[i + 2])
                {
                    matched = true;
                }

                i += 3;
            }
            else
            {
                if (classContent[i] == c)
                {
                    matched = true;
                }

                i++;
            }
        }

        return negated ? !matched : matched;
    }
}

/// <summary>
/// A parsed gitignore rule.
/// </summary>
internal readonly struct GitignoreRule
{
    /// <summary>
    /// The glob pattern (leading/trailing '/' stripped, leading '!' stripped).
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// True if this is a negation rule (original line started with '!').
    /// </summary>
    public required bool Negated { get; init; }

    /// <summary>
    /// True if this rule only matches directories (original line ended with '/').
    /// </summary>
    public required bool DirectoryOnly { get; init; }

    /// <summary>
    /// True if the pattern has no '/' and should match against just the filename.
    /// False if the pattern contains '/' and should match against the full path.
    /// </summary>
    public required bool FileNameOnly { get; init; }
}
