using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;

namespace mrHelper.StorageSupport.src.Impl.Internal.Git
{
   internal class GitRepositoryFullContextDiffProvider : IFullContextDiffProvider
   {
      public GitRepositoryFullContextDiffProvider(IGitCommandService git)
      {
         _git = git;
      }

      /// <summary>
      /// Throws ContextMakingException.
      /// </summary>
      public FullContextDiff GetFullContextDiff(string leftSHA, string rightSHA,
         string leftFileName, string rightFileName)
      {
         FullContextDiff fullContextDiff = new FullContextDiff(new SparsedList<string>(), new SparsedList<string>());

         GitDiffArguments arguments = new GitDiffArguments(
            GitDiffArguments.DiffMode.Context,
            new GitDiffArguments.CommonArguments(leftSHA, rightSHA, leftFileName, rightFileName, null),
            new GitDiffArguments.DiffContextArguments(Constants.FullContextSize));

         IEnumerable<string> fullDiff;
         try
         {
            fullDiff = _git?.ShowDiff(arguments);
         }
         catch (GitNotAvailableDataException ex)
         {
            throw new FullContextDiffProviderException("Cannot obtain git diff", ex);
         }

         if (fullDiff == null)
         {
            throw new FullContextDiffProviderException("Cannot obtain git diff", null);
         }

         fullDiff = fullDiff.Where(x => !String.IsNullOrEmpty(x));

         if (fullDiff.Count() == 0)
         {
            Trace.TraceWarning(String.Format(
               "[FullContextDiffProvider] Context size is zero. LeftSHA: {0}, Right SHA: {1}," +
               " Left file: {2}, Right file: {3}",
               leftSHA, rightSHA, leftFileName, rightFileName));
         }

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
            string lineOrig = line.Substring(1, line.Length - 1);
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

      private readonly IGitCommandService _git;
   }
}

