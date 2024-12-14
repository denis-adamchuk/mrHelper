using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mrHelper.App.Helpers;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Controls
{
   internal partial class ColorGroupConfigPage : UserControl
   {
      internal ColorGroupConfigPage(Action onSelected, IEnumerable<string> groupNames)
      {
         InitializeComponent();
         _onSelected = onSelected;
         _groupNames = groupNames.ToArray();
      }

      internal void Fill()
      {
         fillColorSchemeItemList(_groupNames);
      }

      internal void Reset()
      {
         onResetColorSchemeItemToFactoryValue(ColorType.Background);
         onResetColorSchemeItemToFactoryValue(ColorType.Text);
      }

      internal bool HasAnyCustomValue()
      {
         return listBoxColorSchemeItemSelector.Items
            .Cast<string>()
            .Any(itemName => 
               !isColorSchemeItemHasFactoryValue(getItemByBackgroundItemName(itemName, ColorType.Background)) ||
               !isColorSchemeItemHasFactoryValue(getItemByBackgroundItemName(itemName, ColorType.Text)));
      }

      private void linkLabelChangeColor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onChangeColor(ColorType.Background);
      }

      private void linkLabelChangeTextColor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onChangeColor(ColorType.Text);
      }

      private void linkLabelResetToFactoryValue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeItemToFactoryValue(ColorType.Background);
      }

      private void linkLabelResetTextToFactoryValue_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeItemToFactoryValue(ColorType.Text);
      }

      private void listBoxColorSchemeItemSelector_Format(object sender, ListControlConvertEventArgs e)
      {
         formatColorSchemeItemSelectorItem(e);
      }

      private void listBoxColorSchemeItemSelector_SelectedIndexChanged(object sender, EventArgs e)
      {
         object selectedItem = (sender as ListBox).SelectedItem;
         bool isItemSelected = selectedItem != null;
         linkLabelChangeColor.Enabled = isItemSelected;
         linkLabelChangeTextColor.Visible = isItemSelected &&
            ColorScheme.GetColor(selectedItem as string).TextName != null;
         linkLabelResetToFactoryValue.Enabled = isItemSelected;
         linkLabelResetTextToFactoryValue.Enabled = isItemSelected &&
            ColorScheme.GetColor(selectedItem as string).TextName != null;
         updateResetToFactoryValueLinkLabelVisibility();
      }

      private void listBoxColorSchemeItemSelector_DrawItem(object sender, DrawItemEventArgs e)
      {
         onDrawListBoxColorSchemeItemSelectorItem(sender as ListBox, e);
      }

      private void listBoxColorSchemeItemSelector_MeasureItem(object sender, MeasureItemEventArgs e)
      {
         onMeasureListBoxColorSchemeItemSelectorItem(sender as ListBox, e);
      }

      enum ColorType
      {
         Background,
         Text
      }

      private void onChangeColor(ColorType type)
      {
         ColorSchemeItem item = getSelectedItem(type);
         if (item == null)
         {
            return;
         }

         colorDialog.Color = item.Color;
         if (colorDialog.ShowDialog() == DialogResult.OK)
         {
            onColorSelected(item, colorDialog.Color);
         }
      }

      private void onColorSelected(ColorSchemeItem item, Color color)
      {
         setColorForColorSchemeItem(item.Name, color);

         listBoxColorSchemeItemSelector.Refresh();
         updateResetToFactoryValueLinkLabelVisibility();
         _onSelected();
      }

      private void onResetColorSchemeItemToFactoryValue(ColorType type)
      {
         ColorSchemeItem item = getSelectedItem(type);
         if (item == null)
         {
            return;
         }

         onColorSelected(item, item.FactoryColor);
      }

      private void setColorForColorSchemeItem(string colorSchemeItemName, Color color)
      {
         ColorScheme.SetColor(colorSchemeItemName, color);
      }

      private bool isColorSchemeItemHasFactoryValue(ColorSchemeItem item)
      {
         return item == null || item.Color.Name == item.FactoryColor.Name;
      }

      private void updateResetToFactoryValueLinkLabelVisibility()
      {
         linkLabelResetToFactoryValue.Visible =
            !isColorSchemeItemHasFactoryValue(getSelectedItem(ColorType.Background));
         linkLabelResetTextToFactoryValue.Visible =
            !isColorSchemeItemHasFactoryValue(getSelectedItem(ColorType.Text));
      }

      private void onDrawListBoxColorSchemeItemSelectorItem(ListBox listBox, DrawItemEventArgs e)
      {
         if (e.Index < 0)
         {
            return;
         }

         string colorSchemeItemName = listBox.Items[e.Index].ToString();
         ColorSchemeItem colorSchemeItem = ColorScheme.GetColor(colorSchemeItemName);
         if (colorSchemeItem == null)
         {
            return;
         }

         Color color = colorSchemeItem.Color;
         bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
         color = isSelected ? ThemeSupport.StockColors.GetThemeColors().SelectionBackground : color;
         WinFormsHelpers.FillRectangle(e, e.Bounds, color);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         bool useBackgroundColorForLabels =
            ConfigurationHelper.GetColorMode(Program.Settings) == Common.Constants.Constants.ColorMode.Light;

         string text = colorSchemeItem.DisplayName;
         if (String.IsNullOrEmpty(text))
         {
            text = colorSchemeItem.Name;
         }
         Font font = listBox.Font;
         if (isSelected && colorSchemeItem.UseForTextColorInConfig && useBackgroundColorForLabels)
         {
            using (Brush brush = new SolidBrush(colorSchemeItem.Color))
            {
               e.Graphics.DrawString(text, font, brush, e.Bounds, format);
            }
         }
         else
         {
            Color textColor = colorSchemeItem.TextName == null
               ? ThemeSupport.StockColors.GetThemeColors().OSThemeColors.TextActive
               : ColorScheme.GetColor(colorSchemeItem.TextName).Color;
            using (Brush brush = new SolidBrush(textColor))
            {
               e.Graphics.DrawString(text, font, brush, e.Bounds, format);
            }
         }
      }

      private void onMeasureListBoxColorSchemeItemSelectorItem(ListBox listBox, MeasureItemEventArgs e)
      {
         if (e.Index >= 0)
         {
            e.ItemHeight = listBox.Font.Height + scale(2);
         }
      }

      private void formatColorSchemeItemSelectorItem(ListControlConvertEventArgs e)
      {
         ColorSchemeItem colorSchemeItem = ColorScheme.GetColor(e.ListItem.ToString());
         e.Value = colorSchemeItem == null ? e.ListItem as string : colorSchemeItem.DisplayName;
      }

      private void fillColorSchemeItemList(IEnumerable<string> groupNames)
      {
         listBoxColorSchemeItemSelector.Items.Clear();
         foreach (string groupName in groupNames)
         {
            ColorScheme.GetColors(groupName)
               .ToList()
               .ForEach(colorSchemeItem =>
               {
                  listBoxColorSchemeItemSelector.Items.Add(colorSchemeItem.Name);
               });
         }

         if (listBoxColorSchemeItemSelector.Items.Count > 0)
         {
            listBoxColorSchemeItemSelector.SelectedIndex = 0;
         }
      }

      private ColorSchemeItem getSelectedItem(ColorType type)
      {
         return getItemByBackgroundItemName(listBoxColorSchemeItemSelector.SelectedItem.ToString(), type);
      }

      private ColorSchemeItem getItemByBackgroundItemName(string name, ColorType type)
      {
         void reportError(string itemName)
         {
            Trace.TraceError(
               "[ConfigureColorsForm] Cannot find color scheme item {0} in the color scheme", itemName);
         };

         ColorSchemeItem colorSchemeItem = ColorScheme.GetColor(name);
         if (colorSchemeItem == null)
         {
            reportError(name);
            return null;
         }

         switch (type)
         {
            case ColorType.Background:
               return colorSchemeItem;

            case ColorType.Text:
               {
                  if (colorSchemeItem.TextName != null)
                  {
                     ColorSchemeItem textColorItem = ColorScheme.GetColor(colorSchemeItem.TextName);
                     if (textColorItem == null)
                     {
                        reportError(colorSchemeItem.TextName);
                     }
                     return textColorItem;
                  }
               }
               break;
         }
         return null;
      }

      private int scale(int px) => (int)WinFormsHelpers.ScalePixelsToNewDpi(96, DeviceDpi, px);

      private Action _onSelected;
      private string[] _groupNames;
   }
}
