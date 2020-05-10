using GitLabSharp.Entities;
using mrHelper.Client.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Workflow
{
   public enum EDiscussionUpdateType
   {
      InitialSnapshot,
      PeriodicUpdate,
      NewMergeRequest
   }

   internal interface IDiscussionLoaderListenerInternal
   {
      void OnPostLoadDiscussionsInternal(
         MergeRequestKey mrk, IEnumerable<Discussion> discussions, EDiscussionUpdateType type);
   }
}

