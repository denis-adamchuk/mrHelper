using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   // TODO Should not be bound to exact DataCache
   public class ExpressionResolver : IDisposable
   {
      public ExpressionResolver(DataCache dataCache)
      {
         _dataCache = dataCache;
         _dataCache.Connected += onDataCacheConnected;
      }

      public void Dispose()
      {
         if (_dataCache != null)
         {
            _dataCache.Connected -= onDataCacheConnected;
            _dataCache = null;
         }
      }

      public string Resolve(string expression)
      {
         if (_currentUser == null)
         {
            return expression;
         }

         if (_cached.TryGetValue(expression, out string value))
         {
            return value;
         }

         value = expression.Replace("%CurrentUsername%", _currentUser.Username);
         _cached[expression] = value;
         return value;
      }

      private void onDataCacheConnected(string hostname, User user)
      {
         _currentUser = user;
         _cached.Clear();
      }

      private User _currentUser;
      private DataCache _dataCache;
      private readonly Dictionary<string, string> _cached = new Dictionary<string, string>();
   }
}

