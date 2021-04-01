using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mrHelper.CommonNative
{
   public static class Win32Tools
   {
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

      public static IEnumerable<IntPtr> EnumerateProcessWindowHandles(int processId)
      {
         List<IntPtr> handles = new List<IntPtr>();

         foreach (ProcessThread thread in Process.GetProcessById(processId).Threads)
         {
            NativeMethods.EnumThreadWindows(thread.Id,
               (hWnd, lParam) =>
            {
               handles.Add(hWnd);
               return true;
            }, IntPtr.Zero);
         }

         return handles;
      }

      public static void SendMessageToWindow(IntPtr window, string message)
      {
         IntPtr ptrCopyData = IntPtr.Zero;
         try
         {
            NativeMethods.COPYDATASTRUCT copyData = new NativeMethods.COPYDATASTRUCT
            {
               dwData = new IntPtr(0),
               cbData = message.Length + 1,
               lpData = Marshal.StringToHGlobalAnsi(message)
            };

            ptrCopyData = Marshal.AllocCoTaskMem(Marshal.SizeOf(copyData));
            Marshal.StructureToPtr(copyData, ptrCopyData, false);

            NativeMethods.SendMessage(window, NativeMethods.WM_COPYDATA, IntPtr.Zero, ptrCopyData);
         }
         finally
         {
            if (ptrCopyData != IntPtr.Zero)
            {
               Marshal.FreeCoTaskMem(ptrCopyData);
            }
         }
      }

      public static string ConvertMessageToText(IntPtr message)
      {
         NativeMethods.COPYDATASTRUCT copyData = (NativeMethods.COPYDATASTRUCT)Marshal.PtrToStructure(
            message, typeof(NativeMethods.COPYDATASTRUCT));
         return Marshal.PtrToStringAnsi(copyData.lpData);
      }

      // Taken from https://stackoverflow.com/questions/17879890/understanding-attachthreadinput-detaching-lose-focus
      public static void ForceWindowIntoForeground(IntPtr window)
      {
         int currentThread = NativeMethods.GetCurrentThreadId();

         IntPtr activeWindow = NativeMethods.GetForegroundWindow();
         int activeThread = NativeMethods.GetWindowThreadProcessId(activeWindow, out IntPtr activeProcess);

         int windowThread = NativeMethods.GetWindowThreadProcessId(window, out IntPtr windowProcess);

         if (currentThread != activeThread)
         {
            NativeMethods.AttachThreadInput(currentThread, activeThread, 1);
         }
         if (windowThread != currentThread)
         {
            NativeMethods.AttachThreadInput(windowThread, currentThread, 1);
         }

         NativeMethods.SetForegroundWindow(window);
         restoreWindow(window);

         if (currentThread != activeThread)
         {
            NativeMethods.AttachThreadInput(currentThread, activeThread, 0);
         }
         if (windowThread != currentThread)
         {
            NativeMethods.AttachThreadInput(windowThread, currentThread, 0);
         }
      }

      public static int GetVerticalScrollPosition(IntPtr hWnd)
      {
         return NativeMethods.GetScrollPos(hWnd, NativeMethods.SBS_VERT);
      }

      public static void SetVerticalScrollPosition(IntPtr hWnd, int position)
      {
         NativeMethods.SendMessage(hWnd, NativeMethods.LVM_SCROLL, IntPtr.Zero, (IntPtr)position);
      }

      private static void restoreWindow(IntPtr window)
      {
         int nCmdShow = NativeMethods.SW_SHOWNORMAL;
         if (NativeMethods.IsIconic(window))
         {
            nCmdShow = NativeMethods.SW_RESTORE;
         }
         else if (NativeMethods.IsZoomed(window))
         {
            nCmdShow = NativeMethods.SW_SHOWMAXIMIZED;
         }
         NativeMethods.ShowWindowAsync(window, nCmdShow);
      }
   }
}

