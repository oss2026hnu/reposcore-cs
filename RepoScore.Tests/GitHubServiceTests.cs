using System.Text.Json;
using RepoScore.Services;
using Xunit;

namespace RepoScore.Tests;

public class GitHubServiceTests
{
    [Theory]
    [InlineData("bug", GitHubIssuePrLabel.Bug)]
    [InlineData("documentation", GitHubIssuePrLabel.Documentation)]
    [InlineData("duplicate", GitHubIssuePrLabel.Duplicate)]
    [InlineData("enhancement", GitHubIssuePrLabel.Enhancement)]
    [InlineData("good first issue", GitHubIssuePrLabel.GoodFirstIssue)]
    [InlineData("good-first-issue", GitHubIssuePrLabel.GoodFirstIssue)]
    [InlineData("help wanted", GitHubIssuePrLabel.HelpWanted)]
    [InlineData("help-wanted", GitHubIssuePrLabel.HelpWanted)]
    [InlineData("invalid", GitHubIssuePrLabel.Invalid)]
    [InlineData("pinned", GitHubIssuePrLabel.Pinned)]
    [InlineData("question", GitHubIssuePrLabel.Question)]
    [InlineData("typo", GitHubIssuePrLabel.Typo)]
    [InlineData("wontfix", GitHubIssuePrLabel.Wontfix)]
    [InlineData("unknown-label", GitHubIssuePrLabel.None)]
    [InlineData("", GitHubIssuePrLabel.None)]
    public void ParseGitHubLabel_ReturnsExpectedEnumValue(
        string labelName,
        GitHubIssuePrLabel expected)
    {
        GitHubIssuePrLabel result = GitHubService.ParseGitHubLabel(labelName);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsDocumentTask_WhenDocumentationLabelExists_ReturnsTrue()
    {
        var labels = new List<GitHubIssuePrLabel>
        {
            GitHubIssuePrLabel.Documentation
        };

        bool result = GitHubService.IsDocumentTask(labels);

        Assert.True(result);
    }

    [Fact]
    public void IsDocumentTask_WhenTypoLabelExists_ReturnsTrue()
    {
        var labels = new List<GitHubIssuePrLabel>
        {
            GitHubIssuePrLabel.Typo
        };

        bool result = GitHubService.IsDocumentTask(labels);

        Assert.True(result);
    }

    [Fact]
    public void IsDocumentTask_WhenOnlyEnhancementLabelExists_ReturnsFalse()
    {
        var labels = new List<GitHubIssuePrLabel>
        {
            GitHubIssuePrLabel.Enhancement
        };

        bool result = GitHubService.IsDocumentTask(labels);

        Assert.False(result);
    }

    [Theory]
    [InlineData("COMPLETED", IssueClosedStateReason.Completed)]
    [InlineData("DUPLICATE", IssueClosedStateReason.Duplicate)]
    [InlineData("NOT_PLANNED", IssueClosedStateReason.NotPlanned)]
    [InlineData("NOTPLANNED", IssueClosedStateReason.NotPlanned)]
    [InlineData("UNKNOWN", IssueClosedStateReason.None)]
    public void ParseIssueClosedStateReason_ReturnsExpectedReason(
        string stateReason,
        IssueClosedStateReason expected)
    {
        using JsonDocument document = JsonDocument.Parse($$"""
        {
            "stateReason": "{{stateReason}}"
        }
        """);

        IssueClosedStateReason result =
            GitHubService.ParseIssueClosedStateReason(document.RootElement);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseIssueClosedStateReason_WhenStateReasonIsNull_ReturnsNone()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
            "stateReason": null
        }
        """);

        IssueClosedStateReason result =
            GitHubService.ParseIssueClosedStateReason(document.RootElement);

        Assert.Equal(IssueClosedStateReason.None, result);
    }

    [Fact]
    public void ParseIssueClosedStateReason_WhenStateReasonDoesNotExist_ReturnsNone()
    {
        using JsonDocument document = JsonDocument.Parse("""
        {
            "number": 333
        }
        """);

        IssueClosedStateReason result =
            GitHubService.ParseIssueClosedStateReason(document.RootElement);

        Assert.Equal(IssueClosedStateReason.None, result);
    }
}
