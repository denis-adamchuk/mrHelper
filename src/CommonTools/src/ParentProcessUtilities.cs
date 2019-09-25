using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mrHelper.CommonTools
{

/// <summary>
/// A utility class to determine a process parent.
/// Took from: https://stackoverflow.com/questions/394816/how-to-get-parent-process-in-net-in-managed-way
/// Note: this is an unportable but fast solution
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ParentProcessUtilities
{
   // These members must match PROCESS_BASIC_INFORMATION
   private struct PROCESS_BASIC_INFORMATION
   {
      public IntPtr Reserved1;
      public IntPtr PebBaseAddress;
      public IntPtr Reserved2_0;
      public IntPtr Reserved2_1;
      public IntPtr UniqueProcessId;
      public IntPtr InheritedFromUniqueProcessId;
   }

   [DllImport("ntdll.dll")]
   private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
      ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

   /// <summary>
   /// Gets the parent process of specified process.
   /// </summary>
   /// <param name="process">The process object.</param>
   /// <returns>An instance of the Process class.</returns>
   public static Process GetParentProcess(Process process)
   {
      return GetParentProcess(process.Handle);
   }

   /// <summary>
   /// Gets the parent process of a specified process.
   /// </summary>
   /// <param name="handle">The process handle.</param>
   /// <returns>An instance of the Process class.</returns>
   private static Process GetParentProcess(IntPtr handle)
   {
      PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
      int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
      if (status != 0)
      {
         throw new Win32Exception(status);
      }

      try
      {
         return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
      }
      catch (Exception)
      {
         // not found
         return null;
      }
   }
}

}

