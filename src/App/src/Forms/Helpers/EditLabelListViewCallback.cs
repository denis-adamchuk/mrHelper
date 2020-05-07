using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.Client.Common;
using GitLabSharp.Entities;

namespace mrHelper.App.Forms.Helpers
{
   public class EditLabelListViewCallback : IEditOrderedListViewCallback
   {
      public EditLabelListViewCallback(string hostname)
      {
         _hostname = hostname;
      }

      public Task<bool> CanAddItem(string item, IEnumerable<string> currentItems)
      {
         return Task.FromResult(true);
      }

      private string _hostname;
   }
}

