using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;
using mrHelper.GitLabClient.Managers;

namespace mrHelper.GitLabClient.Loaders
{
   internal class AvatarLoader : BaseLoader, IAvatarLoader, IDisposable
   {
      internal AvatarLoader(InternalCacheUpdater cacheUpdater)
      {
         _cacheUpdater = cacheUpdater;
         _diskCache = new AvatarDiskCache();
         _avatarOperator = new AvatarOperator();
      }

      public void Dispose()
      {
         _avatarOperator.Dispose();
      }

      async public Task LoadAvatars(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         IEnumerable<User> authors = extractAuthors(mergeRequestKeys);
         authors = filterUsers(authors);
         await loadAvatarsForUsersAsync(authors, Constants.AvatarLoaderUserBatchLimits);
      }

      async public Task LoadAvatars(IEnumerable<Discussion> discussions)
      {
         IEnumerable<User> authors = extractAuthors(discussions);
         authors = filterUsers(authors);
         await loadAvatarsForUsersAsync(authors, Constants.AvatarLoaderForDiscussionsUserBatchLimits);
      }

      async public Task LoadAvatars(IEnumerable<User> users)
      {
         users = filterUsers(users);
         await loadAvatarsForUsersAsync(users, Constants.AvatarLoaderForUsersUserBatchLimits);
      }

      private static IEnumerable<User> extractAuthors(IEnumerable<Discussion> discussions)
      {
         return discussions
            .SelectMany(discussion => discussion.Notes)
            .Select(note => note.Author)
            .GroupBy(user => user.Id)
            .Select(group => group.First());
      }

      private IEnumerable<User> extractAuthors(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         return mergeRequestKeys
            .Select(mrk => _cacheUpdater.Cache.GetMergeRequest(mrk))
            .Select(mr => mr.Author)
            .GroupBy(user => user.Id)
            .Select(group => group.First());
      }

      // Remove users whose avatars are already cached at disk
      private IEnumerable<User> filterUsers(IEnumerable<User> users)
      {
         IEnumerable<User> cachedAtDisk = users
            .Where(user => hasAvatarAtDisk(user));
         foreach (User user in cachedAtDisk)
         {
            int userId = user.Id;
            byte[] avatar = readAvatarFromDisk(user);
            _cacheUpdater.UpdateAvatar(userId, avatar);
         }
         return users.Except(cachedAtDisk);
      }

      async private Task loadAvatarsForUsersAsync(IEnumerable<User> users, TaskUtils.BatchLimits batchLimits)
      {
         Exception exception = null;
         async Task loadAvatarsLocal(User user)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               await loadAvatarForUserAsync(user);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(users, x => loadAvatarsLocal(x),
            () => batchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
      }

      async private Task loadAvatarForUserAsync(User user)
      {
         int userId = user.Id;

         // even users returned by filterUsers() might have been already cached by concurrent calls,
         // so let's disk cache first
         byte[] avatar = readAvatarFromDisk(user); 
         if (avatar == null)
         {
            string avatarUrl = user.Avatar_Url;
            if (String.IsNullOrWhiteSpace(avatarUrl))
            {
               return; // don't waste time on missing avatars
            }

            avatar = await call(
               () => _avatarOperator.GetAvatarAsync(avatarUrl),
               String.Format("Cancelled loading avatar for user with id {0}", userId),
               String.Format("Cannot load avatar for user with id {0}", userId));
            saveAvatarToDisk(user, avatar);
         }
         _cacheUpdater.UpdateAvatar(userId, avatar);
      }

      private bool hasAvatarAtDisk(User user) =>
         _diskCache.IsUserAvatarCached(user);

      private byte[] readAvatarFromDisk(User user) =>
         _diskCache.LoadUserAvatar(user);

      private void saveAvatarToDisk(User user, byte[] bytes) =>
         _diskCache.SaveUserAvatar(user, bytes);

      private readonly AvatarOperator _avatarOperator;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly AvatarDiskCache _diskCache;
   }
}

