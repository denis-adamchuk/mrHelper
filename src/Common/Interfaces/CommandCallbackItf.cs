namespace mrHelper.Common.Interfaces
{
   public interface ICommandCallback
   {
      string GetCurrentHostName();

      string GetCurrentAccessToken();

      string GetCurrentProjectName();

      int GetCurrentMergeRequestIId();
   }
}
