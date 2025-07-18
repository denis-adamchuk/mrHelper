using System;

namespace mrHelper.App.Controls
{
   public enum ColumnType
   {
      IId,
      Color,
      Avatar,
      Author,
      Title,
      Labels,
      Size,
      Jira,
      PlannedTime,
      TotalTime,
      SourceBranch,
      TargetBranch,
      State,
      Resolved,
      RefreshTime,
      Activities,
      Project
   }

   public class MergeRequestListViewSubItemInfo
   {
      public MergeRequestListViewSubItemInfo(Func<bool, string> getText, Func<string> getUrl, ColumnType columnType)
      {
         _getText = getText;
         _getUrl = getUrl;
         ColumnType = columnType;
      }

      public bool Clickable => Url != String.Empty;
      public string Text => _getText?.Invoke(false) ?? String.Empty;
      public string Url => _getUrl?.Invoke() ?? String.Empty;
      public string TooltipText => !String.IsNullOrWhiteSpace(Url) ? Url : (_getText?.Invoke(true) ?? String.Empty);
      public ColumnType ColumnType { get; }

      /// <summary>
      /// bool -- true for tooltip, false for list view
      /// </summary>
      private readonly Func<bool, string> _getText;
      private readonly Func<string> _getUrl;
   }
}

