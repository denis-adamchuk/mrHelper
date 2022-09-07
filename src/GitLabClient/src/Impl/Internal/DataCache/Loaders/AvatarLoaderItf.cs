using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IAvatarLoader
   {
      Task LoadAvatars(IEnumerable<MergeRequestKey> mergeRequestKeys);

      Task LoadAvatars(IEnumerable<GitLabSharp.Entities.Discussion> discussions);
   }
}

