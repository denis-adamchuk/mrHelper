namespace mrCore
{
   public class GitUtils
   {
      static public int SetGlobalDiffTool(string name, string command)
      {
         // No need to change current directory because we're changing a global setting (not a repo one)
         var process = Process.Start("git", "config --global difftool." + name + "" + ".cmd " + command);
         process.WaitForExit();
         return process.ExitCode;
      }
   }
}

