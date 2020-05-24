using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.Client.Common;
using GitLabSharp.Entities;

namespace mrHelper.App.Forms.Helpers
{
   public class EditProjectsListViewCallback : IEditOrderedListViewCallback
   {
      public EditProjectsListViewCallback(string hostname, ISearchManager searchManager)
      {
         _hostname = hostname;
         _searchManager = searchManager;
      }

      public async Task<string> CanAddItem(string item, IEnumerable<string> currentItems)
      {
         if (item.Count(x => x == '/') != 1)
         {
            MessageBox.Show("Wrong format of project name. It should include a group name too.",
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
         }

         int slashIndex = item.IndexOf('/');
         if (item.IndexOf(" ", 0, slashIndex) != -1)
         {
            User user = await _searchManager.SearchUserByNameAsync(_hostname, item.Substring(0, slashIndex), false);
            if (user == null)
            {
               MessageBox.Show("Project name has a space and looks like a name of a user but there is no such user",
                  "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return null;
            }

            item = user.Username + item.Substring(slashIndex);
         }

         Project project = await _searchManager.SearchProjectAsync(_hostname, item);
         if (project == null)
         {
            MessageBox.Show(String.Format("There is no project {0} at {1}", item, _hostname),
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
         }

         if (currentItems.Any(x => x == project.Path_With_Namespace))
         {
            MessageBox.Show(String.Format("Project {0} is already in the list", item),
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
         }

         return project.Path_With_Namespace;
      }

      private readonly string _hostname;
      private readonly ISearchManager _searchManager;
   }
}

