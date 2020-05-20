using System.Threading.Tasks;
using GitLabSharp.Entities;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionEditor
   {
      Task<Discussion> GetDiscussion();

      Task ReplyAsync(string body);

      Task<DiscussionNote> ModifyNoteBodyAsync(int noteId, string body);

      Task DeleteNoteAsync(int noteId);

      Task ResolveNoteAsync(int noteId, bool resolved);

      Task<Discussion> ResolveDiscussionAsync(bool resolved);
   }
}

