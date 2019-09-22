using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mrHelper.Common.Tools
{
   public static class Win32Tools
   {
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

      [DllImport("user32.dll", CharSet = CharSet.Ansi)]
      public static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

      public class CopyDataAccessException : Exception
      {
         public CopyDataAccessException(string message) : base(message) { }
      }

      public static void EnableCopyDataMessageHandling(IntPtr handle)
      {
         NativeMethods.CHANGEFILTERSTRUCT changeFilter = new NativeMethods.CHANGEFILTERSTRUCT();
         changeFilter.size = (uint)Marshal.SizeOf(changeFilter);
         changeFilter.info = 0;
         if (!NativeMethods.ChangeWindowMessageFilterEx(handle, NativeMethods.WM_COPYDATA,
            NativeMethods.ChangeWindowMessageFilterExAction.Allow, ref changeFilter))
         {
            int error = Marshal.GetLastWin32Error();
            throw new CopyDataAccessException(String.Format(
               "ChangeWindowMessageFilterEx() failed with error code {0}", error));
         }
      }

      public static void SendMessageToProcess(int pid, string message)
      {
         Process process = Process.GetProcessById(pid);

         IntPtr ptrCopyData = IntPtr.Zero;
         try
         {
            NativeMethods.COPYDATASTRUCT copyData = new NativeMethods.COPYDATASTRUCT();
            copyData.dwData = new IntPtr(0);
            copyData.cbData = message.Length + 1;
            copyData.lpData = Marshal.StringToHGlobalAnsi(message);

            ptrCopyData = Marshal.AllocCoTaskMem(Marshal.SizeOf(copyData));
            Marshal.StructureToPtr(copyData, ptrCopyData, false);

            NativeMethods.SendMessage(process.MainWindowHandle, NativeMethods.WM_COPYDATA, IntPtr.Zero, ptrCopyData);
         }
         finally
         {
            if (ptrCopyData != IntPtr.Zero)
            {
               Marshal.FreeCoTaskMem(ptrCopyData);
            }
         }
      }

      public static string HandleSentMessage(IntPtr message)
      {
         NativeMethods.COPYDATASTRUCT copyData = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(
            message, typeof(NativeMethods.COPYDATASTRUCT));
         int dataType = (int)copyData.dwData;
         return Marshal.PtrToStringAnsi(copyData.lpData);
      }
   }
}
