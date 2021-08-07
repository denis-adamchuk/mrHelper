using System.Linq;
using GitLabSharp.Entities;

namespace mrHelper.StorageSupport
{
   internal static class DiffStructCounter
   {
      private const string plus = "+";
      private const string minus = "-";

      internal static void Count(DiffStruct diff, out DiffSize? diffSize)
      {
         if (string.IsNullOrEmpty(diff.Diff))
         {
            diffSize = null;
            return;
         }

         // cannot use out parameters inside anonymous lambdas
         int localAdded = 0;
         int localDeleted = 0;
         diff.Diff
            .Split('\n')
            .ToList()
            .ForEach(s =>
            {
               localAdded += s.StartsWith(plus) ? 1 : 0;
               localDeleted += s.StartsWith(minus) ? 1 : 0;
            });
         diffSize = new DiffSize(localAdded, localDeleted);
      }
   }
}

