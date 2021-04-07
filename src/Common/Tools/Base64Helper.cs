using System;

namespace mrHelper.Common.Tools
{
   public static class Base64Helper
   {
      public static byte[] FromBase64StringSafe(string base64String)
      {
         if (base64String != null)
         {
            try
            {
               return Convert.FromBase64String(base64String);
            }
            catch (FormatException ex)
            {
               Exceptions.ExceptionHandlers.Handle("Cannot decode base64 string", ex);
            }
         }
         return null;
      }

      public static string ToBase64StringSafe(byte[] bytes)
      {
         return bytes == null ? null : Convert.ToBase64String(bytes);
      }
   }
}

