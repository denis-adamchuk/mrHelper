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
      IEnumerable<string> Diff(GitDiffArguments argument);
      Task<IEnumerable<string>> DiffAsync(GitDiffArguments argument);

      IEnumerable<string> ShowFileByRevision(GitRevisionArguments arguments);
      Task<IEnumerable<string>> ShowFileByRevisionAsync(GitRevisionArguments arguments);

      IEnumerable<string> GetListOfRenames(GitListOfRenamesArguments arguments);
      Task<IEnumerable<string>> GetListOfRenamesAsync(GitListOfRenamesArguments arguments);

      event Action<IGitRepository, DateTime> Updated;
      event Action<IGitRepository> Disposed;

      string HostName { get; }
      string ProjectName { get; }
   }
}

