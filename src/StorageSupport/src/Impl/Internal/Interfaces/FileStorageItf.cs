namespace mrHelper.StorageSupport
{
   internal interface IFileStorage : ILocalCommitStorage
   {
      FileStorageComparisonCache ComparisonCache { get; }
      FileStorageDiffCache DiffCache { get; }
      FileStorageRevisionCache FileCache { get; }
   }
}

