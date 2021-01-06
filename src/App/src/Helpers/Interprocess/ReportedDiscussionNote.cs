using System;
using mrHelper.Core.Matching;

namespace mrHelper.App.Interprocess
{
   internal struct ReportedDiscussionNoteKey
   {
      public ReportedDiscussionNoteKey(int id, string discussionId)
      {
         Id = id;
         DiscussionId = discussionId;
      }

      internal int Id { get; }
      internal string DiscussionId { get; }
   }

   internal struct ReportedDiscussionNoteContent
   {
      public ReportedDiscussionNoteContent(string body)
      {
         Body = body;
      }

      internal string Body { get; }
   }

   internal struct ReportedDiscussionNoteDetails
   {
      public ReportedDiscussionNoteDetails(string authorName, DateTime createdAt)
      {
         AuthorName = authorName;
         CreatedAt = createdAt;
      }

      internal string AuthorName { get; }
      internal DateTime CreatedAt { get; }
   }

   internal struct ReportedDiscussionNotePosition
   {
      public ReportedDiscussionNotePosition(DiffPosition diffPosition)
      {
         DiffPosition = diffPosition;
      }

      internal DiffPosition DiffPosition { get; }
   }

   internal struct ReportedDiscussionNote
   {
      public ReportedDiscussionNote(int id, string discussionId, DiffPosition diffPosition,
         string body, string authorName, DateTime createdAt)
      {
         Key = new ReportedDiscussionNoteKey(id, discussionId);
         Position = new ReportedDiscussionNotePosition(diffPosition);
         Details = new ReportedDiscussionNoteDetails(authorName, createdAt);
         Content = new ReportedDiscussionNoteContent(body);
      }

      internal ReportedDiscussionNoteKey Key { get; }
      internal ReportedDiscussionNotePosition Position { get; }
      internal ReportedDiscussionNoteDetails Details { get; }
      internal ReportedDiscussionNoteContent Content { get; }
   }
}

