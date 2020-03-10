using System;
using mrHelper.Common.Interfaces;
using mrHelper.GitClient;

namespace mrHelper.App.Helpers
{
   public static class GitTools
   {
      public static string AdjustSHA(string sha, IGitRepository repo)
      {
         if (repo == null || repo.ContainsSHA(sha))
         {
            return sha;
         }

         string fakeSha = FakeSHA(sha);
         if (repo.ContainsBranch(fakeSha))
         {
            return fakeSha;
         }

         return sha;
      }

      public static string FakeSHA(string sha)
      {
         return "fake_" + sha;
      }
   }
}

