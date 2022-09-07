using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient.Loaders.Cache;
using mrHelper.GitLabClient.Operators;

namespace mrHelper.GitLabClient.Loaders
{
   internal class AvatarLoader : BaseDataCacheLoader, IAvatarLoader
   {
      internal AvatarLoader(DataCacheOperator op, InternalCacheUpdater cacheUpdater)
         : base(op)
      {
         _cacheUpdater = cacheUpdater;
         _diskCache = new DiskCache(PathFinder.AvatarStorage);
      }

      async public Task LoadAvatars(IEnumerable<MergeRequestKey> mergeRequestKeys)
      {
         Exception exception = null;
         async Task loadAvatarsLocal(MergeRequestKey mrk)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               await loadAvatarsAsync(mrk);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(mergeRequestKeys, x => loadAvatarsLocal(x),
            () => Constants.AvatarLoaderMergeRequestBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
      }

      async public Task LoadAvatars(IEnumerable<Discussion> discussions)
      {
         Exception exception = null;
         async Task loadAvatarsLocal(Discussion discussion)
         {
            if (exception != null)
            {
               return;
            }

            try
            {
               await loadAvatarsAsync(discussion);
            }
            catch (BaseLoaderException ex)
            {
               exception = ex;
            }
         }

         await TaskUtils.RunConcurrentFunctionsAsync(discussions, x => loadAvatarsLocal(x),
            () => Constants.AvatarLoaderMergeRequestBatchLimits, () => exception != null);
         if (exception != null)
         {
            throw exception;
         }
      }


      async private Task loadAvatarsAsync(MergeRequestKey mrk)
      {
         MergeRequest mr = _cacheUpdater.Cache.GetMergeRequest(mrk);
         await loadAvatarForUserAsync(mr.Author);
      }


      async private Task loadAvatarsAsync(Discussion discussion)
      {
         foreach (DiscussionNote note in discussion.Notes)
         {
            await loadAvatarForUserAsync(note.Author);
         }
      }

      async private Task loadAvatarForUserAsync(User user)
      {
         int userId = user.Id;
         string avatarUrl = user.Avatar_Url;
         byte[] avatar = readAvatarFromDisk(avatarUrl);
         if (avatar == null)
         {
            avatar = await call(
               () => _operator.AvatarOperator.GetAvatarAsync(avatarUrl),
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

      private readonly InternalCacheUpdater _cacheUpdater;
      private readonly DiskCache _diskCache;
   }
}

