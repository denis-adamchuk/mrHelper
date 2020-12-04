using System;

namespace mrHelper.App.Controls
{
   public class MergeRequestListViewSubItemInfo
   {
      public MergeRequestListViewSubItemInfo(Func<bool, string> getText, Func<string> getUrl)
      {
         _getText = getText;
         _getUrl = getUrl;
      }

      public bool Clickable => _getUrl() != String.Empty;
      public string Text => _getText(false);
      public string Url => _getUrl();
      public string TooltipText
      {
         get
         {
            return !String.IsNullOrWhiteSpace(Url) ? Url : _getText(true);
         }
      }

      /// <summary>
      /// bool -- true for tooltip, false for list view
      /// </summary>
      private readonly Func<bool, string> _getText;
      private readonly Func<string> _getUrl;
   }
}

