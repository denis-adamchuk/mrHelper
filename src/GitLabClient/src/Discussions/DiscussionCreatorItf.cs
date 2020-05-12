using System.Threading.Tasks;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Discussions
{
   public interface IDiscussionCreator
   {
      Task CreateNoteAsync(CreateNewNoteParameters parameters);

      Task CreateDiscussionAsync(NewDiscussionParameters parameters, bool revertOnError);
   }
}

