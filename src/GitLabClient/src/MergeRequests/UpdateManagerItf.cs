using mrHelper.Client.Types;

namespace mrHelper.Client.MergeRequests
{
   public interface IUpdateManager
   {
      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk);

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of any merge request
      /// </summary>
      IInstantProjectChecker GetLocalProjectChecker(ProjectKey pk);

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk);
   }
}
