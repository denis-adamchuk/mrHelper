﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;
using mrHelper.CommonNative;

namespace mrHelper.CommonControls.Tools
{
   public static class WinFormsHelpers
   {
      public static bool TestListViewHeaderHit(ListView listView, Point screenPosition)
      {
         int headerHeight = Win32Tools.GetListViewHeaderHeight(listView.Handle);
         return listView.PointToClient(screenPosition).Y <= headerHeight;
      }

      public static NativeMethods.NMHDR GetNotifyMessageHeader(Message msg)
      {
         return (NativeMethods.NMHDR)msg.GetLParam(typeof(NativeMethods.NMHDR));
      }

      public static NativeMethods.NMHEADER GetHeaderControlNotifyMessage(Message msg)
      {
         return (NativeMethods.NMHEADER)msg.GetLParam(typeof(NativeMethods.NMHEADER));
      }

      public static void SetListViewRowHeight(ListView listView, int maxLineCount)
      {
         // It is expected to use font size in pixels here
         int extraPixels = (int)WinFormsHelpers.ScalePixelsToNewDpi(96, listView.DeviceDpi, 2);
         int height = listView.Font.Height * maxLineCount + extraPixels;

         if (listView.SmallImageList != null
          && listView.SmallImageList.ImageSize.Height == height)
         {
            return;
         }

         listView.SmallImageList?.Dispose();

         ImageList imgList = new ImageList
         {
            ImageSize = new Size(1, height)
         };
         listView.SmallImageList = imgList;
      }

      public static int GetListViewRowHeight(ListView listView)
      {
         return listView?.SmallImageList?.ImageSize.Height ?? 0; 
      }

      public static void CloseAllFormsExceptOne(string exceptionalFormName)
      {
         for (int iForm = Application.OpenForms.Count - 1; iForm >= 0; --iForm)
         {
            Form form = Application.OpenForms[iForm];
            if (form.Name != exceptionalFormName)
            {
               form.Close();
            }
         }
      }

      public static void PositionDialogOnControl(Form dialog, Control control)
      {
         if (dialog == null)
         {
            throw new ArgumentException("dialog argument cannot be null");
         }
         if (control != null)
         {
            if (dialog.Parent == null)
            {
               dialog.StartPosition = FormStartPosition.Manual;
               Point controlLocationAtScreen = control.PointToScreen(new Point(0, 0));
               int x = controlLocationAtScreen.X + (control.Width - dialog.Width) / 2;
               int y = controlLocationAtScreen.Y + (control.Height - dialog.Height) / 2;
               Rectangle workingArea = Screen.GetWorkingArea(control);
               x = Math.Min(x, workingArea.Width  - dialog.Width);
               y = Math.Min(y, workingArea.Height - dialog.Height);
               dialog.Location = new Point(Math.Max(0, x), Math.Max(0, y));
            }
            else
            {
               Debug.Assert(false); // not implemented
            }
         }
      }

      public static DialogResult ShowDialogOnControl(Form dialog, Control control)
      {
         PositionDialogOnControl(dialog, control);
         return dialog.ShowDialog();
      }

      public static Form FindMainForm()
      {
         return FindFormByName("MainForm");
      }

      public static Form FindFormByName(string name)
      {
         return FindFormByTagAndName(name, obj => true);
      }

      public static Form FindFormByTagAndName(string name, Func<object, bool> doesMatchTag)
      {
         for (int iForm = Application.OpenForms.Count - 1; iForm >= 0; --iForm)
         {
            Form form = Application.OpenForms[iForm];
            if (form.Name == name && doesMatchTag(form.Tag))
            {
               return form;
            }
         }
         return null;
      }

      public static void FillComboBox<T>(ComboBox comboBox, T[] choices, Func<T, bool> isDefaultChoice)
      {
         int iSelectedIndex = choices.Any() ? 0 : -1;

         comboBox.Items.Clear();
         for (int iChoice = 0; iChoice < choices.Length; ++ iChoice)
         {
            comboBox.Items.Add(choices[iChoice]);
            if (isDefaultChoice(choices[iChoice]))
            {
               iSelectedIndex = iChoice;
            }
         }

         comboBox.SelectedIndex = iSelectedIndex;
      }

      public static void SelectComboBoxItem(ComboBox comboBox, Func<object, bool> predicate)
      {
         if (comboBox.Items.Count > 0)
         {
            int selectedIndex = 0;
            if (predicate != null)
            {
               object item = comboBox.Items.Cast<object>().FirstOrDefault(x => predicate(x));
               int defaultIndex = item == null ? -1 : comboBox.Items.IndexOf(item);
               if (defaultIndex != -1)
               {
                  selectedIndex = defaultIndex;
               }
            }
            comboBox.SelectedIndex = selectedIndex;
         }
      }

      public static void UncheckAllExceptOne(ToolStripMenuItem[] checkBoxGroup, ToolStripMenuItem checkBox)
      {
         if (checkBoxGroup.Contains(checkBox))
         {
            foreach (ToolStripMenuItem cb in checkBoxGroup)
            {
               if (cb != checkBox)
               {
                  cb.Checked = false;
                  cb.CheckOnClick = true;
               }
            }
         }
      }

      public static void UncheckAllExceptOne(ToolStripButton[] buttonGroup, ToolStripButton button)
      {
         if (buttonGroup.Contains(button))
         {
            foreach (ToolStripButton btn in buttonGroup)
            {
               if (btn != button)
               {
                  btn.Checked = false;
                  btn.CheckOnClick = true;
               }
            }
         }
      }

      public static Dictionary<string, int> GetListViewDisplayIndices(ListView listView)
      {
         Dictionary<string, int> columnIndices = new Dictionary<string, int>();
         foreach (ColumnHeader column in listView.Columns)
         {
            columnIndices[column.Tag.ToString()] = column.DisplayIndex;
         }
         return columnIndices;
      }

      /// <summary>
      /// Changes DisplayIndex properties of Column Headers of the given List View control
      /// in accordance with columnDisplayIndices argument.
      /// This function expects that columnDisplayIndices has indices for all list view columns.
      /// </summary>
      public static void ReorderListViewColumns(ListView listview, Dictionary<string, int> columnDisplayIndices)
      {
         // Remove indices from an auxiliary collection one-by-one
         List<int> indices = Enumerable.Range(0, listview.Columns.Count).ToList();
         foreach (KeyValuePair<string, int> column in columnDisplayIndices)
         {
            if (!indices.Remove(column.Value))
            {
               Trace.TraceWarning(String.Format(
                  "[WinFormsHelpers] columnDisplayIndices argument contains unexpected value {0}", column.Value));
            }
         }

         // If not all indices are removed, this means that not all indices are stored in Settings, let's discard them
         if (indices.Any())
         {
            throw new ArgumentException("Wrong value of columnDisplayIndices argument");
         }

         foreach (ColumnHeader column in listview.Columns)
         {
            string columnName = column.Tag.ToString();
            column.DisplayIndex = columnDisplayIndices[columnName];
         }
      }

      /// <summary>
      /// This function is needed because there is no way to catch a moment when list view's columns
      /// are reordered and can tell their new indices.
      /// ColumnReordered() event is called before list view column indices are changed.
      /// </summary>
      public static Dictionary<string, int> GetListViewDisplayIndicesOnColumnReordered(ListView listView,
         int oldDisplayIndex, int newDisplayIndex)
      {
         Debug.Assert(oldDisplayIndex != newDisplayIndex);

         bool moveForward = newDisplayIndex > oldDisplayIndex;
         Dictionary<string, int> columnsModified =
            listView
            .Columns
            .Cast<ColumnHeader>()
            .ToDictionary(
               item => item.Tag.ToString(),
               item =>
               {
                  if (moveForward && item.DisplayIndex > oldDisplayIndex && item.DisplayIndex <= newDisplayIndex)
                  {
                     return item.DisplayIndex - 1;
                  }
                  else if (!moveForward && item.DisplayIndex >= newDisplayIndex && item.DisplayIndex < oldDisplayIndex)
                  {
                     return item.DisplayIndex + 1;
                  }
                  else if (item.DisplayIndex == oldDisplayIndex)
                  {
                     return newDisplayIndex;
                  }
                  return item.DisplayIndex;
               });

         return columnsModified;
      }

      /// <summary>
      /// From https://stackoverflow.com/questions/28880301/listview-ownerdraw-with-allowcolumnreorder-dont-work-correct
      /// </summary>
      public static Rectangle GetFirstColumnCorrectRectangle(ListView listView, ListViewItem item)
      {
         for (int iColumn = 0; iColumn < listView.Columns.Count; ++iColumn)
         {
            if (listView.Columns[iColumn].DisplayIndex ==
                listView.Columns[0].DisplayIndex - 1)
            {
               return new Rectangle(item.SubItems[iColumn].Bounds.Right, item.SubItems[iColumn].Bounds.Y,
                                    listView.Columns[0].Width, item.SubItems[iColumn].Bounds.Height);
            }
         }
         Debug.Assert(false);
         return new Rectangle();
      }

      public static IEnumerable<Control> GetAllSubControls(Control container)
      {
         List<Control> controlList = new List<Control>();
         foreach (Control control in container.Controls)
         {
            controlList.AddRange(GetAllSubControls(control));
            controlList.Add(control);
         }
         return controlList;
      }

      public static void FixNonStandardDPIIssue(Control control, float designTimeFontSize, int designTimeDPI = 96)
      {
         // Sometimes Windows DPI behavior is strange when changed to non-default (and even back)
         // without signing out - windows got scaled incorrectly but after signing out they work ok.
         // There is a workaround for it.
         // Component positions are defined at design-time with DPI 96 and when ResumeLayout occurs within
         // InitializeComponent(), .NET checks CurrentAutoScaleDimensions to figure out a scale factor.
         // CurrentAutoScaleDimensions depends on the current font and we need to set it explicitly in advance.
         // This font has to be scaled in accordance with current DPI what gives a proper scale factor for
         // ResumeLayout().

         float currentDPI = control.DeviceDpi;
         if (Convert.ToInt32(currentDPI) == designTimeDPI)
         {
            return;
         }

         float newEmSize = designTimeFontSize * (designTimeDPI / currentDPI);
         float oldEmSize = control.Font.Size;

         control.Font = new System.Drawing.Font(control.Font.FontFamily, newEmSize,
            control.Font.Style, System.Drawing.GraphicsUnit.Point, control.Font.GdiCharSet, control.Font.GdiVerticalFont);

         Trace.TraceInformation(String.Format(
            "[{0}] FixNonStandardDPIIssue(): Current DPI = {1}. Old font emSize = {2}. New font emSize = {3}. "
          + "Design-time: Font-Size: {4}, DPI: {5}",
            control.ToString(), currentDPI, oldEmSize, newEmSize, designTimeFontSize, designTimeDPI));
      }

      public static double ScalePixelsToNewDpi(int oldDpi, int newDpi, double pixels) => pixels * newDpi / (double)oldDpi;

      public static double GetFontSizeInPoints(Control control) => control.Font.SizeInPoints;

      public static double GetFontSizeInPixels(Control control) =>
         GetFontSizeInPoints(control) * control.DeviceDpi / 72.0;

      public static void LogScaleDimensions(ContainerControl control)
      {
         Trace.TraceInformation(String.Format(
            "[{0}] CurrentAutoScaleDimensions = {1}/{2}, AutoScaleDimensions = {3}/{4}",
            control.ToString(),
            control.CurrentAutoScaleDimensions.Width, control.CurrentAutoScaleDimensions.Height,
            control.AutoScaleDimensions.Width, control.AutoScaleDimensions.Height));
      }

      public static void LogScreenResolution(Control control)
      {
         Trace.TraceInformation(String.Format(
            "[{0}] Screen resolution: {1}x{2}",
            control.ToString(),
            Screen.GetWorkingArea(control).Width, Screen.GetWorkingArea(control).Height));
      }

      private static readonly SizeF DefaultDesignTimeDimensions = new SizeF(6F, 13F);

      // Compute AutoScaleDimensions change comparing to Design-time settings
      public static SizeF GetAutoScaleDimensionsChangeRate(ContainerControl control,
         float designTimeDimensionWidth = 6F, float designTimeDimensionHeight = 13F)
      {
         return new SizeF(control.CurrentAutoScaleDimensions.Width / designTimeDimensionWidth,
                          control.CurrentAutoScaleDimensions.Height / designTimeDimensionHeight);
      }

      public static bool ShowConfirmationDialog(string confirmationText)
      {
         return MessageBox.Show(confirmationText, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
      }

      private static Graphics GetGraphics(DrawItemEventArgs e) => e.Graphics;
      private static Graphics GetGraphics(DrawListViewItemEventArgs e) => e.Graphics;
      private static Graphics GetGraphics(DrawListViewSubItemEventArgs e) => e.Graphics;
      private static Graphics GetGraphics(DrawListViewColumnHeaderEventArgs e) => e.Graphics;

      public static void FillRectangle<T>(T e, Rectangle bounds, Color backColor)
      {
         using (Brush brush = new SolidBrush(backColor))
         {
            GetGraphics((dynamic)e).FillRectangle(brush, bounds);
         }
      }

      public static void SetOverlayEllipseIcon(Color? color)
      {
         if (Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero)
         {
            if (!color.HasValue)
            {
               TaskbarManager.Instance.SetOverlayIcon(null, String.Empty);
               return;
            }

            // Took an idea from
            // https://github.com/gitextensions/gitextensions/blob/master/GitUI/CommandsDialogs/FormBrowse.cs#L376
            const int imgDim = 32;
            const int dotDim = 24;
            const int pad = 2;
            using (Bitmap bmp = DrawEllipse(imgDim, dotDim, pad, color.Value))
            {
               using (Icon overlay = Icon.FromHandle(bmp.GetHicon()))
               {
                  TaskbarManager.Instance.SetOverlayIcon(overlay, String.Empty);
               }
            }
         }
      }

      public static Bitmap DrawEllipse(int imgDim, int dotDim, int pad, Color c)
      {
         Bitmap bitmap = new Bitmap(imgDim, imgDim);
         using (Graphics g = Graphics.FromImage(bitmap))
         {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using (SolidBrush brush = new SolidBrush(c))
            {
               g.FillEllipse(brush, new Rectangle(imgDim - dotDim - pad, imgDim - dotDim - pad, dotDim, dotDim));
            }
         }
         return bitmap;
      }

      public static void DrawClippedCircleImage(Graphics g, Image image, Rectangle imageRect)
      {
         if (image == null)
         {
            return;
         }

         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

         using (GraphicsPath path = new GraphicsPath())
         {
            path.AddEllipse(imageRect);
            g.SetClip(path);
         }
         g.DrawImage(image, imageRect);
         g.ResetClip();

         g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
      }

      // inspired by https://stackoverflow.com/a/47205281
      public static Image ClipRectToCircle(Image srcImage, Color backGround, int dpi)
      {
         float horizontalRate = dpi / srcImage.HorizontalResolution;
         float verticalRate = dpi / srcImage.VerticalResolution;

         int srcImageWidthScaled = (int)(srcImage.Width * horizontalRate);
         int srcImageHeightScaled = (int)(srcImage.Height * verticalRate);

         int dstImageWidth = Math.Min(srcImageWidthScaled, srcImageHeightScaled);
         int dstImageHeight = dstImageWidth;

         Bitmap dstImage = new Bitmap(dstImageWidth, dstImageHeight, srcImage.PixelFormat);

         using (Graphics g = Graphics.FromImage(dstImage))
         {
            // fills background color
            using (Brush brush = new SolidBrush(backGround))
            {
               g.FillRectangle(brush, 0, 0, dstImage.Width, dstImage.Height);
            }

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // adds the new ellipse & draws the image again 
            using (GraphicsPath path = new GraphicsPath())
            {
               PointF center = new PointF((float)dstImage.Width / 2, (float)dstImage.Height / 2);
               float radius = (float)dstImage.Width / 2;

               RectangleF r = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
               path.AddEllipse(r);
               g.SetClip(path);

               int imgX = (int)(dstImageWidth - srcImageWidthScaled) / 2;
               int imgY = (int)(dstImageHeight - srcImageHeightScaled) / 2;
               g.DrawImageUnscaled(srcImage, imgX, imgY);
            }

            return dstImage;
         }
      }

      // inspired by https://stackoverflow.com/a/32991419
      private static GraphicsPath getRoundRectagle(Rectangle bounds, int radius, bool isHScrollVisible)
      {
         GraphicsPath path = new GraphicsPath();
         if (!isHScrollVisible)
         {
            path.StartFigure();
            path.AddArc(bounds.Left, bounds.Top, radius, radius, 180, 90);
            path.AddArc(bounds.Right - radius, bounds.Top, radius, radius, 270, 90);
            path.AddArc(bounds.Right - radius, bounds.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
         }
         else
         {
            int hScrollHeight = SystemInformation.HorizontalScrollBarHeight;
            Point topLeft = new Point(bounds.Left, bounds.Top);
            Point bottomLeft = new Point(bounds.Left, bounds.Bottom + hScrollHeight);
            Point bottomRight = new Point(bounds.Right, bounds.Bottom + hScrollHeight);
            Point topRight = new Point(bounds.Right, bounds.Top);
            Point topRight2 = new Point(topRight.X, topRight.Y + radius);
            Point topLeft3 = new Point(topLeft.X + radius, topLeft.Y);
            Point topRight3 = new Point(topRight.X - radius, topRight.Y);

            path.AddArc(topLeft.X, topLeft.Y, radius, radius, 180, 90);
            path.AddLine(topLeft3, topRight3);
            path.AddArc(topRight3.X, topRight3.Y, radius, radius, 270, 90);
            path.AddLine(topRight2, bottomRight);
            path.AddLine(bottomRight, bottomLeft);
            path.CloseFigure();

         }
         return path;
      }

      public static GraphicsPath GetRoundedPath(Rectangle bounds, int radius, bool isHScrollVisible)
      {
         return getRoundRectagle(bounds, radius, isHScrollVisible);
      }

      public class BorderlessRenderer : ToolStripProfessionalRenderer
      {
         public BorderlessRenderer()
         {
            // Remove extra padding
            RoundedEdges = false;
         }

         protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
         {
            // Suppress base class functionality to not draw borders
            // base.OnRenderToolStripBorder(e);
         }
      }

      public static void PerformClick(Tuple<Button, bool>[] buttonsToClick)
      {
         foreach (Tuple<Button, bool> button in buttonsToClick)
         {
            if (button.Item2)
            {
               button.Item1.PerformClick();
            }
         }
      }

      public static void ConvertMouseWheelToClick(Button buttonOnScrollDown, Button buttonOnScrollUp, int delta)
      {
         IEnumerable<Tuple<Button, bool>> clickMap = Enumerable.Zip(
            new Button[]{ buttonOnScrollDown, buttonOnScrollUp},
            new bool[]{ delta < 0, delta > 0},
            (arg1, arg2) => new Tuple<Button, bool>(arg1, arg2));
         WinFormsHelpers.PerformClick(clickMap.ToArray());
      }

      public static Bitmap ReplaceColorInBitmap(
         Bitmap sourceImage, Color sourceColor, Color destColor)
      {
         Bitmap image = (Bitmap)sourceImage.Clone();
         for (int i = 0; i < image.Width; ++i)
         {
            for (int j = 0; j < image.Height; ++j)
            {
               var cl = image.GetPixel(i, j);
               if (cl.R == sourceColor.R && cl.G == sourceColor.G && cl.B == sourceColor.B)
               {
                  image.SetPixel(i, j, destColor);
               }
            }
         }
         return image;
      }

      // taken from https://stackoverflow.com/a/57254324
      public static Icon ConvertToIco(Image img, int size)
      {
         Icon icon;
         using (var msImg = new System.IO.MemoryStream())
         using (var msIco = new System.IO.MemoryStream())
         {
            img.Save(msImg, System.Drawing.Imaging.ImageFormat.Png);
            using (var bw = new System.IO.BinaryWriter(msIco))
            {
               bw.Write((short)0);           //0-1 reserved
               bw.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
               bw.Write((short)1);           //4-5 number of images
               bw.Write((byte)size);         //6 image width
               bw.Write((byte)size);         //7 image height
               bw.Write((byte)0);            //8 number of colors
               bw.Write((byte)0);            //9 reserved
               bw.Write((short)0);           //10-11 color planes
               bw.Write((short)32);          //12-13 bits per pixel
               bw.Write((int)msImg.Length);  //14-17 size of image data
               bw.Write(22);                 //18-21 offset of image data
               bw.Write(msImg.ToArray());    // write image data
               bw.Flush();
               bw.Seek(0, SeekOrigin.Begin);
               icon = new Icon(msIco);
            }
         }
         return icon;
      }

      public static bool IsLightThemeUsed()
      {
         try
         {
            RegistryHive hive = RegistryHive.CurrentUser;
            RegistryView view = RegistryView.Registry32;
            RegistryKey hklm = RegistryKey.OpenBaseKey(hive, view);
            RegistryKey personalizeKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (personalizeKey != null)
            {
               string valueName = "SystemUsesLightTheme";
               object value = personalizeKey.GetValue(valueName);
               if (value != null)
               {
                  RegistryValueKind valueKind = personalizeKey.GetValueKind(valueName);
                  if (valueKind == RegistryValueKind.DWord)
                  {
                     return (int)value == 1;
                  }
               }
            }
         }
         catch (Exception ex) // Any exception from Registry API
         {
            Trace.TraceError(
               "[WinFormsHelper.IsLightThemeUsed] An exception occurred on attempt to access the registry: {0}",
               ex.ToString());
         }
         return false;
      }
   }
}

