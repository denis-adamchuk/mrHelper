using System.Diagnostics;

namespace mrHelper.GitLabClient.Operators.Search
{
   internal static class MergeRequestSearchProcessorFactory
   {
      internal static MergeRequestSearchProcessor Create(object search, bool onlyOpen)
      {
         if (search is SearchByIId sid)
         {
            return new MergeRequestSearchByIIdProcessor(sid.IId, sid.ProjectName, onlyOpen);
         }
         else if (search is SearchByProject sbp)
         {
            return new MergeRequestSearchByProjectProcessor(sbp.ProjectKey.ProjectName, onlyOpen);
         }
         else if (search is SearchByTargetBranch sbtb)
         {
            return new MergeRequestSearchByTargetBranchProcessor(sbtb.TargetBranchName, onlyOpen);
         }
         else if (search is SearchByText sbt)
         {
            return new MergeRequestSearchByTextProcessor(sbt.Text, onlyOpen);
         }
         else if (search is SearchByUsername sbu)
         {
            return new MergeRequestSearchByUsernameProcessor(sbu.Username, onlyOpen);
         }

         Debug.Assert(false);
         return null;
      }
   }
}

