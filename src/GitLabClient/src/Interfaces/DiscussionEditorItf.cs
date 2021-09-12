using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
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

      public bool IsNotFoundException()
      {
         if (InnerException != null && (InnerException is GitLabRequestException))
         {
            GitLabRequestException rx = InnerException as GitLabRequestException;
            if (rx.InnerException is System.Net.WebException wx && wx.Response != null)
            {
               System.Net.HttpWebResponse response = wx.Response as System.Net.HttpWebResponse;
               return response.StatusCode == System.Net.HttpStatusCode.NotFound;
            }
         }
         return false;
      }
   }

   public class DiscussionEditorCancelledException : DiscussionEditorException
   {
      public DiscussionEditorCancelledException() : base(String.Empty, null)
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

