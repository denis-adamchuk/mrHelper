namespace mrCustomActions
{
   public interface ICommand
   {
      string GetName();

      void Run();
   }
}
