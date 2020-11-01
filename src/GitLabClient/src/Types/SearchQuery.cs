using System;
using System.Collections.Generic;

namespace mrHelper.GitLabClient
{
   public class SearchQuery
   {
      public int?     IId;
      public string   ProjectName;
      public string   TargetBranchName;
      public string   Text;
      public string   AuthorUserName;
      public string[] Labels;
      public int?     MaxResults;
      public string   State;

      public new string ToString()
      {
         return String.Format(
            "IId: {0}, Project: {1}, TargetBranch: {2}, Text: {3}, Author: {4}, " +
            "Label: {5}, MaxResults: {6}, OnlyOpen: {7}",
            IId.HasValue ? IId.Value.ToString() : "N/A",
            ProjectName != null ? ProjectName : "N/A",
            TargetBranchName != null ? TargetBranchName : "N/A",
            Text != null ? Text : "N/A",
            AuthorUserName != null ? AuthorUserName : "N/A",
            Labels != null ? String.Join(",", Labels) : "N/A",
            MaxResults.HasValue ? MaxResults.Value.ToString() : "N/A",
            State != null ? State : "N/A");
      }
   }

   public class SearchQueryCollection
   {
      public SearchQueryCollection(IEnumerable<SearchQuery> queries)
      {
         Queries = queries;
      }

      public SearchQueryCollection(SearchQuery query)
      {
         Queries = new SearchQuery[] { query };
      }

      public void Assign(IEnumerable<SearchQuery> queries)
      {
         Queries = queries;
      }

      public IEnumerable<SearchQuery> Queries { get; private set; }
   }
}

