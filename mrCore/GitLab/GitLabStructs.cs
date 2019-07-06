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

   public struct DiffRefs
   {
      public string BaseSHA;
      public string HeadSHA;
      public string StartSHA;
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
      public DiffRefs Refs;
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
      public DiffRefs Refs;
      public System.DateTime CreatedAt;
   }

   public struct Position
   {
      public string OldPath;
      public string NewPath;
      public string OldLine;
      public string NewLine;
      public DiffRefs Refs;
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
      public bool Resolvable;
      public bool? Resolved;
      public Position? Position; // notes with type DiffNote must have them (others must not)
   }

   public struct Discussion
   {
      public string Id;
      public List<DiscussionNote> Notes;
      public bool IndividualNote;
   }

   public struct DiscussionParameters
   {
      public string Body;
      public Position? Position;
   }
}
