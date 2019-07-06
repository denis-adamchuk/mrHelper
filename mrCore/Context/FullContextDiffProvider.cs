using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
{
   // Contains a diff between two revisions with all lines from each of revision including missing lines.
   public struct FullContextDiff
   {
      public List<string> sha1context;
      public List<string> sha2context;
   }

   // Provides two lists of the same size. First list contains lines from sha1 and null for missing lines. 
   // Seconds list contains lines from sha2 and null for missing lines.
   public class FullContextDiffProvider
   {
      static int maxDiffContext = 20000;

      public FullContextDiffProvider(GitRepository gitRepository)
      {
         _gitRepository = gitRepository;
      }

      public FullContextDiff GetFullContextDiff(string sha1, string sha2, string filename)
      {
         FullContextDiff fullContextDiff = new FullContextDiff();
         fullContextDiff.sha1context = new List<string>();
         fullContextDiff.sha2context = new List<string>();
         List<string> fullDiff = _gitRepository.Diff(sha1, sha2, filename, maxDiffContext);
         bool skip = true;
         foreach (string line in fullDiff)
         {
            char sign = line[0];
            if (skip)
            {
               // skip meta information about diff
               if (sign == '@')
               {
                  // next lines should not be skipped because they contain a diff itself
                  skip = false;
               }
               continue;
            }
            switch (sign)
            {
               case '-':
                  fullContextDiff.sha1context.Add(line);
                  fullContextDiff.sha2context.Add(null);
                  break;
               case '+':
                  fullContextDiff.sha1context.Add(null);
                  fullContextDiff.sha2context.Add(line);
                  break;
               case ' ':
                  fullContextDiff.sha1context.Add(line);
                  fullContextDiff.sha2context.Add(line);
                  break;
            }
         }
         return fullContextDiff;
      }

      GitRepository _gitRepository;
   }
}

