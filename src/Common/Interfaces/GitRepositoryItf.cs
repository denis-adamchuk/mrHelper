using mrHelper.Common.Tools;
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

      public override string ToString()
      {
         return "diff -U" + context.ToString() + " " + sha1 + " " + sha2 + " -- " +
            StringUtils.EscapeSpaces(filename1) + " " + StringUtils.EscapeSpaces(filename2);
      }
   }

   public struct GitRevisionArguments
   {
      public string sha;
      public string filename;

      public override string ToString()
      {
         return "show " + sha + ":" + StringUtils.EscapeSpaces(filename);
      }
   }

   public struct GitNumStatArguments
   {
      public string sha1;
      public string sha2;
      public string filter;

      public override string ToString()
      {
         return String.Format("diff {0} {1} --numstat --diff-filter={2}", sha1, sha2, filter);
      }
   }

   public interface IGitRepositoryData
   {
      IEnumerable<string> Get(GitDiffArguments argument);
      IEnumerable<string> Get(GitRevisionArguments arguments);
      IEnumerable<string> Get(GitNumStatArguments arguments);
   }

   public interface IGitRepository
   {
      IGitRepositoryData Data { get; }

      ProjectKey ProjectKey { get; }
   }
}

