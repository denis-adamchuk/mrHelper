using System;

namespace mrHelper.Common.Tools
{
   public class DiskCacheReadException : Exceptions.ExceptionEx
   {
      public DiskCacheReadException(Exception innerException) : base(String.Empty, innerException) { }
   }


   public class DiskCacheWriteException : Exceptions.ExceptionEx
   {
      public DiskCacheWriteException(Exception innerException) : base(String.Empty, innerException) { }
   }


   public class DiskCache
   {
      public DiskCache(string path)
      {
         _path = path;
      }

      public bool Has(string key)
      {
         return System.IO.Directory.Exists(_path)
             && System.IO.File.Exists(System.IO.Path.Combine(_path, key));
      }

      public byte[] LoadBytes(string key)
      {
         if (!System.IO.Directory.Exists(_path))
         {
            return null;
         }

         string filename = System.IO.Path.Combine(_path, key);
         if (!System.IO.File.Exists(filename))
         {
            return null;
         }

         return System.IO.File.ReadAllBytes(filename);
      }

      public void SaveBytes(string key, byte[] bytes)
      {
         if (!System.IO.Directory.Exists(_path))
         {
            try
            {
               System.IO.Directory.CreateDirectory(_path);
            }
            catch (Exception ex) // Any exception from Directory.CreateDirectory()
            {
               throw new DiskCacheWriteException(ex);
            }
         }

         string filename = System.IO.Path.Combine(_path, key);
         if (System.IO.Directory.Exists(filename) || System.IO.File.Exists(filename))
         {
            try
            {
               System.IO.Directory.Delete(filename, true);
            }
            catch (Exception ex)
            {
               throw new DiskCacheWriteException(ex);
            }
         }

         try
         {
            System.IO.File.WriteAllBytes(filename, bytes);
         }
         catch (Exception ex)
         {
            throw new DiskCacheWriteException(ex);
         }
      }

      private readonly string _path;
   }
}

