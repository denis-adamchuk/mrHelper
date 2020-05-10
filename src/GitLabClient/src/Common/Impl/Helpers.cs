using System;
using System.Diagnostics;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;
using mrHelper.Common.Tools;

namespace mrHelper.Client.Common
{
   public static class Helpers
   {
      public static bool IsUserMentioned(string text, User user)
      {
         if (user.Id == (default(User)).Id)
         {
            Debug.Assert(false);
            return false;
         }

         if (StringUtils.ContainsNoCase(text, user.Name))
         {
            return true;
         }

         string label = Constants.GitLabLabelPrefix + user.Username;
         int idx = text.IndexOf(label, StringComparison.CurrentCultureIgnoreCase);
         while (idx >= 0)
         {
            if (idx == text.Length - label.Length)
            {
               // text ends with label
               return true;
            }

            if (!Char.IsLetter(text[idx + label.Length]))
            {
               // label is in the middle of text
               return true;
            }

            Debug.Assert(idx != text.Length - 1);
            idx = text.IndexOf(label, idx + 1, StringComparison.CurrentCultureIgnoreCase);
         }

         return false;
      }

   }
}

