using System.Drawing;
using System;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using mrHelper.Common.Exceptions;
using mrHelper.App.Helpers;
using mrHelper.Common.Constants;
using mrHelper.CommonControls.Tools;

// Code below was inspired and is mostly (75%) copied from 
// https://github.com/BlueMystical/Dark-Mode-Forms/blob/main/SourceFiles/DarkModeCS.cs
// Author: BlueMystic (bluemystic.play@gmail.com)
namespace ThemeSupport
{
   public enum DisplayMode
   {
      ClearMode,
      DarkMode
   }

   public class OSThemeColors
   {
      public OSThemeColors(DisplayMode mode)
      {
         if (mode == DisplayMode.DarkMode)
         {
            Control = Color.FromArgb(39, 39, 39);
            ControlDark = Color.FromArgb(30, 30, 30);
            Background = Color.FromArgb(26, 26, 26);
            BackgroundSplit = Color.FromArgb(55, 55, 55);

            TextActive = Color.FromArgb(185, 185, 185);
            TextInactive =  Color.FromArgb(140, 140, 140);
            TextInSelection = Color.FromArgb(185, 185, 185);
            Border = Color.FromArgb(100, 100, 100);
         }
         else
         {
            Control = Color.FromArgb(255, 255, 255);
            ControlDark = Color.FromArgb(237, 237, 237);
            Background = Color.FromArgb(240, 240, 240);
            BackgroundSplit = Color.FromArgb(211, 211, 211);

            TextActive = Color.FromArgb(0, 0, 0);
            TextInactive = Color.FromArgb(169, 169, 169);
            TextInSelection = Color.FromArgb(255, 255, 255);
            Border = Color.FromArgb(0, 0, 139);
         }
      }

      // Background of containers, menus, tool bars, some UserControls, LV Column Header
      public Color Background { get; }

      // ToolStrip separator, LV Column Header Grid
      public Color BackgroundSplit { get; }

      // Background of all controls, Discussions view background, ToolStrip/Menu hovering, TextBox background
      public Color Control { get; }

      // Tab Control, LV Group Header, ToolStrip/Menu selection
      public Color ControlDark { get; }

      // For Main Texts
      public Color TextActive { get; }

      // For Inactive Texts
      public Color TextInactive { get; }

      // For Highlight Texts
      public Color TextInSelection { get; }

      // Just a border of a selected button
      public Color Border { get; }
   }

   public class StockColors
   {
      public OSThemeColors OSThemeColors { get; }

      public Color SelectionBackground =>
         IsDarkMode ? Color.FromArgb(0, 60, 107) : Color.FromArgb(0, 120, 215);

      public Color ListViewBackground => OSThemeColors.Control;

      public Color TooltipBackground => OSThemeColors.Control;

      public Color LinkTextColor =>
         IsDarkMode ? Color.FromArgb(106, 163, 186) : Color.FromArgb(0, 0, 238);

      public Color ListViewGroupHeaderBackground => OSThemeColors.ControlDark;

      public Color ListViewGroupHeaderTextColor => IsDarkMode ? Color.FromArgb(0, 204, 204) : Color.Blue;

      public Color ListViewColumnHeaderBackground => OSThemeColors.Background;

      public Color ListViewColumnHeaderTextColor => OSThemeColors.TextActive;

      public Color ListViewColumnHeaderGridColor => OSThemeColors.BackgroundSplit;

      public Color? TextBoxBorder => IsDarkMode ? OSThemeColors.Border : new Color?();

      public static StockColors GetThemeColors()
      {
         Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(mrHelper.App.Program.Settings);
         if (colorMode == Constants.ColorMode.Dark)
         {
            return DarkThemeColors;
         }
         else
         {
            Debug.Assert(colorMode == Constants.ColorMode.Light);
            return ClearThemeColors;
         }
      }

      private static readonly StockColors DarkThemeColors = new StockColors(DisplayMode.DarkMode);
      private static readonly StockColors ClearThemeColors = new StockColors(DisplayMode.ClearMode);

      private StockColors(DisplayMode mode)
      {
         OSThemeColors = new OSThemeColors(mode);
         _mode = mode;
      }

      private bool IsDarkMode => _mode == DisplayMode.DarkMode;

      private readonly DisplayMode _mode;
   }

   // This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
   public class ThemeSupportHelper
   {
      #region Win32 API Declarations

      [Flags]
      public enum DWMWINDOWATTRIBUTE : uint
      {
         // Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
         DWMWA_NCRENDERING_ENABLED = 1,

         // Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value from the DWMNCRENDERINGPOLICY enumeration.
         DWMWA_NCRENDERING_POLICY,

         // Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a value of type BOOL. TRUE to disable transitions, or FALSE to enable transitions.
         DWMWA_TRANSITIONS_FORCEDISABLED,

         // Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by DWM. The pvAttribute parameter points to a value of type BOOL. TRUE to enable content rendered in the non-client area to be visible on the frame; otherwise, FALSE.
         DWMWA_ALLOW_NCPAINT,

         // Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space. The retrieved value is of type RECT. If the window is minimized or otherwise not visible to the user, then the value of the RECT retrieved is undefined. You should check whether the retrieved RECT contains a boundary that you can work with, and if it doesn't then you can conclude that the window is minimized or otherwise not visible.
         DWMWA_CAPTION_BUTTON_BOUNDS,

         // Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute parameter points to a value of type BOOL. TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, FALSE.
         DWMWA_NONCLIENT_RTL_LAYOUT,

         // Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available. This value is normally set during a window's creation, and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to require a iconic thumbnail or peek representation; otherwise, FALSE.
         DWMWA_FORCE_ICONIC_REPRESENTATION,

         // Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from the DWMFLIP3DWINDOWPOLICY enumeration.
         DWMWA_FLIP3D_POLICY,

         // Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of type RECT.
         DWMWA_EXTENDED_FRAME_BOUNDS,

         // Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek representation (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with DWMWA_FORCE_ICONIC_REPRESENTATION. DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, FALSE. Windows Vista and earlier: This value is not supported.
         DWMWA_HAS_ICONIC_BITMAP,

         // Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview of the window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over the window's thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter points to a value of type BOOL. TRUE to prevent peek functionality, or FALSE to allow it. Windows Vista and earlier: This value is not supported.
         DWMWA_DISALLOW_PEEK,

         // Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute parameter points to a value of type BOOL. TRUE to prevent the window from fading during another window's peek, or FALSE for normal behavior. Windows Vista and earlier: This value is not supported.
         DWMWA_EXCLUDED_FROM_PEEK,

         // Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM. Using with DirectComposition: Use the DWMWA_CLOAK flag to cloak the layered child window when animating a representation of the window's content via a DirectComposition visual that has been associated with the layered child window. For more details on this usage case, see How to animate the bitmap of a layered child window. Windows 7 and earlier: This value is not supported.
         DWMWA_CLOAK,

         // Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why. DWM_CLOAKED_APP (value 0x0000001). The window was cloaked by its owner application. DWM_CLOAKED_SHELL(value 0x0000002). The window was cloaked by the Shell. DWM_CLOAKED_INHERITED(value 0x0000004). The cloak value was inherited from its owner window. Windows 7 and earlier: This value is not supported.
         DWMWA_CLOAKED,

         // Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents. Windows 7 and earlier: This value is not supported.
         DWMWA_FREEZE_REPRESENTATION,

         // Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes. If this flag is set, then a Win32 app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush (see Compositor.CreateHostBackdropBrush). The pvAttribute parameter points to a value of type BOOL. TRUE to enable host backdrop brushes for the window, or FALSE to disable it. This value is supported starting with Windows 11 Build 22000.
         DWMWA_USE_HOSTBACKDROPBRUSH,

         // Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 10 Build 17763.
         DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,

         // Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 11 Build 22000.
         DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

         // Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window. The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE. This value is supported starting with Windows 11 Build 22000.
         DWMWA_WINDOW_CORNER_PREFERENCE = 33,

         // Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF. The app is responsible for changing the border color according to state changes, such as a change in window activation. This value is supported starting with Windows 11 Build 22000.
         DWMWA_BORDER_COLOR,

         // Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
         DWMWA_CAPTION_COLOR,

         // Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
         DWMWA_TEXT_COLOR,

         // Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window. The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT. This value is supported starting with Windows 11 Build 22000.
         DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,

         // The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
         DWMWA_LAST,
      }

      [Flags]
      public enum DWM_WINDOW_CORNER_PREFERENCE
      {
         DWMWCP_DEFAULT = 0,
         DWMWCP_DONOTROUND = 1,
         DWMWCP_ROUND = 2,
         DWMWCP_ROUNDSMALL = 3
      }

      [Serializable, StructLayout(LayoutKind.Sequential)]
      public struct RECT
      {
         public int Left;
         public int Top;
         public int Right;
         public int Bottom;

         public Rectangle ToRectangle()
         {
            return Rectangle.FromLTRB(Left, Top, Right, Bottom);
         }
      }

      [DllImport("DwmApi")]
      public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

      [DllImport("dwmapi.dll")]
      public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

      [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
      private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

      #endregion Win32 API Declarations

      #region Constructors

      // This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
      public ThemeSupportHelper(Form form, DisplayMode displayMode)
      {
         _ownerForm = form;
         form.Load += (object sender, EventArgs e) =>
         {
            applyThemeFromConfiguration();
         };
      }

      #endregion Constructors

      #region Public Methods

      public void ApplyThemeFromConfiguration()
      {
         applyThemeFromConfiguration();
      }

      // Registers the Control as processed. Prevents applying theme to the Control.
      // Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
      public static void ExcludeFromProcessing(Control control)
      {
         controlStatusStorage.ExcludeFromProcessing(control);
      }

      #endregion Public Methods

      #region Private Static Members

      // Stores additional info related to the Controls
      private static readonly ControlStatusStorage controlStatusStorage = new ControlStatusStorage();

      #endregion

      #region Private Methods

      private void onHandleCreated(object sender, EventArgs e)
      {
         applySystemDarkTheme((Control)sender, getCurrentDisplayMode() == DisplayMode.DarkMode);
      }

      private void onControlAdded(object sender, ControlEventArgs e)
      {
         applyThemeToControl(e.Control);
      }

      private void onOwnerFormControlAdded(object sender, ControlEventArgs e)
      {
         applyThemeToControl(e.Control);
      }

      private void onScaledTextPaint(object sender, PaintEventArgs e)
      {
         // This function is needed for the controls, whose text in disabled mode
         // is always drawn in GrayText color by Windows Forms.
         // This function draws text _over_ the text drawn by Windows Forms.
         Control control = (Control)sender;
         if (!control.Enabled)
         {
            // The values found experimentally and tested on the scales 100%, 125%, 150%, 200%
            // TODO This looks bad on scale 175% in Light theme.
            // Original Dark-Mode-CS uses 16/0.
            double horzOffsetPx = 15.2;
            double vertOffsetPx = 1;
            TextRenderer.DrawText(e.Graphics, control.Text, control.Font,
               new Point(scaleUp(horzOffsetPx, control), scaleDn(vertOffsetPx, control)), _OSColors.TextInactive);
         }
      }

      private void onButtonPaint(object sender, PaintEventArgs e)
      {
         // This function is needed for the buttons, whose text in disabled mode
         // is always drawn in Black color by Windows Forms.
         // This function draws text _over_ the text drawn by Windows Forms.
         Button btn = (Button)sender;
         if (!btn.Enabled)
         {
            TextFormatFlags flags = 
               TextFormatFlags.HorizontalCenter |
               TextFormatFlags.VerticalCenter |
               TextFormatFlags.WordBreak;
            TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, e.ClipRectangle, _OSColors.TextInactive, flags);
         }
      }

      private void onGroupBoxPaint(object sender, PaintEventArgs e)
      {
         if (getCurrentDisplayMode() != DisplayMode.DarkMode)
         {
            return;
         }

         Graphics g = e.Graphics;
         GroupBox box = (GroupBox)sender;
         Color textColor = box.Enabled ? _OSColors.TextActive : _OSColors.TextInactive;
         Brush textBrush = new SolidBrush(textColor);
         Brush borderBrush = new SolidBrush(_OSColors.Border);
         Pen borderPen = new Pen(borderBrush);
         SizeF strSize = g.MeasureString(box.Text, box.Font);
         Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                        box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                        box.ClientRectangle.Width - 1,
                                        box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

         // Clear text and border
         g.Clear(box.BackColor);

         // Draw text
         g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

         // Drawing Border
         //Left
         g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
         //Right
         g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
         //Bottom
         g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
         //Top1
         g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
         //Top2
         g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
      }

      private void onTabControlDrawItem(object sender, DrawItemEventArgs e)
      {
         TabControl tab = (TabControl)sender;

         // Draw the background of the main control
         using (SolidBrush backColor = new SolidBrush(tab.Parent.BackColor))
         {
            e.Graphics.FillRectangle(backColor, tab.ClientRectangle);
         }

         // Draw tabs
         using (Brush tabBack = new SolidBrush(_OSColors.ControlDark))
         {
            for (int iTab = 0; iTab < tab.TabPages.Count; iTab++)
            {
               TabPage tabPage = tab.TabPages[iTab];
               tabPage.BackColor = _OSColors.Background;
               tabPage.BorderStyle = BorderStyle.FixedSingle;
               tabPage.ControlAdded -= onControlAdded;
               tabPage.ControlAdded += onControlAdded;

               bool IsSelected = (tab.SelectedIndex == iTab);
               if (IsSelected)
               {
                  e.Graphics.FillRectangle(tabBack, e.Bounds);
                  TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, e.Bounds,
                     _OSColors.TextActive);
               }
               else
               {
                  TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                     tab.GetTabRect(iTab), _OSColors.TextActive);
               }
            }
         }
      }

      private void onListViewDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
      {
         using (SolidBrush backBrush = new SolidBrush(StockColors.GetThemeColors().ListViewColumnHeaderBackground))
         {
            using (SolidBrush foreBrush = new SolidBrush(_OSColors.TextActive))
            {
               using (StringFormat sf = new StringFormat())
               {
                  ListView listView = (ListView)sender;
                  sf.Alignment = StringAlignment.Center;
                  e.Graphics.FillRectangle(backBrush, e.Bounds);
                  e.Graphics.DrawString(e.Header.Text, listView.Font, foreBrush, e.Bounds, sf);
               }
            }
         }
      }

      private void onListViewDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         Color backgroundColor = e.Item.Selected ?
            StockColors.GetThemeColors().SelectionBackground : StockColors.GetThemeColors().ListViewBackground;
         WinFormsHelpers.FillRectangle(e, e.Bounds, backgroundColor);

         Color textColor = e.Item.Selected ? _OSColors.TextInSelection : _OSColors.TextActive;
         using (Brush textBrush = new SolidBrush(textColor))
         {
            e.Graphics.DrawString(e.SubItem.Text, e.Item.ListView.Font, textBrush, e.Bounds);
         }
      }

      // Apply the Theme into the Window and all its controls.
      public void applyThemeFromConfiguration()
      {
         bool isDarkMode = getCurrentDisplayMode() == DisplayMode.DarkMode;
         try
         {
            _OSColors = StockColors.GetThemeColors().OSThemeColors;
            if (_OSColors != null)
            {
               // Apply Window's Dark Mode to the Form's Title bar
               applySystemDarkTheme(_ownerForm, isDarkMode);

               _ownerForm.BackColor = _OSColors.Background;
               _ownerForm.ForeColor = _OSColors.TextInactive;

               if (_ownerForm != null && _ownerForm.Controls != null)
               {
                  foreach (Control _control in _ownerForm.Controls)
                  {
                     applyThemeToControl(_control);
                  }
                  _ownerForm.ControlAdded -= onOwnerFormControlAdded;
                  _ownerForm.ControlAdded += onOwnerFormControlAdded;
               }
            }
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot apply theme", ex);
         }
      }

      // Recursively apply the Colors from 'OScolors' to the Control and all its childs.
      // <param name="control">Can be a Form or any Winforms Control.</param>
      private void applyThemeToControl(Control control)
      {
         bool isDarkMode = getCurrentDisplayMode() == DisplayMode.DarkMode;
         ControlStatusInfo info = controlStatusStorage.GetControlStatusInfo(control);
         if (info != null)
         {
            //we already have some information about this Control

            //if the user chose to skip the control, exit
            if (info.IsExcluded) return;

            //prevent applying a theme multiple times to the same control
            //without this, it happens at least is some MDI forms
            //if the Control already has the current theme, exit (otherwise we are going to re-theme it)
            if (info.LastThemeAppliedIsDark == isDarkMode) return;

            //we remember it will soon have the current theme
            info.LastThemeAppliedIsDark = isDarkMode;
         }
         else
         {
            //this is the first time we see this Control

            //we remember it will soon have the current theme
            controlStatusStorage.RegisterProcessedControl(control, isDarkMode);
         }

         control.HandleCreated -= onHandleCreated;
         control.HandleCreated += onHandleCreated;

         control.ControlAdded -= onControlAdded;
         control.ControlAdded += onControlAdded;

         string Mode = isDarkMode ? "DarkMode_Explorer" : "ClearMode_Explorer";
         SetWindowTheme(control.Handle, Mode, null); //<- Attempts to apply Dark Mode using Win32 API if available.

         control.GetType().GetProperty("BackColor")?.SetValue(control, _OSColors.Control);
         control.GetType().GetProperty("ForeColor")?.SetValue(control, _OSColors.TextActive);

         // Here we fine tune individual Controls
         //
         // mrHelper controls derived from common controls shall go first.
         if (control is Aga.Controls.Tree.TreeViewAdv)
         {
            Aga.Controls.Tree.TreeViewAdv treeView = control as Aga.Controls.Tree.TreeViewAdv;
            if (isDarkMode && treeView.BorderStyle == BorderStyle.Fixed3D)
            {
               treeView.BorderStyle = BorderStyle.FixedSingle;
            }
         }
         else if (control is mrHelper.App.Controls.DiscussionPanel ||
                  control is mrHelper.App.Controls.DiscussionBox)
         {
            // Make panel and boxes have the same background color.
            // Without this customization, Panel override below changes the BackColor.
            control.BackColor = _OSColors.Control;
         }
         else if (control is mrHelper.App.Controls.NoteEditPanel ||
                  control is mrHelper.App.Controls.ReplyOnRelatedNotePanel)
         {
            // These ones look better when their background is the same as Form color.
            // Note that these "panels" are UserControl objects.
            // By default, UserControl has OSColors.Control background.
            control.BackColor = _OSColors.Background;
         }
         else if (control is mrHelper.CommonControls.Controls.MultilineLabel)
         {
            // MultilineLabel is a TextBox which wants to emulate Label background.
            control.BackColor = control.Parent.BackColor;
         }
         else if (control is LinkLabel linkLabel)
         {
            linkLabel.LinkColor = StockColors.GetThemeColors().LinkTextColor;
            linkLabel.BackColor = Color.Transparent;
         }
         else if (control is Label)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
         }
         else if (control is NumericUpDown)
         {
            Mode = isDarkMode ? "DarkMode_ItemsView" : "ClearMode_ItemsView";
            SetWindowTheme(control.Handle, Mode, null);
         }
         else if (control is Button)
         {
            Button button = control as Button;
            button.FlatStyle = isDarkMode ? FlatStyle.Flat : FlatStyle.Standard;
            button.FlatAppearance.CheckedBackColor = _OSColors.Border;
            button.FlatAppearance.BorderColor = (_ownerForm.AcceptButton == button) ?
              _OSColors.Border : _OSColors.Control;
            control.Paint -= onButtonPaint;
            control.Paint += onButtonPaint;
         }
         else if (control is ComboBox comboBox)
         {
            // Fixing a glitch that makes all instances of the ComboBox showing as having a Selected value,
            // even when they don't
            control.BeginInvoke(new Action(() =>
            {
               if (comboBox.DropDownStyle == ComboBoxStyle.DropDownList)
               {
                  // See #619
                  if ((control as ComboBox).SelectionStart < 0)
                  {
                     (control as ComboBox).SelectionStart = 0;
                  }
                  (control as ComboBox).SelectionLength = 0;
               }
            }));

            // Fixes a glitch showing the Combo Background white when the control is Disabled
            if (!control.Enabled && isDarkMode)
            {
               comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Apply Windows Color Mode
            Mode = isDarkMode ? "DarkMode_CFD" : "ClearMode_CFD";
            SetWindowTheme(control.Handle, Mode, null);
         }
         else if (control is TextBox)
         {
            TextBox textBox = control as TextBox;
            if (isDarkMode && textBox.BorderStyle == BorderStyle.Fixed3D)
            {
               textBox.BorderStyle = BorderStyle.FixedSingle;
            }
         }
         else if (control is mrHelper.App.Controls.FilterTextBoxPanel)
         {
            Panel panel = control as Panel;
            panel.BackColor = _OSColors.Background;
            if (!isDarkMode)
            {
               panel.BorderStyle = BorderStyle.Fixed3D;
            }
         }
         else if (control is Panel)
         {
            Panel panel = control as Panel;
            panel.BackColor = _OSColors.Background;
            panel.BorderStyle = BorderStyle.None;
         }
         else if (control is GroupBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.Paint -= onGroupBoxPaint;
            control.Paint += onGroupBoxPaint;
         }
         else if (control is TabControl)
         {
            TabControl tab = control as TabControl;
            if (tab.DrawMode != TabDrawMode.OwnerDrawFixed)
            {
               tab.Appearance = TabAppearance.Normal;
               tab.DrawMode = TabDrawMode.OwnerDrawFixed;
               tab.DrawItem -= onTabControlDrawItem;
               tab.DrawItem += onTabControlDrawItem;
            }
         }
         else if (control is ListView)
         {
            ListView listView = control as ListView;
            if (listView.View == View.Details && !listView.OwnerDraw)
            {
               listView.OwnerDraw = true;
               listView.DrawColumnHeader -= onListViewDrawColumnHeader;
               listView.DrawColumnHeader += onListViewDrawColumnHeader;
               listView.DrawSubItem -= onListViewDrawSubItem;
               listView.DrawSubItem += onListViewDrawSubItem;
            }
         }
         else if (control is PictureBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
         }
         else if (control is CheckBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.Paint -= onScaledTextPaint;
            control.Paint += onScaledTextPaint;
         }
         else if (control is RadioButton)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.Paint -= onScaledTextPaint;
            control.Paint += onScaledTextPaint;
         }
         else if (control is MenuStrip)
         {
            (control as MenuStrip).RenderMode = ToolStripRenderMode.Professional;
            (control as MenuStrip).Renderer = new MyRenderer(new CustomColorTable(_OSColors), _OSColors);
         }
         else if (control is ToolStrip)
         {
            (control as ToolStrip).RenderMode = ToolStripRenderMode.Professional;
            (control as ToolStrip).Renderer = new MyRenderer(new CustomColorTable(_OSColors), _OSColors);
         }
         else if (control is ToolStripPanel) //<- empty area around ToolStrip
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
         }
         else if (control is ToolStripDropDown)
         {
            (control as ToolStripDropDown).Opening -= onToolStripDropDownOpening;
            (control as ToolStripDropDown).Opening += onToolStripDropDownOpening;
         }
         else if (control is ToolStripDropDownMenu)
         {
            (control as ToolStripDropDownMenu).Opening -= onToolStripDropDownOpening;
            (control as ToolStripDropDownMenu).Opening += onToolStripDropDownOpening;
         }
         else if (control is ContextMenuStrip)
         {
            (control as ContextMenuStrip).RenderMode = ToolStripRenderMode.Professional;
            (control as ContextMenuStrip).Renderer = new MyRenderer(new CustomColorTable(_OSColors), _OSColors);
            (control as ContextMenuStrip).Opening -= onToolStripDropDownOpening;
            (control as ContextMenuStrip).Opening += onToolStripDropDownOpening;
         }

         if (control.ContextMenuStrip != null)
         {
            applyThemeToControl(control.ContextMenuStrip);
         }

         foreach (Control childControl in control.Controls)
         {
            // Recursively process its children
            applyThemeToControl(childControl);
         }
      }

      private DisplayMode getCurrentDisplayMode()
      {
         Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(mrHelper.App.Program.Settings);
         return colorMode == Constants.ColorMode.Dark ? DisplayMode.DarkMode : DisplayMode.ClearMode;
      }

      // handle hierarchical context menus (otherwise, only the root level gets themed)
      // 
      private void onToolStripDropDownOpening(object sender, CancelEventArgs e)
      {
         ToolStripDropDown tsdd = sender as ToolStripDropDown;
         if (tsdd == null) return; //should not occur

         foreach (ToolStripMenuItem toolStripMenuItem in tsdd.Items.OfType<ToolStripMenuItem>())
         {
            toolStripMenuItem.DropDownOpening -= onToolStripMenuItemOpening; //just to make sure
            toolStripMenuItem.DropDownOpening += onToolStripMenuItemOpening;
         }
      }

      // 
      // handle hierarchical context menus (otherwise, only the root level gets themed)
      // 
      private void onToolStripMenuItemOpening(object sender, EventArgs e)
      {
         ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
         if (tsmi == null) return; //should not occur

         if (tsmi.DropDown.Items.Count > 0) applyThemeToControl(tsmi.DropDown);

         //once processed, remove itself to prevent multiple executions (when user leaves and reenters the sub-menu)
         tsmi.DropDownOpening -= onToolStripMenuItemOpening;
      }

      // Colorea una imagen usando una Matrix de Color.
      // <param name="bmp">Imagen a Colorear</param>
      // <param name="c">Color a Utilizar</param>
      internal static Bitmap changeToColor(Bitmap bmp, Color c)
      {
         Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height);
         using (Graphics g = Graphics.FromImage(bmp2))
         {
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            float tR = c.R / 255f;
            float tG = c.G / 255f;
            float tB = c.B / 255f;

            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
        new float[] { 1,    0,  0,  0,  0 },
        new float[] { 0,    1,  0,  0,  0 },
        new float[] { 0,    0,  1,  0,  0 },
        new float[] { 0,    0,  0,  1,  0 },  //<- not changing alpha
					new float[] { tR,   tG, tB, 0,  1 }
            });

            System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();
            attributes.SetColorMatrix(colorMatrix);

            g.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height),
              0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes);
         }
         return bmp2;
      }

      // Attempts to apply Window's Dark Style to the Control and all its childs.
      private static void applySystemDarkTheme(Control control = null, bool IsDarkMode = true)
      {
         // DWMWA_USE_IMMERSIVE_DARK_MODE:   https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
         // Use with DwmSetWindowAttribute.
         // Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled.
         // For compatibility reasons, all windows default to light mode regardless of the system setting.
         // The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode.
         // This value is supported starting with Windows 11 Build 22000.
         // SetWindowTheme:     https://learn.microsoft.com/en-us/windows/win32/api/uxtheme/nf-uxtheme-setwindowtheme
         // Causes a window to use a different set of visual style information than its class normally uses. Fix for Scrollbars!
         int[] DarkModeOn = IsDarkMode ? new[] { 0x01 } : new[] { 0x00 }; //<- 1=True, 0=False
         string Mode = IsDarkMode ? "DarkMode_Explorer" : "ClearMode_Explorer";

         SetWindowTheme(control.Handle, Mode, null); //DarkMode_Explorer, ClearMode_Explorer, DarkMode_CFD, DarkMode_ItemsView,

         int attribute1 = (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
         int attribute2 = (int)DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE;
         if (DwmSetWindowAttribute(control.Handle, attribute1, DarkModeOn, 4) != 0)
         {
            DwmSetWindowAttribute(control.Handle, attribute2, DarkModeOn, 4);
         }

         foreach (Control child in control.Controls)
         {
            if (child.Controls.Count != 0)
            {
               applySystemDarkTheme(child, IsDarkMode);
            }
         }
      }
      private static int scaleUp(double px, Control control)
      {
         return (int)Math.Ceiling(WinFormsHelpers.ScalePixelsToNewDpi(96, control.DeviceDpi, px));
      }

      private static int scaleDn(double px, Control control)
      {
         return (int)Math.Floor(WinFormsHelpers.ScalePixelsToNewDpi(96, control.DeviceDpi, px));
      }

      #endregion Private Methods

      #region Private members

      // The Parent form for them all.
      private Form _ownerForm { get; set; }

      // Windows Colors. Can be customized.
      private OSThemeColors _OSColors { get; set; }

      #endregion
   }

   /* Custom Renderers for Menus and ToolBars */
   public class MyRenderer : ToolStripProfessionalRenderer
   {
      public MyRenderer(ProfessionalColorTable table, OSThemeColors colors)
         : base(table)
      {
         MyColors = colors;
      }

      protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
      {
         // Base implementation draws a white border for the toolbar
      }

      // Draws background of the whole ToolBar Or MenuBar
      protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
      {
         e.ToolStrip.BackColor = MyColors.Background;
         base.OnRenderToolStripBackground(e);
      }

      // Draws background of the whole ToolBar Or MenuBar
      protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
      {
         e.ToolStrip.BackColor = MyColors.Background;
         base.OnRenderToolStripStatusLabelBackground(e);
      }

      // For Normal Buttons on a ToolBar
      protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
      {
         Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

         Color gradientBegin = MyColors.Background;
         Color gradientEnd = MyColors.Background;

         ToolStripButton button = e.Item as ToolStripButton;
         if (button.Pressed || button.Checked)
         {
            gradientBegin = MyColors.ControlDark;
            gradientEnd = MyColors.ControlDark;
         }
         else if (button.Selected)
         {
            gradientBegin = MyColors.Control;
            gradientEnd = MyColors.Control;
         }

         using (Brush b = new LinearGradientBrush(
            bounds, gradientBegin, gradientEnd, LinearGradientMode.Vertical))
         {
            e.Graphics.FillRectangle(b, bounds);
         }

         if (button.Pressed || button.Checked || button.Selected)
         {
            using (Pen BordersPencil = new Pen(MyColors.Border))
            {
               Rectangle b = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
               e.Graphics.DrawRectangle(BordersPencil, b);
            }
         }
      }

      protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
      {
         using (Brush brush = new SolidBrush(MyColors.BackgroundSplit))
         {
            if (e.Vertical)
            {
               Point location = new Point(e.Item.Size.Width / 2, (int)(e.Item.Size.Height * 0.1));
               Size size = new Size(1, (int)(e.Item.Size.Height * 0.8));
               Rectangle bounds = new Rectangle(location, size);
               e.Graphics.FillRectangle(brush, bounds);
            }
            else
            {
               Point location = new Point((int)(e.Item.Size.Width * 0.1), e.Item.Size.Height / 2);
               Size size = new Size((int)(e.Item.Size.Width * 0.8), 1);
               Rectangle bounds = new Rectangle(location, size);
               e.Graphics.FillRectangle(brush, bounds);
            }
         }
      }

      // For the Text Color of all Items
      protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
      {
         if (e.Item is ToolStripMenuItem || e.Item is ToolStripItem)
         {
            e.TextColor = e.Item.Enabled ? MyColors.TextActive : MyColors.TextInactive;
         }
         base.OnRenderItemText(e);
      }

      protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
      {
         // Does nothing, see 
         // https://stackoverflow.com/questions/30819050/what-does-toolstripprofessionalrenderer-onrenderitembackground-do
         base.OnRenderItemBackground(e);
      }

      // For Menu Items BackColor
      protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
      {
         Graphics g = e.Graphics;
         Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

         Color gradientBegin = MyColors.Background;
         Color gradientEnd = MyColors.Background;

         bool DrawIt = false;
         ToolStripItem menu = e.Item;
         if (menu.Pressed)
         {
            gradientBegin = MyColors.ControlDark;
            gradientEnd = MyColors.ControlDark;
            DrawIt = true;
         }
         else if (menu.Selected)
         {
            gradientBegin = MyColors.Control;
            gradientEnd = MyColors.Control;
            DrawIt = true;
         }

         if (DrawIt)
         {
            using (Brush b = new LinearGradientBrush(
               bounds, gradientBegin, gradientEnd, LinearGradientMode.Vertical))
            {
               g.FillRectangle(b, bounds);
            }
         }
      }

      private OSThemeColors MyColors { get; } //<- Your Custom Colors Collection
   }

   public class CustomColorTable : ProfessionalColorTable
   {
      public CustomColorTable(OSThemeColors _Colors)
      {
         Colors = _Colors;
         base.UseSystemColors = false;
      }

      public override Color ImageMarginGradientBegin
      {
         get { return Colors.Background; }
      }

      public override Color ImageMarginGradientMiddle
      {
         get { return Colors.Background; }
      }

      public override Color ImageMarginGradientEnd
      {
         get { return Colors.Background; }
      }

      private OSThemeColors Colors { get; }
   }

   // 
   // Stores additional info related to the Controls
   // 
   public class ControlStatusStorage
   {
      // 
      // Storage for the data. ConditionalWeakTable ensures there are no unnecessary references left, preventing garbage collection.
      // 
      private readonly ConditionalWeakTable<Control, ControlStatusInfo> _controlsProcessed = new ConditionalWeakTable<Control, ControlStatusInfo>();

      // 
      // Registers the Control as processed. Prevents applying theme to the Control.
      // Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
      // 
      public void ExcludeFromProcessing(Control control)
      {
         _controlsProcessed.Remove(control);
         _controlsProcessed.Add(control, new ControlStatusInfo() { IsExcluded = true });
      }

      // 
      // Gets the additional info associated with a Control
      // 
      // <returns>a ControlStatusInfo object if the control has been already processed or marked for exclusion, null otherwise</returns>
      public ControlStatusInfo GetControlStatusInfo(Control control)
      {
         _controlsProcessed.TryGetValue(control, out ControlStatusInfo info);
         return info;
      }

      public void RegisterProcessedControl(Control control, bool isDarkMode)
      {
         _controlsProcessed.Add(control,
            new ControlStatusInfo() { IsExcluded = false, LastThemeAppliedIsDark = isDarkMode });
      }
   }

   // 
   // Additional information related to the Controls
   // 
   public class ControlStatusInfo
   {
      // 
      // true if the user wants to skip theming the Control
      // 
      public bool IsExcluded { get; set; }

      // 
      // whether the last theme applied was dark
      // 
      public bool LastThemeAppliedIsDark { get; set; }
   }
}