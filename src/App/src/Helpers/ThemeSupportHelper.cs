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
            Background = Color.FromArgb(26, 26, 26);
            BackgroundDark = ControlPaint.Dark(Background);
            Surface = Color.FromArgb(48, 48, 48);
            Control = Color.FromArgb(39, 39, 39);
            ControlLight = Surface; // In ClearMode they are equal too
            ControlLightLight = Color.FromArgb(55, 55, 55);
            ControlDark = Color.FromArgb(36, 36, 36);
            TextActive = Color.FromArgb(203, 216, 216);
            TextInactive =  Color.FromArgb(95, 95, 95);
            TextInSelection = Color.FromArgb(203, 216, 216);
            ButtonBorder = Color.LightBlue;
         }
         else
         {
            /// Windows System Colors for UI components following Google Material Design concepts.
            Background = SystemColors.Control;
            BackgroundDark = SystemColors.ControlDark;
            Surface = SystemColors.ControlLightLight;
            Control = SystemColors.ButtonFace;
            ControlLight = SystemColors.ButtonHighlight;
            ControlLightLight = SystemColors.ControlLightLight;
            ControlDark = SystemColors.ControlLight;
            TextActive = SystemColors.ControlText;
            TextInactive = SystemColors.GrayText;
            TextInSelection = SystemColors.HighlightText;
            ButtonBorder = Color.DarkBlue;
         }
      }

      /// <summary>For the very back of the Window. Also: BackgroundPanel and some UserControls</summary>
      public Color Background { get; }

      /// <summary>For Borders around the Background. Also: ToolStrip separator</summary>
      public Color BackgroundDark { get; }

      /// <summary>For Container (such as Panel) above the Background. Also: LV Column Header</summary>
      public Color Surface { get; }

      /// <summary>For the background of any Control. Also: Discussions background</summary>
      public Color Control { get; }

      /// <summary>For Borders of any Control. Also: LV Group Header</summary>
      public Color ControlDark { get; }

      /// <summary>For Highlight elements in a Control. Also: ToolStrip hovering, TextBox background</summary>
      public Color ControlLight { get; }

      /// <summary>For Highlight elements in a Control. Also: ToolStrip selection</summary>
      public Color ControlLightLight { get; }

      /// <summary>For Main Texts</summary>
      public Color TextActive { get; }

      /// <summary>For Inactive Texts</summary>
      public Color TextInactive { get; }

      /// <summary>For Highlight Texts</summary>
      public Color TextInSelection { get; }

      /// <summary>Just a border of a selected button</summary>
      public Color ButtonBorder { get; }
   }

   public class StockColors
   {
      public OSThemeColors OSThemeColors { get; }

      public Color SelectionBackground = Color.FromArgb(0, 120, 215);

      public Color TooltipBackground => OSThemeColors.Control;

      public Color LinkTextColor =>
         IsDarkMode ? Color.FromArgb(135, 206, 235) : Color.FromArgb(0, 0, 238);

      public Color ListViewGroupHeaderBackground => OSThemeColors.ControlDark;

      public Color ListViewGroupHeaderTextColor => IsDarkMode ? Color.Cyan : Color.Blue;

      public Color ListViewColumnHeaderBackground => OSThemeColors.Surface;

      public Color ListViewColumnHeaderTextColor => OSThemeColors.TextActive;

      public Color ListViewColumnHeaderGridColor => OSThemeColors.Background;

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

   /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.
   public class ThemeSupportHelper
   {
      #region Win32 API Declarations

      [Flags]
      public enum DWMWINDOWATTRIBUTE : uint
      {
         /// <summary>
         /// Use with DwmGetWindowAttribute. Discovers whether non-client rendering is enabled. The retrieved value is of type BOOL. TRUE if non-client rendering is enabled; otherwise, FALSE.
         /// </summary>
         DWMWA_NCRENDERING_ENABLED = 1,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Sets the non-client rendering policy. The pvAttribute parameter points to a value from the DWMNCRENDERINGPOLICY enumeration.
         /// </summary>
         DWMWA_NCRENDERING_POLICY,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Enables or forcibly disables DWM transitions. The pvAttribute parameter points to a value of type BOOL. TRUE to disable transitions, or FALSE to enable transitions.
         /// </summary>
         DWMWA_TRANSITIONS_FORCEDISABLED,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Enables content rendered in the non-client area to be visible on the frame drawn by DWM. The pvAttribute parameter points to a value of type BOOL. TRUE to enable content rendered in the non-client area to be visible on the frame; otherwise, FALSE.
         /// </summary>
         DWMWA_ALLOW_NCPAINT,

         /// <summary>
         /// Use with DwmGetWindowAttribute. Retrieves the bounds of the caption button area in the window-relative space. The retrieved value is of type RECT. If the window is minimized or otherwise not visible to the user, then the value of the RECT retrieved is undefined. You should check whether the retrieved RECT contains a boundary that you can work with, and if it doesn't then you can conclude that the window is minimized or otherwise not visible.
         /// </summary>
         DWMWA_CAPTION_BUTTON_BOUNDS,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Specifies whether non-client content is right-to-left (RTL) mirrored. The pvAttribute parameter points to a value of type BOOL. TRUE if the non-client content is right-to-left (RTL) mirrored; otherwise, FALSE.
         /// </summary>
         DWMWA_NONCLIENT_RTL_LAYOUT,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Forces the window to display an iconic thumbnail or peek representation (a static bitmap), even if a live or snapshot representation of the window is available. This value is normally set during a window's creation, and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to require a iconic thumbnail or peek representation; otherwise, FALSE.
         /// </summary>
         DWMWA_FORCE_ICONIC_REPRESENTATION,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Sets how Flip3D treats the window. The pvAttribute parameter points to a value from the DWMFLIP3DWINDOWPOLICY enumeration.
         /// </summary>
         DWMWA_FLIP3D_POLICY,

         /// <summary>
         /// Use with DwmGetWindowAttribute. Retrieves the extended frame bounds rectangle in screen space. The retrieved value is of type RECT.
         /// </summary>
         DWMWA_EXTENDED_FRAME_BOUNDS,

         /// <summary>
         /// Use with DwmSetWindowAttribute. The window will provide a bitmap for use by DWM as an iconic thumbnail or peek representation (a static bitmap) for the window. DWMWA_HAS_ICONIC_BITMAP can be specified with DWMWA_FORCE_ICONIC_REPRESENTATION. DWMWA_HAS_ICONIC_BITMAP normally is set during a window's creation and not changed throughout the window's lifetime. Some scenarios, however, might require the value to change over time. The pvAttribute parameter points to a value of type BOOL. TRUE to inform DWM that the window will provide an iconic thumbnail or peek representation; otherwise, FALSE. Windows Vista and earlier: This value is not supported.
         /// </summary>
         DWMWA_HAS_ICONIC_BITMAP,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Do not show peek preview for the window. The peek view shows a full-sized preview of the window when the mouse hovers over the window's thumbnail in the taskbar. If this attribute is set, hovering the mouse pointer over the window's thumbnail dismisses peek (in case another window in the group has a peek preview showing). The pvAttribute parameter points to a value of type BOOL. TRUE to prevent peek functionality, or FALSE to allow it. Windows Vista and earlier: This value is not supported.
         /// </summary>
         DWMWA_DISALLOW_PEEK,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Prevents a window from fading to a glass sheet when peek is invoked. The pvAttribute parameter points to a value of type BOOL. TRUE to prevent the window from fading during another window's peek, or FALSE for normal behavior. Windows Vista and earlier: This value is not supported.
         /// </summary>
         DWMWA_EXCLUDED_FROM_PEEK,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Cloaks the window such that it is not visible to the user. The window is still composed by DWM. Using with DirectComposition: Use the DWMWA_CLOAK flag to cloak the layered child window when animating a representation of the window's content via a DirectComposition visual that has been associated with the layered child window. For more details on this usage case, see How to animate the bitmap of a layered child window. Windows 7 and earlier: This value is not supported.
         /// </summary>
         DWMWA_CLOAK,

         /// <summary>
         /// Use with DwmGetWindowAttribute. If the window is cloaked, provides one of the following values explaining why. DWM_CLOAKED_APP (value 0x0000001). The window was cloaked by its owner application. DWM_CLOAKED_SHELL(value 0x0000002). The window was cloaked by the Shell. DWM_CLOAKED_INHERITED(value 0x0000004). The cloak value was inherited from its owner window. Windows 7 and earlier: This value is not supported.
         /// </summary>
         DWMWA_CLOAKED,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Freeze the window's thumbnail image with its current visuals. Do no further live updates on the thumbnail image to match the window's contents. Windows 7 and earlier: This value is not supported.
         /// </summary>
         DWMWA_FREEZE_REPRESENTATION,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Enables a non-UWP window to use host backdrop brushes. If this flag is set, then a Win32 app that calls Windows::UI::Composition APIs can build transparency effects using the host backdrop brush (see Compositor.CreateHostBackdropBrush). The pvAttribute parameter points to a value of type BOOL. TRUE to enable host backdrop brushes for the window, or FALSE to disable it. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_USE_HOSTBACKDROPBRUSH,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 10 Build 17763.
         /// </summary>
         DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Allows the window frame for this window to be drawn in dark mode colors when the dark mode system setting is enabled. For compatibility reasons, all windows default to light mode regardless of the system setting. The pvAttribute parameter points to a value of type BOOL. TRUE to honor dark mode for the window, FALSE to always use light mode. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_USE_IMMERSIVE_DARK_MODE = 20,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Specifies the rounded corner preference for a window. The pvAttribute parameter points to a value of type DWM_WINDOW_CORNER_PREFERENCE. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_WINDOW_CORNER_PREFERENCE = 33,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Specifies the color of the window border. The pvAttribute parameter points to a value of type COLORREF. The app is responsible for changing the border color according to state changes, such as a change in window activation. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_BORDER_COLOR,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Specifies the color of the caption. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_CAPTION_COLOR,

         /// <summary>
         /// Use with DwmSetWindowAttribute. Specifies the color of the caption text. The pvAttribute parameter points to a value of type COLORREF. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_TEXT_COLOR,

         /// <summary>
         /// Use with DwmGetWindowAttribute. Retrieves the width of the outer border that the DWM would draw around this window. The value can vary depending on the DPI of the window. The pvAttribute parameter points to a value of type UINT. This value is supported starting with Windows 11 Build 22000.
         /// </summary>
         DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,

         /// <summary>
         /// The maximum recognized DWMWINDOWATTRIBUTE value, used for validation purposes.
         /// </summary>
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

      #region Public Members

      #endregion Public Members

      #region Constructors

      /// <summary>This tries to automatically apply Windows Dark Mode (if enabled) to a Form.</summary>
      /// <param name="_Form">The Form to become Dark</param>
      public ThemeSupportHelper(Form form, DisplayMode displayMode)
      {
         //Sets the Properties:
         _ownerForm = form;

         // This Fires after the normal 'Form_Load' event
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

      /// <summary>
      /// Registers the Control as processed. Prevents applying theme to the Control.
      /// Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
      /// </summary>
      public static void ExcludeFromProcessing(Control control)
      {
         controlStatusStorage.ExcludeFromProcessing(control);
      }

      #endregion Public Methods

      #region Private Static Members

      /// <summary>
      /// Stores additional info related to the Controls
      /// </summary>
      private static readonly ControlStatusStorage controlStatusStorage = new ControlStatusStorage();

      /// <summary>
      /// stores the event handler reference in order to prevent its uncontrolled multiple addition
      /// </summary>
      private static ControlEventHandler ownerFormControlAdded;

      /// <summary>
      /// stores the event handler reference in order to prevent its uncontrolled multiple addition
      /// </summary>
      private static EventHandler controlHandleCreated;

      /// <summary>
      /// stores the event handler reference in order to prevent its uncontrolled multiple addition
      /// </summary>
      private static ControlEventHandler controlControlAdded;

      #endregion

      #region Private Methods

      /// <summary>Apply the Theme into the Window and all its controls.</summary>
      /// <param name="pIsDarkMode">'true': apply Dark Mode, 'false': apply Clear Mode</param>
      public void applyThemeFromConfiguration()
      {
         bool isDarkMode = getCurrentDisplayMode() == DisplayMode.DarkMode;
         try
         {
            _OSColors = StockColors.GetThemeColors().OSThemeColors;
            if (_OSColors != null)
            {
               //Apply Window's Dark Mode to the Form's Title bar:
               applySystemDarkTheme(_ownerForm, isDarkMode);

               _ownerForm.BackColor = _OSColors.Background;
               _ownerForm.ForeColor = _OSColors.TextInactive;

               if (_ownerForm != null && _ownerForm.Controls != null)
               {
                  foreach (Control _control in _ownerForm.Controls)
                  {
                     applyThemeToControl(_control);
                  }

                  if (ownerFormControlAdded == null)
                  {
                     ownerFormControlAdded = (object sender, ControlEventArgs e) =>
                     {
                        applyThemeToControl(e.Control);
                     };
                  }
                  _ownerForm.ControlAdded -= ownerFormControlAdded; //prevent uncontrolled multiple addition
                  _ownerForm.ControlAdded += ownerFormControlAdded;
               }
            }
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot apply theme", ex);
         }
      }

      /// <summary>Recursively apply the Colors from 'OScolors' to the Control and all its childs.</summary>
      /// <param name="control">Can be a Form or any Winforms Control.</param>
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

         BorderStyle BStyle = (isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D);
         FlatStyle FStyle = (isDarkMode ? FlatStyle.Flat : FlatStyle.Standard);

         if (controlHandleCreated == null) controlHandleCreated = (object sender, EventArgs e) =>
         {
            applySystemDarkTheme((Control)sender, getCurrentDisplayMode() == DisplayMode.DarkMode);
         };
         control.HandleCreated -= controlHandleCreated; //prevent uncontrolled multiple addition
         control.HandleCreated += controlHandleCreated;

         if (controlControlAdded == null) controlControlAdded = (object sender, ControlEventArgs e) =>
         {
            applyThemeToControl(e.Control);
         };
         control.ControlAdded -= controlControlAdded; //prevent uncontrolled multiple addition
         control.ControlAdded += controlControlAdded;

         string Mode = isDarkMode ? "DarkMode_Explorer" : "ClearMode_Explorer";
         SetWindowTheme(control.Handle, Mode, null); //<- Attempts to apply Dark Mode using Win32 API if available.

         control.GetType().GetProperty("BackColor")?.SetValue(control, _OSColors.Control);
         control.GetType().GetProperty("ForeColor")?.SetValue(control, _OSColors.TextActive);

         /* Here we fine tune individual Controls  */
         //
         // mrHelper controls derived from common controls shall go first.
         if (control is mrHelper.App.Controls.DiscussionPanel ||
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
         else if (control is mrHelper.App.Controls.BackgroundPanel)
         {
            // Special kind of Panel which inherits Form color
            control.BackColor = _OSColors.Background;
         }
         else if (control is mrHelper.CommonControls.Controls.SmartTextBox ||
                  control is TextBox || control is RichTextBox)
         {
            // White background for ClearMode
            control.BackColor = _OSColors.ControlLight;
            //control.GetType().GetProperty("BorderStyle")?.SetValue(control, BStyle);
         }
         else if (control is LinkLabel linkLabel)
         {
            linkLabel.LinkColor = StockColors.GetThemeColors().LinkTextColor;
            linkLabel.BackColor = Color.Transparent;
         }
         else if (control is Label lbl)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
            control.Paint += (object sender, PaintEventArgs e) =>
            {
               if (control.Enabled == false && isDarkMode)
               {
                  e.Graphics.Clear(control.Parent.BackColor);
                  e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                  using (Brush B = new SolidBrush(control.ForeColor))
                  {
                     //StringFormat sf = lbl.CreateStringFormat();
                     MethodInfo mi = lbl.GetType().GetMethod("CreateStringFormat", BindingFlags.NonPublic | BindingFlags.Instance);
                     StringFormat sf = mi.Invoke(lbl, new object[] { }) as StringFormat;

                     e.Graphics.DrawString(lbl.Text, lbl.Font, B, new System.Drawing.PointF(1, 0), sf);
                  }
               }
            };
         }
         else if (control is NumericUpDown)
         {
            Mode = isDarkMode ? "DarkMode_ItemsView" : "ClearMode_ItemsView";
            SetWindowTheme(control.Handle, Mode, null);
         }
         else if (control is Button)
         {
            var button = control as Button;
            button.FlatStyle = isDarkMode ? FlatStyle.Flat : FlatStyle.Standard;
            button.FlatAppearance.CheckedBackColor = _OSColors.ButtonBorder;
            button.BackColor = _OSColors.Control;
            button.FlatAppearance.BorderColor = (_ownerForm.AcceptButton == button) ?
              _OSColors.ButtonBorder : _OSColors.Control;
         }
         else if (control is ComboBox comboBox)
         {
            // Fixing a glitch that makes all instances of the ComboBox showing as having a Selected value, even when they dont
            control.BeginInvoke(new Action(() => (control as ComboBox).SelectionLength = 0));

            // Fixes a glitch showing the Combo Background white when the control is Disabled:
            if (!control.Enabled && isDarkMode)
            {
               comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }

            // Apply Windows Color Mode:
            Mode = isDarkMode ? "DarkMode_CFD" : "ClearMode_CFD";
            SetWindowTheme(control.Handle, Mode, null);
         }
         else if (control is Panel)
         {
            var panel = control as Panel;
            // Process the panel within the container
            panel.BackColor = _OSColors.Surface;
            panel.BorderStyle = BorderStyle.None;
         }
         else if (control is GroupBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("ForeColor")?.SetValue(control, _OSColors.TextActive);
            control.Paint += (object sender, PaintEventArgs e) =>
            {
               if (control.Enabled == false && isDarkMode)
               {
                  var radio = (sender as GroupBox);
                  Brush B = new SolidBrush(control.ForeColor);

                  e.Graphics.DrawString(radio.Text, radio.Font,
                    B, new System.Drawing.PointF(6, 0));
               }
            };
         }
         else if (control is TableLayoutPanel)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("ForeColor")?.SetValue(control, _OSColors.TextInactive);
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
         }
         else if (control is TabControl)
         {
            var tab = control as TabControl;
            tab.Appearance = TabAppearance.Normal;
            tab.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tab.DrawItem += (object sender, DrawItemEventArgs e) =>
            {
               //Draw the background of the main control
               using (SolidBrush backColor = new SolidBrush(tab.Parent.BackColor))
               {
                  e.Graphics.FillRectangle(backColor, tab.ClientRectangle);
               }

               using (Brush tabBack = new SolidBrush(_OSColors.Surface))
               {
                  for (int i = 0; i < tab.TabPages.Count; i++)
                  {
                     TabPage tabPage = tab.TabPages[i];
                     tabPage.BackColor = _OSColors.Surface;
                     tabPage.BorderStyle = BorderStyle.FixedSingle;
                     tabPage.ControlAdded += (object _s, ControlEventArgs _e) =>
                     {
                        applyThemeToControl(_e.Control);
                     };

                     var tBounds = e.Bounds;
                     //tBounds.Inflate(100, 100);

                     bool IsSelected = (tab.SelectedIndex == i);
                     if (IsSelected)
                     {
                        e.Graphics.FillRectangle(tabBack, tBounds);
                        TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, e.Bounds, _OSColors.TextActive);
                     }
                     else
                     {
                        TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font, tab.GetTabRect(i), _OSColors.TextInactive);
                     }
                  }
               }
            };
         }
         else if (control is PictureBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.GetType().GetProperty("ForeColor")?.SetValue(control, _OSColors.TextActive);
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
         }
         else if (control is CheckBox)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.ForeColor = control.Enabled ? _OSColors.TextActive : _OSColors.TextInactive;
            control.Paint += (object sender, PaintEventArgs e) =>
            {
               if (control.Enabled == false && isDarkMode)
               {
                  var radio = (sender as CheckBox);
                  Brush B = new SolidBrush(control.ForeColor);

                  e.Graphics.DrawString(radio.Text, radio.Font,
                    B, new System.Drawing.PointF(16, 0));
               }
            };
         }
         else if (control is RadioButton)
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, control.Parent.BackColor);
            control.ForeColor = control.Enabled ? _OSColors.TextActive : _OSColors.TextInactive;
            control.Paint += (object sender, PaintEventArgs e) =>
            {
               if (control.Enabled == false && isDarkMode)
               {
                  var radio = (sender as RadioButton);
                  Brush B = new SolidBrush(control.ForeColor);

                  e.Graphics.DrawString(radio.Text, radio.Font,
                    B, new System.Drawing.PointF(16, 0));
               }
            };
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
            (control as ToolStripDropDown).Opening -= onToolStripDropDownOpening; //just to make sure
            (control as ToolStripDropDown).Opening += onToolStripDropDownOpening;
         }
         else if (control is ToolStripDropDownMenu)
         {
            (control as ToolStripDropDownMenu).Opening -= onToolStripDropDownOpening; //just to make sure
            (control as ToolStripDropDownMenu).Opening += onToolStripDropDownOpening;
         }
         else if (control is ContextMenuStrip)
         {
            (control as ContextMenuStrip).RenderMode = ToolStripRenderMode.Professional;
            (control as ContextMenuStrip).Renderer = new MyRenderer(new CustomColorTable(_OSColors), _OSColors);
            (control as ContextMenuStrip).Opening -= onToolStripDropDownOpening; //just to make sure
            (control as ContextMenuStrip).Opening += onToolStripDropDownOpening;
         }
         else if (control is MdiClient) //<- empty area of MDI container window
         {
            control.GetType().GetProperty("BackColor")?.SetValue(control, _OSColors.Surface);
         }
         else if (control is PropertyGrid)
         {
            var pGrid = control as PropertyGrid;
            pGrid.BackColor = _OSColors.Control;
            pGrid.ViewBackColor = _OSColors.Control;
            pGrid.LineColor = _OSColors.Surface;
            pGrid.ViewForeColor = _OSColors.TextActive;
            pGrid.ViewBorderColor = _OSColors.ControlDark;
            pGrid.CategoryForeColor = _OSColors.TextActive;
            pGrid.CategorySplitterColor = _OSColors.ControlLight;
         }
         else if (control is ListView)
         {
            var lView = control as ListView;
            if (lView.View == View.Details && !lView.OwnerDraw)
            {
               lView.OwnerDraw = true;
               lView.DrawColumnHeader += (object sender, DrawListViewColumnHeaderEventArgs e) =>
               {
                  using (SolidBrush backBrush = new SolidBrush(StockColors.GetThemeColors().ListViewColumnHeaderBackground))
                  {
                     using (SolidBrush foreBrush = new SolidBrush(_OSColors.TextActive))
                     {
                        using (var sf = new StringFormat())
                        {
                           sf.Alignment = StringAlignment.Center;
                           e.Graphics.FillRectangle(backBrush, e.Bounds);
                           e.Graphics.DrawString(e.Header.Text, lView.Font, foreBrush, e.Bounds, sf);
                        }
                     }
                  }

                  e.DrawDefault = false;
               };
               lView.DrawSubItem += (sender, e) =>
               {
                  Color backgroundColor = e.Item.Selected ?
                     StockColors.GetThemeColors().SelectionBackground : _OSColors.Control;
                  mrHelper.CommonControls.Tools.WinFormsHelpers.FillRectangle(e, e.Bounds, backgroundColor);

                  Color textColor = e.Item.Selected ? _OSColors.TextInSelection : _OSColors.TextActive;
                  using (Brush textBrush = new SolidBrush(textColor))
                  {
                     e.Graphics.DrawString(e.SubItem.Text, e.Item.ListView.Font, textBrush, e.Bounds);
                  }
               };
            }
         }
         else if (control is TreeView)
         {
            control.GetType().GetProperty("BorderStyle")?.SetValue(control, BorderStyle.None);
         }

         if (control.ContextMenuStrip != null)
            applyThemeToControl(control.ContextMenuStrip);

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

      /// handle hierarchical context menus (otherwise, only the root level gets themed)
      /// </summary>
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

      /// <summary>
      /// handle hierarchical context menus (otherwise, only the root level gets themed)
      /// </summary>
      private void onToolStripMenuItemOpening(object sender, EventArgs e)
      {
         ToolStripMenuItem tsmi = sender as ToolStripMenuItem;
         if (tsmi == null) return; //should not occur

         if (tsmi.DropDown.Items.Count > 0) applyThemeToControl(tsmi.DropDown);

         //once processed, remove itself to prevent multiple executions (when user leaves and reenters the sub-menu)
         tsmi.DropDownOpening -= onToolStripMenuItemOpening;
      }

      /// <summary>Colorea una imagen usando una Matrix de Color.</summary>
      /// <param name="bmp">Imagen a Colorear</param>
      /// <param name="c">Color a Utilizar</param>
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

      /// <summary>Attempts to apply Window's Dark Style to the Control and all its childs.</summary>
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

      #endregion Private Methods

      #region Private members

      /// <summary>The Parent form for them all.</summary>
      private Form _ownerForm { get; set; }

      /// <summary>Windows Colors. Can be customized.</summary>
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
         e.ToolStrip.BackColor = MyColors.Control;
         base.OnRenderToolStripBackground(e);
      }

      // Draws background of the whole ToolBar Or MenuBar
      protected override void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e)
      {
         e.ToolStrip.BackColor = MyColors.Control;
         base.OnRenderToolStripStatusLabelBackground(e);
      }

      // For Normal Buttons on a ToolBar:
      protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
      {
         Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

         Color gradientBegin = MyColors.Control; // Color.FromArgb(203, 225, 252);
         Color gradientEnd = MyColors.Control;

         Pen BordersPencil = new Pen(MyColors.Control);

         ToolStripButton button = e.Item as ToolStripButton;
         if (button.Pressed || button.Checked)
         {
            gradientBegin = MyColors.ControlLightLight;
            gradientEnd = MyColors.ControlLightLight;
         }
         else if (button.Selected)
         {
            gradientBegin = MyColors.ControlLight;
            gradientEnd = MyColors.ControlLight;
         }

         using (Brush b = new LinearGradientBrush(
           bounds,
           gradientBegin,
           gradientEnd,
           LinearGradientMode.Vertical))
         {
            e.Graphics.FillRectangle(b, bounds);
         }

         e.Graphics.DrawRectangle(
           BordersPencil,
           bounds);

         e.Graphics.DrawLine(
           BordersPencil,
           bounds.X,
           bounds.Y,
           bounds.Width - 1,
           bounds.Y);

         e.Graphics.DrawLine(
           BordersPencil,
           bounds.X,
           bounds.Y,
           bounds.X,
           bounds.Height - 1);

			if (!(button.Owner.GetItemAt(button.Bounds.X, button.Bounds.Bottom + 1) is ToolStripButton))
         {
				e.Graphics.DrawLine(
              BordersPencil,
              bounds.X,
              bounds.Height - 1,
              bounds.X + bounds.Width - 1,
              bounds.Height - 1);
         }
      }

      protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
      {
         using (Brush brush = new SolidBrush(MyColors.BackgroundDark))
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

      // For the Text Color of all Items:
      protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
      {
         if (e.Item is ToolStripMenuItem)
         {
            if (e.Item.Enabled)
            {
               e.TextColor = MyColors.TextActive;
            }
            else
            {
               e.TextColor = MyColors.TextInactive;
            }
         }
         base.OnRenderItemText(e);
      }

      protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
      {
         // Does nothing, see 
         // https://stackoverflow.com/questions/30819050/what-does-toolstripprofessionalrenderer-onrenderitembackground-do
         base.OnRenderItemBackground(e);
      }

      // For Menu Items BackColor:
      protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
      {
         Graphics g = e.Graphics;
         Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);

         Color gradientBegin = MyColors.Background; // Color.FromArgb(203, 225, 252);
         Color gradientEnd = MyColors.Background; // Color.FromArgb(125, 165, 224);

         bool DrawIt = false;
         var _menu = e.Item as ToolStripItem;
         if (_menu.Pressed)
         {
            gradientBegin = MyColors.ControlLightLight; // Color.FromArgb(254, 128, 62);
            gradientEnd = MyColors.ControlLightLight; // Color.FromArgb(255, 223, 154);
            DrawIt = true;
         }
         else if (_menu.Selected)
         {
            gradientBegin = MyColors.ControlLight;// Color.FromArgb(255, 255, 222);
            gradientEnd = MyColors.ControlLight; // Color.FromArgb(255, 203, 136);
            DrawIt = true;
         }

         if (DrawIt)
         {
            using (Brush b = new LinearGradientBrush(
            bounds,
            gradientBegin,
            gradientEnd,
            LinearGradientMode.Vertical))
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
         get { return Colors.Control; }
      }

      public override Color ImageMarginGradientMiddle
      {
         get { return Colors.Control; }
      }

      public override Color ImageMarginGradientEnd
      {
         get { return Colors.Control; }
      }

      private OSThemeColors Colors { get; }
   }

   /// <summary>
   /// Stores additional info related to the Controls
   /// </summary>
   public class ControlStatusStorage
   {
      /// <summary>
      /// Storage for the data. ConditionalWeakTable ensures there are no unnecessary references left, preventing garbage collection.
      /// </summary>
      private readonly ConditionalWeakTable<Control, ControlStatusInfo> _controlsProcessed = new ConditionalWeakTable<Control, ControlStatusInfo>();

      /// <summary>
      /// Registers the Control as processed. Prevents applying theme to the Control.
      /// Call it before applying the theme to your Form (or to any other Control containing (directly or indirectly) this Control)
      /// </summary>
      public void ExcludeFromProcessing(Control control)
      {
         _controlsProcessed.Remove(control);
         _controlsProcessed.Add(control, new ControlStatusInfo() { IsExcluded = true });
      }

      /// <summary>
      /// Gets the additional info associated with a Control
      /// </summary>
      /// <returns>a ControlStatusInfo object if the control has been already processed or marked for exclusion, null otherwise</returns>
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

   /// <summary>
   /// Additional information related to the Controls
   /// </summary>
   public class ControlStatusInfo
   {
      /// <summary>
      /// true if the user wants to skip theming the Control
      /// </summary>
      public bool IsExcluded { get; set; }

      /// <summary>
      /// whether the last theme applied was dark
      /// </summary>
      public bool LastThemeAppliedIsDark { get; set; }
   }
}