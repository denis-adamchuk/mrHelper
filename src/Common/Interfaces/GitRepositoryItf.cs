using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public struct GitDiffArguments
   {
      public string sha1;
      public string sha2;
      public string filename1;
      public string filename2;
      public int context;
   }

   public struct GitRevisionArguments
   {
      public string sha;
      public string filename;
   }

   public struct GitListOfRenamesArguments
   {
      public string sha1;
      public string sha2;
   }

   /// <summary>
   /// Set of operations on git repository available across the application
   /// </summary>
   public interface IGitRepository
   {
      List<string> Diff(GitDiffArguments argument);
      Task<List<string>> DiffAsync(GitDiffArguments argument);

      List<string> ShowFileByRevision(GitRevisionArguments arguments);
      Task<List<string>> ShowFileByRevisionAsync(GitRevisionArguments arguments);

      List<string> GetListOfRenames(GitListOfRenamesArguments arguments);
      Task<List<string>> GetListOfRenamesAsync(GitListOfRenamesArguments arguments);
   }
}

