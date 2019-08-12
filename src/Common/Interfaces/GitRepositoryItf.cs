using System;

namespace mrHelper.Common.Interfaces
{
   /// <summary>
   /// Set of operations on git repository available across the application
   /// </summary>
   public interface IGitRepository
   {
      public List<string> Diff(string leftcommit, string rightcommit, string filename1, string filename2, int context);

      public List<string> GetListOfRenames(string leftcommit, string rightcommit);

      public List<string> ShowFileByRevision(string filename, string sha);
   }
}

