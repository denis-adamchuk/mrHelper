namespace mrHelper.CustomActions
{
   public interface ICommandCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestIId();
   }
}
