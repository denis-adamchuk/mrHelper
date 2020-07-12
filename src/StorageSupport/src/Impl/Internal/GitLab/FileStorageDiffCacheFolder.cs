namespace mrHelper.StorageSupport
{
   internal class FileStorageDiffCacheFolder
   {
      internal FileStorageDiffCacheFolder(string path)
      {
         _path = path;
      }

      internal string LeftSubfolder => System.IO.Path.Combine(_path, "left");
      internal string RightSubfolder => System.IO.Path.Combine(_path, "right");

      private readonly string _path;
   }
}

