using System.Collections.Generic;
using System.Linq;

namespace RepoScore.Data
{
    public enum SortBy { Score, Id }

    public enum SortOrder { Asc, Desc }

    public static class ReportSorter
    {
        public static List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)>
        SortReportData(
            List<(string Id, int docIssues, int featBugIssues, int typoPrs, int docPrs, int featBugPrs, int Score)> data,
            SortBy sortBy,
            SortOrder sortOrder)
        {
            return sortBy switch
            {
                SortBy.Id => sortOrder == SortOrder.Asc
                    ? data.OrderBy(x => x.Id).ToList()
                    : data.OrderByDescending(x => x.Id).ToList(),
                _ => sortOrder == SortOrder.Asc
                    ? data.OrderBy(x => x.Score).ToList()
                    : data.OrderByDescending(x => x.Score).ToList()
            };
        }
    }
}
