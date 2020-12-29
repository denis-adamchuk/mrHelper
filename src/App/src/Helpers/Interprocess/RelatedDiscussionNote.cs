using mrHelper.Core.Matching;

namespace mrHelper.App.Interprocess
{
   internal struct RelatedDiscussionNoteKey
   {
      public RelatedDiscussionNoteKey(int id, string discussionId) : this()
      {
         Id = id;
         DiscussionId = discussionId;
      }

      internal int Id { get; }
      internal string DiscussionId { get; }
   }

   internal struct RelatedDiscussionNoteContent
   {
      public RelatedDiscussionNoteContent(string body) : this()
      {
         Body = body;
      }

      internal string Body { get; }
   }

   internal struct RelatedDiscussionNotePosition
   {
      public RelatedDiscussionNotePosition(DiffPosition diffPosition) : this()
      {
         DiffPosition = diffPosition;
      }

      internal DiffPosition DiffPosition { get; }
   }

   internal struct RelatedDiscussionNote
   {
      public RelatedDiscussionNote(string discussionId, DiffPosition diffPosition, string body)
      {
         Key = new RelatedDiscussionNoteKey(id, discussionId);
         Position = new RelatedDiscussionNotePosition(diffPosition);
         Content = new RelatedDiscussionNoteContent(body);
      }

      internal RelatedDiscussionNoteKey Key { get; }
      internal RelatedDiscussionNotePosition Position { get; }
      internal RelatedDiscussionNoteContent Content { get; }
   }
}

