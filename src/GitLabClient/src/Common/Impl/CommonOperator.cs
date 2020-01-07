using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;

namespace mrHelper.Client.Common
{
   /// <summary>
   /// Implements common interaction with GitLab
   /// </summary>
   internal static class CommonOperator
   {
      async internal static Task<IEnumerable<MergeRequest>> GetMergeRequestsAsync(GitLabClient client, string projectName)
      {
         try
         {
           return (IEnumerable<MergeRequest>)(await client.RunAsync(async (gitlab) =>
              await gitlab.Projects.Get(projectName).MergeRequests.LoadAllTaskAsync(
                 new MergeRequestsFilter { WIP = MergeRequestsFilter.WorkInProgressFilter.All })));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load merge requests from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal static Task<Version> GetLatestVersionAsync(GitLabClient client, string projectName, int iid)
      {
         try
         {
            IEnumerable<Version> versions = (IEnumerable<Version>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(projectName).MergeRequests.Get(iid).
                  Versions.LoadTaskAsync(new PageFilter { PerPage = 1, PageNumber = 1 })));
            return versions.Count() > 0 ? versions.First() : new Version();
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load versions from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal static Task<Note> GetMostRecentUpdatedNoteAsync(GitLabClient client, string projectName, int iid)
      {
         try
         {
            IEnumerable<Note> notes = (IEnumerable<Note>)(await client.RunAsync(async (gitlab) =>
               await gitlab.Projects.Get(projectName).MergeRequests.Get(iid).
                  Notes.LoadTaskAsync(new PageFilter { PerPage = 1, PageNumber = 1 },
                                      new SortFilter { Ascending = false, OrderBy = "updated_at" })));
            return notes.Count() > 0 ? notes.First() : new Note();
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load notes from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }

      async internal static Task<MergeRequest> GetMergeRequestAsync(GitLabClient client, string projectName, int iid)
      {
         try
         {
            return (MergeRequest)(await client.RunAsync(async (gl) =>
               await gl.Projects.Get(projectName).MergeRequests.Get(iid).LoadTaskAsync()));
         }
         catch (Exception ex)
         {
            if (ex is GitLabSharpException || ex is GitLabRequestException || ex is GitLabClientCancelled)
            {
               GitLabExceptionHandlers.Handle(ex, "Cannot load merge request from GitLab");
               throw new OperatorException(ex);
            }
            throw;
         }
      }
   }
}

