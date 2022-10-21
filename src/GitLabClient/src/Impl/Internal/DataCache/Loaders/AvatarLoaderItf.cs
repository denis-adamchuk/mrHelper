using GitLabSharp.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitLabClient.Loaders
{
   internal interface IAvatarLoader
   {
      Task LoadAvatars(IEnumerable<MergeRequestKey> mergeRequestKeys);

      Task LoadAvatars(IEnumerable<Discussion> discussions);

      Task LoadAvatars(IEnumerable<User> users);
   }
}

