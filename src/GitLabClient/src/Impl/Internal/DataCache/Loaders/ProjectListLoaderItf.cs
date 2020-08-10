using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IProjectListLoader
   {
      Task Load();
   }
}

