using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using mrHelper.App.Controls;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.Common.Exceptions;
using mrHelper.CommonControls.Tools;

namespace mrHelper.App.Forms
{
   internal enum DefaultCategory
   {
      General,
      Discussions
   }

   internal partial class ConfigureColorsForm : ThemedForm
   {
      internal ConfigureColorsForm(DefaultCategory defaultCategory)
      {
         _defaultCategory = defaultCategory;

         CommonControls.Tools.WinFormsHelpers.FixNonStandardDPIIssue(this,
            (float)Common.Constants.Constants.FontSizeChoices["Design"]);
         InitializeComponent();

         {
            ColorGroupConfigPage page = new ColorGroupConfigPage(
               onSelected, new string[] { "MergeRequests", "General" });
            tabControl.TabPages.Add(new ColorGroupConfigTabPage("General", page));
         }

         {
            ColorGroupConfigPage page = new ColorGroupConfigPage(
               onSelected, new string[] { "Discussions" });
            tabControl.TabPages.Add(new ColorGroupConfigTabPage("Discussions", page));
         }

         applyFont(Program.Settings.MainWindowFontSizeName);

         labelColorScheme.Text = String.Format("Configure colors for {0} theme",
            ConfigurationHelper.GetColorMode(Program.Settings).ToString());
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         fillColorSchemeItemList();

         switch (_defaultCategory)
         {
            case DefaultCategory.General:
               tabControl.SelectedTab = tabControl.TabPages
                  .Cast<ColorGroupConfigTabPage>()
                  .FirstOrDefault(tab => tab.Text == "General");
               break;

            case DefaultCategory.Discussions:
               tabControl.SelectedTab = tabControl.TabPages
                  .Cast<ColorGroupConfigTabPage>()
                  .FirstOrDefault(tab => tab.Text == "Discussions");
               break;

            default:
               Debug.Assert(false);
               break;
         }

         updateResetSchemeToFactoryValuesLinkLabelVisibility();
      }

      private void linkLabelResetAllColors_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
      {
         onResetColorSchemeToFactoryValues();
      }

      private void fillColorSchemeItemList()
      {
         foreach (ColorGroupConfigTabPage tabPage in tabControl.TabPages.Cast<ColorGroupConfigTabPage>())
         {
            tabPage.Page.Fill();
         }
      }

      private void onResetColorSchemeToFactoryValues()
      {
         ColorScheme.ResetToDefault();

         foreach (ColorGroupConfigTabPage tabPage in tabControl.TabPages.Cast<ColorGroupConfigTabPage>())
         {
            tabPage.Page.Reset();
         }
      }

      private void updateResetSchemeToFactoryValuesLinkLabelVisibility()
      {
         linkLabelResetAllColors.Visible = tabControl.TabPages
            .Cast<ColorGroupConfigTabPage>()
            .Any(page => page.Page.HasAnyCustomValue());
      }

      private void onSelected()
      {
         updateResetSchemeToFactoryValuesLinkLabelVisibility();
      }

      private readonly DefaultCategory _defaultCategory;

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
   }
}
