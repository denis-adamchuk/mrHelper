using System;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

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

