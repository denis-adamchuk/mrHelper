using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class AvatarLoader : BaseLoader, IAvatarLoader, IDisposable
   {
      internal AvatarLoader(InternalCacheUpdater cacheUpdater)
      {
         _cacheUpdater = cacheUpdater;
         _diskCache = new DiskCache(PathFinder.AvatarStorage);
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

      private IEnumerable<User> filterUsers(IEnumerable<User> users)
      {
         IEnumerable<User> cachedAtDisk = users.Where(user => hasAvatarAtDisk(user.Avatar_Url));
         foreach (User user in cachedAtDisk)
         {
            int userId = user.Id;
            string avatarUrl = user.Avatar_Url;
            byte[] avatar = readAvatarFromDisk(avatarUrl);
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
         string avatarUrl = user.Avatar_Url;

         // even users returned by filterOutUser() might have been already cached by concurrent calls,
         // so let's disk cache first
         byte[] avatar = readAvatarFromDisk(avatarUrl); 
         if (avatar == null)
         {
            avatar = await call(
               () => _avatarOperator.GetAvatarAsync(avatarUrl),
               String.Format("Cancelled loading avatar for user with id {0}", userId),
               String.Format("Cannot load avatar for user with id {0}", userId));
            saveAvatarToDisk(avatarUrl, avatar);
         }
         _cacheUpdater.UpdateAvatar(userId, avatar);
      }

      private string getAvatarCacheKey(string avatarUrl)
      {
         return CryptoHelper.GetHashString(avatarUrl);
      }

      private bool hasAvatarAtDisk(string avatarUrl)
      {
         string key = getAvatarCacheKey(avatarUrl);
         return _diskCache.Has(key);
      }

      private byte[] readAvatarFromDisk(string avatarUrl)
      {
         string key = getAvatarCacheKey(avatarUrl);
         try
         {
            return _diskCache.LoadBytes(key);
         }
         catch (DiskCacheReadException ex)
         {
            Common.Exceptions.ExceptionHandlers.Handle("Cannot read avatar from disk", ex);
         }
         return null;
      }

      private void saveAvatarToDisk(string avatarUrl, byte[] bytes)
      {
         string key = getAvatarCacheKey(avatarUrl);
         try
         {
            _diskCache.SaveBytes(key, bytes);
         }
         catch (DiskCacheWriteException ex)
         {
            Common.Exceptions.ExceptionHandlers.Handle("Cannot write avatar to disk", ex);
         }
      }

      private readonly AvatarOperator _avatarOperator;
      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DiskCache _diskCache;
   }
}

