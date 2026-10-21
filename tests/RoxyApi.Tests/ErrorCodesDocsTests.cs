using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace RoxyApi.Tests;

// The ERRORS region of README.md and AGENTS.md is rendered by sync-docs from the spec's
// info["x-error-codes"]. This pins the codes, in spec order, so a hand edit or a stale
// region fails by name even where the sync-docs drift check has not run.
public class ErrorCodesDocsTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RoxyApi.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("repository root not found");
    }

    [Theory]
    [InlineData("README.md")]
    [InlineData("AGENTS.md")]
    public void The_errors_region_lists_exactly_the_spec_codes(string file)
    {
        var spec = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specs", "openapi.json")))!;
        var expected = spec["info"]!["x-error-codes"]!.AsArray().Select(e => e!["code"]!.GetValue<string>()).ToList();

        var text = File.ReadAllText(Path.Combine(RepoRoot(), file));
        var region = Regex.Match(text, "<!-- BEGIN:ERRORS -->(.*?)<!-- END:ERRORS -->", RegexOptions.Singleline);
        Assert.True(region.Success, $"{file} has no ERRORS region");
        var actual = Regex.Matches(region.Groups[1].Value, @"^\| (?:\d+|Any) \| `([a-z_]+)` \|", RegexOptions.Multiline)
            .Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ErrorCodes_has_one_constant_per_spec_code()
    {
        var spec = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "specs", "openapi.json")))!;
        var expected = spec["info"]!["x-error-codes"]!.AsArray().Select(e => e!["code"]!.GetValue<string>()).Order().ToList();

        var actual = typeof(ErrorCodes).GetFields().Where(f => f.IsLiteral).Select(f => (string)f.GetRawConstantValue()!).Order().ToList();

        Assert.Equal(expected, actual);
        Assert.Equal("rate_limit_per_minute", ErrorCodes.RateLimitPerMinute);
    }
}
