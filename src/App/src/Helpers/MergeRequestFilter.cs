using GitLabSharp.Entities;
using mrHelper.Client.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.App.Helpers
{
   internal static class MergeRequestFilter
   {
      private static MergeRequest GetMergeRequest(MergeRequest x) => x;

      public static bool IsFilteredMergeRequest(MergeRequest mergeRequest, string[] selected)
      {
         if (selected == null || (selected.Length == 1 && selected[0] == String.Empty))
         {
            return false;
         }

         foreach (string item in selected)
         {
            if (item.StartsWith(Common.Constants.Constants.AuthorLabelPrefix))
            {
               if (mergeRequest.Author.Username.StartsWith(item.Substring(1)))
               {
                  return false;
               }
            }
            else if (item.StartsWith(Common.Constants.Constants.GitLabLabelPrefix))
            {
               if (mergeRequest.Labels.Any(x => x.StartsWith(item)))
               {
                  return false;
               }
            }
            else if (item != String.Empty)
            {
               if (mergeRequest.IId.ToString().Contains(item)
                || mergeRequest.Author.Username.Contains(item)
                || mergeRequest.Author.Name.Contains(item)
                || mergeRequest.Labels.Any(x => x.Contains(item))
                || mergeRequest.Title.Contains(item))
               {
                  return false;
               }
            }
         }

         return true;
      }
   }
}
