using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IMergeRequestListLoader
   {
      Task Load();
   }
}

