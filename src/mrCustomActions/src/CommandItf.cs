using System.Threading.Tasks;

namespace mrCustomActions
{
   public interface ICommand
   {
      string GetName();

      Task Run();
   }
}
