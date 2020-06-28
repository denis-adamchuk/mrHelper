using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class RepositoryUpdateException : ExceptionEx
   {
      internal RepositoryUpdateException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   internal class SSLVerificationException : RepositoryUpdateException
   {
      internal SSLVerificationException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class AuthenticationFailedException : RepositoryUpdateException
   {
      internal AuthenticationFailedException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class CouldNotReadUsernameException : RepositoryUpdateException
   {
      internal CouldNotReadUsernameException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class NotEmptyDirectoryException : RepositoryUpdateException
   {
      internal NotEmptyDirectoryException(string path, Exception innerException)
         : base(path, innerException)
      {
      }
   }

   internal class UpdateCancelledException : RepositoryUpdateException
   {
      internal UpdateCancelledException()
         : base(String.Empty, null)
      {
      }
   }
}

