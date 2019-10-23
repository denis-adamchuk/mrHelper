using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   /// <summary>
   /// Set of operations on git repository available across the application
   /// </summary>
   public interface IGitRepository
   {
      List<string> Diff(string leftcommit, string rightcommit, string filename1, string filename2, int context);
      Task<List<string>> DiffAsync(string leftcommit, string rightcommit, string filename1, string filename2, int context);

      List<string> GetListOfRenames(string leftcommit, string rightcommit);

      List<string> ShowFileByRevision(string filename, string sha);
      Task<List<string>> ShowFileByRevisionAsync(string filename, string sha);

      string Path { get; }
   }
}

