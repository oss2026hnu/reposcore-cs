using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RepoScore.Services;
using System.Text.Json;

namespace RepoScore.Data
{
    public enum OutputFormat { Csv, Txt, Html }

    public enum ClaimsMode { Issue, User }

    public static class ReportFormatter
    {
        public static string BuildTextReport(
            string repo,
            List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)> reportData)
        {
            var rows = reportData.Select(r => new
            {
                Id = r.Id,
                Issues = $"{r.docIssues + r.featBugIssues} ({r.docIssues}/{r.featBugIssues})",
                PullRequests = $"{r.docPrs + r.featBugPrs + r.typoPrs} ({r.docPrs}/{r.featBugPrs}/{r.typoPrs})",
                Score = r.Score.ToString(),
                Raw = r
            }).ToList();
            
            string userHeader = "유저";
            string issueHeader = "이슈 (문서/기능버그)";
            string prHeader = "PR (문서/기능버그/오타)";
            string scoreHeader = "점수";

            int userWidth = Math.Max(GetDisplayWidth(userHeader), rows.Any() ? rows.Max(x => GetDisplayWidth(x.Id)) : 0);
            int issueWidth = Math.Max(GetDisplayWidth(issueHeader), rows.Any() ? rows.Max(x => GetDisplayWidth(x.Issues)) : 0);
            int prWidth = Math.Max(GetDisplayWidth(prHeader), rows.Any() ? rows.Max(x => GetDisplayWidth(x.PullRequests)) : 0);
            int scoreWidth = Math.Max(GetDisplayWidth(scoreHeader), rows.Any() ? rows.Max(x => GetDisplayWidth(x.Score)) : 0);

            string separator =
                new string('-', userWidth) + "-+-" +
                new string('-', issueWidth) + "-+-" +
                new string('-', prWidth) + "-+-" +
                new string('-', scoreWidth);

            var sb = new StringBuilder();
            sb.AppendLine($"=== {repo} 오픈소스 기여도 분석 리포트 ===");
            sb.AppendLine($"분석 일시: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            sb.AppendLine(
                PadRightKorean(userHeader, userWidth) + " | " +
                PadLeftKorean(issueHeader, issueWidth) + " | " +
                PadLeftKorean(prHeader, prWidth) + " | " +
                PadLeftKorean(scoreHeader, scoreWidth));

            sb.AppendLine(separator);

            foreach (var row in rows)
            {
                sb.AppendLine(
                    PadRightKorean(row.Id, userWidth) + " | " +
                    PadLeftKorean(row.Issues, issueWidth) + " | " +
                    PadLeftKorean(row.PullRequests, prWidth) + " | " +
                    PadLeftKorean(row.Score, scoreWidth));

                var r = row.Raw;

                int maxAdditionalPr = 3 * Math.Max(r.featBugPrs, 1);
                int totalDocTypoPr = r.docPrs + r.typoPrs;
                int rejectedPr = Math.Max(0, totalDocTypoPr - maxAdditionalPr);

                int validPrCount = r.featBugPrs + Math.Min(totalDocTypoPr, maxAdditionalPr);
                int maxIssueCount = 4 * validPrCount;
                int totalIssues = r.featBugIssues + r.docIssues;
                int rejectedIssue = Math.Max(0, totalIssues - maxIssueCount);

                if (rejectedPr > 0 || rejectedIssue > 0)
                {
                    sb.AppendLine($"   [미인정 항목] 문서/오타 PR {rejectedPr}개 초과(한도 {maxAdditionalPr}개) / 이슈 {rejectedIssue}개 초과(한도 {maxIssueCount}개)");

                    if (rejectedPr > 0)
                    {
                        int docSuggestionCount = (rejectedPr + 2) / 3;
                        sb.AppendLine($"   [추가 제안] 기능/버그 PR {docSuggestionCount}개 추가 시 문서PR 인정 한도 +{docSuggestionCount * 3}");
                    }

                    if (rejectedIssue > 0)
                    {
                        int issueSuggestionCount = (rejectedIssue + 3) / 4;
                        if (totalDocTypoPr < maxAdditionalPr)
                        {
                            sb.AppendLine($"   [추가 제안] 문서 PR {issueSuggestionCount}개 추가 혹은 기능/버그 PR {issueSuggestionCount}개 추가시 이슈 인정한도 +{issueSuggestionCount * 4}");
                        }
                        else
                        {
                            sb.AppendLine($"   [추가 제안] 기능/버그 PR {issueSuggestionCount}개 추가시 이슈 인정한도 +{issueSuggestionCount * 4}");
                        }
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static string BuildHtmlReport(string repoName, List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)> reportData)
        {
            var labels = JsonSerializer.Serialize(reportData.Select(r => $"{r.Id} (점수: {r.Score})"));
            var featBugPrData = JsonSerializer.Serialize(reportData.Select(r => r.featBugPrs));
            var docPrData = JsonSerializer.Serialize(reportData.Select(r => r.docPrs));
            var typoPrData = JsonSerializer.Serialize(reportData.Select(r => r.typoPrs));
            var featBugIssueData = JsonSerializer.Serialize(reportData.Select(r => r.featBugIssues));
            var docIssueData = JsonSerializer.Serialize(reportData.Select(r => r.docIssues));

            int chartHeight = Math.Max(400, reportData.Count * 30);

            return $@"
<!DOCTYPE html>
<html lang=""ko"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>RepoScore Report - {repoName}</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js""></script>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Helvetica, Arial, sans-serif, ""Apple Color Emoji"", ""Segoe UI Emoji""; margin: 2em; background-color: #f6f8fa; color: #24292e; }}
        h1 {{ border-bottom: 1px solid #e1e4e8; padding-bottom: 0.3em; }}
        .chart-container {{ position: relative; height: {chartHeight}px; width: 90vw; margin-top: 2em; background-color: #fff; border: 1px solid #e1e4e8; border-radius: 6px; padding: 1em; }}
    </style>
</head>
<body>
    <h1>RepoScore Report: {repoName}</h1>
    <div class=""chart-container"">
        <canvas id=""contributionChart""></canvas>
    </div>
    <script>
        const ctx = document.getElementById('contributionChart');
        new Chart(ctx, {{
            type: 'bar',
            data: {{
                labels: {labels},
                datasets: [
                    {{ label: '문서 이슈', data: {docIssueData}, backgroundColor: '#a2eeef' }},
                    {{ label: '기능/버그 이슈', data: {featBugIssueData}, backgroundColor: '#28a745' }},
                    {{ label: '오타 PR', data: {typoPrData}, backgroundColor: '#fbca04' }},
                    {{ label: '문서 PR', data: {docPrData}, backgroundColor: '#0366d6' }},
                    {{ label: '기능/버그 PR', data: {featBugPrData}, backgroundColor: '#d73a49' }}
                ]
            }},
            options: {{
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                scales: {{ x: {{ stacked: true }}, y: {{ stacked: true }} }},
                plugins: {{
                    title: {{ display: true, text: '사용자별 기여 항목 분포' }},
                    legend: {{ position: 'top' }}
                }}
            }}
        }});
    </script>
</body>
</html>
";
        }

        public static string BuildClaimsReport(ClaimsData data, ClaimsMode mode)
        {
            var sb = new StringBuilder();

            if (data.ClaimedMap.Count == 0 && data.UnclaimedUrls.Count == 0)
            {
                return "최근 48시간 내 선점된 이슈가 없습니다.\n";
            }

            if (mode == ClaimsMode.User)
            {
                if (data.UnclaimedUrls.Count > 0)
                {
                    sb.AppendLine("미선점 이슈");
                    foreach (var url in data.UnclaimedUrls) sb.AppendLine($" - {url}");
                }

                if (data.ClaimedMap.Count > 0)
                {
                    sb.AppendLine("\n선점된 이슈");
                    foreach (var (login, claims) in data.ClaimedMap)
                    {
                        sb.AppendLine($"{login}");
                        foreach (var claim in claims)
                        {
                            sb.AppendLine($" - {claim.Url}");
                            if (claim.Labels.Count > 0) sb.AppendLine($"   라벨: {string.Join(", ", claim.Labels)}");
                            sb.AppendLine(FormatClaimStatus(claim));
                        }
                    }
                }
            }
            else
            {
                var claimedIssues = data.ClaimedMap.SelectMany(kv => kv.Value.Select(c => (Login: kv.Key, Claim: c)))
                                                  .OrderBy(x => x.Claim.Number).ToList();

                if (claimedIssues.Count > 0)
                {
                    sb.AppendLine("선점된 이슈");
                    foreach (var (login, claim) in claimedIssues)
                    {
                        sb.AppendLine($" #{claim.Number} {claim.Url}");
                        sb.AppendLine($"   선점자: {login}");
                        if (claim.Labels.Count > 0) sb.AppendLine($"   라벨: {string.Join(", ", claim.Labels)}");
                        sb.AppendLine(FormatClaimStatus(claim));
                    }
                }

                if (data.UnclaimedUrls.Count > 0)
                {
                    sb.AppendLine("\n미선점 이슈");
                    foreach (var url in data.UnclaimedUrls) sb.AppendLine($" - {url}");
                }
            }

            return sb.ToString();
        }

        public static string FormatClaimStatus(IssueRecord claim)
        {
            if (!claim.HasPr)
            {
                return FormatRemainingTime(claim.Remaining);
            }

            if (claim.LinkedPullRequests != null && claim.LinkedPullRequests.Count > 0)
            {
                var prNumbers = string.Join(", ", claim.LinkedPullRequests.Select(pr => $"#{pr.Number}"));
                return $"   PR 생성됨 - {prNumbers}";
            }

            return "   PR 생성됨";
        }

        public static string FormatRemainingTime(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero) return "   기한 초과";
            return $"   남은 시간: {(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public static string PadLeft(string text, int width)
        {
            return text.PadLeft(width);
        }

        public static string PadRightKorean(string text, int width)
        {
            int textWidth = GetDisplayWidth(text);
            if (textWidth >= width) return text;

            return text + new string(' ', width - textWidth);
        }

        public static string PadLeftKorean(string text, int width)
        {
            int textWidth = GetDisplayWidth(text);
            if (textWidth >= width) return text;

            return new string(' ', width - textWidth) + text;
        }

        public static int GetDisplayWidth(string text)
        {
            int width = 0;

            foreach (char c in text)
            {
                width += c > 127 ? 2 : 1;
            }

            return width;
        }
    }
}
