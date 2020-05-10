namespace mrHelper.Client.Common
{
   public interface INotifier<T>
   {
      void AddListener(T listener);
      void RemoveListener(T listener);
   }
}

