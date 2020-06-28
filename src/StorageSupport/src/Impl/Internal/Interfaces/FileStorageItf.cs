namespace mrHelper.StorageSupport
{
   internal interface IFileStorage : ILocalCommitStorage
   {
      FileStorageComparisonCache ComparisonCache { get; }
      FileStorageDiffCache DiffCache { get; }
      FileStorageFileCache FileCache { get; }
   }
}

