using System;

namespace mrHelper.Client
{
   public class DiscussionCreatorException : Exception
   {
      public DiscussionCreatorException(bool handled)
      {
         Handled = handled;
      }

      public bool Handled { get; }
   }

   public class DiscussionCreator
   {
      public DiscussionCreator(MergeRequestDescriptor mrd, DiscussionOperator discussionOperator)
      {
         DiscussionOperator = discussionOperator;
         MergeRequestDescriptor = mrd;
      }

      async public Task CreateDiscussionAsync(NewDiscussionParameters parameters)
      {
         try
         {
            await DiscussionOperator.CreateDiscussionAsync(MergeRequestDescriptor, parameters);
         }
         catch (OperatorException ex)
         {
            bool handled = handleGitlabError(parameters, gl, ex);
            throw new DiscussionCreatorException(handled);
         }
      }

      private void handleGitlabError(NewDiscussionParameters parameters, OperatorException ex)
      {
         var webException = ex.GitLabRequestException.WebException;
         var response = ((System.Net.HttpWebResponse)webException.Response);

         if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
         {
            // Something went wrong at the GitLab site, let's report a discussion without Position
            return createMergeRequestWithoutPosition(parameters);
         }
         else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
         {
            // Something went wrong at the GitLab site, let's report a discussion without Position
            cleanupBadNotes(parameters);
            return createMergeRequestWithoutPosition(parameters);
         }

         return false;
      }

      async private bool createMergeRequestWithoutPosition(NewDiscussionParameters parameters)
      {
         Debug.Assert(parameters.Position.HasValue);

         Trace.TraceInformation("Reporting a discussion without Position (fallback)");

         parameters.Body = getFallbackInfo(parameters.Position) + "<br>" + parameters.Body;
         parameters.Position = null;

         try
         {
            DiscussionOperator.CreateDiscussionAsync(MergeRequestDescriptor, parameters);
         }
         catch (OperatorException ex)
         {
            ExceptionHandlers.Handle(ex, "Cannot create a discussion (again)");
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

      // Instead of searching for a latest discussion note with some heuristically prepared parameters,
      // let's clean up all similar notes, including a recently added one
      private void cleanupBadNotes(NewDiscussionParameters parameters)
      {
         Debug.Assert(parameters.Position.HasValue);

         Trace.TraceInformation("Looking up for a note with bad position...");

         int deletedCount = 0;

         List<Discussion> discussions = DiscussionOperator.GetDiscussionsAsync(MergeRequestDescriptor);
         if (discussions == null)
         {
            Trace.TraceWarning(String.Format("No discussions found"));
            return;
         }

         foreach (Discussion discussion in discussions)
         {
            foreach (DiscussionNote note in discussion.Notes)
            {
               if (arePositionsEqual(note.Position, parameters.Position.Value))
               {
                  Trace.TraceInformation(
                     "Deleting discussion note." +
                     " Id: {0}, Author.Username: {1}, Created_At: {2} (LocalTime), Body:\n{3}",
                     note.Id.ToString(), note.Author.Username, note.Created_At.ToLocalTime(), note.Body);

                  DiscussionOperator.DeleteNoteAsync(noteId);
                  ++deletedCount;
               }
            }
         }

         Trace.TraceInformation(String.Format("Deleted {0} notes", deletedCount));
      }

      /// <summary>
      /// Compares GitLabSharp.Position object which is received from GitLab
      /// to GitLabSharp.PositionParameters whichi is sent to GitLab for equality
      /// </summary>
      /// <returns>true if objects point to the same position</returns>
      private bool arePositionsEqual(Position pos, PositionParameters posParams)
      {
         return pos.Base_SHA == posParams.BaseSHA
             && pos.Head_SHA == posParams.HeadSHA
             && pos.Start_SHA == posParams.StartSHA
             && pos.Old_Line == posParams.OldLine
             && pos.Old_Path == posParams.OldPath
             && pos.New_Line == posParams.NewLine
             && pos.New_Path == posParams.NewPath;
      }

      private DiscussionOperator DiscussionOperator { get; }
      private MergeRequestDescriptor MergeRequestDescriptor { get; }
   }
}

