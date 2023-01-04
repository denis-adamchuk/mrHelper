using System.Linq;

namespace mrHelper.App.Controls
{
   partial class DiscussionPanel
   {
      /// <summary> 
      /// Required designer variable.
      /// </summary>
      private System.ComponentModel.IContainer components = null;

      /// <summary> 
      /// Clean up any resources being used.
      /// </summary>
      /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
      protected override void Dispose(bool disposing)
      {
         if (disposing && (components != null))
         {
            components.Dispose();
         }

         _discussionSort.SortStateChanged -= onSortStateChanged;

         _searchFilter.FilterStateChanged -= onSearchFilterChanged;
         _pageFilter.FilterStateChanged -= onPageFilterChanged;
         _displayFilter.FilterStateChanged -= onDisplayFilterChanged;

         _discussionLoader.Loaded -= onDiscussionsLoaded;

         _discussionLayout.DiffContextPositionChanged -= onDiffContextPositionChanged;
         _discussionLayout.DiscussionColumnWidthChanged -= onDiscussionColumnWidthChanged;
         _discussionLayout.NeedShiftRepliesChanged -= onNeedShiftRepliesChanged;

         _htmlTooltip.Dispose();

         _redrawTimer.Tick -= onRedrawTimer;
         _redrawTimer.Stop();
         _redrawTimer.Dispose();

         _popupWindow.Dispose();

         _pathCache.Dispose();

         base.Dispose(disposing);
      }

      #region Component Designer generated code

      /// <summary> 
      /// Required method for Designer support - do not modify 
      /// the contents of this method with the code editor.
      /// </summary>
      private void InitializeComponent()
      {
         this.SuspendLayout();
         // 
         // DiscussionPanel
         // 
         this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
         this.AutoScroll = true;
         this.AutoScrollMargin = new System.Drawing.Size(0, 200);
         this.Name = "DiscussionPanel";
         this.Size = new System.Drawing.Size(368, 355);
         this.ResumeLayout(false);

      }

      #endregion
   }
}
