using System;
using System.Collections.Generic;

namespace mrHelper.Common.Interfaces
{
   /// <summary>
   /// Set of operations on git repository available across the application
   /// </summary>
   public interface IGitRepository
   {
      List<string> Diff(string leftcommit, string rightcommit, string filename1, string filename2, int context);

      List<string> GetListOfRenames(string leftcommit, string rightcommit);

      List<string> ShowFileByRevision(string filename, string sha);

      string Path { get; }
   }
}

