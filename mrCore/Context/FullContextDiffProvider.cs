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
      public List<string> Left;
      public List<string> Right;
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

      public FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA, string filename)
      {
         FullContextDiff fullContextDiff = new FullContextDiff();
         fullContextDiff.Left = new List<string>();
         fullContextDiff.Right = new List<string>();
         List<string> fullDiff = _gitRepository.Diff(leftSHA, rightSHA, filename, maxDiffContext);
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
            var lineOrig = line.Substring(1, line.Length - 1);
            switch (sign)
            {
               case '-':
                  fullContextDiff.Left.Add(lineOrig);
                  fullContextDiff.Right.Add(null);
                  break;
               case '+':
                  fullContextDiff.Left.Add(null);
                  fullContextDiff.Right.Add(lineOrig);
                  break;
               case ' ':
                  fullContextDiff.Left.Add(lineOrig);
                  fullContextDiff.Right.Add(lineOrig);
                  break;
            }
         }
         return fullContextDiff;
      }

      GitRepository _gitRepository;
   }
}

