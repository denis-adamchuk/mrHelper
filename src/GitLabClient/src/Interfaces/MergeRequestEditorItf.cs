using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;

namespace mrHelper.GitLabClient
{
   public interface IMergeRequestEditor
   {
      Task ModifyMergeRequest(UpdateMergeRequestParameters parameters);

      Task AddTrackedTime(TimeSpan span, bool add);
   }
}

