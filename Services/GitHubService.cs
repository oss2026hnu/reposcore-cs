using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

namespace RepoScore.Services
{
    /// <summary>
    /// GitHub 이슈 및 Pull Request에 부여된 레이블의 종류를 나타내는 열거형입니다.
    /// </summary>
    public enum GitHubIssuePrLabel
    {
        None, Bug, Documentation, Duplicate, Enhancement, GoodFirstIssue,
        HelpWanted, Invalid, Pinned, Question, Typo, Wontfix
    }

    /// <summary>
    /// 이슈가 닫힌 구체적인 사유를 나타내는 열거형입니다.
    /// </summary>
    public enum IssueClosedStateReason
    {
        None,
        Completed,
        Duplicate,
        NotPlanned
    }

    /// <summary>
    /// 특정 이슈에 대한 선점 댓글 정보를 저장하고 캐싱하기 위한 클래스입니다.
    /// </summary>
    public class ClaimComment
    {
        /// <summary>
        /// 댓글 작성자의 GitHub 로그인 ID를 가져오거나 설정합니다.
        /// </summary>
        public string AuthorLogin { get; set; } = string.Empty;

        /// <summary>
        /// 댓글이 작성된 일시를 가져오거나 설정합니다.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }

    /// <summary>
    /// GitHub 이슈의 상세 기여 정보를 담는 레코드 클래스입니다.
    /// </summary>
    public class IssueRecord
    {
        /// <summary>
        /// 이슈 번호를 가져오거나 설정합니다.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// 이슈의 GitHub 상세 URL 주소를 가져오거나 설정합니다.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 이슈의 제목을 가져오거나 설정합니다.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 이슈 작성자의 GitHub 로그인 ID를 가져오거나 설정합니다.
        /// </summary>
        public string AuthorLogin { get; set; } = string.Empty;

        /// <summary>
        /// 해당 이슈와 연결된 Pull Request가 존재하는지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool HasPr { get; set; }

        /// <summary>
        /// 이 이슈와 연동된 Pull Request 기록 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<PRRecord> LinkedPullRequests { get; set; } = new();

        /// <summary>
        /// 이슈가 종료된 사유를 가져오거나 설정합니다.
        /// </summary>
        public IssueClosedStateReason ClosedReason { get; set; } = IssueClosedStateReason.None;

        /// <summary>
        /// 선점 만료까지 남은 잔여 시간을 가져오거나 설정합니다.
        /// </summary>
        public TimeSpan Remaining { get; set; }

        /// <summary>
        /// 이슈에 부착된 유효 레이블 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<GitHubIssuePrLabel> Labels { get; set; } = new();

        /// <summary>
        /// 이슈가 최종 업데이트된 일시를 가져오거나 설정합니다.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// 캐싱된 이슈 선점 댓글 목록을 가져오거나 설정합니다. 값이 null일 경우 JSON 직렬화에서 제외됩니다.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ClaimComment>? CachedClaimComments { get; set; } = null;
    }

    /// <summary>
    /// 저장소 이슈들의 최근 선점 및 미선점 현황 데이터를 관리하는 클래스입니다.
    /// </summary>
    public class ClaimsData
    {
        /// <summary>
        /// 사용자별로 선점한 이슈 목록을 매핑한 딕셔너리를 가져오거나 설정합니다.
        /// </summary>
        public Dictionary<string, List<IssueRecord>> ClaimedMap { get; set; } = new();

        /// <summary>
        /// 아직 아무도 선점하지 않은 열린 이슈들의 URL 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<string> UnclaimedUrls { get; set; } = new();
    }

    /// <summary>
    /// GitHub Pull Request(PR)의 상세 기여 정보를 담는 레코드 클래스입니다.
    /// </summary>
    public class PRRecord
    {
        /// <summary>
        /// Pull Request 번호를 가져오거나 설정합니다.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// Pull Request의 GitHub 상세 URL 주소를 가져오거나 설정합니다.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Pull Request의 제목을 가져오거나 설정합니다.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Pull Request 작성자의 GitHub 로그인 ID를 가져오거나 설정합니다.
        /// </summary>
        public string AuthorLogin { get; set; } = string.Empty;

        /// <summary>
        /// 해당 Pull Request가 본문에 최종 병합(Merged)되었는지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool IsMerged { get; set; } = false;

        /// <summary>
        /// Pull Request에 부착된 유효 레이블 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<GitHubIssuePrLabel> Labels { get; set; } = new();

        /// <summary>
        /// Pull Request가 최종 업데이트된 일시를 가져오거나 설정합니다.
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; }

        /// <summary>
        /// 본문 분석을 통해 연동이 확인된 이슈 번호 목록을 가져오거나 설정합니다. 기본값일 경우 JSON 직렬화에서 제외됩니다.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<int> LinkedIssueNumbers { get; set; } = new();
    }

    /// <summary>
    /// Pull Request 정보와 해당 PR 본문에서 참조 중인 이슈 번호 목록을 쌍으로 묶어 관리하는 데이터 구조 클래스입니다.
    /// </summary>
    public class PRWithLinkedIssues
    {
        /// <summary>
        /// 대상 Pull Request 기록 객체를 가져오거나 설정합니다.
        /// </summary>
        public PRRecord Pr { get; set; } = new();

        /// <summary>
        /// 이 Pull Request와 연결된 이슈 번호 목록을 가져오거나 설정합니다.
        /// </summary>
        public List<int> LinkedIssueNumbers { get; set; } = new();
    }

    /// <summary>
    /// GitHub API를 통해 저장소 데이터를 효율적으로 원격 조회하는 서비스 클래스입니다.
    /// </summary>
    public class GitHubService
    {
        private readonly Octokit.GraphQL.Connection _graphQLConnection;
        private readonly string _owner;
        private readonly string _repo;

        private static readonly string[] s_defaultClaimKeywords = { "제가 하겠습니다", "진행하겠습니다", "할게요", "I'll take this" };
        private readonly string[] _claimKeywords;

        /// <summary>
        /// 지정된 저장소 정보 및 인증 토큰을 사용하여 <see cref="GitHubService"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public GitHubService(string owner, string repo, string token, string[]? keywords = null)
        {
            _owner = owner;
            _repo = repo;
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            _claimKeywords = keywords ?? s_defaultClaimKeywords;

            _graphQLConnection = new Octokit.GraphQL.Connection(
                new Octokit.GraphQL.ProductHeaderValue("reposcore-cs"), token);
        }

        /// <summary>
        /// 저장소 내에서 메인 브랜치에 병합(Merged)이 완료된 전체 Pull Request 목록을 GraphQL로 비동기 조회합니다.
        /// </summary>
        public async System.Threading.Tasks.Task<List<PRRecord>> GetPullRequestsAsync(DateTimeOffset? since = null)
        {
            string searchString = $"repo:{_owner}/{_repo} is:pr is:merged";
            if (since.HasValue)
            {
                searchString += $" updated:>={since.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}";
            }

            var prRecords = new List<PRRecord>();
            string? cursor = null;
            bool hasNextPage = true;

            while (hasNextPage)
            {
                var query = new Octokit.GraphQL.Query()
                    .Search(query: searchString, type: SearchType.Issue, first: 100, after: cursor)
                    .Select(s => new
                    {
                        s.PageInfo.HasNextPage,
                        s.PageInfo.EndCursor,
                        Items = s.Nodes.OfType<Octokit.GraphQL.Model.PullRequest>().Select(pr => new
                        {
                            pr.Number,
                            pr.Title,
                            pr.Url,
                            pr.Merged,
                            pr.UpdatedAt,
                            AuthorLogin = pr.Author.Login,
                            Labels = pr.Labels(10, null, null, null, null).Nodes.Select(l => l.Name).ToList()
                        }).ToList()
                    });

                var result = await _graphQLConnection.Run(query);

                foreach (var pr in result.Items)
                {
                    prRecords.Add(new PRRecord
                    {
                        Number = pr.Number,
                        Title = pr.Title,
                        Url = pr.Url,
                        AuthorLogin = pr.AuthorLogin ?? "",
                        IsMerged = pr.Merged,
                        UpdatedAt = pr.UpdatedAt,
                        Labels = pr.Labels.Select(ParseGitHubLabel).Where(l => l != GitHubIssuePrLabel.None).ToList()
                    });
                }

                hasNextPage = result.HasNextPage;
                cursor = result.EndCursor;
            }

            return prRecords;
        }

        /// <summary>
        /// GraphQL 멀티 쿼리(Alias)를 활용하여 단 1번의 호출로 전수 통계용 이슈 리스트와 선점 계산용 열린 이슈 리스트를 최적화 수집합니다.
        /// </summary>
        /// <param name="since">지정된 경우, 해당 일시 이후에 최종 업데이트된 데이터만 필터링합니다.</param>
        /// <returns>통합 이슈 리스트와 열린 이슈 리스트의 튜플 구조</returns>
        public async System.Threading.Tasks.Task<(List<IssueRecord> AllIssues, List<IssueRecord> OpenIssues)> GetIssuesCombinedAsync(DateTimeOffset? since = null)
        {
            // 오픈 이슈 쿼리(Alias openIssues)에는 댓글(comments)을 요청하고,
            // 클로즈 이슈 쿼리(Alias closedIssues)에는 댓글을 배제하여 교수님의 피드백을 반영했습니다.
            const string rawGraphQl = @"
            query($owner: String!, $repoName: String!, $openQuery: String!, $closedQuery: String!, $openAfter: String, $closedAfter: String) {
                repository(owner: $owner, name: $repoName) { id }
                openIssues: search(query: $openQuery, type: ISSUE, first: 100, after: $openAfter) {
                    pageInfo { hasNextPage endCursor }
                    nodes {
                        ... on Issue {
                            number title url stateReason updatedAt
                            author { login }
                            labels(first: 10) { nodes { name } }
                            comments(first: 30) {
                                nodes { body createdAt author { login } }
                            }
                        }
                    }
                }
                closedIssues: search(query: $closedQuery, type: ISSUE, first: 100, after: $closedAfter) {
                    pageInfo { hasNextPage endCursor }
                    nodes {
                        ... on Issue {
                            number title url stateReason updatedAt
                            author { login }
                            labels(first: 10) { nodes { name } }
                        }
                    }
                }
            }";

            string baseSearch = $"repo:{_owner}/{_repo} is:issue -reason:\"not planned\" -reason:\"duplicate\"";
            if (since.HasValue)
            {
                baseSearch += $" updated:>={since.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}";
            }

            string openQueryStr = $"{baseSearch} is:open";
            string closedQueryStr = $"{baseSearch} is:closed";

            var allIssues = new List<IssueRecord>();
            var openIssues = new List<IssueRecord>();

            string? openCursor = null;
            string? closedCursor = null;
            bool openHasNext = true;
            bool closedHasNext = true;
            var now = DateTimeOffset.UtcNow;

            while (openHasNext || closedHasNext)
            {
                var requestPayload = JsonSerializer.Serialize(new
                {
                    query = rawGraphQl,
                    variables = new Dictionary<string, object?>
                    {
                        ["owner"] = _owner,
                        ["repoName"] = _repo,
                        ["openQuery"] = openQueryStr,
                        ["closedQuery"] = closedQueryStr,
                        ["openAfter"] = openHasNext ? openCursor : null,
                        ["closedAfter"] = closedHasNext ? closedCursor : null
                    }
                });

                var rawResponse = await _graphQLConnection.Run(requestPayload);
                using var document = JsonDocument.Parse(rawResponse);

                if (document.RootElement.TryGetProperty("errors", out var errorsElement))
                {
                    var firstError = errorsElement.EnumerateArray().FirstOrDefault();
                    throw new InvalidOperationException(firstError.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : "GraphQL 쿼리 에러");
                }

                if (!document.RootElement.TryGetProperty("data", out var dataElement)) break;

                // 1. 오픈 이슈 파싱 (댓글 포함 채우기)
                if (openHasNext && dataElement.TryGetProperty("openIssues", out var openNode))
                {
                    var pageInfo = openNode.GetProperty("pageInfo");
                    openHasNext = pageInfo.GetProperty("hasNextPage").GetBoolean();
                    openCursor = pageInfo.GetProperty("endCursor").GetString();

                    foreach (var node in openNode.GetProperty("nodes").EnumerateArray())
                    {
                        var record = ParseSingleIssueJson(node, now, includeComments: true);
                        openIssues.Add(record);
                        allIssues.Add(record);
                    }
                }
                else { openHasNext = false; }

                // 2. 닫힌 이슈 파싱 (댓글 제외하여 응답 최적화)
                if (closedHasNext && dataElement.TryGetProperty("closedIssues", out var closedNode))
                {
                    var pageInfo = closedNode.GetProperty("pageInfo");
                    closedHasNext = pageInfo.GetProperty("hasNextPage").GetBoolean();
                    closedCursor = pageInfo.GetProperty("endCursor").GetString();

                    foreach (var node in closedNode.GetProperty("nodes").EnumerateArray())
                    {
                        var record = ParseSingleIssueJson(node, now, includeComments: false);
                        allIssues.Add(record);
                    }
                }
                else { closedHasNext = false; }
            }

            return (allIssues, openIssues);
        }

        /// <summary>
        /// 공통적인 단일 이슈 JSON 데이터를 시스템 레코드 구조 객체로 변환해 주는 공용 내부 헬퍼 메서드입니다.
        /// </summary>
        private IssueRecord ParseSingleIssueJson(JsonElement node, DateTimeOffset now, bool includeComments)
        {
            var labelNames = new List<string>();
            if (node.TryGetProperty("labels", out var labelsElement) && labelsElement.TryGetProperty("nodes", out var nodes))
            {
                foreach (var labelNode in nodes.EnumerateArray())
                {
                    if (labelNode.TryGetProperty("name", out var nameEl))
                        labelNames.Add(nameEl.GetString() ?? "");
                }
            }

            string authorLogin = "";
            if (node.TryGetProperty("author", out var authEl) && authEl.ValueKind == JsonValueKind.Object)
            {
                authorLogin = authEl.TryGetProperty("login", out var logEl) ? logEl.GetString() ?? "" : "";
            }

            var record = new IssueRecord
            {
                Number = node.TryGetProperty("number", out var num) ? num.GetInt32() : 0,
                Title = node.TryGetProperty("title", out var tit) ? tit.GetString() ?? "" : "",
                Url = node.TryGetProperty("url", out var url) ? url.GetString() ?? "" : "",
                AuthorLogin = authorLogin,
                ClosedReason = ParseIssueClosedStateReason(node),
                Labels = labelNames.Select(ParseGitHubLabel).Where(l => l != GitHubIssuePrLabel.None).ToList(),
                UpdatedAt = node.TryGetProperty("updatedAt", out var up) ? DateTimeOffset.Parse(up.GetString()!) : DateTimeOffset.MinValue
            };

            if (includeComments && node.TryGetProperty("comments", out var commEl) && commEl.TryGetProperty("nodes", out var commNodes))
            {
                record.CachedClaimComments = commNodes.EnumerateArray()
                    .Where(c =>
                    {
                        if (!c.TryGetProperty("body", out var b) || string.IsNullOrEmpty(b.GetString())) return false;
                        var createdAtStr = c.TryGetProperty("createdAt", out var cr) ? cr.GetString() : null;
                        if (createdAtStr == null) return false;
                        return (now - DateTimeOffset.Parse(createdAtStr)).TotalHours <= 48
                            && _claimKeywords.Any(k => b.GetString()!.Contains(k, StringComparison.OrdinalIgnoreCase));
                    })
                    .Select(c => new ClaimComment
                    {
                        AuthorLogin = c.TryGetProperty("author", out var ca) && ca.ValueKind == JsonValueKind.Object && ca.TryGetProperty("login", out var cl) ? cl.GetString() ?? "unknown" : "unknown",
                        CreatedAt = DateTimeOffset.Parse(c.GetProperty("createdAt").GetString()!)
                    }).ToList();
            }

            return record;
        }

        /// <summary>
        /// 기존 레거시 GetIssuesAsync 메서드는 전수 조사가 가능한 새 통합 API를 바라보도록 우회 구현합니다.
        /// </summary>
        public async System.Threading.Tasks.Task<List<IssueRecord>> GetIssuesAsync(DateTimeOffset? since = null)
        {
            var (allIssues, _) = await GetIssuesCombinedAsync(since);
            return allIssues;
        }

        /// <summary>
        /// 저장소의 열린 이슈를 대상으로 최근 48시간 내 선점 현황을 비동기 조회합니다. (네트워크 중복 호출 최적화 완료)
        /// </summary>
        public async System.Threading.Tasks.Task<(ClaimsData claimsData, List<IssueRecord> updatedOpenIssues, List<PRRecord> updatedOpenPrs)>
            GetRecentClaimsDataAsync(
                List<IssueRecord>? cachedOpenIssues = null,
                List<PRRecord>? cachedOpenPrs = null,
                DateTimeOffset? since = null)
        {
            var now = DateTimeOffset.UtcNow;
            bool isFullRefresh = since == null || (now - since.Value).TotalHours > 48;

            var freshOpenPrs = await GetOpenPullRequestsWithLinkedIssuesAsync(isFullRefresh ? null : since);

            List<PRRecord> updatedOpenPrs;
            if (isFullRefresh || cachedOpenPrs == null)
            {
                updatedOpenPrs = freshOpenPrs.Select(p =>
                {
                    p.Pr.LinkedIssueNumbers = p.LinkedIssueNumbers;
                    return p.Pr;
                }).ToList();
            }
            else
            {
                updatedOpenPrs = new List<PRRecord>(cachedOpenPrs);
                foreach (var freshPrWithLinks in freshOpenPrs)
                {
                    var freshPr = freshPrWithLinks.Pr;
                    freshPr.LinkedIssueNumbers = freshPrWithLinks.LinkedIssueNumbers;
                    int idx = updatedOpenPrs.FindIndex(p => p.Number == freshPr.Number);
                    if (idx >= 0)
                        updatedOpenPrs[idx] = freshPr;
                    else
                        updatedOpenPrs.Add(freshPr);
                }
            }

            // [핵심 최적화 변경점]: 여기서 중복 쿼리를 날리던 내부 메서드 대신 새로 구현한 통합 메서드를 호출합니다.
            var (allIssues, freshIssues) = await GetIssuesCombinedAsync(isFullRefresh ? null : since);

            List<IssueRecord> updatedOpenIssues;
            if (isFullRefresh || cachedOpenIssues == null)
            {
                updatedOpenIssues = freshIssues;
            }
            else
            {
                var openIssueDict = cachedOpenIssues.ToDictionary(i => i.Number);
                foreach (var freshIssue in freshIssues)
                    openIssueDict[freshIssue.Number] = freshIssue;

                // 닫힌 이슈 번호 추적 처리
                var closedIssueNumbers = allIssues.Where(i => i.ClosedReason != IssueClosedStateReason.None).Select(i => i.Number);
                foreach (var closedNumber in closedIssueNumbers)
                    openIssueDict.Remove(closedNumber);

                updatedOpenIssues = openIssueDict.Values.ToList();
            }

            var claimsData = new ClaimsData();

            foreach (var issue in updatedOpenIssues)
            {
                var issueLabels = issue.Labels;
                var comments = issue.CachedClaimComments ?? new List<ClaimComment>();
                bool isClaimed = false;

                foreach (var comment in comments)
                {
                    if ((now - comment.CreatedAt).TotalHours > 48) continue;

                    var login = comment.AuthorLogin;
                    var deadlineHours = IsDocumentTask(issueLabels) ? 24.0 : 48.0;
                    var remaining = comment.CreatedAt.AddHours(deadlineHours) - now;

                    var linkedPrs = updatedOpenPrs
                        .Where(pr => pr.LinkedIssueNumbers.Contains(issue.Number))
                        .ToList();

                    if (!claimsData.ClaimedMap.ContainsKey(login))
                        claimsData.ClaimedMap[login] = new List<IssueRecord>();

                    claimsData.ClaimedMap[login].Add(new IssueRecord
                    {
                        Number = issue.Number,
                        Url = issue.Url,
                        HasPr = linkedPrs.Count > 0,
                        LinkedPullRequests = linkedPrs,
                        Remaining = remaining,
                        Labels = issueLabels
                    });
                    isClaimed = true;
                    break;
                }

                if (!isClaimed)
                    claimsData.UnclaimedUrls.Add(issue.Url);
            }

            return (claimsData, updatedOpenIssues, updatedOpenPrs);
        }

        /// <summary>
        /// since 이후 업데이트된 열린 PR과 본문에서 파싱한 연결 이슈 번호 목록을 비동기 반환합니다.
        /// </summary>
        public async System.Threading.Tasks.Task<List<PRWithLinkedIssues>> GetOpenPullRequestsWithLinkedIssuesAsync(DateTimeOffset? since = null)
        {
            var prsWithIssues = new List<PRWithLinkedIssues>();
            string? cursor = null;
            bool hasNextPage = true;

            var regex = new Regex(@"(?<!\w)#(\d+)\b");

            while (hasNextPage)
            {
                var query = new Octokit.GraphQL.Query()
                    .Repository(_repo, _owner)
                    .PullRequests(first: 100, states: new[] { PullRequestState.Open }, after: cursor)
                    .Select(s => new
                    {
                        s.PageInfo.HasNextPage,
                        s.PageInfo.EndCursor,
                        Items = s.Nodes.Select(pr => new
                        {
                            pr.Number,
                            pr.Title,
                            pr.Url,
                            pr.Body,
                            pr.UpdatedAt,
                            AuthorLogin = pr.Author.Login,
                            Labels = pr.Labels(10, null, null, null, null).Nodes.Select(l => l.Name).ToList()
                        }).ToList()
                    });

                var result = await _graphQLConnection.Run(query);

                foreach (var pr in result.Items)
                {
                    if (since.HasValue && pr.UpdatedAt < since.Value)
                        continue;

                    var linkedIssueNumbers = new HashSet<int>();

                    if (!string.IsNullOrWhiteSpace(pr.Body))
                    {
                        var matches = regex.Matches(pr.Body);
                        foreach (Match match in matches)
                        {
                            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out int issueNum))
                                linkedIssueNumbers.Add(issueNum);
                        }
                    }

                    prsWithIssues.Add(new PRWithLinkedIssues
                    {
                        Pr = new PRRecord
                        {
                            Number = pr.Number,
                            Title = pr.Title,
                            Url = pr.Url,
                            AuthorLogin = pr.AuthorLogin ?? "",
                            IsMerged = false,
                            UpdatedAt = pr.UpdatedAt,
                            Labels = pr.Labels.Select(ParseGitHubLabel).Where(l => l != GitHubIssuePrLabel.None).ToList()
                        },
                        LinkedIssueNumbers = linkedIssueNumbers.ToList()
                    });
                }

                hasNextPage = result.HasNextPage;
                cursor = result.EndCursor;
            }

            return prsWithIssues;
        }

        internal static bool IsDocumentTask(List<GitHubIssuePrLabel> issueLabels)
        {
            return issueLabels.Contains(GitHubIssuePrLabel.Documentation) || issueLabels.Contains(GitHubIssuePrLabel.Typo);
        }

        internal static GitHubIssuePrLabel ParseGitHubLabel(string labelName)
        {
            if (string.IsNullOrEmpty(labelName)) return GitHubIssuePrLabel.None;

            var normalized = labelName.ToLowerInvariant().Replace(" ", "").Replace("-", "");
            return normalized switch
            {
                "bug" => GitHubIssuePrLabel.Bug,
                "documentation" => GitHubIssuePrLabel.Documentation,
                "duplicate" => GitHubIssuePrLabel.Duplicate,
                "enhancement" => GitHubIssuePrLabel.Enhancement,
                "goodfirstissue" => GitHubIssuePrLabel.GoodFirstIssue,
                "helpwanted" => GitHubIssuePrLabel.HelpWanted,
                "invalid" => GitHubIssuePrLabel.Invalid,
                "pinned" => GitHubIssuePrLabel.Pinned,
                "question" => GitHubIssuePrLabel.Question,
                "typo" => GitHubIssuePrLabel.Typo,
                "wontfix" => GitHubIssuePrLabel.Wontfix,
                _ => GitHubIssuePrLabel.None,
            };
        }

        internal static IssueClosedStateReason ParseIssueClosedStateReason(JsonElement issueNode)
        {
            if (!issueNode.TryGetProperty("stateReason", out var stateReasonElement) ||
                stateReasonElement.ValueKind == JsonValueKind.Null)
            {
                return IssueClosedStateReason.None;
            }

            var reason = stateReasonElement.GetString()?.ToUpperInvariant();
            return reason switch
            {
                "COMPLETED" => IssueClosedStateReason.Completed,
                "DUPLICATE" => IssueClosedStateReason.Duplicate,
                "NOT_PLANNED" or "NOTPLANNED" => IssueClosedStateReason.NotPlanned,
                _ => IssueClosedStateReason.None
            };
        }
    }
}
