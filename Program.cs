using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cocona;
using RepoScore.Data;
using RepoScore.Services;
using Spectre.Console;

var app = CoconaApp.Create();

app.AddCommand((
    [Argument(Description = "대상 저장소 (예: owner/repo)")] string repo,
    [Option(Description = "GitHub Token (미입력시 GITHUB_TOKEN 사용)")] string? token = null,
    [Option(Description = "최근 이슈 선점 현황 조회 (issue|user)")] string? claims = null,
    [Option(Description = "출력 형식 (csv, txt)")] string format = "csv",
    [Option(Description = "출력 디렉토리 경로")] string output = "./results",
    [Option(Description = "정렬 기준 (score | id)")] string sortBy = "score",
    [Option(Description = "정렬 방법 (asc | desc)")] string sortOrder = "desc",
    [Option(Description = "이슈 선점 키워드 (쉼표 구분)")] string? keywords = null
) =>
{
    token ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token)) { Console.Error.WriteLine("오류: GitHub 토큰이 필요합니다."); return; }

    var parts = repo.Split('/');
    if (parts.Length != 2) { Console.Error.WriteLine("오류: 저장소 이름은 'owner/repo' 형식이어야 합니다."); return; }

    var service = new GitHubService(parts[0], parts[1], token, keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    try
    {
        if (claims != null)
        {
            Console.Error.WriteLine($"[{repo}] 최근 이슈 선점 현황을 조회합니다...");
            Console.Write(BuildClaimsReport(service.GetRecentClaimsData(), string.IsNullOrEmpty(claims) ? "issue" : claims));
            return;
        }

        Console.Error.WriteLine($"{repo} 기여자 데이터 수집 및 분석 중...");
        if (!Directory.Exists(output)) Directory.CreateDirectory(output);
        string cachePath = Path.Combine(output, "cache.json");
        var cache = CacheManager.LoadCache(cachePath, repo);
        DateTimeOffset? since = cache.LastAnalyzedAt > DateTimeOffset.MinValue ? cache.LastAnalyzedAt : null;

        var contributors = service.GetAllContributors();
        if (contributors.Count == 0) { Console.Error.WriteLine("조회된 기여자가 없습니다."); return; }

        var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>();
        foreach (var user in contributors)
        {
            var newClaims = service.GetClaims(user, since);
            var newPrs = service.GetPullRequests(user, since);
            if (!cache.UserClaims.ContainsKey(user)) cache.UserClaims[user] = new List<ClaimRecord>();
            if (!cache.UserPullRequests.ContainsKey(user)) cache.UserPullRequests[user] = new List<PRRecord>();
            cache.UserClaims[user].AddRange(newClaims);
            cache.UserPullRequests[user].AddRange(newPrs);

            var userClaims = cache.UserClaims[user].Where(c => c.ClosedReason != IssueClosedStateReason.NotPlanned && c.ClosedReason != IssueClosedStateReason.Duplicate);
            var userPrs = cache.UserPullRequests[user].Where(p => p.IsMerged);

            reportData.Add((user, userClaims.Count(c => c.Labels.Contains(GitHubIssuePrLabel.Documentation)), userClaims.Count(c => c.Labels.Contains(GitHubIssuePrLabel.Bug) || c.Labels.Contains(GitHubIssuePrLabel.Enhancement)), userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Typo)), userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Documentation)), userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Bug) || p.Labels.Contains(GitHubIssuePrLabel.Enhancement)), ScoreCalculator.CalculateFinalScore(userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Bug) || p.Labels.Contains(GitHubIssuePrLabel.Enhancement)), userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Documentation)), userPrs.Count(p => p.Labels.Contains(GitHubIssuePrLabel.Typo)), userClaims.Count(c => c.Labels.Contains(GitHubIssuePrLabel.Bug) || c.Labels.Contains(GitHubIssuePrLabel.Enhancement)), userClaims.Count(c => c.Labels.Contains(GitHubIssuePrLabel.Documentation)))));
        }
        CacheManager.SaveCache(cachePath, cache);

        reportData = SortReportData(reportData, sortBy, sortOrder);
        var csv = new StringBuilder("아이디, 문서이슈, 버그/기능이슈, 오타PR, 문서PR, 버그/기능PR, 총점\n");
        foreach (var r in reportData) csv.AppendLine($"{r.Id}, {r.docIssues}, {r.featBugIssues}, {r.typoPrs}, {r.docPrs}, {r.featBugPrs}, {r.Score}");
        File.WriteAllText(Path.Combine(output, "results.csv"), csv.ToString());

        if (format.ToLower() == "txt")
        {
            File.WriteAllText(Path.Combine(output, "results.txt"), BuildTextReport(repo, reportData));
            Console.Error.WriteLine("가독성 리포트(TXT) 저장 완료.");
        }

        Console.Error.WriteLine("분석 완료.");
    }
    catch (Exception ex) { Console.Error.WriteLine($"오류 발생: {ex.Message}"); }
});
app.Run();

static List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
SortReportData(List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)> data, string sortBy, string sortOrder)
{
    return sortBy.ToLower() switch
    {
        "score" => sortOrder.ToLower() == "asc" ? data.OrderBy(x => x.Score).ToList() : data.OrderByDescending(x => x.Score).ToList(),
        _ => sortOrder.ToLower() == "asc" ? data.OrderBy(x => x.Id).ToList() : data.OrderByDescending(x => x.Id).ToList()
    };
}

static string BuildTextReport(string repo, List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)> reportData)
{
    var table = new Table();
    table.Border(TableBorder.Rounded);
    table.AddColumn("유저");
    table.AddColumn(new TableColumn("이슈/PR").RightAligned());
    table.AddColumn(new TableColumn("점수").RightAligned());

    foreach (var r in reportData)
        table.AddRow(r.Id, $"{r.docIssues + r.featBugIssues} / {r.typoPrs + r.docPrs + r.featBugPrs}", r.Score.ToString());

    using var writer = new StringWriter();
    var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) });
    console.Write(table);

    return $"=== {repo} 오픈소스 기여도 분석 리포트 ===\n분석 일시: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n{writer}";
}

static string BuildClaimsReport(ClaimsData data, string mode)
{
    var sb = new StringBuilder();
    if (data.ClaimedMap.Count == 0) return "최근 48시간 내 선점된 이슈가 없습니다.\n";
    sb.AppendLine("미선점 이슈");
    foreach (var url in data.UnclaimedUrls) sb.AppendLine($" - {url}");
    sb.AppendLine("\n선점된 이슈");
    foreach (var (login, claims) in data.ClaimedMap)
    {
        sb.AppendLine($"{login}");
        foreach (var claim in claims)
        {
            sb.AppendLine($" - {claim.Url}");
            if (claim.Labels.Count > 0) sb.AppendLine($"    라벨: {string.Join(", ", claim.Labels)}");
            sb.AppendLine(claim.HasPr ? "    PR 생성됨" : $"    남은 시간: {(int)claim.Remaining.TotalHours:D2}:{claim.Remaining.Minutes:D2}:{claim.Remaining.Seconds:D2}");
        }
    }
    return sb.ToString();
}
