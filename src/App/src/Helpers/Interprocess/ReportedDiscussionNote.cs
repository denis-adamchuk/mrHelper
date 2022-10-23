using System;
using GitLabSharp.Entities;
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
      public ReportedDiscussionNoteDetails(User author, DateTime createdAt)
      {
         Author = author;
         CreatedAt = createdAt;
      }

      internal User Author { get; }
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
         string body, User author, DateTime createdAt)
      {
         Key = new ReportedDiscussionNoteKey(id, discussionId);
         Position = new ReportedDiscussionNotePosition(diffPosition);
         Details = new ReportedDiscussionNoteDetails(author, createdAt);
         Content = new ReportedDiscussionNoteContent(body);
      }

      internal ReportedDiscussionNoteKey Key { get; }
      internal ReportedDiscussionNotePosition Position { get; }
      internal ReportedDiscussionNoteDetails Details { get; }
      internal ReportedDiscussionNoteContent Content { get; }
   }
}

