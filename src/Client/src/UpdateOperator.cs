using System;

namespace mrHelper.Client
{
   internal class UpdateOperator
   {
      internal UpdateOperator(UserDefinedSettings settings)
      {
         throw new NotImplementedException();
      }

      async internal Task<List<MergeRequests>> GetMergeRequests(string host, string project)
      {
         throw new NotImplementedException();
      }

      async internal Task<List<Versions>> GetVersions(MergeRequestDescriptor mrd)
      {
         throw new NotImplementedException();
      }
   }
}

