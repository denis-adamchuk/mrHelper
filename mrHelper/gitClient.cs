using System.Diagnostics;

namespace mrHelper
{
   class gitClient
   {
      static public void CloneRepo(string host, string project, string localDir)
      {
         Process.Start("git", "clone " + "https://" + host + "/" + project + " " + localDir);
      }

      static public void Fetch()
      {
         Process.Start("git", "fetch");
      }

      static public void DiffTool(string leftCommit, string rightCommit)
      {
         Process.Start("git", "difftool --dir-diff --tool=beyondcompare3dd " + leftCommit + " " + rightCommit);
      }
   }
}
