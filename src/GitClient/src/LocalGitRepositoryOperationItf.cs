using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitClient
{
   public class LocalGitRepositoryOperationException : ExceptionEx
   {
      internal LocalGitRepositoryOperationException(Action rollbackAction, Exception innerException)
         : base(String.Empty, innerException)
      {
         _rollbackAction = rollbackAction;
      }

      public void Rollback()
      {
         _rollbackAction?.Invoke();
      }

      private Action _rollbackAction;
   }

   public interface ILocalGitRepositoryOperation
   {
      Task Run(params object[] args);
      Task Cancel();
   }
}

