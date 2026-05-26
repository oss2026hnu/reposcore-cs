using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using RepoScore.Services;

namespace RepoScore.Data
{
    /// <summary>
    /// 저장소별 분석 캐시 데이터를 표현하는 클래스입니다. JSON 파일로 직렬화되어 저장됩니다.
    /// </summary>
    public class RepoCache
    {
        /// <summary>
        /// 분석 대상 저장소 이름 (예: owner/repo)입니다.
        /// </summary>
        public string Repository { get; set; } = string.Empty;

        /// <summary>
        /// 전체 데이터 분석 캐시가 마지막으로 갱신된 시각입니다.
        /// </summary>
        public DateTimeOffset LastAnalyzedAt { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// 분석 시 사용된 이슈 선점 키워드 배열입니다. 캐시 무효화 여부를 판단할 때 사용됩니다.
        /// </summary>
        public string[]? Keywords { get; set; }

        /// <summary>
        /// 유저별로 그룹화된 이슈 목록 기록입니다.
        /// </summary>
        public Dictionary<string, List<IssueRecord>> UserIssues { get; set; } = new();

        /// <summary>
        /// 유저별로 그룹화된 Pull Request 목록 기록입니다.
        /// </summary>
        public Dictionary<string, List<PRRecord>> UserPullRequests { get; set; } = new();

        /// <summary>
        /// Claims 조회용 캐시: 열린 이슈 목록 (CachedClaimComments 포함).
        /// --claims 옵션 실행 시 증분 갱신에 활용됩니다.
        /// </summary>
        public List<IssueRecord> CachedOpenIssues { get; set; } = new();

        /// <summary>
        /// Claims 조회용 캐시: 열린 PR 목록 (LinkedIssueNumbers 포함).
        /// </summary>
        public List<PRRecord> CachedOpenPrs { get; set; } = new();

        /// <summary>
        /// Claims 캐시의 마지막 갱신 시각 (--claims 전용, LastAnalyzedAt과 별도 관리).
        /// </summary>
        public DateTimeOffset LastClaimsAnalyzedAt { get; set; } = DateTimeOffset.MinValue;
    }

    /// <summary>
    /// 캐시 파일의 로드 및 저장을 담당하는 클래스입니다.
    /// 이전 분석 결과를 JSON 파일로 유지하여 불필요한 API 요청을 줄입니다.
    /// </summary>
    public static class CacheManager
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// 기존 캐시 파일을 읽어 RepoCache 객체를 반환합니다.
        /// 파일이 없거나 손상된 경우, 또는 noCache 플래그가 true이면 빈 캐시를 반환합니다.
        /// </summary>
        /// <param name="cacheFilePath">읽어올 캐시 파일의 경로</param>
        /// <param name="repoName">조회할 대상 저장소 이름</param>
        /// <param name="noCache">기존 캐시 무시 여부</param>
        /// <returns>로드된 RepoCache 객체 또는 새 RepoCache 인스턴스</returns>
        public static RepoCache LoadCache(string cacheFilePath, string repoName, bool noCache = false)
        {
            if (noCache)
            {
                Console.Error.WriteLine("ℹ️  캐시를 무시하고 전체 데이터를 다시 수집합니다.");
                return new RepoCache { Repository = repoName };
            }

            if (!File.Exists(cacheFilePath))
            {
                return new RepoCache { Repository = repoName };
            }

            try
            {
                string json = File.ReadAllText(cacheFilePath);
                var cache = JsonSerializer.Deserialize<RepoCache>(json, s_jsonOptions);

                if (cache == null || cache.Repository != repoName)
                {
                    return new RepoCache { Repository = repoName };
                }
                return cache;
            }
            catch
            {
                Console.Error.WriteLine("⚠️ 기존 캐시 파일이 손상되어 새로 수집을 시작합니다.");
                return new RepoCache { Repository = repoName };
            }
        }

        /// <summary>
        /// 분석 결과 캐시를 JSON 파일로 저장합니다. 저장 시각과 키워드를 함께 기록합니다.
        /// </summary>
        /// <param name="cacheFilePath">저장할 캐시 파일의 경로</param>
        /// <param name="cacheData">저장할 대상 캐시 데이터</param>
        /// <param name="keywords">분석 시 사용된 이슈 선점 키워드 배열</param>
        public static void SaveCache(string cacheFilePath, RepoCache cacheData, string[]? keywords)
        {
            var dir = Path.GetDirectoryName(cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            cacheData.LastAnalyzedAt = DateTimeOffset.UtcNow;
            cacheData.Keywords = keywords;

            string json = JsonSerializer.Serialize(cacheData, s_jsonOptions);
            File.WriteAllText(cacheFilePath, json);
        }

        /// <summary>
        /// Claims 캐시(열린 이슈/PR)를 캐시 파일에 저장합니다.
        /// LastAnalyzedAt은 건드리지 않고 LastClaimsAnalyzedAt만 갱신합니다.
        /// </summary>
        /// <param name="cacheFilePath">저장할 캐시 파일의 경로</param>
        /// <param name="cacheData">갱신 대상인 기존 캐시 데이터</param>
        /// <param name="openIssues">저장할 열린 이슈 목록</param>
        /// <param name="openPrs">저장할 열린 PR 목록</param>
        public static void SaveClaimsCache(
            string cacheFilePath,
            RepoCache cacheData,
            List<IssueRecord> openIssues,
            List<PRRecord> openPrs)
        {
            var dir = Path.GetDirectoryName(cacheFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            cacheData.CachedOpenIssues = openIssues;
            cacheData.CachedOpenPrs = openPrs;
            cacheData.LastClaimsAnalyzedAt = DateTimeOffset.UtcNow;

            string json = JsonSerializer.Serialize(cacheData, s_jsonOptions);
            File.WriteAllText(cacheFilePath, json);
        }

        /// <summary>
        /// 캐시에 저장된 키워드와 현재 실행 키워드가 동일한지 비교합니다.
        /// 키워드가 달라진 경우 캐시 무효화 여부 판단에 사용됩니다.
        /// </summary>
        /// <param name="cacheData">기존에 저장된 캐시 데이터</param>
        /// <param name="currentKeywords">현재 실행 시 전달된 키워드 배열</param>
        /// <returns>키워드가 동일하면 true, 다르면 false를 반환합니다.</returns>
        public static bool HasSameKeywords(RepoCache cacheData, string[]? currentKeywords)
        {
            var cachedKeywords = cacheData.Keywords;

            if (cachedKeywords == null && currentKeywords == null)
                return true;

            if (cachedKeywords == null || currentKeywords == null)
                return false;

            return cachedKeywords
                .OrderBy(x => x)
                .SequenceEqual(currentKeywords.OrderBy(x => x));
        }
    }
}
