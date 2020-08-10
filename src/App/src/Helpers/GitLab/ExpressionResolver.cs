using System;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Helpers
{
   public class ExpressionResolver : IDisposable
   {
      public ExpressionResolver(DataCache dataCache)
      {
         _dataCache = dataCache;
         _dataCache.Connected += onDataCacheConnected;
      }

      public void Dispose()
      {
         _dataCache.Connected -= onDataCacheConnected;
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
      private readonly DataCache _dataCache;
      private readonly Dictionary<string, string> _cached = new Dictionary<string, string>();
   }
}

