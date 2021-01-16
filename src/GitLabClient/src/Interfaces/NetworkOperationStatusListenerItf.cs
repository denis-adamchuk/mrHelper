namespace mrHelper.GitLabClient
{
   internal interface INetworkOperationStatusListener
   {
      void OnFailure();

      void OnSuccess();
   }
}

