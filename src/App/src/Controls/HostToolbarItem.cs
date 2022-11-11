using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using mrHelper.Common.Tools;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal class HostToolbarItem : ToolStripButton
   {
      public HostToolbarItem(string hostname, Form deviceDpiHolder)
         : base(StringUtils.GetHostWithoutPrefix(hostname), null)
      {
         ToolTipText = String.Format("Show merge requests for {0}", hostname);
         HostName = hostname;

         _deviceDpiHolder = deviceDpiHolder;
         _deviceDpiHolder.DpiChanged += _deviceDpiHolder_DpiChanged;

         _summaryColor = null;
         updateIcon();
      }

      public new void Dispose()
      {
         base.Dispose();
         _deviceDpiHolder.DpiChanged -= _deviceDpiHolder_DpiChanged;
      }

      internal string HostName { get; }

      internal void UpdateIcon(Color? summaryColor)
      {
         if (summaryColor == _summaryColor)
         {
            return;
         }

         _summaryColor = summaryColor;
         updateIcon();
      }

      private void updateIcon()
      {
         Image?.Dispose();
         Debug.Assert(ImageScaling == ToolStripItemImageScaling.SizeToFit);

         int deviceDpi = getDeviceDpi();
         if (_summaryColor.HasValue)
         {
            int imageSize = (int)WinFormsHelpers.ScalePixelsToNewDpi(96, deviceDpi, DefaultToolbarImageSize.Width);
            int ellipseSize = (int)(imageSize * 0.70); // imageSize - 30%
            int padding = (int)WinFormsHelpers.ScalePixelsToNewDpi(96, deviceDpi, 2); // 2px by default
            Image = WinFormsHelpers.DrawEllipse(imageSize, ellipseSize, padding, _summaryColor.Value);
            TextImageRelation = TextImageRelation.ImageBeforeText;
         }
         else
         {
            // Image size automatically upscales to parent's ImageScalingSize but let's set it to some default value
            Image = new Bitmap(DefaultToolbarImageSize.Width, DefaultToolbarImageSize.Height);
            TextImageRelation = TextImageRelation.Overlay;
         }
      }

      private void _deviceDpiHolder_DpiChanged(object sender, DpiChangedEventArgs e) => updateIcon();

      private int getDeviceDpi() => _deviceDpiHolder.DeviceDpi;

      private static readonly Size DefaultToolbarImageSize = new Size(32, 32);

      private Color? _summaryColor;
      private Form _deviceDpiHolder;
   }
}

