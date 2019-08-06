namespace Common
{
   public interface IClientCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestId();

      string GetCurrentLocalGitFolder();
   }
}
