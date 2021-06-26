using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IComparisonCache
   {
      Comparison LoadComparison(string baseSha, string headSha);

      void SaveComparison(string baseSha, string headSha, Comparison comparison);
   }
}

