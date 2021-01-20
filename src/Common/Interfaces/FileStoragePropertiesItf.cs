using mrHelper.Common.Tools;

namespace mrHelper.Common.Interfaces
{
   public interface IFileStorageProperties
   {
      int GetRevisionCountToKeep();

      int GetComparisonCountToKeep();

      TaskUtils.BatchLimits GetComparisonBatchLimitsForAwaitedUpdate();

      TaskUtils.BatchLimits GetFileBatchLimitsForAwaitedUpdate();

      TaskUtils.BatchLimits GetComparisonBatchLimitsForNonAwaitedUpdate();

      TaskUtils.BatchLimits GetFileBatchLimitsForNonAwaitedUpdate();
   }
}

