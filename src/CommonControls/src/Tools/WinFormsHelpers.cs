﻿using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace mrHelper.CommonControls.Tools
{
   public static class WinFormsHelpers
   {
      public static void CloseAllFormsExceptOne(Form exceptionalForm)
      {
         for (int iForm = Application.OpenForms.Count - 1; iForm >= 0; --iForm)
         {
            Form form = Application.OpenForms[iForm];
            if (form != exceptionalForm)
            {
               form.Close();
            }
         }
      }

      public static Form FindFormByTag(string name, Func<object, bool> doesMatchTag)
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
               object item = comboBox.Items.Cast<object>().SingleOrDefault(x => predicate(x));
               int defaultIndex = item == null ? -1 : comboBox.Items.IndexOf(item);
               if (defaultIndex != -1)
               {
                  selectedIndex = defaultIndex;
               }
            }
            comboBox.SelectedIndex = selectedIndex;
         }
      }

      public static Dictionary<string, int> GetListViewDisplayIndices(ListView listView)
      {
         Dictionary<string, int> columnIndices = new Dictionary<string, int>();
         foreach (ColumnHeader column in listView.Columns)
         {
            columnIndices[(string)column.Tag] = column.DisplayIndex;
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
            string columnName = (string)column.Tag;
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
               item => (string)item.Tag,
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
         // Component positions are defined at design-time with DPI 96 and when ResumeLauout occurs within
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

      /// <summary>
      /// Convert font size in points to font size in pixels.
      /// Note - control.Font.Height gives another value which is not ok for CSS font-size
      /// </summary>
      public static double GetFontSizeInPixels(Control control)
      {
         return control.Font.SizeInPoints * 96.0 / 72.0;
      }

      public static void LogScaleDimensions(ContainerControl control)
      {
         Trace.TraceInformation(String.Format(
            "[{0}] CurrentAutoScaleDimensions = {1}/{2}, AutoScaleDimensions = {3}/{4}",
            control.ToString(),
            control.CurrentAutoScaleDimensions.Width, control.CurrentAutoScaleDimensions.Height,
            control.AutoScaleDimensions.Width, control.AutoScaleDimensions.Height));
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
      private static Graphics GetGraphics(DrawListViewSubItemEventArgs e) => e.Graphics;

      public static void FillRectangle<T>(T e, Rectangle bounds, Color backColor, bool isSelected)
      {
         if (isSelected)
         {
            GetGraphics((dynamic)e).FillRectangle(SystemBrushes.Highlight, bounds);
         }
         else
         {
            using (Brush brush = new SolidBrush(backColor))
            {
               GetGraphics((dynamic)e).FillRectangle(brush, bounds);
            }
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
            using (Bitmap bmp = new Bitmap(imgDim, imgDim))
            {
               using (Graphics g = Graphics.FromImage(bmp))
               {
                  g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                  g.Clear(Color.Transparent);
                  using (SolidBrush brush = new SolidBrush(color.Value))
                  {
                     g.FillEllipse(brush, new Rectangle(imgDim - dotDim - pad, imgDim - dotDim - pad, dotDim, dotDim));
                  }
               }

               using (Icon overlay = Icon.FromHandle(bmp.GetHicon()))
               {
                  TaskbarManager.Instance.SetOverlayIcon(overlay, String.Empty);
               }
            }
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
         catch (Exception ex)
         {
            Trace.TraceError(
               "[WinFormsHelper.IsLightThemeUsed] An exception occurred on attempt to access the registry: {0}",
               ex.ToString());
         }
         return false;
      }
   }
}

