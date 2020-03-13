using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   public class LocalGitRepositoryOperationException : ExceptionEx
   {
      internal LocalGitRepositoryOperationException(
         Action rollbackAction1, Action rollbackAction2, Exception innerException)
         : base(String.Empty, innerException)
      {
         _rollbackAction1 = rollbackAction1;
         _rollbackAction2 = rollbackAction2;
      }

      public void Rollback1()
      {
         _rollbackAction1?.Invoke();
      }

      public void Rollback2()
      {
         _rollbackAction2?.Invoke();
      }

      private Action _rollbackAction1;
      private Action _rollbackAction2;
   }

   public interface ILocalGitRepositoryOperation
   {
      Task Run(params object[] args);
      Task Cancel();
   }
}

