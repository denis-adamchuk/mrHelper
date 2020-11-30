using System;
using System.Security.Cryptography;

namespace mrHelper.Common.Tools
{
   public static class CryptoHelper
   {
      public static byte[] ProtectSafe(byte[] data)
      {
         try
         {
            // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
            // only by the same current user.
            return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
         }
         catch (CryptographicException ex)
         {
            Exceptions.ExceptionHandlers.Handle("Cannot encrypt data", ex);
            return null;
         }
      }

      public static byte[] UnprotectSafe(byte[] data)
      {
         try
         {
            //Decrypt the data using DataProtectionScope.CurrentUser.
            return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
         }
         catch (CryptographicException ex)
         {
            Exceptions.ExceptionHandlers.Handle("Cannot decrypt data", ex);
            return null;
         }
      }
   }
}

