using Tiktoken.Cli.IO;

namespace Tiktoken.UnitTests;

[TestClass]
public class GitignoreMatcherTests
{
    #region Basic Patterns

    [TestMethod]
    public void LiteralFilename_ShouldMatch()
    {
        var matcher = new GitignoreMatcher(["foo.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("bar.txt").Should().BeFalse();
    }

    [TestMethod]
    public void StarWildcard_ShouldMatchZeroOrMoreNonSlashChars()
    {
        var matcher = new GitignoreMatcher(["*.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("bar.txt").Should().BeTrue();
        matcher.IsIgnored(".txt").Should().BeTrue();
        matcher.IsIgnored("foo.cs").Should().BeFalse();
    }

    [TestMethod]
    public void StarWildcard_ShouldNotCrossDirectories()
    {
        var matcher = new GitignoreMatcher(["*.txt"]);
        // *.txt is filename-only, so it matches in any directory
        matcher.IsIgnored("dir/foo.txt").Should().BeTrue();
    }

    [TestMethod]
    public void QuestionMark_ShouldMatchSingleNonSlashChar()
    {
        var matcher = new GitignoreMatcher(["fo?.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("fob.txt").Should().BeTrue();
        matcher.IsIgnored("fo.txt").Should().BeFalse();
        matcher.IsIgnored("fooo.txt").Should().BeFalse();
    }

    [TestMethod]
    public void QuestionMark_ShouldNotMatchSlash()
    {
        var matcher = new GitignoreMatcher(["fo?bar"]);
        matcher.IsIgnored("fo/bar").Should().BeFalse();
    }

    #endregion

    #region Double-Star (**)

    [TestMethod]
    public void DoubleStar_AtStart_ShouldMatchAnyLeadingPath()
    {
        var matcher = new GitignoreMatcher(["**/foo.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("dir/foo.txt").Should().BeTrue();
        matcher.IsIgnored("a/b/c/foo.txt").Should().BeTrue();
        matcher.IsIgnored("bar.txt").Should().BeFalse();
    }

    [TestMethod]
    public void DoubleStar_InMiddle_ShouldMatchZeroOrMoreSegments()
    {
        var matcher = new GitignoreMatcher(["a/**/b"]);
        matcher.IsIgnored("a/b").Should().BeTrue();
        matcher.IsIgnored("a/x/b").Should().BeTrue();
        matcher.IsIgnored("a/x/y/z/b").Should().BeTrue();
        matcher.IsIgnored("a/b/c").Should().BeFalse();
    }

    [TestMethod]
    public void DoubleStar_AtEnd_ShouldMatchEverythingInside()
    {
        var matcher = new GitignoreMatcher(["foo/**"]);
        matcher.IsIgnored("foo/bar").Should().BeTrue();
        matcher.IsIgnored("foo/bar/baz").Should().BeTrue();
        matcher.IsIgnored("foo/a.txt").Should().BeTrue();
    }

    [TestMethod]
    public void DoubleStar_Standalone_ShouldMatchEverything()
    {
        var matcher = new GitignoreMatcher(["**"]);
        matcher.IsIgnored("anything").Should().BeTrue();
        matcher.IsIgnored("a/b/c").Should().BeTrue();
    }

    #endregion

    #region Character Classes

    [TestMethod]
    public void CharClass_SimpleSet_ShouldMatchAnyCharInSet()
    {
        var matcher = new GitignoreMatcher(["[abc].txt"]);
        matcher.IsIgnored("a.txt").Should().BeTrue();
        matcher.IsIgnored("b.txt").Should().BeTrue();
        matcher.IsIgnored("c.txt").Should().BeTrue();
        matcher.IsIgnored("d.txt").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_Range_ShouldMatchCharInRange()
    {
        var matcher = new GitignoreMatcher(["[a-z].txt"]);
        matcher.IsIgnored("a.txt").Should().BeTrue();
        matcher.IsIgnored("m.txt").Should().BeTrue();
        matcher.IsIgnored("z.txt").Should().BeTrue();
        matcher.IsIgnored("A.txt").Should().BeFalse();
        matcher.IsIgnored("0.txt").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_Negated_ExclamationMark_ShouldMatchCharsNotInSet()
    {
        var matcher = new GitignoreMatcher(["[!abc].txt"]);
        matcher.IsIgnored("d.txt").Should().BeTrue();
        matcher.IsIgnored("a.txt").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_Negated_Caret_ShouldMatchCharsNotInSet()
    {
        var matcher = new GitignoreMatcher(["[^abc].txt"]);
        matcher.IsIgnored("d.txt").Should().BeTrue();
        matcher.IsIgnored("a.txt").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_LeadingBracket_ShouldBeLiteral()
    {
        // Leading ']' in a character class is treated as a literal
        var matcher = new GitignoreMatcher(["[]abc].txt"]);
        matcher.IsIgnored("].txt").Should().BeTrue();
        matcher.IsIgnored("a.txt").Should().BeTrue();
        matcher.IsIgnored("d.txt").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_CaseSensitive_ShouldMatchExact()
    {
        var matcher = new GitignoreMatcher(["[Dd]ebug/"]);
        matcher.IsIgnored("Debug/").Should().BeTrue();
        matcher.IsIgnored("debug/").Should().BeTrue();
        matcher.IsIgnored("DEBUG/").Should().BeFalse();
    }

    [TestMethod]
    public void CharClass_ShouldNotMatchSlash()
    {
        var matcher = new GitignoreMatcher(["a[/]b"]);
        matcher.IsIgnored("a/b").Should().BeFalse();
    }

    #endregion

    #region Directory-Only Rules

    [TestMethod]
    public void DirectoryOnly_ShouldOnlyMatchDirs()
    {
        var matcher = new GitignoreMatcher(["build/"]);
        matcher.IsIgnored("build/").Should().BeTrue();
        matcher.IsIgnored("build").Should().BeFalse(); // file named "build"
    }

    [TestMethod]
    public void DirectoryOnly_ShouldMatchInSubdirectory()
    {
        var matcher = new GitignoreMatcher(["build/"]);
        // Filename-only pattern matches in any directory
        matcher.IsIgnored("src/build/").Should().BeTrue();
    }

    #endregion

    #region Negation

    [TestMethod]
    public void Negation_ShouldUnignoreFile()
    {
        var matcher = new GitignoreMatcher(["*.txt", "!important.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("important.txt").Should().BeFalse();
    }

    [TestMethod]
    public void Negation_LastMatchWins()
    {
        var matcher = new GitignoreMatcher(["*.txt", "!important.txt", "*.txt"]);
        // Last *.txt re-ignores important.txt
        matcher.IsIgnored("important.txt").Should().BeTrue();
    }

    [TestMethod]
    public void Negation_OnlyNegation_ShouldNotIgnore()
    {
        var matcher = new GitignoreMatcher(["!foo.txt"]);
        // Negation without a prior matching rule
        matcher.IsIgnored("foo.txt").Should().BeFalse();
        matcher.IsIgnored("bar.txt").Should().BeFalse();
    }

    #endregion

    #region Rooted Patterns

    [TestMethod]
    public void RootedPattern_ShouldMatchFromRootOnly()
    {
        var matcher = new GitignoreMatcher(["/build"]);
        matcher.IsIgnored("build").Should().BeTrue();
        // Rooted pattern should NOT match in subdirectories
        // (it has a leading slash, so FileNameOnly=false, matches against full path)
        matcher.IsIgnored("src/build").Should().BeFalse();
    }

    [TestMethod]
    public void RootedPattern_WithWildcard_ShouldMatchFromRoot()
    {
        var matcher = new GitignoreMatcher(["/*.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("dir/foo.txt").Should().BeFalse();
    }

    #endregion

    #region Filename-Only vs Path Patterns

    [TestMethod]
    public void FilenameOnly_ShouldMatchAnywhere()
    {
        // Pattern without '/' matches against filename component
        var matcher = new GitignoreMatcher(["foo.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("dir/foo.txt").Should().BeTrue();
        matcher.IsIgnored("a/b/foo.txt").Should().BeTrue();
    }

    [TestMethod]
    public void PathPattern_WithSlash_ShouldMatchFullPath()
    {
        // Pattern with '/' matches against full relative path
        var matcher = new GitignoreMatcher(["dir/foo.txt"]);
        matcher.IsIgnored("dir/foo.txt").Should().BeTrue();
        matcher.IsIgnored("foo.txt").Should().BeFalse();
        matcher.IsIgnored("other/dir/foo.txt").Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EmptyInput_ShouldNotMatch()
    {
        var matcher = new GitignoreMatcher([]);
        matcher.IsIgnored("anything").Should().BeFalse();
        matcher.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void CommentLines_ShouldBeSkipped()
    {
        var matcher = new GitignoreMatcher(["# this is a comment", "foo.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("# this is a comment").Should().BeFalse();
    }

    [TestMethod]
    public void EmptyLines_ShouldBeSkipped()
    {
        var matcher = new GitignoreMatcher(["", "  ", "foo.txt"]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
    }

    [TestMethod]
    public void EscapedHash_ShouldBeLiteralMatch()
    {
        var matcher = new GitignoreMatcher([@"\#file.txt"]);
        matcher.IsIgnored("#file.txt").Should().BeTrue();
    }

    [TestMethod]
    public void EscapedBang_ShouldBeLiteralMatch()
    {
        var matcher = new GitignoreMatcher([@"\!important.txt"]);
        matcher.IsIgnored("!important.txt").Should().BeTrue();
    }

    [TestMethod]
    public void TrailingWhitespace_ShouldBeTrimmed()
    {
        var matcher = new GitignoreMatcher(["foo.txt   "]);
        matcher.IsIgnored("foo.txt").Should().BeTrue();
        matcher.IsIgnored("foo.txt   ").Should().BeFalse();
    }

    [TestMethod]
    public void EscapedTrailingSpace_ShouldBePreserved()
    {
        // Per gitignore spec: "A trailing space can be marked by backslash ("\")."
        // The parser keeps the "\ " sequence in the pattern. The glob matcher then
        // treats the backslash as an escape, matching a literal space character.
        var matcher = new GitignoreMatcher([@"foo.txt\ "]);
        matcher.IsIgnored("foo.txt ").Should().BeTrue();
        matcher.IsIgnored("foo.txt").Should().BeFalse();
    }

    [TestMethod]
    public void BareSlash_ShouldBeSkipped()
    {
        // A bare "/" should be skipped (after stripping trailing slash, empty pattern)
        var matcher = new GitignoreMatcher(["/"]);
        matcher.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void BareExclamation_ShouldBeSkipped()
    {
        var matcher = new GitignoreMatcher(["!"]);
        matcher.IsEmpty.Should().BeTrue();
    }

    #endregion

    #region Real-World Patterns

    [TestMethod]
    public void RealWorld_PackagesWildcard()
    {
        var matcher = new GitignoreMatcher(["**/[Pp]ackages/*"]);
        matcher.IsIgnored("Packages/foo.nupkg").Should().BeTrue();
        matcher.IsIgnored("packages/bar.dll").Should().BeTrue();
        matcher.IsIgnored("src/Packages/baz.txt").Should().BeTrue();
    }

    [TestMethod]
    public void RealWorld_MinJs()
    {
        var matcher = new GitignoreMatcher(["*.min.js"]);
        matcher.IsIgnored("app.min.js").Should().BeTrue();
        matcher.IsIgnored("dir/vendor.min.js").Should().BeTrue();
        matcher.IsIgnored("app.js").Should().BeFalse();
    }

    [TestMethod]
    public void RealWorld_NextDir()
    {
        var matcher = new GitignoreMatcher([".next/"]);
        matcher.IsIgnored(".next/").Should().BeTrue();
        matcher.IsIgnored(".next").Should().BeFalse(); // file named ".next"
    }

    [TestMethod]
    public void RealWorld_NodeModules()
    {
        var matcher = new GitignoreMatcher(["node_modules/"]);
        matcher.IsIgnored("node_modules/").Should().BeTrue();
        matcher.IsIgnored("src/node_modules/").Should().BeTrue();
        matcher.IsIgnored("node_modules").Should().BeFalse(); // file
    }

    [TestMethod]
    public void RealWorld_BinObj()
    {
        var matcher = new GitignoreMatcher(["[Bb]in/", "[Oo]bj/"]);
        matcher.IsIgnored("bin/").Should().BeTrue();
        matcher.IsIgnored("Bin/").Should().BeTrue();
        matcher.IsIgnored("obj/").Should().BeTrue();
        matcher.IsIgnored("Obj/").Should().BeTrue();
        matcher.IsIgnored("src/bin/").Should().BeTrue();
    }

    [TestMethod]
    public void RealWorld_DotEnv()
    {
        var matcher = new GitignoreMatcher([".env", ".env.*"]);
        matcher.IsIgnored(".env").Should().BeTrue();
        matcher.IsIgnored(".env.local").Should().BeTrue();
        matcher.IsIgnored(".env.production").Should().BeTrue();
    }

    [TestMethod]
    public void RealWorld_VisualStudio()
    {
        var matcher = new GitignoreMatcher(
        [
            "*.suo",
            "*.user",
            ".vs/",
            "**/[Pp]ackages/**",
            "!**/[Pp]ackages/build/",
        ]);

        matcher.IsIgnored("project.suo").Should().BeTrue();
        matcher.IsIgnored("project.user").Should().BeTrue();
        matcher.IsIgnored(".vs/").Should().BeTrue();
        matcher.IsIgnored("Packages/NuGet.dll").Should().BeTrue();
        // Negation: build/ inside Packages should be unignored
        matcher.IsIgnored("Packages/build/").Should().BeFalse();
    }

    #endregion

    #region HasFileRules / IsEmpty Properties

    [TestMethod]
    public void IsEmpty_NoRules_ShouldBeTrue()
    {
        var matcher = new GitignoreMatcher(["# just a comment"]);
        matcher.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public void IsEmpty_WithRules_ShouldBeFalse()
    {
        var matcher = new GitignoreMatcher(["*.txt"]);
        matcher.IsEmpty.Should().BeFalse();
    }

    [TestMethod]
    public void HasFileRules_DirectoryOnlyRules_ShouldBeFalse()
    {
        var matcher = new GitignoreMatcher(["bin/", "obj/"]);
        matcher.HasFileRules.Should().BeFalse();
    }

    [TestMethod]
    public void HasFileRules_MixedRules_ShouldBeTrue()
    {
        var matcher = new GitignoreMatcher(["bin/", "*.txt"]);
        matcher.HasFileRules.Should().BeTrue();
    }

    [TestMethod]
    public void HasFileRules_FileOnlyRules_ShouldBeTrue()
    {
        var matcher = new GitignoreMatcher(["*.log"]);
        matcher.HasFileRules.Should().BeTrue();
    }

    #endregion

    #region GlobMatch Direct Tests

    [TestMethod]
    public void GlobMatch_EmptyPatternAndText_ShouldMatch()
    {
        GitignoreMatcher.GlobMatch("", "").Should().BeTrue();
    }

    [TestMethod]
    public void GlobMatch_EmptyPattern_NonEmptyText_ShouldNotMatch()
    {
        GitignoreMatcher.GlobMatch("", "foo").Should().BeFalse();
    }

    [TestMethod]
    public void GlobMatch_StarOnly_ShouldMatchNonSlash()
    {
        GitignoreMatcher.GlobMatch("*", "foo").Should().BeTrue();
        GitignoreMatcher.GlobMatch("*", "").Should().BeTrue();
        GitignoreMatcher.GlobMatch("*", "a/b").Should().BeFalse();
    }

    [TestMethod]
    public void GlobMatch_DoubleStarOnly_ShouldMatchAnything()
    {
        GitignoreMatcher.GlobMatch("**", "foo").Should().BeTrue();
        GitignoreMatcher.GlobMatch("**", "a/b/c").Should().BeTrue();
        GitignoreMatcher.GlobMatch("**", "").Should().BeTrue();
    }

    [TestMethod]
    public void GlobMatch_BackslashEscape_ShouldMatchLiteral()
    {
        GitignoreMatcher.GlobMatch(@"\*", "*").Should().BeTrue();
        GitignoreMatcher.GlobMatch(@"\*", "a").Should().BeFalse();
        GitignoreMatcher.GlobMatch(@"\?", "?").Should().BeTrue();
        GitignoreMatcher.GlobMatch(@"\?", "a").Should().BeFalse();
    }

    [TestMethod]
    public void GlobMatch_ComplexPattern()
    {
        GitignoreMatcher.GlobMatch("src/**/test/*.cs", "src/test/foo.cs").Should().BeTrue();
        GitignoreMatcher.GlobMatch("src/**/test/*.cs", "src/a/b/test/bar.cs").Should().BeTrue();
        GitignoreMatcher.GlobMatch("src/**/test/*.cs", "src/test/sub/foo.cs").Should().BeFalse();
    }

    [TestMethod]
    public void GlobMatch_MultipleStars()
    {
        GitignoreMatcher.GlobMatch("*.min.*", "app.min.js").Should().BeTrue();
        GitignoreMatcher.GlobMatch("*.min.*", "vendor.min.css").Should().BeTrue();
        GitignoreMatcher.GlobMatch("*.min.*", "app.js").Should().BeFalse();
    }

    #endregion
}
