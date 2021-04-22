using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal partial class ConfigureColorsForm : CustomFontForm
   {
      public ConfigureColorsForm(IconCache iconCache,
         Action updateTrayAndTaskBar,
         Action<ColorScheme> onColorSchemeChange)
      {
         _iconCache = iconCache;
         _updateTrayAndTaskBar = updateTrayAndTaskBar;
         _onColorSchemeChange = onColorSchemeChange;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();
         applyFont(Program.Settings.MainWindowFontSizeName);

         fillColorList();
         fillColorSchemeList();
         selectCurrentColorScheme();
      }

      private void comboBoxColorSchemes_SelectedIndexChanged(object sender, EventArgs e)
      {
         applyColorSchemeChange((sender as ComboBox).Text);
      }

      private void listBoxColorSchemeItemSelector_Format(object sender, ListControlConvertEventArgs e)
      {
         formatColorSchemeItemSelectorItem(e);
      }

      private void listBoxColorSchemeItemSelector_SelectedIndexChanged(object sender, EventArgs e)
      {
         object selectedItem = (sender as ListBox).SelectedItem;
         if (selectedItem != null)
         {
            onListBoxColorSelected(selectedItem as string);
         }
      }

      private void comboBoxColorSelector_SelectedIndexChanged(object sender, EventArgs e)
      {
         if ((sender as ComboBox).SelectedItem is ColorSelectorComboBoxItem selectedItem)
         {
            onComboBoxColorSelected(selectedItem.Color);
         }
      }

      private void listBoxColorSchemeItemSelector_DrawItem(object sender, DrawItemEventArgs e)
      {
         onDrawListBoxColorSchemeItemSelectorItem(e);
      }

      private void listBoxColorSchemeItemSelector_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         onMeasureListBoxColorSchemeItemSelectorItem(e);
      }

      private void comboBoxColorSelector_DrawItem(object sender, DrawItemEventArgs e)
      {
         onDrawComboBoxColorSelectorItem(e);
      }

      private void linkLabelResetAllColors_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeToFactoryValues();
         onResetColorSchemeItemToFactoryValue();
      }

      private void linkLabelResetToFactoryValue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeItemToFactoryValue();
      }

      private void applyColorSchemeChange(string colorSchemeName)
      {
         initializeColorScheme();
         if (Program.Settings.ColorSchemeFileName != colorSchemeName)
         {
            Program.Settings.ColorSchemeFileName = colorSchemeName;
         }
         fillColorSchemeItemList();
      }

      private void fillColorSchemeList()
      {
         string defaultFileName = Constants.DefaultColorSchemeFileName;
         string defaultFilePath = Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

         comboBoxColorSchemes.Items.Clear();

         string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
         if (files.Contains(defaultFilePath))
         {
            // put Default scheme first in the list
            comboBoxColorSchemes.Items.Add(defaultFileName);
         }

         foreach (string file in files)
         {
            if (file.EndsWith(Constants.ColorSchemeFileNamePrefix))
            {
               string scheme = Path.GetFileName(file);
               if (file != defaultFilePath)
               {
                  comboBoxColorSchemes.Items.Add(scheme);
               }
            }
         }
      }

      private void fillColorList()
      {
         comboBoxColorSelector.Items.Clear();
         Constants.ColorSchemeKnownColorNames
            .ToList()
            .ForEach(humanFriendlyName =>
            {
               string colorName = humanFriendlyName.Replace(" ", "");
               Color color = Color.FromName(colorName);
               if (color.A != 0 || color.R != 0 || color.G != 0 || color.B != 0)
               {
                  addColorToList(color, humanFriendlyName);
                  addIconToCache(color);
               }
               else
               {
                  Trace.TraceWarning("[ConfigureColorsForm] Cannot create a Color from name {0}", colorName);
               }
            });
      }

      private void fillColorSchemeItemList()
      {
         listBoxColorSchemeItemSelector.Items.Clear();
         _colorScheme.GetColors("MergeRequests")
            .Concat(_colorScheme.GetColors("Status"))
            .ToList()
            .ForEach(colorSchemeItem =>
            {
               addColorToList(colorSchemeItem.Color);
               addIconToCache(colorSchemeItem.Color);
               listBoxColorSchemeItemSelector.Items.Add(colorSchemeItem.Name);
            });

         if (listBoxColorSchemeItemSelector.Items.Count > 0)
         {
            listBoxColorSchemeItemSelector.SelectedIndex = 0;
         }
      }

      private void addColorToList(Color color, string humanFriendlyName = null)
      {
         if (comboBoxColorSelector.Items
            .Cast<ColorSelectorComboBoxItem>()
            .Select(item => item.Color)
            .Any(itemColor => itemColor.Equals(color)))
         {
            return;
         }

         string colorName = String.IsNullOrEmpty(humanFriendlyName)
            ? color.IsNamedColor ? color.Name : "Custom"
            : humanFriendlyName;
         comboBoxColorSelector.Items.Add(new ColorSelectorComboBoxItem(colorName, color));
      }

      private void addIconToCache(Color color)
      {
         if (_iconCache.ContainsKey(color))
         {
            return;
         }

         Bitmap imageWithoutBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16, Color.Green, color);
         Icon iconWithoutBorder = WinFormsHelpers.ConvertToIco(imageWithoutBorder, 16);

         Bitmap imageWithBorder = WinFormsHelpers.ReplaceColorInBitmap(
            Properties.Resources.gitlab_icon_stub_16x16_border, Color.Green, color);
         Icon iconWithBorder = WinFormsHelpers.ConvertToIco(imageWithBorder, 16);

         _iconCache.Add(color, new IconCache.IconGroup(iconWithoutBorder, iconWithBorder));
      }

      private void selectCurrentColorScheme()
      {
         string selectedScheme = comboBoxColorSchemes.Items
            .Cast<string>()
            .FirstOrDefault(scheme => scheme == Program.Settings.ColorSchemeFileName);
         if (selectedScheme != null)
         {
            comboBoxColorSchemes.SelectedItem = selectedScheme;
         }
         else if (comboBoxColorSchemes.Items.Count > 0)
         {
            comboBoxColorSchemes.SelectedIndex = 0;
         }
      }

      bool createColorScheme(string filename)
      {
         string filepath = Path.Combine(Directory.GetCurrentDirectory(), filename);
         try
         {
            _colorScheme = new ColorScheme(filepath);
            return true;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create a color scheme", ex);
         }
         return false;
      }

      private void initializeColorScheme()
      {
         if (comboBoxColorSchemes.SelectedIndex < 0 || comboBoxColorSchemes.Items.Count < 1)
         {
            // nothing is selected or list is empty, create an empty scheme
            _colorScheme = new ColorScheme();
         }

         // try to create a scheme for the selected item
         else if (!createColorScheme(comboBoxColorSchemes.Text))
         {
            Trace.TraceError("[ConfigureColorsForm] Cannot initialize color scheme {0}", comboBoxColorSchemes.Text);
            if (comboBoxColorSchemes.SelectedIndex > 0)
            {
               comboBoxColorSchemes.SelectedIndex = 0;
            }
            else
            {
               _colorScheme = new ColorScheme();
            }
         }

         _onColorSchemeChange?.Invoke(_colorScheme);
      }

      private void onListBoxColorSelected(string colorSchemeItemName)
      {
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            Trace.TraceError(
               "[ConfigureColorsForm] Cannot find color scheme item {0} in the color scheme",
               colorSchemeItemName);
            return;
         }
         selectComboBoxColor(colorSchemeItem.Color);
      }

      private void onResetColorSchemeToFactoryValues()
      {
         Program.Settings.CustomColors = new Dictionary<string, string>();

         listBoxColorSchemeItemSelector.Refresh();
         updateResetSchemeToFactoryValuesLinkLabelVisibility();
         updateResetToFactoryValueLinkLabelVisibility();
         _updateTrayAndTaskBar?.Invoke();
      }

      private void onResetColorSchemeItemToFactoryValue()
      {
         string colorSchemeItemName = listBoxColorSchemeItemSelector.SelectedItem.ToString();
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            Trace.TraceError(
               "[ConfigureColorsForm] Cannot find color scheme item {0} in the color scheme",
               colorSchemeItemName);
            return;
         }
         selectComboBoxColor(colorSchemeItem.FactoryColor);
      }

      private void selectComboBoxColor(Color color)
      {
         comboBoxColorSelector.SelectedItem = null;
         ColorSelectorComboBoxItem comboBoxItem = comboBoxColorSelector.Items
            .Cast<ColorSelectorComboBoxItem>()
            .FirstOrDefault(item => item.Color.Equals(color));
         if (comboBoxItem == null)
         {
            Debug.Assert(false);
            Trace.TraceError(
               "[ConfigureColorsForm] Cannot find color {0} in comboBoxColorSelector",
               color.ToString());
            return;
         }
         comboBoxColorSelector.SelectedItem = comboBoxItem;
      }

      private void onComboBoxColorSelected(Color color)
      {
         object selectedItem = listBoxColorSchemeItemSelector.SelectedItem;
         if (selectedItem == null)
         {
            return;
         }

         string colorSchemeItemName = selectedItem.ToString();
         setColorForColorSchemeItem(colorSchemeItemName, color);

         listBoxColorSchemeItemSelector.Refresh();
         updateResetSchemeToFactoryValuesLinkLabelVisibility();
         updateResetToFactoryValueLinkLabelVisibility();
         _updateTrayAndTaskBar?.Invoke();
      }

      private void setColorForColorSchemeItem(string colorSchemeItemName, Color color)
      {
         ColorSchemeItem colorSchemeItem = colorSchemeItemName != null
            ? _colorScheme.GetColor(colorSchemeItemName) : null;
         if (colorSchemeItem != null && !color.Equals(colorSchemeItem.Color))
         {
            Dictionary<string, string> dict = Program.Settings.CustomColors;
            if (colorSchemeItem.FactoryColor.Equals(color))
            {
               dict.Remove(colorSchemeItem.Name);
            }
            else
            {
               string colorAsText = color.IsNamedColor
                  ? color.Name : String.Format("{0},{1},{2},{3}", color.A, color.R, color.G, color.B);
               dict[colorSchemeItem.Name] = colorAsText;
            }
            Program.Settings.CustomColors = dict;
         }
      }

      private void updateResetSchemeToFactoryValuesLinkLabelVisibility()
      {
         linkLabelResetAllColors.Visible = listBoxColorSchemeItemSelector.Items
            .Cast<string>()
            .Any(itemName => !isColorSchemeItemHasFactoryValue(itemName));
      }

      private void updateResetToFactoryValueLinkLabelVisibility()
      {
         object selectedItem = listBoxColorSchemeItemSelector.SelectedItem;
         if (selectedItem == null)
         {
            return;
         }
         linkLabelResetToFactoryValue.Visible = !isColorSchemeItemHasFactoryValue(selectedItem.ToString());
      }

      private bool isColorSchemeItemHasFactoryValue(string colorSchemeItemName)
      {
         ColorSchemeItem updatedColorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         return updatedColorSchemeItem == null
             || updatedColorSchemeItem.Color.Name == updatedColorSchemeItem.FactoryColor.Name;
      }

      private void onDrawListBoxColorSchemeItemSelectorItem(DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         string colorSchemeItemName = listBoxColorSchemeItemSelector.Items[e.Index].ToString();
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            return;
         }

         Color color = colorSchemeItem.Color;
         bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
         WinFormsHelpers.FillRectangle(e, e.Bounds, color, isSelected);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         string text = colorSchemeItem.DisplayName;
         Font font = listBoxColorSchemeItemSelector.Font;
         if (isSelected)
         {
            using (Brush brush = new SolidBrush(color))
            {
               e.Graphics.DrawString(text, font, brush, e.Bounds, format);
            }
         }
         else
         {
            e.Graphics.DrawString(text, font, SystemBrushes.ControlText, e.Bounds, format);
         }
      }

      private void onMeasureListBoxColorSchemeItemSelectorItem(MeasureItemEventArgs e)
      {
         if (e.Index >= 0)
         {
            e.ItemHeight = listBoxColorSchemeItemSelector.Font.Height + 2;
         }
      }

      private void onDrawComboBoxColorSelectorItem(DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         e.DrawBackground();
         e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

         int iconMargin = 5;
         int iconSize = e.Bounds.Height; // draw square icon
         ColorSelectorComboBoxItem item = (ColorSelectorComboBoxItem)(comboBoxColorSelector.Items[e.Index]);
         Icon icon = _iconCache.Get(item.Color);
         if (icon != null)
         {
            Rectangle iconRect = new Rectangle(e.Bounds.X + iconMargin, e.Bounds.Y, iconSize, iconSize);
            e.Graphics.DrawIcon(icon, iconRect);
         }

         int iconTextMargin = 5;
         Rectangle textRect = new Rectangle(
            e.Bounds.X + iconMargin + iconSize + iconTextMargin, e.Bounds.Y,
            e.Bounds.Width - iconMargin - iconSize - iconTextMargin, e.Bounds.Height);
         bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
         Brush textBrush = isSelected ? SystemBrushes.HighlightText : SystemBrushes.ControlText;
         e.Graphics.DrawString(item.HumanFriendlyName, comboBoxColorSelector.Font, textBrush, textRect);
      }

      private void formatColorSchemeItemSelectorItem(ListControlConvertEventArgs e)
      {
         ColorSchemeItem colorSchemeItem = _colorScheme.GetColor(e.ListItem.ToString());
         e.Value = colorSchemeItem == null ? e.ListItem as string : colorSchemeItem.DisplayName;
      }

      private readonly IconCache _iconCache;
      private readonly Action _updateTrayAndTaskBar;
      private readonly Action<ColorScheme> _onColorSchemeChange;

      private class ColorSelectorComboBoxItem
      {
         internal ColorSelectorComboBoxItem(string humanFriendlyName, Color color)
         {
            HumanFriendlyName = humanFriendlyName;
            Color = color;
         }

         /// <summary>
         /// ToString() override for ComboBox item sorting purpose
         /// </summary>
         public override string ToString()
         {
            return HumanFriendlyName;
         }

         internal string HumanFriendlyName { get; }

         internal Color Color { get; }
      }

      private ColorScheme _colorScheme;
   }
}
