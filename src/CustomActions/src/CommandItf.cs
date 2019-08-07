using System.Threading.Tasks;

namespace mrHelper.CustomActions
{
   public interface ICommand
   {
      string GetName();

      Task Run();
   }
}
