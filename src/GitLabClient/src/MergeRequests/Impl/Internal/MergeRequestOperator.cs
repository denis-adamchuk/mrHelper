using GitLabSharp.Accessors;
using mrHelper.Client.Common;
using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestOperator : BaseOperator
   {
      internal MergeRequestOperator(string host, IHostProperties settings)
         : base(host, settings)
      {
      }

      internal Task CreateMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         throw new NotImplementedException();
      }
   }
}

