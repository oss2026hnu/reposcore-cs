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
    public enum GitHubIssuePrLabel
    {
        None, Bug, Documentation, Duplicate, Enhancement, GoodFirstIssue,
        HelpWanted, Invalid, Pinned, Question, Typo, Wontfix
    }

    public enum IssueClosedStateReason
    {
        None,
        Completed,
        Duplicate,
        NotPlanned
    }

    // 선점 댓글 정보를 캐시하기 위한 레코드
    public class ClaimComment
    {
        public string AuthorLogin { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class IssueRecord
    {
        public int Number { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AuthorLogin { get; set; } = string.Empty;
        public bool HasPr { get; set; }
        public List<PRRecord> LinkedPullRequests { get; set; } = new();
        public IssueClosedStateReason ClosedReason { get; set; } = IssueClosedStateReason.None;
        public TimeSpan Remaining { get; set; }
        public List<GitHubIssuePrLabel> Labels { get; set; } = new();
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ClaimComment>? CachedClaimComments { get; set; } = null;
    }

    public class ClaimsData
    {
        public Dictionary<string, List<IssueRecord>> ClaimedMap { get; set; } = new();
        public List<string> UnclaimedUrls { get; set; } = new();
    }

    public class PRRecord
    {
        public int Number { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AuthorLogin { get; set; } = string.Empty;
        public bool IsMerged { get; set; } = false;
        public List<GitHubIssuePrLabel> Labels { get; set; } = new();
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public List<int> LinkedIssueNumbers { get; set; } = new();
    }

    public class PRWithLinkedIssues
    {
        public PRRecord Pr { get; set; } = new();
        public List<int> LinkedIssueNumbers { get; set; } = new();
    }

    // GitHub REST/GraphQL API를 통해 저장소 데이터를 조회하는 서비스 클래스.
    // PR 조회, 이슈 조회, 기여자 목록 조회, 이슈 선점 현황 조회 기능을 담당.
    public class GitHubService
    {
        private readonly Octokit.GraphQL.Connection _graphQLConnection;
        private readonly string _owner;
        private readonly string _repo;

        private static readonly string[] s_defaultClaimKeywords = ["제가 하겠습니다", "진행하겠습니다", "할게요", "I'll take this"];
        private readonly string[] _claimKeywords;

        public GitHubService(string owner, string repo, string token, string[]? keywords = null)
        {
            _owner = owner;
            _repo = repo;
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            _claimKeywords = keywords ?? s_defaultClaimKeywords;

            _graphQLConnection = new Octokit.GraphQL.Connection(
                new Octokit.GraphQL.ProductHeaderValue("reposcore-cs"), token);
        }

        // 저장소의 머지된 전체 PR 목록을 GraphQL로 비동기 조회.
        // since가 지정된 경우 해당 시각 이후 업데이트된 PR만 가져옴.
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

        // 저장소의 전체 이슈 목록을 GraphQL로 비동기 조회.
        // "not planned", "duplicate" 사유로 닫힌 이슈는 제외.
        // since가 지정된 경우 해당 시각 이후 업데이트된 이슈만 가져옴.
        // repository 필드를 search와 함께 요청하여 존재하지 않는 저장소면 예외 발생.
        public async System.Threading.Tasks.Task<List<IssueRecord>> GetIssuesAsync(DateTimeOffset? since = null)
        {
            const string rawGraphQl = @"
            query($owner: String!, $repoName: String!, $searchQuery: String!, $after: String) {
                repository(owner: $owner, name: $repoName) {
                    id
                }
                search(query: $searchQuery, type: ISSUE, first: 100, after: $after) {
                    pageInfo {
                        hasNextPage
                        endCursor
                    }
                    nodes {
                        ... on Issue {
                            number
                            title
                            url
                            stateReason
                            updatedAt
                            author {
                                login
                            }
                            labels(first: 10) {
                                nodes {
                                    name
                                }
                            }
                        }
                    }
                }
            }";

            string searchString = $"repo:{_owner}/{_repo} is:issue -reason:\"not planned\" -reason:\"duplicate\"";
            if (since.HasValue)
            {
                searchString += $" updated:>={since.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}";
            }

            var issueRecords = new List<IssueRecord>();
            string? cursor = null;
            bool hasNextPage = true;

            while (hasNextPage)
            {
                var requestPayload = JsonSerializer.Serialize(new
                {
                    query = rawGraphQl,
                    variables = new Dictionary<string, object?>
                    {
                        ["owner"] = _owner,
                        ["repoName"] = _repo,
                        ["searchQuery"] = searchString,
                        ["after"] = cursor
                    }
                });

                var rawResponse = await _graphQLConnection.Run(requestPayload);
                using var document = JsonDocument.Parse(rawResponse);

                // errors 필드가 있으면 저장소 없음 등의 오류 → 예외로 전달
                if (document.RootElement.TryGetProperty("errors", out var errorsElement))
                {
                    var firstError = errorsElement.EnumerateArray().FirstOrDefault();
                    var message = firstError.TryGetProperty("message", out var msgEl)
                        ? msgEl.GetString() ?? "알 수 없는 오류"
                        : "알 수 없는 오류";
                    throw new InvalidOperationException(message);
                }

                if (!document.RootElement.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("search", out var searchElement))
                {
                    break;
                }

                var pageInfo = searchElement.GetProperty("pageInfo");
                hasNextPage = pageInfo.GetProperty("hasNextPage").GetBoolean();
                cursor = pageInfo.GetProperty("endCursor").GetString();

                if (searchElement.TryGetProperty("nodes", out var nodesElement) && nodesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var node in nodesElement.EnumerateArray())
                    {
                        if (node.ValueKind != JsonValueKind.Object) continue;

                        var labelNames = new List<string>();
                        if (node.TryGetProperty("labels", out var labelsElement) &&
                            labelsElement.TryGetProperty("nodes", out var labelNodesElement))
                        {
                            foreach (var labelNode in labelNodesElement.EnumerateArray())
                            {
                                if (labelNode.TryGetProperty("name", out var labelNameElement))
                                    labelNames.Add(labelNameElement.GetString() ?? "");
                            }
                        }

                        var updatedAt = node.TryGetProperty("updatedAt", out var updatedElement)
                            ? DateTimeOffset.Parse(updatedElement.GetString()!) : DateTimeOffset.MinValue;

                        string authorLogin = "";
                        if (node.TryGetProperty("author", out var authorElement) && authorElement.ValueKind == JsonValueKind.Object)
                        {
                            if (authorElement.TryGetProperty("login", out var loginElement))
                            {
                                authorLogin = loginElement.GetString() ?? "";
                            }
                        }

                        issueRecords.Add(new IssueRecord
                        {
                            Number = node.TryGetProperty("number", out var numEl) ? numEl.GetInt32() : 0,
                            Title = node.TryGetProperty("title", out var titEl) ? titEl.GetString() ?? "" : "",
                            Url = node.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? "" : "",
                            AuthorLogin = authorLogin,
                            ClosedReason = ParseIssueClosedStateReason(node),
                            Labels = labelNames.Select(ParseGitHubLabel).Where(l => l != GitHubIssuePrLabel.None).ToList(),
                            UpdatedAt = updatedAt
                        });
                    }
                }
            }

            return issueRecords;
        }

        // 저장소의 열린 이슈를 대상으로 최근 48시간 내 선점 현황을 비동기 조회.
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

            var (freshIssues, closedIssueNumbers) = await FetchOpenIssuesWithClaimCommentsAsync(
                isFullRefresh ? null : since);

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

        // 열린 이슈와 선점 댓글을 함께 비동기 조회.
        private async System.Threading.Tasks.Task<(List<IssueRecord> openIssues, HashSet<int> closedIssueNumbers)>
            FetchOpenIssuesWithClaimCommentsAsync(DateTimeOffset? since = null)
        {
            var openIssues = new List<IssueRecord>();
            var closedIssueNumbers = new HashSet<int>();
            string? cursor = null;
            bool hasNextPage = true;
            var now = DateTimeOffset.UtcNow;

            var updatedIssueNumbers = new HashSet<int>();
            if (since.HasValue)
            {
                const string allIssuesQuery = @"
                query($searchQuery: String!, $after: String) {
                    search(query: $searchQuery, type: ISSUE, first: 100, after: $after) {
                        pageInfo { hasNextPage endCursor }
                        nodes {
                            ... on Issue { number }
                        }
                    }
                }";

                string searchString = $"repo:{_owner}/{_repo} is:issue updated:>={since.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}";
                string? searchCursor = null;
                bool searchHasNextPage = true;

                while (searchHasNextPage)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        query = allIssuesQuery,
                        variables = new Dictionary<string, object>
                        {
                            ["searchQuery"] = searchString,
                            ["after"] = searchCursor!
                        }
                    });

                    var rawResponse = await _graphQLConnection.Run(payload);
                    using var doc = JsonDocument.Parse(rawResponse);

                    if (!doc.RootElement.TryGetProperty("data", out var dataEl) ||
                        !dataEl.TryGetProperty("search", out var searchEl))
                        break;

                    var pageInfo = searchEl.GetProperty("pageInfo");
                    searchHasNextPage = pageInfo.GetProperty("hasNextPage").GetBoolean();
                    searchCursor = pageInfo.GetProperty("endCursor").GetString();

                    if (searchEl.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var node in nodes.EnumerateArray())
                        {
                            if (node.TryGetProperty("number", out var numEl))
                                updatedIssueNumbers.Add(numEl.GetInt32());
                        }
                    }
                }
            }

            while (hasNextPage)
            {
                var query = new Octokit.GraphQL.Query()
                    .Repository(_repo, _owner)
                    .Issues(
                        first: 100,
                        after: cursor,
                        states: new[] { IssueState.Open },
                        orderBy: new IssueOrder { Field = IssueOrderField.UpdatedAt, Direction = OrderDirection.Desc })
                    .Select(s => new
                    {
                        s.PageInfo.HasNextPage,
                        s.PageInfo.EndCursor,
                        Items = s.Nodes.Select(issue => new
                        {
                            issue.Number,
                            issue.Url,
                            issue.UpdatedAt,
                            Labels = issue.Labels(10, null, null, null, null).Nodes.Select(l => l.Name).ToList(),
                            Comments = issue.Comments(10, null, null, null, null).Nodes.Select(c => new
                            {
                                c.Body,
                                c.CreatedAt,
                                AuthorLogin = c.Author.Login
                            }).ToList()
                        }).ToList()
                    });

                var result = await _graphQLConnection.Run(query);

                foreach (var issue in result.Items)
                {
                    if (since.HasValue && issue.UpdatedAt < since.Value)
                    {
                        hasNextPage = false;
                        break;
                    }

                    var issueLabels = issue.Labels
                        .Select(ParseGitHubLabel)
                        .Where(l => l != GitHubIssuePrLabel.None)
                        .ToList();

                    var claimComments = issue.Comments
                        .Where(c => (now - c.CreatedAt).TotalHours <= 48
                            && _claimKeywords.Any(k => c.Body.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        .Select(c => new ClaimComment
                        {
                            AuthorLogin = c.AuthorLogin ?? "unknown",
                            CreatedAt = c.CreatedAt
                        })
                        .ToList();

                    openIssues.Add(new IssueRecord
                    {
                        Number = issue.Number,
                        Url = issue.Url,
                        Labels = issueLabels,
                        UpdatedAt = issue.UpdatedAt,
                        CachedClaimComments = claimComments
                    });
                }

                if (!hasNextPage) break;
                hasNextPage = result.HasNextPage;
                cursor = result.EndCursor;
            }

            if (since.HasValue)
            {
                var openNumbers = openIssues.Select(i => i.Number).ToHashSet();
                foreach (var num in updatedIssueNumbers)
                {
                    if (!openNumbers.Contains(num))
                        closedIssueNumbers.Add(num);
                }
            }

            return (openIssues, closedIssueNumbers);
        }

        // since 이후 업데이트된 열린 PR과 본문에서 파싱한 연결 이슈 번호 목록을 비동기 반환.
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
