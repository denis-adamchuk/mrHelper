using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace mrHelper.StorageSupport
{
   internal struct GitFilePath
   {
      internal GitFilePath(string path)
      {
         Value = path;
      }

      internal string ToDiskPath(string prefix)
      {
         List<string> splitted = Value
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar)
            .ToList();
         splitted.Insert(0, prefix);
         return System.IO.Path.Combine(splitted.ToArray());
      }

      internal readonly string Value;
   }

   internal struct FileRevision
   {
      // public constructor allows to use Activator.CreateInstance()
      public FileRevision(string gitFilepath, string sha)
      {
         GitFilePath = new GitFilePath(gitFilepath);
         SHA = sha;
      }

      internal readonly GitFilePath GitFilePath;
      internal readonly string SHA;
   }
}

