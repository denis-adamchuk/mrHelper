using System;
using System.Security.Cryptography;
using System.Text;

namespace mrHelper.Common.Tools
{
   public static class CryptoHelper
   {
      public static byte[] ProtectSafe(byte[] data)
      {
         if (data != null)
         {
            try
            {
               // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
               // only by the same current user.
               return ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex) // Any exception from ProtectedData.Protect()
            {
               Exceptions.ExceptionHandlers.Handle("Cannot encrypt data", ex);
            }
         }
         return null;
      }

      public static byte[] UnprotectSafe(byte[] data)
      {
         if (data != null)
         {
            try
            {
               //Decrypt the data using DataProtectionScope.CurrentUser.
               return ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex) // Any exception from ProtectedData.Unprotect()
            {
               Exceptions.ExceptionHandlers.Handle("Cannot decrypt data", ex);
            }
         }
         return null;
      }

      public static byte[] GetHash(string inputString)
      {
         using (HashAlgorithm algorithm = SHA256.Create())
         {
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
         }
      }

      public static string GetHashString(string inputString)
      {
         StringBuilder sb = new StringBuilder();
         foreach (byte b in GetHash(inputString))
         {
            sb.Append(b.ToString("X2"));
         }
         return sb.ToString();
      }
   }
}

