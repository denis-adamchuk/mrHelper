using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   public interface IMergeRequestLoader
   {
      Task LoadMergeRequest(MergeRequestKey mrk);
   }
}

