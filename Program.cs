using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cocona;
using RepoScore.Data;
using RepoScore.Services;
using Spectre.Console;
using System.Globalization;

CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

await CoconaApp.RunAsync(async (
[Argument(Description = "대상 저장소 목록 (예: owner/repo1 owner/repo2)")] string[] repos,
[Option('t', Description = "GitHub Token (미입력시 GITHUB_TOKEN 사용)")] string? token = null,
[Option(Description = "최근 이슈 선점 현황 조회")] ClaimsMode? claims = null,
[Option('f', Description = "출력 형식")] OutputFormat format = OutputFormat.Csv,
[Option('o', Description = "출력 디렉토리 경로")] string output = "./results",
[Option(Description = "정렬 기준")] SortBy sortBy = SortBy.Score,
[Option(Description = "정렬 방법")] SortOrder sortOrder = SortOrder.Desc,
[Option(Description = "이슈 선점 키워드 (쉼표 구분, 미입력시 기본값 사용)")] string? keywords = null,
[Option(Description = "캐시를 무시하고 전체 데이터를 다시 수집할지 여부")] bool noCache = false
) =>
{
    var formatErrors = new List<string>();
    foreach (var repo in repos)
    {
        var parts = repo.Split('/');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            formatErrors.Add($"오류: '{repo}'는 'owner/repo' 형식이 아닙니다.");
    }

    if (formatErrors.Count > 0)
    {
        foreach (var error in formatErrors)
            Console.Error.WriteLine(error);
        Console.Error.WriteLine();
        Console.Error.WriteLine("도움말을 보려면 --help 옵션을 사용하세요.");
        throw new CommandExitedException(1);
    }

    token ??= Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        Console.Error.WriteLine("오류: GitHub 토큰이 필요합니다.");
        Console.Error.WriteLine();
        Console.Error.WriteLine("도움말을 보려면 --help 옵션을 사용하세요.");
        throw new CommandExitedException(1);
    }

    string[]? parsedKeywords = keywords != null
        ? keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : null;

    // 저장소별 (issues, prs) 결과를 담을 딕셔너리 — 병렬 처리 후 합산에 사용
    var repoResults = new System.Collections.Concurrent.ConcurrentDictionary<string, (Dictionary<string, List<IssueRecord>> UserIssues, Dictionary<string, List<PRRecord>> UserPrs)>();

    // 다중 저장소를 병렬로 처리 (동시 실행 상한: 8)
    using var semaphore = new System.Threading.SemaphoreSlim(8);

    var repoTasks = repos.Select(async repo =>
    {
        await semaphore.WaitAsync();
        try
        {
            var parts = repo.Split('/');
            string ownerName = parts[0];
            string repoName = parts[1];

            string repoOutput = repos.Length > 1
                ? Path.Combine(output, $"{ownerName}_{repoName}")
                : output;
            if (!Directory.Exists(repoOutput)) Directory.CreateDirectory(repoOutput);
            string cachePath = Path.Combine(repoOutput, "cache.json");
            var cache = CacheManager.LoadCache(cachePath, repo, noCache);

            var service = new GitHubService(ownerName, repoName, token, parsedKeywords);

            try
            {
                // ── Claims 전용 모드 ──────────────────────────────────────────────────
                if (claims != null)
                {
                    AnsiConsole.MarkupLine($"[[[blue]{ownerName}/{repoName}[/]]] 최근 이슈 선점 현황을 조회합니다...\n");

                    DateTimeOffset? claimsSince = (!noCache && cache.LastClaimsAnalyzedAt != DateTimeOffset.MinValue)
                        ? cache.LastClaimsAnalyzedAt
                        : (DateTimeOffset?)null;

                    if (claimsSince.HasValue)
                        Console.Error.WriteLine($"[{repo}] Claims 캐시 존재: {claimsSince.Value.ToLocalTime():yyyy-MM-dd HH:mm} — 이후 변경분만 재조회합니다.");
                    else
                        Console.Error.WriteLine($"[{repo}] Claims 캐시 없음: 전체 데이터를 수집합니다.");

                    var cachedOpenIssues = cache.CachedOpenIssues.Count > 0 ? cache.CachedOpenIssues : null;
                    var cachedOpenPrs = cache.CachedOpenPrs.Count > 0 ? cache.CachedOpenPrs : null;

                    var (claimsData, updatedOpenIssues, updatedOpenPrs) = await service.GetRecentClaimsDataAsync(
                        cachedOpenIssues, cachedOpenPrs, claimsSince);

                    var report = ReportFormatter.BuildClaimsReport(claimsData, (ClaimsMode)claims);
                    Console.Write(report);

                    CacheManager.SaveClaimsCache(cachePath, cache, updatedOpenIssues, updatedOpenPrs);
                    Console.Error.WriteLine($"[{repo}] Claims 캐시 갱신 완료: {cachePath}");

                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]{repo}[/] 기여자 데이터 수집 및 분석 중...");

                if (!Directory.Exists(repoOutput)) Directory.CreateDirectory(repoOutput);

                if (!CacheManager.HasSameKeywords(cache, parsedKeywords))
                {
                    Console.Error.WriteLine($"[{repo}] 키워드 옵션이 이전 실행과 달라 캐시를 무효화합니다.");

                    cache = new RepoCache
                    {
                        Repository = repo,
                        Keywords = parsedKeywords
                    };
                }

                DateTimeOffset? since = cache.LastAnalyzedAt == DateTimeOffset.MinValue
                    ? null
                    : cache.LastAnalyzedAt;

                if (since.HasValue)
                    Console.Error.WriteLine($"[{repo}] 기존 캐시 존재: {since.Value.ToLocalTime():yyyy-MM-dd HH:mm}");
                else
                    Console.Error.WriteLine($"[{repo}] 기존 캐시 없음: 전체 데이터를 수집합니다.");

                // PR과 이슈를 병렬로 조회
                var prsTask = service.GetPullRequestsAsync(since);
                var issuesTask = service.GetIssuesAsync(since);
                await Task.WhenAll(prsTask, issuesTask);
                var allNewPrs = prsTask.Result;
                var allNewIssues = issuesTask.Result;

                List<string> contributors = allNewPrs.Select(p => p.AuthorLogin)
                    .Concat(allNewIssues.Select(i => i.AuthorLogin))
                    .Concat(cache.UserIssues.Keys)
                    .Concat(cache.UserPullRequests.Keys)
                    .Where(login => !string.IsNullOrEmpty(login))
                    .Distinct()
                    .ToList();

                if (contributors.Count == 0)
                {
                    Console.Error.WriteLine($"[{repo}] 조회된 기여자가 없습니다.");
                    return;
                }

                var reportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>();

                // 저장소별 집계용 (합산에 사용)
                var repoUserIssues = new Dictionary<string, List<IssueRecord>>();
                var repoUserPrs = new Dictionary<string, List<PRRecord>>();

                foreach (var user in contributors)
                {
                    var newIssues = allNewIssues.Where(i => i.AuthorLogin == user).ToList();
                    var newPrs = allNewPrs.Where(p => p.AuthorLogin == user).ToList();

                    if (!cache.UserIssues.ContainsKey(user)) cache.UserIssues[user] = new List<IssueRecord>();
                    if (!cache.UserPullRequests.ContainsKey(user)) cache.UserPullRequests[user] = new List<PRRecord>();

                    foreach (var ni in newIssues)
                    {
                        int index = cache.UserIssues[user].FindIndex(c => c.Number == ni.Number);
                        if (index >= 0) cache.UserIssues[user][index] = ni;
                        else cache.UserIssues[user].Add(ni);
                    }

                    foreach (var npr in newPrs)
                    {
                        int index = cache.UserPullRequests[user].FindIndex(p => p.Number == npr.Number);
                        if (index >= 0) cache.UserPullRequests[user][index] = npr;
                        else cache.UserPullRequests[user].Add(npr);
                    }

                    var userIssuesToCalc = cache.UserIssues[user];
                    var prsToCalc = cache.UserPullRequests[user];

                    var featureBugPrs = prsToCalc.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Bug) || p.Labels.Contains(GitHubIssuePrLabel.Enhancement)).ToList();
                    var docPrs = prsToCalc.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Documentation)).ToList();
                    var typoPrs = prsToCalc.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Typo)).ToList();
                    var featureBugIssues = userIssuesToCalc.Where(c => c.Labels.Contains(GitHubIssuePrLabel.Bug) || c.Labels.Contains(GitHubIssuePrLabel.Enhancement)).ToList();
                    var docIssues = userIssuesToCalc.Where(c => c.Labels.Contains(GitHubIssuePrLabel.Documentation)).ToList();

                    int finalScore = ScoreCalculator.CalculateFinalScore(featureBugPrs.Count, docPrs.Count, typoPrs.Count, featureBugIssues.Count, docIssues.Count);

                    reportData.Add((user, docIssues.Count, featureBugIssues.Count, typoPrs.Count, docPrs.Count, featureBugPrs.Count, finalScore));

                    if (repos.Length > 1)
                    {
                        repoUserIssues[user] = cache.UserIssues[user];
                        repoUserPrs[user] = cache.UserPullRequests[user];
                    }
                }

                CacheManager.SaveCache(cachePath, cache, parsedKeywords);
                Console.Error.WriteLine($"[{repo}] 캐시 갱신 및 저장 완료: {cachePath}");

                reportData = ReportSorter.SortReportData(reportData, sortBy, sortOrder);

                var csv = new StringBuilder();
                csv.AppendLine("아이디, 문서이슈, 버그/기능이슈, 오타PR, 문서PR, 버그/기능PR, 총점");
                foreach (var r in reportData) csv.AppendLine($"{r.Id}, {r.docIssues}, {r.featBugIssues}, {r.typoPrs}, {r.docPrs}, {r.featBugPrs}, {r.Score}");

                string csvPath = Path.Combine(repoOutput, "results.csv");
                File.WriteAllText(csvPath, csv.ToString(), Encoding.UTF8);
                Console.Error.WriteLine($"[{repo}] 기본 데이터(CSV) 저장 완료: {csvPath}");

                if (format == OutputFormat.Txt)
                {
                    string txtPath = Path.Combine(repoOutput, "results.txt");
                    string txtContent = ReportFormatter.BuildTextReport(repo, reportData);
                    File.WriteAllText(txtPath, txtContent, Encoding.UTF8);
                    Console.Error.WriteLine($"[{repo}] 가독성 리포트(TXT) 추가 저장 완료: {txtPath}");
                }

                if (format == OutputFormat.Html)
                {
                    string htmlPath = Path.Combine(repoOutput, "results.html");
                    string htmlContent = ReportFormatter.BuildHtmlReport(repo, reportData);
                    File.WriteAllText(htmlPath, htmlContent, Encoding.UTF8);
                    Console.Error.WriteLine($"[{repo}] HTML 리포트 추가 저장 완료: {htmlPath}");
                }

                if (repos.Length > 1)
                {
                    repoResults[repo] = (repoUserIssues, repoUserPrs);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Could not resolve to a Repository") ||
                    (ex.InnerException?.Message.Contains("Could not resolve to a Repository") == true))
                {
                    Console.Error.WriteLine($"오류: '{repo}' 저장소가 존재하지 않거나 접근할 수 없습니다.");
                    Environment.Exit(1);
                    return;
                }
                AnsiConsole.WriteException(ex);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }).ToList();

    await Task.WhenAll(repoTasks);

    if (repos.Length > 1 && repoResults.Count > 0)
    {
        try
        {
            AnsiConsole.MarkupLine($"\n[green]전체 저장소 합산 리포트 생성 중...[/]");

            // 저장소별 결과를 순회하며 URL 기반 중복 제거 후 합산
            var totalUserIssues = new Dictionary<string, List<IssueRecord>>();
            var totalUserPullRequests = new Dictionary<string, List<PRRecord>>();

            foreach (var (_, (userIssues, userPrs)) in repoResults)
            {
                foreach (var (user, issues) in userIssues)
                {
                    if (!totalUserIssues.ContainsKey(user)) totalUserIssues[user] = new List<IssueRecord>();
                    foreach (var issue in issues)
                    {
                        bool isDuplicate = string.IsNullOrEmpty(issue.Url)
                            ? totalUserIssues[user].Any(i => string.IsNullOrEmpty(i.Url) && i.Number == issue.Number)
                            : totalUserIssues[user].Any(i => i.Url == issue.Url);
                        if (!isDuplicate)
                            totalUserIssues[user].Add(issue);
                    }
                }

                foreach (var (user, prs) in userPrs)
                {
                    if (!totalUserPullRequests.ContainsKey(user)) totalUserPullRequests[user] = new List<PRRecord>();
                    foreach (var pr in prs)
                    {
                        bool isDuplicate = string.IsNullOrEmpty(pr.Url)
                            ? totalUserPullRequests[user].Any(p => string.IsNullOrEmpty(p.Url) && p.Number == pr.Number)
                            : totalUserPullRequests[user].Any(p => p.Url == pr.Url);
                        if (!isDuplicate)
                            totalUserPullRequests[user].Add(pr);
                    }
                }
            }

            var totalReportData = new List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>();

            var allUsers = totalUserIssues.Keys.Union(totalUserPullRequests.Keys).ToList();

            foreach (var user in allUsers)
            {
                var allIssues = totalUserIssues.TryGetValue(user, out var issues) ? issues : new List<IssueRecord>();
                var allPrs = totalUserPullRequests.TryGetValue(user, out var prs) ? prs : new List<PRRecord>();

                var featureBugPrs = allPrs.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Bug) || p.Labels.Contains(GitHubIssuePrLabel.Enhancement)).ToList();
                var docPrs = allPrs.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Documentation)).ToList();
                var typoPrs = allPrs.Where(p => p.Labels.Contains(GitHubIssuePrLabel.Typo)).ToList();
                var featureBugIssues = allIssues.Where(c => c.Labels.Contains(GitHubIssuePrLabel.Bug) || c.Labels.Contains(GitHubIssuePrLabel.Enhancement)).ToList();
                var docIssues = allIssues.Where(c => c.Labels.Contains(GitHubIssuePrLabel.Documentation)).ToList();

                int finalScore = ScoreCalculator.CalculateFinalScore(featureBugPrs.Count, docPrs.Count, typoPrs.Count, featureBugIssues.Count, docIssues.Count);

                totalReportData.Add((user, docIssues.Count, featureBugIssues.Count, typoPrs.Count, docPrs.Count, featureBugPrs.Count, finalScore));
            }

            totalReportData = ReportSorter.SortReportData(totalReportData, sortBy, sortOrder);

            string totalOutput = output;
            if (!Directory.Exists(totalOutput)) Directory.CreateDirectory(totalOutput);

            var totalCsv = new StringBuilder();
            totalCsv.AppendLine("아이디, 문서이슈, 버그/기능이슈, 오타PR, 문서PR, 버그/기능PR, 총점");
            foreach (var r in totalReportData) totalCsv.AppendLine($"{r.Id}, {r.docIssues}, {r.featBugIssues}, {r.typoPrs}, {r.docPrs}, {r.featBugPrs}, {r.Score}");

            string totalCsvPath = Path.Combine(totalOutput, "results.csv");
            File.WriteAllText(totalCsvPath, totalCsv.ToString(), Encoding.UTF8);
            Console.Error.WriteLine($"전체 합산 데이터(CSV) 저장 완료: {totalCsvPath}");

            if (format == OutputFormat.Txt)
            {
                string totalLabel = string.Join(" + ", repos);
                string totalTxtPath = Path.Combine(totalOutput, "results.txt");
                string totalTxtContent = ReportFormatter.BuildTextReport(totalLabel, totalReportData);
                File.WriteAllText(totalTxtPath, totalTxtContent, Encoding.UTF8);
                Console.Error.WriteLine($"전체 합산 리포트(TXT) 저장 완료: {totalTxtPath}");
            }

            if (format == OutputFormat.Html)
            {
                string totalLabel = string.Join(" + ", repos);
                string totalHtmlPath = Path.Combine(totalOutput, "results.html");
                string totalHtmlContent = ReportFormatter.BuildHtmlReport(totalLabel, totalReportData);
                File.WriteAllText(totalHtmlPath, totalHtmlContent, Encoding.UTF8);
                Console.Error.WriteLine($"전체 합산 HTML 리포트 저장 완료: {totalHtmlPath}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }
});
