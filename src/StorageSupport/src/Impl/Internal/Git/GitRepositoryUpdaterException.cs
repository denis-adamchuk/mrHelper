using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.StorageSupport
{
   internal class GitRepositoryUpdaterException : ExceptionEx
   {
      internal GitRepositoryUpdaterException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   internal class SSLVerificationException : GitRepositoryUpdaterException
   {
      internal SSLVerificationException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class AuthenticationFailedException : GitRepositoryUpdaterException
   {
      internal AuthenticationFailedException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class CouldNotReadUsernameException : GitRepositoryUpdaterException
   {
      internal CouldNotReadUsernameException(Exception innerException)
         : base(String.Empty, innerException)
      {
      }
   }

   internal class NotEmptyDirectoryException : GitRepositoryUpdaterException
   {
      internal NotEmptyDirectoryException(string path, Exception innerException)
         : base(path, innerException)
      {
      }
   }

   internal class UpdateCancelledException : GitRepositoryUpdaterException
   {
      internal UpdateCancelledException()
         : base(String.Empty, null)
      {
      }
   }
}

