namespace mrHelper.GitLabClient.Interfaces
{
   public interface INetworkOperationStatusListener
   {
      void OnFailure(string hostname);

      void OnSuccess(string hostname);
   }
}

