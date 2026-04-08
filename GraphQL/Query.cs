using RepoScore.Services;

namespace RepoScore.GraphQL
{
    public class Query
    {
        public string Hello() => "Hello GraphQL";

        public async Task<int> GetIssueCount(GitHubService service)
        {
            var issues = await service.GetIssuesAsync();
            return issues.Count;
        }
    }
}
