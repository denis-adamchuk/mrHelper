using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using mrHelper.Client.Common;

namespace mrHelper.Client.MergeRequests
{
   internal class MergeRequestEditor : IMergeRequestEditor
   {
      internal MergeRequestEditor(MergeRequestOperator mergeRequestOperator,
         ModificationNotifier modificationNotifier)
      {
         _operator = mergeRequestOperator;
         _modificationNotifier = modificationNotifier;
      }

      public Task ModifyMergeRequest(CreateNewMergeRequestParameters parameters)
      {
         throw new NotImplementedException();
      }

      private readonly MergeRequestOperator _operator;
      private readonly ModificationNotifier _modificationNotifier;
   }
}

