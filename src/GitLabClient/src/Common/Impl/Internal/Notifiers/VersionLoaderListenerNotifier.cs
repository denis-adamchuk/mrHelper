using mrHelper.Client.Types;
using System.Collections.Generic;

namespace mrHelper.Client.Common
{
   internal class VersionLoaderNotifier : BaseNotifier<IVersionLoaderListener>, IVersionLoaderListener
   {
      public void OnPreLoadComparableEntities(MergeRequestKey mrk) =>
         notifyAll(x => x.OnPreLoadComparableEntities(mrk));

      public void OnPostLoadComparableEntities(MergeRequestKey mrk, System.Collections.IEnumerable commits) =>
         notifyAll(x => x.OnPostLoadComparableEntities(mrk, commits));

      public void OnFailedLoadComparableEntities(MergeRequestKey mrk) =>
         notifyAll(x => x.OnFailedLoadComparableEntities(mrk));

      public void OnPreLoadVersions(MergeRequestKey mrk) =>
         notifyAll(x => x.OnPreLoadVersions(mrk));

      public void OnPostLoadVersions(MergeRequestKey mrk, IEnumerable<GitLabSharp.Entities.Version> versions) =>
         notifyAll(x => x.OnPostLoadVersions(mrk, versions));

      public void OnFailedLoadVersions(MergeRequestKey mrk) =>
         notifyAll(x => x.OnFailedLoadVersions(mrk));
   }
}

