namespace mrHelper.GitLabClient
{
   public interface INetworkOperationStatusListener
   {
      void OnFailure();

      void OnSuccess();
   }
}

