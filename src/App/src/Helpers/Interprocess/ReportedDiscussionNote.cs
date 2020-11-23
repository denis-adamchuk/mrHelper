using mrHelper.Core.Matching;

namespace mrHelper.App.Interprocess
{
   internal struct ReportedDiscussionNoteKey
   {
      public ReportedDiscussionNoteKey(int id, string discussionId) : this()
      {
         Id = id;
         DiscussionId = discussionId;
      }

      internal int Id { get; }
      internal string DiscussionId { get; }
   }

   internal struct ReportedDiscussionNoteContent
   {
      public ReportedDiscussionNoteContent(string body) : this()
      {
         Body = body;
      }

      internal string Body { get; }
   }

   internal struct ReportedDiscussionNotePosition
   {
      public ReportedDiscussionNotePosition(DiffPosition diffPosition) : this()
      {
         DiffPosition = diffPosition;
      }

      internal DiffPosition DiffPosition { get; }
   }

   internal struct ReportedDiscussionNote
   {
      public ReportedDiscussionNote(int id, string discussionId, DiffPosition diffPosition, string body)
      {
         Key = new ReportedDiscussionNoteKey(id, discussionId);
         Position = new ReportedDiscussionNotePosition(diffPosition);
         Content = new ReportedDiscussionNoteContent(body);
      }

      internal ReportedDiscussionNoteKey Key { get; }
      internal ReportedDiscussionNotePosition Position { get; }
      internal ReportedDiscussionNoteContent Content { get; }
   }
}

