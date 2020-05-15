using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using GitLabSharp.Accessors;
using mrHelper.Client.Types;
using mrHelper.Client.Common;
using mrHelper.Common.Exceptions;

namespace mrHelper.Client.Discussions
{
   public class DiscussionCreatorException : ExceptionEx
   {
      public DiscussionCreatorException(bool handled, Exception innerException)
         : base("Discussion creation failed", innerException)
      {
         Handled = handled;
      }

      public bool Handled { get; }
   }

   /// <summary>
   /// Implements logic of new discussion creation
   /// </summary>
   internal class DiscussionCreator : IDiscussionCreator
   {
      internal DiscussionCreator(MergeRequestKey mrk, DiscussionOperator discussionOperator, User currentUser)
      {
         _discussionOperator = discussionOperator;
         _mergeRequestKey = mrk;
         _currentUser = currentUser;
      }

      async public Task CreateNoteAsync(CreateNewNoteParameters parameters)
      {
         try
         {
            await _discussionOperator.CreateNoteAsync(_mergeRequestKey, parameters);
         }
         catch (OperatorException ex)
         {
            throw new DiscussionCreatorException(false, ex);
         }
      }

      async public Task CreateDiscussionAsync(NewDiscussionParameters parameters, bool revertOnError)
      {
         try
         {
            await _discussionOperator.CreateDiscussionAsync(_mergeRequestKey, parameters);
         }
         catch (OperatorException ex)
         {
            bool handled = await handleGitlabError(parameters, ex, revertOnError);
            throw new DiscussionCreatorException(handled, ex);
         }
      }

      async private Task<bool> handleGitlabError(NewDiscussionParameters parameters, OperatorException ex,
         bool revertOnError)
      {
         if (ex == null)
         {
            Trace.TraceWarning("[DiscussionCreator] An exception with null value was caught");
            return false;
         }

         if (!parameters.Position.HasValue)
         {
            Trace.TraceWarning("[DiscussionCreator] Unexpected situation at GitLab");
            return false;
         }

         if (ex.InnerException is GitLabRequestException rx)
         {
            if (rx.InnerException is System.Net.WebException wx)
            {
               if (wx.Response == null)
               {
                  Trace.TraceWarning("[DiscussionCreator] Null Response in WebException");
                  return false;
               }

               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
               {
                  // Something went wrong at the GitLab, let's report a discussion without Position
                  return await createMergeRequestWithoutPosition(parameters);
               }
               else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
               {
                  // Something went wrong at the GitLab
                  if (revertOnError)
                  {
                     await deleteMostRecentNote(parameters);
                     return await createMergeRequestWithoutPosition(parameters);
                  }
                  return true;
               }
            }
         }

         return false;
      }

      async private Task<bool> createMergeRequestWithoutPosition(NewDiscussionParameters parameters)
      {
         Debug.Assert(parameters.Position.HasValue);

         Trace.TraceInformation("[DicsussionCreator] Reporting a discussion without Position (fallback)");

         parameters.Body = getFallbackInfo(parameters.Position.Value) + "<br>" + parameters.Body;
         parameters.Position = null;

         try
         {
            await _discussionOperator.CreateDiscussionAsync(_mergeRequestKey, parameters);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle("Cannot create a discussion (again)", ex);
            return false;
         }
         return true;
      }

      private string getFallbackInfo(PositionParameters position)
      {
         return "<b>" + (position.OldPath?.ToString() ?? "N/A") + "</b>"
            + " (line " + (position.OldLine?.ToString() ?? "N/A") + ") <i>vs</i> "
            + "<b>" + (position.NewPath?.ToString() ?? "N/A") + "</b>"
            + " (line " + (position.NewLine?.ToString() ?? "N/A") + ")";
      }

      async private Task deleteMostRecentNote(NewDiscussionParameters parameters)
      {
         Debug.Assert(parameters.Position.HasValue);

         Trace.TraceInformation("[DicsussionCreator] Looking up for a note with bad position...");

         IEnumerable<Discussion> discussions;
         try
         {
            discussions = await _discussionOperator.GetDiscussionsAsync(_mergeRequestKey);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle("Cannot obtain discussions", ex);
            return;
         }

         int? deletedNoteId = new Nullable<int>();
         foreach (Discussion discussion in discussions.Reverse())
         {
            if (discussion.Notes.Count() == 1)
            {
               DiscussionNote note = discussion.Notes.First();
               if (note.Type == "DiffNote" && note.Author.Equals(_currentUser) && note.Body == parameters.Body)
               {
                  Trace.TraceInformation(
                     "[DicsussionCreator] Deleting discussion note." +
                     " Id: {0}, Author.Username: {1}, Created_At: {2} (LocalTime), Body:\n{3}",
                     note.Id.ToString(), note.Author.Username, note.Created_At.ToLocalTime(), note.Body);

                  try
                  {
                     await _discussionOperator.DeleteNoteAsync(_mergeRequestKey, note.Id);
                     deletedNoteId = note.Id;
                  }
                  catch (OperatorException ex)
                  {
                     ExceptionHandlers.Handle("Cannot delete discussion note", ex);
                  }

                  break;
               }
            }
         }

         string message = deletedNoteId.HasValue
            ? String.Format("[DicsussionCreator] Deleted note with Id {0}", deletedNoteId.Value)
            : "Could not find a note to delete (or could not delete it)";
         Trace.TraceInformation(message);
      }

      private readonly DiscussionOperator _discussionOperator;
      private readonly MergeRequestKey _mergeRequestKey;
      private readonly User _currentUser;
   }
}
