using GitLabSharp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mrHelper.Client.Common
{
   static internal class GlobalUserCache
   {
      static internal User GetUser(string hostname, string username)
      {
         if (_users.TryGetValue(hostname, out HashSet<User> users))
         {
            return users.SingleOrDefault(x => 0 == String.Compare(x.Username, username,
               StringComparison.CurrentCultureIgnoreCase));
         }
         return null;
      }

      static internal void AddUser(string hostname, User user)
      {
         if (!_users.ContainsKey(hostname))
         {
            _users[hostname] = new HashSet<User>();
         }
         _users[hostname].Add(user);
      }

      static Dictionary<string, HashSet<User>> _users = new Dictionary<string, HashSet<User>>();
   }
}

