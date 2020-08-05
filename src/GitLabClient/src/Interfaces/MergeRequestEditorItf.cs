using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestEditor
   {
      Task<MergeRequest> ModifyMergeRequest(UpdateMergeRequestParameters parameters);

      Task AddTrackedTime(TimeSpan span, bool add);
   }
}

