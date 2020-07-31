using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using mrHelper.Common.Constants;
using GitLabSharp.Entities;
using mrHelper.GitLabClient;

namespace mrHelper.App.Forms.Helpers
{
   public class EditUsersListViewCallback : IEditOrderedListViewCallback
   {
      public EditUsersListViewCallback(RawDataAccessor rawDataAccessor)
      {
         _rawDataAccessor = rawDataAccessor;
      }

      public async Task<string> CanAddItem(string item, IEnumerable<string> currentItems)
      {
         string username = item.ToLower();
         if (item.StartsWith(Constants.GitLabLabelPrefix) || item.StartsWith(Constants.AuthorLabelPrefix))
         {
            username = item.Substring(1);
         }

         User user = await _rawDataAccessor.UserAccessor.SearchUserByUsernameAsync(username);
         if (user == null)
         {
            MessageBox.Show(String.Format("User \"{0}\" is not found at the selected host", username),
               "User will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
         }

         if (currentItems.Any(x => x == user.Username))
         {
            MessageBox.Show(String.Format("User {0} is already in the list", user.Username),
               "User will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
         }

         return user.Username;
      }

      private readonly RawDataAccessor _rawDataAccessor;
   }
}

