namespace mrHelper.Client.Common
{
   public interface ILoader<T>
   {
      INotifier<T> GetNotifier();
   }
}

