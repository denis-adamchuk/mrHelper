using System;
using mrHelper.Client.Types;

namespace mrHelper.App.Helpers
{
   internal struct DiffStatistic
   {
      internal DiffStatistic(int files, int insertions, int deletions)
      {
         _filesChanged = files;
         _insertions = insertions;
         _deletions = deletions;
      }

      public override string ToString()
      {
         string fileNumber = String.Format("{0} {1}", _filesChanged, _filesChanged > 1 ? "files" : "file");
         return String.Format("+ {1} / - {2}\n{0}", fileNumber, _insertions, _deletions);
      }

      private readonly int _filesChanged;
      private readonly int _insertions;
      private readonly int _deletions;
   }

   internal interface IDiffStatisticProvider : IDisposable
   {
      event Action Update;

      DiffStatistic? GetStatistic(MergeRequestKey mrk, out string statusMessage);
   }
}
