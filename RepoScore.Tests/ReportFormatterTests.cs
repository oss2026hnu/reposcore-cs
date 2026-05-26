using RepoScore.Data;
using RepoScore.Services;
using Xunit;

namespace RepoScore.Tests;

public class ReportFormatterTests
{
    [Fact]
    public void BuildClaimsReport_IncludesLinkedPrNumber_WhenIssueHasLinkedPr()
    {
        var data = new ClaimsData
        {
            ClaimedMap = new Dictionary<string, List<IssueRecord>>
            {
                ["arror1784"] = new List<IssueRecord>
                {
                    new IssueRecord
                    {
                        Number = 392,
                        Url = "https://github.com/oss2026hnu/reposcore-cs/issues/392",
                        HasPr = true,
                        LinkedPullRequests = new List<PRRecord>
                        {
                            new PRRecord { Number = 393, Url = "https://github.com/oss2026hnu/reposcore-cs/pull/393", Title = "Fix issue 392" }
                        }
                    }
                }
            }
        };

        string report = ReportFormatter.BuildClaimsReport(data, ClaimsMode.Issue);

        Assert.Contains("PR 생성됨 - #393", report);
    }

    [Fact]
    public void BuildTextReport_DoesNotIncludeSuggestion_WhenNoOverflow()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user1", 1, 1, 0, 0, 1, 10)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.DoesNotContain("[추가 제안]", report);
    }

    [Fact]
    public void BuildTextReport_IncludesDocPrSuggestion_WhenDocOverflowOnly()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user2", 0, 1, 1, 3, 1, 0)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.Contains("   [추가 제안] 기능/버그 PR 1개 추가 시 문서PR 인정 한도 +3", report);
        Assert.DoesNotContain("이슈 인정 한도 +4", report);
    }

    [Fact]
    public void BuildTextReport_IncludesDynamicDocSuggestion_WhenDocOverflowRequiresMultiplePrs()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user2", 0, 1, 2, 5, 1, 0)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.Contains("   [추가 제안] 기능/버그 PR 2개 추가 시 문서PR 인정 한도 +6", report);
    }

    [Fact]
    public void BuildTextReport_IncludesIssueSuggestion_WhenIssueOverflowAndDocUnderLimit()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user3", 9, 0, 1, 0, 1, 0)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.Contains("   [추가 제안] 문서 PR 1개 추가 혹은 기능/버그 PR 1개 추가시 이슈 인정한도 +4", report);
        Assert.DoesNotContain("기능/버그 PR 1개 추가 시 문서PR 인정 한도 +3", report);
    }

    [Fact]
    public void BuildTextReport_IncludesDynamicIssueSuggestion_WhenIssueOverflowRequiresMultiplePrs()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user5", 21, 0, 0, 0, 1, 0)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.Contains("   [추가 제안] 문서 PR 5개 추가 혹은 기능/버그 PR 5개 추가시 이슈 인정한도 +20", report);
        Assert.DoesNotContain("기능/버그 PR 1개 추가 시 문서PR 인정 한도 +3", report);
    }

    [Fact]
    public void BuildTextReport_IncludesIssueSuggestion_WhenIssueOverflowAndDocLimitReached()
    {
        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        {
            ("user4", 17, 0, 3, 0, 1, 0)
        };

        string report = ReportFormatter.BuildTextReport("repo", reportData);

        Assert.Contains("   [추가 제안] 기능/버그 PR 1개 추가시 이슈 인정한도 +4", report);
        Assert.DoesNotContain("기능/버그 PR 1개 추가 시 문서PR 인정 한도 +3", report);
    }
}
