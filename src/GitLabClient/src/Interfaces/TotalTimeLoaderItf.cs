using System;

namespace mrHelper.GitLabClient
{
   public interface ITotalTimeLoader
   {
      event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoading;
      event Action<ITotalTimeCache, MergeRequestKey> TotalTimeLoaded;
   }
}

