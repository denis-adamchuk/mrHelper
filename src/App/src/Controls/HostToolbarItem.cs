using System;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal class HostToolbarItem : ToolStripButton
   {
      public HostToolbarItem(string hostname)
         : base(StringUtils.GetHostWithoutPrefix(hostname), null)
      {
         ToolTipText = String.Format("Show merge requests for {0}", hostname);
         HostName = hostname;
         updateIcon(null);
      }

      internal string HostName { get; }

      internal void UpdateIcon(Color? summaryColor)
      {
         if (summaryColor == _summaryColor)
         {
            return;
         }

         updateIcon(summaryColor);
      }

      private void updateIcon(Color? summaryColor)
      {
         Image = summaryColor.HasValue
            ? WinFormsHelpers.DrawEllipse(32, 24, 2, summaryColor.Value)
            : new Bitmap(24, 24);
         ImageScaling = summaryColor.HasValue
            ? ToolStripItemImageScaling.SizeToFit
            : ToolStripItemImageScaling.None;
         TextImageRelation = summaryColor.HasValue
            ? TextImageRelation.ImageBeforeText
            : TextImageRelation.Overlay;

         _summaryColor = summaryColor;
      }

      private Color? _summaryColor;
   }
}

