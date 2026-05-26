using RepoScore.Data;
using Xunit;

namespace RepoScore.Tests;

public class ScoreCalculatorTests
{
    [Fact]
    public void CalculateFinalScore_WithOnlyFeatureBugPullRequests_ReturnsExpectedScore()
    {
        int score = ScoreCalculator.CalculateFinalScore(
            featureBugPrCount: 2,
            docPrCount: 0,
            typoPrCount: 0,
            featureBugIssueCount: 0,
            docIssueCount: 0);

        Assert.Equal(6, score);
    }

    [Fact]
    public void CalculateFinalScore_LimitsAdditionalDocumentAndTypoPullRequests()
    {
        int score = ScoreCalculator.CalculateFinalScore(
            featureBugPrCount: 1,
            docPrCount: 10,
            typoPrCount: 10,
            featureBugIssueCount: 0,
            docIssueCount: 0);

        Assert.Equal(9, score);
    }

    [Fact]
    public void CalculateFinalScore_LimitsIssuesByValidPullRequestCount()
    {
        int score = ScoreCalculator.CalculateFinalScore(
            featureBugPrCount: 1,
            docPrCount: 0,
            typoPrCount: 0,
            featureBugIssueCount: 10,
            docIssueCount: 10);

        Assert.Equal(11, score);
    }

    [Fact]
    public void CalculateFinalScore_WithNoFeatureBugPullRequest_AllowsLimitedDocumentAndTypoPullRequests()
    {
        int score = ScoreCalculator.CalculateFinalScore(
            featureBugPrCount: 0,
            docPrCount: 2,
            typoPrCount: 2,
            featureBugIssueCount: 0,
            docIssueCount: 0);

        Assert.Equal(5, score);
    }
}
