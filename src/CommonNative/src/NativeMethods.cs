/// Copy/pasted from https://www.codeproject.com/tips/1017834/how-to-send-data-from-one-process-to-another-in-cs

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace mrHelper.CommonNative
{
   public static class NativeMethods
   {
      /// <summary>
      /// Retrieves a handle to the top-level window whose class name and window name match
      /// the specified strings.
      /// This function does not search child windows.
      /// This function does not perform a case-sensitive search.
      /// </summary>
      /// <param name="lpClassName">If lpClassName is null, it finds any window whose title matches
      /// the lpWindowName parameter.</param>
      /// <param name="lpWindowName">The window name (the window's title). If this parameter is null,
      /// all window names match.</param>
      /// <returns>If the function succeeds, the return value is a handle to the window
      /// that has the specified class name and window name.</returns>
      [DllImport("user32.dll", SetLastError = true)]
      public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

      [DllImport("user32.dll")]
      public static extern IntPtr GetForegroundWindow();

      [DllImport("user32.dll")]
      public static extern bool SetForegroundWindow(IntPtr hWnd);

      public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

      [DllImport("user32.dll")]
      public static extern int GetWindowThreadProcessId(IntPtr hwnd, out IntPtr lpdwProcessId);

      [DllImport("kernel32.dll")]
      public static extern int GetCurrentThreadId();

      [DllImport("user32.dll")]
      public static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

      [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
      public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

      [DllImport("user32.dll")]
      public static extern bool IsZoomed(IntPtr hWnd);

      [DllImport("user32.dll")]
      public static extern bool IsIconic(IntPtr hWnd);

      [DllImport("user32.dll")]
      public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

      [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      public static extern int AttachThreadInput(int idAttach, int idAttachTo, int fAttach);

      /// <summary>
      /// Win32 API Constants for ShowWindowAsync()
      /// </summary>
      public const int SW_HIDE = 0;
      public const int SW_SHOWNORMAL = 1;
      public const int SW_SHOWMINIMIZED = 2;
      public const int SW_SHOWMAXIMIZED = 3;
      public const int SW_SHOWNOACTIVATE = 4;
      public const int SW_RESTORE = 9;
      public const int SW_SHOWDEFAULT = 10;

      public const int EM_GETLINECOUNT = 0xba;

      /// <summary>
      /// Handle used to send the message to all windows
      /// </summary>
      public static IntPtr HWND_BROADCAST = new IntPtr(0xffff);

      /// <summary>
      /// An application sends the WM_COPYDATA message to pass data to another application.
      /// </summary>
      public static uint WM_COPYDATA = 0x004A;

      /// <summary>
      /// Contains data to be passed to another application by the WM_COPYDATA message.
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct COPYDATASTRUCT
      {
         /// <summary>
         /// User defined data to be passed to the receiving application.
         /// </summary>
         public IntPtr dwData;

         /// <summary>
         /// The size, in bytes, of the data pointed to by the lpData member.
         /// </summary>
         public int cbData;

         /// <summary>
         /// The data to be passed to the receiving application. This member can be IntPtr.Zero.
         /// </summary>
         public IntPtr lpData;
      }

      public const int WM_NOTIFY = 0x004E;

      // https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/NativeMethods.cs,7acb2432aa13375f
      public const int HDN_DIVIDERDBLCLICKA = -305;
      public const int HDN_DIVIDERDBLCLICKW = -325;

      [StructLayout(LayoutKind.Sequential)]
      public class NMHDR
      {
         public IntPtr hwndFrom;
         public int idFrom;
         public int code;
      }

      [StructLayout(LayoutKind.Sequential)]
      public class NMHEADER
      {
         public NMHDR nmhdr;
         public int iItem = 0;
         public int iButton = 0;
         public IntPtr pItem = IntPtr.Zero;    // HDITEM*
      }

      /// <summary>
      /// Sends the specified message to a window or windows.
      /// </summary>
      /// <param name="hWnd">A handle to the window whose window procedure will receive the message.
      /// If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level
      /// windows in the system.</param>
      /// <param name="Msg">The message to be sent.</param>
      /// <param name="wParam">Additional message-specific information.</param>
      /// <param name="lParam">Additional message-specific information.</param>
      /// <returns>The return value specifies the result of the message processing;
      /// it depends on the message sent.</returns>
      [DllImport("user32.dll", CharSet = CharSet.Unicode)]
      public static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

      /// <summary>
      /// Values used in the struct CHANGEFILTERSTRUCT
      /// </summary>
      public enum MessageFilterInfo : uint
      {
         /// <summary>
         /// Certain messages whose value is smaller than WM_USER are required to pass
         /// through the filter, regardless of the filter setting.
         /// There will be no effect when you attempt to use this function to
         /// allow or block such messages.
         /// </summary>
         None = 0,

         /// <summary>
         /// The message has already been allowed by this window's message filter,
         /// and the function thus succeeded with no change to the window's message filter.
         /// Applies to MSGFLT_ALLOW.
         /// </summary>
         AlreadyAllowed = 1,

         /// <summary>
         /// The message has already been blocked by this window's message filter,
         /// and the function thus succeeded with no change to the window's message filter.
         /// Applies to MSGFLT_DISALLOW.
         /// </summary>
         AlreadyDisAllowed = 2,

         /// <summary>
         /// The message is allowed at a scope higher than the window.
         /// Applies to MSGFLT_DISALLOW.
         /// </summary>
         AllowedHigher = 3
      }

      /// <summary>
      /// Values used by ChangeWindowMessageFilterEx
      /// </summary>
      public enum ChangeWindowMessageFilterExAction : uint
      {
         /// <summary>
         /// Resets the window message filter for hWnd to the default.
         /// Any message allowed globally or process-wide will get through,
         /// but any message not included in those two categories,
         /// and which comes from a lower privileged process, will be blocked.
         /// </summary>
         Reset = 0,

         /// <summary>
         /// Allows the message through the filter.
         /// This enables the message to be received by hWnd,
         /// regardless of the source of the message,
         /// even it comes from a lower privileged process.
         /// </summary>
         Allow = 1,

         /// <summary>
         /// Blocks the message to be delivered to hWnd if it comes from
         /// a lower privileged process, unless the message is allowed process-wide
         /// by using the ChangeWindowMessageFilter function or globally.
         /// </summary>
         DisAllow = 2
      }

      /// <summary>
      /// Contains extended result information obtained by calling
      /// the ChangeWindowMessageFilterEx function.
      /// </summary>
      [StructLayout(LayoutKind.Sequential)]
      public struct CHANGEFILTERSTRUCT
      {
         /// <summary>
         /// The size of the structure, in bytes. Must be set to sizeof(CHANGEFILTERSTRUCT),
         /// otherwise the function fails with ERROR_INVALID_PARAMETER.
         /// </summary>
         public uint size;

         /// <summary>
         /// If the function succeeds, this field contains one of the following values,
         /// <see cref="MessageFilterInfo"/>
         /// </summary>
         public MessageFilterInfo info;
      }

      /// <summary>
      /// Modifies the User Interface Privilege Isolation (UIPI) message filter for a specified window
      /// </summary>
      /// <param name="hWnd">
      /// A handle to the window whose UIPI message filter is to be modified.</param>
      /// <param name="msg">The message that the message filter allows through or blocks.</param>
      /// <param name="action">The action to be performed, and can take one of the following values
      /// <see cref="MessageFilterInfo"/></param>
      /// <param name="changeInfo">Optional pointer to a
      /// <see cref="CHANGEFILTERSTRUCT"/> structure.</param>
      /// <returns>If the function succeeds, it returns TRUE; otherwise, it returns FALSE.
      /// To get extended error information, call GetLastError.</returns>
      [DllImport("user32.dll", SetLastError = true)]
      public static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg,
        ChangeWindowMessageFilterExAction action, ref CHANGEFILTERSTRUCT changeInfo);

      public const int CTRL_C_EVENT = 0;
      [DllImport("kernel32.dll")]
      public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
      [DllImport("kernel32.dll", SetLastError = true)]
      public static extern bool AttachConsole(uint dwProcessId);
      [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
      public static extern bool FreeConsole();
      [DllImport("kernel32.dll")]
      public static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

      // Delegate type to be used as the Handler Routine for SCCH
      public delegate Boolean ConsoleCtrlDelegate(uint CtrlType);

      internal const int LVM_FIRST = 0x1000;
      internal const int LVM_SCROLL = LVM_FIRST + 20;
      internal const int LVM_GETHEADER = LVM_FIRST + 31;
      internal const int SBS_HORZ = 0;
      internal const int SBS_VERT = 1;

      [DllImport("user32.dll")]
      public static extern int GetScrollPos(IntPtr hWnd, int nBar);

      [DllImport("user32.dll")]
      public static extern bool LockWindowUpdate(IntPtr Handle);

      [Serializable, StructLayout(LayoutKind.Sequential)]
      public struct RECT 
      {
          public int Left;
          public int Top;
          public int Right;
          public int Bottom;
      }

      [DllImport("user32.dll")]
      public static extern bool GetWindowRect(IntPtr Handle, out RECT rect);

      /// <summary>
      /// A set of constants to handle window activation
      /// </summary>
      public static uint WM_MOUSEACTIVATE = 0x0021;
      public static uint MA_ACTIVATE         = 1;
      public static uint MA_ACTIVATEANDEAT   = 2;
      public static uint MA_NOACTIVATE       = 4;
      public static uint MA_NOACTIVATEANDEAT = 4;
   }
}

