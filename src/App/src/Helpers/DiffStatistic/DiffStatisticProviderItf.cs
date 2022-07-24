using mrHelper.GitLabClient;
using System;

namespace mrHelper.App.Helpers
{
   internal struct DiffStatistic
   {
      internal DiffStatistic(int files, int insertions, int deletions)
      {
         FilesChanged = files;
         Insertions = insertions;
         Deletions = deletions;
      }

      internal string Format()
      {
         string fileNumber = String.Format("{0} {1}", FilesChanged, FilesChanged > 1 ? "files" : "file");
         return String.Format("+ {1} / - {2}\n{0}", fileNumber, Insertions, Deletions);
      }

      internal readonly int FilesChanged;
      internal readonly int Insertions;
      internal readonly int Deletions;
   }

   internal interface IDiffStatisticProvider
   {
      event Action Update;

      DiffStatistic? GetStatistic(MergeRequestKey mrk, out string statusMessage);
   }
}
