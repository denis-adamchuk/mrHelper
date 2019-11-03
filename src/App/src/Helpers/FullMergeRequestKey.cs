using GitLabSharp.Entities;
using mrHelper.Client.Tools;

namespace mrHelper.App.Helpers
{
   internal struct FullMergeRequestKey
   {
      public string HostName;
      public Project Project;
      public MergeRequest MergeRequest;

      public FullMergeRequestKey(string hostname, Project project, MergeRequest mergeRequest)
      {
         HostName = hostname;
         Project = project;
         MergeRequest = mergeRequest;
      }

      public static bool SameMergeRequest(FullMergeRequestKey fmk1, FullMergeRequestKey fmk2)
      {
         return fmk1.HostName == fmk2.HostName
             && fmk1.Project.Path_With_Namespace == fmk2.Project.Path_With_Namespace
             && fmk1.MergeRequest.IId == fmk2.MergeRequest.IId;
      }

      public static bool SameMergeRequest(FullMergeRequestKey fmk1, MergeRequestKey mrk2)
      {
         return fmk1.HostName == mrk2.ProjectKey.HostName
             && fmk1.Project.Path_With_Namespace == mrk2.ProjectKey.ProjectName
             && fmk1.MergeRequest.IId == mrk2.IId;
      }
   }
}

