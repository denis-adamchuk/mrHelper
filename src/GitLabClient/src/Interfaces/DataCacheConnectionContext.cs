namespace mrHelper.GitLabClient
{
   public class DataCacheConnectionContext
   {
      public DataCacheConnectionContext(SearchQueryCollection queryCollection)
      {
         QueryCollection = queryCollection;
      }

      public SearchQueryCollection QueryCollection { get; }
   }
}

