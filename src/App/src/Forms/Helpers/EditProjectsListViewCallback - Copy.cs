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
      public EditProjectsListViewCallback(string hostname)
      {
         _hostname = hostname;
      }

      public async Task<bool> CanAddItem(string item, IEnumerable<string> currentItems)
      {
         if (item.Count(x => x == '/') != 1)
         {
            MessageBox.Show("Wrong format of project name. It should include a group name too.",
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         SearchManager searchManager = new SearchManager(Program.Settings);

         int slashIndex = item.IndexOf('/');
         if (item.IndexOf(" ", 0, slashIndex) != -1)
         {
            User? user = await searchManager.SearchUserAsync(_hostname, item.Substring(0, slashIndex));
            if (user == null)
            {
               MessageBox.Show("Project name has a space and looks like a name of a user but there is no such user",
                  "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return false;
            }

            item = user.Value.Username + item.Substring(slashIndex);
         }

         if (currentItems.Any(x => 0 == String.Compare(x, item, true)))
         {
            MessageBox.Show(String.Format("Project {0} is already in the list", item),
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         Project? project = await searchManager.SearchProjectAsync(_hostname, item);
         if (project == null)
         {
            MessageBox.Show(String.Format("There is no project {0} at {1}", item, _hostname),
               "Project will not be added", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
         }

         return true;
      }

      private string _hostname;
   }
}

