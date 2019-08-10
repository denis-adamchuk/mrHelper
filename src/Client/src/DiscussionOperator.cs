using System;

namespace mrHelper.Client
{
   public class DiscussionOperator
   {
      internal DiscussionOperator(UserDefinedSettings settings, MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }

      Task<List<Discussion>> GetDiscussionsAsync()
      {
         throw new NotImplementedException();
      }

      Task ReplyAsync(int discussionId, string body)
      {
         throw new NotImplementedException();
      }

      Task ModifyNoteBody(string discussionId, int noteId, string body)
      {
         throw new NotImplementedException();
      }

      Task DeleteNoteAsync(int noteId)
      {
         throw new NotImplementedException();
      }

      Task ResolveNote(string discussionId, int noteId)
      {
         throw new NotImplementedException();
      }

      Task ResolveDiscussion(string discussionId)
      {
         throw new NotImplementedException();
      }

      Task CreateDiscussion(NewDiscussionParameters parameters)
      {
         throw new NotImplementedException();
      }
   }
}

