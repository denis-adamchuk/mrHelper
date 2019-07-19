namespace mrCustomActions
{
   public interface ICommandCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestId();
   }
}
