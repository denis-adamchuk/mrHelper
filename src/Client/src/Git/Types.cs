using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Git
{
   internal static class Types
   {
      internal struct DiffCacheKey
      {
         public string sha1;
         public string sha2;
         public string filename1;
         public string filename2;
         public int context;
      }

      internal struct RevisionCacheKey
      {
         public string sha;
         public string filename;
      }

      internal struct ListOfRenamesCacheKey
      {
         public string sha1;
         public string sha2;
      }
   }
}
