
namespace RepoScore.Data
{
    /// <summary>
    /// 오픈소스 기여 점수를 계산하는 클래스입니다.
    /// PR 및 이슈 개수 비율에 따른 유효 개수 제한(Validity Limits)을 적용하여 최종 점수를 산출합니다.
    /// </summary>
    public static class ScoreCalculator
    {
        private const int ScorePrFeatureBug = 3;
        private const int ScorePrDoc = 2;
        private const int ScorePrTypo = 1;

        private const int ScoreIssueFeatureBug = 2;
        private const int ScoreIssueDoc = 1;

        /// <summary>
        /// 학생의 기여 내역을 바탕으로 최종 점수를 계산합니다.
        /// </summary>
        /// <param name="featureBugPrCount">기능/버그 PR 개수 (Merged)</param>
        /// <param name="docPrCount">문서 PR 개수 (Merged)</param>
        /// <param name="typoPrCount">오타 PR 개수 (Merged)</param>
        /// <param name="featureBugIssueCount">기능/버그 이슈 개수 (Open/Resolved)</param>
        /// <param name="docIssueCount">문서 이슈 개수 (Open/Resolved)</param>
        /// <returns>최종 산출된 점수</returns>
        public static int CalculateFinalScore(
            int featureBugPrCount,
            int docPrCount,
            int typoPrCount,
            int featureBugIssueCount,
            int docIssueCount)
        {
            // 1단계: 유효 PR 개수 제한 산정 (P_valid)
            int maxAdditionalPrCount = 3 * Math.Max(featureBugPrCount, 1);
            int validPrCount = featureBugPrCount + Math.Min(docPrCount + typoPrCount, maxAdditionalPrCount);

            // 2단계: 유효 이슈 개수 제한 산정 (I_valid)
            int validIssueCount = Math.Min(featureBugIssueCount + docIssueCount, 4 * validPrCount);

            // 3단계: PR 최적화 계산 (배점이 높은 기능/버그 -> 문서 -> 오타 순으로 채움)
            int optimizedFeatureBugPrCount = Math.Min(featureBugPrCount, validPrCount);

            int remainingPrSlots = validPrCount - optimizedFeatureBugPrCount;
            int optimizedDocPrCount = Math.Min(docPrCount, remainingPrSlots);

            int optimizedTypoPrCount = validPrCount - optimizedFeatureBugPrCount - optimizedDocPrCount;

            // 4단계: 이슈 최적화 계산 (배점이 높은 기능/버그 -> 문서 순으로 채움)
            int optimizedFeatureBugIssueCount = Math.Min(featureBugIssueCount, validIssueCount);
            int optimizedDocIssueCount = validIssueCount - optimizedFeatureBugIssueCount;

            // 5단계: 최종 점수 합산
            int finalScore = (optimizedFeatureBugPrCount * ScorePrFeatureBug)
                           + (optimizedDocPrCount * ScorePrDoc)
                           + (optimizedTypoPrCount * ScorePrTypo)
                           + (optimizedFeatureBugIssueCount * ScoreIssueFeatureBug)
                           + (optimizedDocIssueCount * ScoreIssueDoc);

            return finalScore;
        }
    }
}
