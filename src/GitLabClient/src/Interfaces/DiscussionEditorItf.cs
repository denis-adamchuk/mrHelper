using System;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class DiscussionEditorException : ExceptionEx
   {
      internal DiscussionEditorException(string message, Exception innerException)
         : base(message, innerException)
      {
      }
   }

   public interface IDiscussionEditor
   {
      Task ReplyAsync(string body);

      Task ReplyAndResolveDiscussionAsync(string body, bool resolve);

      Task<DiscussionNote> ModifyNoteBodyAsync(int noteId, string body);

      Task DeleteNoteAsync(int noteId);

      Task ResolveNoteAsync(int noteId, bool resolve);

      Task<Discussion> ResolveDiscussionAsync(bool resolve);
   }
}

