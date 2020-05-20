using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Common.Constants;
using GitLabSharp.Entities;
using mrHelper.Client.Common;

namespace mrHelper.App.Forms.Helpers
{
   public class EditUsersListViewCallback : IEditOrderedListViewCallback
   {
      public EditUsersListViewCallback(string hostname, ISearchManager searchManager)
      {
         _hostname = hostname;
         _searchManager = searchManager;
      }

      public async Task<bool> CanAddItem(string item, IEnumerable<string> currentItems)
      {
         string username = item.ToLower();
         if (item.StartsWith(Constants.GitLabLabelPrefix) || item.StartsWith(Constants.AuthorLabelPrefix))
         {
            username = item.Substring(1);
         }

         User user = await _searchManager.SearchUserByNameAsync(_hostname, username, true);
         if (user == null)
         {
            MessageBox.Show(String.Format("User \"{0}\" is not found at {1}", username, _hostname),
               "User will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         if (currentItems.Any(x => 0 == String.Compare(x, item, true)))
         {
            MessageBox.Show(String.Format("User {0} is already in the list", item),
               "User will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         return true;
      }

      private string _hostname;
      private readonly ISearchManager _searchManager;
   }
}

