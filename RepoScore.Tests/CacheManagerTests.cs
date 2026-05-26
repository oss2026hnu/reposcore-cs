using RepoScore.Data;
using RepoScore.Services;
using Xunit;

namespace RepoScore.Tests;

public class CacheManagerTests
{
    [Fact]
    public void LoadCache_WhenFileDoesNotExist_ReturnsNewCache()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string cacheFilePath = Path.Combine(directoryPath, "cache.json");

        RepoCache cache = CacheManager.LoadCache(cacheFilePath, "owner/repo");

        Assert.Equal("owner/repo", cache.Repository);
        Assert.Equal(DateTimeOffset.MinValue, cache.LastAnalyzedAt);
        Assert.Empty(cache.UserIssues);
        Assert.Empty(cache.UserPullRequests);
    }

    [Fact]
    public void SaveCache_ThenLoadCache_RestoresSavedCacheData()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string cacheFilePath = Path.Combine(directoryPath, "cache.json");

        var cache = new RepoCache
        {
            Repository = "owner/repo",
            UserIssues =
            {
                ["user1"] = new List<IssueRecord>
                {
                    new IssueRecord
                    {
                        Number = 333,
                        Url = "https://github.com/oss2026hnu/reposcore-cs/issues/333",
                        Title = "단위 테스트 및 테스트 자동화 환경 도입",
                        Labels = new List<GitHubIssuePrLabel>
                        {
                            GitHubIssuePrLabel.Enhancement
                        }
                    }
                }
            },
            UserPullRequests =
            {
                ["user1"] = new List<PRRecord>
                {
                    new PRRecord
                    {
                        Number = 400,
                        Url = "https://github.com/oss2026hnu/reposcore-cs/pull/400",
                        Title = "Add unit tests",
                        IsMerged = true,
                        Labels = new List<GitHubIssuePrLabel>
                        {
                            GitHubIssuePrLabel.Enhancement
                        }
                    }
                }
            }
        };

        CacheManager.SaveCache(cacheFilePath, cache, new[] { "제가 하겠습니다", "할게요" });

        RepoCache loadedCache = CacheManager.LoadCache(cacheFilePath, "owner/repo");

        Assert.Equal("owner/repo", loadedCache.Repository);
        Assert.True(loadedCache.LastAnalyzedAt > DateTimeOffset.MinValue);
        Assert.True(CacheManager.HasSameKeywords(loadedCache, new[] { "할게요", "제가 하겠습니다" }));

        Assert.Single(loadedCache.UserIssues["user1"]);
        Assert.Equal(333, loadedCache.UserIssues["user1"][0].Number);
        Assert.Contains(GitHubIssuePrLabel.Enhancement, loadedCache.UserIssues["user1"][0].Labels);

        Assert.Single(loadedCache.UserPullRequests["user1"]);
        Assert.Equal(400, loadedCache.UserPullRequests["user1"][0].Number);
        Assert.True(loadedCache.UserPullRequests["user1"][0].IsMerged);
    }

    [Fact]
    public void LoadCache_WhenRepositoryDoesNotMatch_ReturnsNewCache()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string cacheFilePath = Path.Combine(directoryPath, "cache.json");

        CacheManager.SaveCache(
            cacheFilePath,
            new RepoCache
            {
                Repository = "owner/old-repo"
            },
            keywords: null);

        RepoCache loadedCache = CacheManager.LoadCache(cacheFilePath, "owner/new-repo");

        Assert.Equal("owner/new-repo", loadedCache.Repository);
        Assert.Equal(DateTimeOffset.MinValue, loadedCache.LastAnalyzedAt);
        Assert.Empty(loadedCache.UserIssues);
        Assert.Empty(loadedCache.UserPullRequests);
    }

    [Fact]
    public void LoadCache_WhenNoCacheIsTrue_ReturnsNewCache()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        string cacheFilePath = Path.Combine(directoryPath, "cache.json");

        CacheManager.SaveCache(
            cacheFilePath,
            new RepoCache
            {
                Repository = "owner/repo"
            },
            keywords: null);

        RepoCache loadedCache = CacheManager.LoadCache(
            cacheFilePath,
            "owner/repo",
            noCache: true);

        Assert.Equal("owner/repo", loadedCache.Repository);
        Assert.Equal(DateTimeOffset.MinValue, loadedCache.LastAnalyzedAt);
    }

    [Fact]
    public void HasSameKeywords_WhenBothKeywordListsAreNull_ReturnsTrue()
    {
        var cache = new RepoCache
        {
            Keywords = null
        };

        bool result = CacheManager.HasSameKeywords(cache, null);

        Assert.True(result);
    }

    [Fact]
    public void HasSameKeywords_WhenKeywordOrderIsDifferent_ReturnsTrue()
    {
        var cache = new RepoCache
        {
            Keywords = new[] { "할게요", "제가 하겠습니다" }
        };

        bool result = CacheManager.HasSameKeywords(
            cache,
            new[] { "제가 하겠습니다", "할게요" });

        Assert.True(result);
    }

    [Fact]
    public void HasSameKeywords_WhenKeywordsAreDifferent_ReturnsFalse()
    {
        var cache = new RepoCache
        {
            Keywords = new[] { "제가 하겠습니다" }
        };

        bool result = CacheManager.HasSameKeywords(
            cache,
            new[] { "진행하겠습니다" });

        Assert.False(result);
    }
}
