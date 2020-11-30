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
            catch (Exception ex)
            {
               Exceptions.ExceptionHandlers.Handle("Cannot decode base64 string", ex);
            }
         }
         return null;
      }

      public static string ToBase64StringSafe(byte[] bytes)
      {
         if (bytes != null)
         {
            try
            {
               return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
               Exceptions.ExceptionHandlers.Handle("Cannot encode base64 string", ex);
            }
         }
         return null;
      }
   }
}

