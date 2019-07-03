using System.Collections.Generic;

namespace mrCore
{
   public struct User
   {
      public int Id;
      public string Name;
      public string Username;
   }

   public enum MergeRequestState
   {
      Opened,
      Closed,
      Locked,
      Merged
   }

   public struct Project
   {
      public int Id;
      public string NameWithNamespace;
   }

   public struct MergeRequest
   {
      public int Id;
      public string Title;
      public string Description;
      public string SourceBranch;
      public string TargetBranch;
      public MergeRequestState State;
      public List<string> Labels;
      public string WebUrl;
      public bool WorkInProgress;
      public User Author;
      public string BaseSHA;
      public string HeadSHA;
      public string StartSHA;
   }

   public struct Commit
   {
      public string Id;
      public string ShortId;
      public string Title;
      public string Message;
      public System.DateTime CommitedDate;
   }

   public struct Version
   {
      public int Id;
      public string HeadSHA;
      public string BaseSHA;
      public string StartSHA;
      public System.DateTime CreatedAt;
   }

   public enum DiscussionNoteType
   {
      Default,
      DiffNote,
      DiscussionNote
   }
   
   public struct DiscussionNote
   {
      public int Id;
      public string Body;
      public System.DateTime CreatedAt;
      public User Author;
      public DiscussionNoteType Type;
      public bool System;
   }

   public struct Discussion
   {
      public string Id;
      public List<DiscussionNote> Notes;
      public bool IndividualNote;
   }
}
