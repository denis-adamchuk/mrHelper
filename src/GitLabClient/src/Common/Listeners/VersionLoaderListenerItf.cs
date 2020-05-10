using System.Collections.Generic;
using mrHelper.Client.Types;

namespace mrHelper.Client.Common
{
   public interface IVersionLoaderListener
   {
      void OnPreLoadComparableEntities(MergeRequestKey mrk);
      void OnPostLoadComparableEntities(MergeRequestKey mrk, System.Collections.IEnumerable commits);
      void OnFailedLoadComparableEntities(MergeRequestKey mrk);

      void OnPreLoadVersions(MergeRequestKey mrk);
      void OnPostLoadVersions(MergeRequestKey mrk, IEnumerable<GitLabSharp.Entities.Version> versions);
      void OnFailedLoadVersions(MergeRequestKey mrk);
   }
}

