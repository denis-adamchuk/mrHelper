using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using mrHelper.CommonNative;

namespace mrHelper.App
{
   internal class LaunchOptions
   {
      public LaunchOptions(LaunchMode mode, object specialOptions)
      {
         Mode = mode;
         SpecialOptions = specialOptions;
      }

      internal enum LaunchMode
      {
         Normal,
         DiffTool,
         Register,
         Unregister
      }

      internal LaunchMode Mode { get; }
      internal object SpecialOptions { get; }

      internal class NormalModeOptions
      {
         public NormalModeOptions(bool startMinimized, string startUrl)
         {
            StartMinimized = startMinimized;
            StartUrl = startUrl;
         }

         internal bool StartMinimized { get; }
         internal string StartUrl { get; }
      }

      internal class DiffToolModeOptions
      {
         public DiffToolModeOptions(LaunchContext launchContext)
         {
            LaunchContext = launchContext;
         }

         internal LaunchContext LaunchContext { get; }
      }

      internal static LaunchOptions FromContext(LaunchContext context)
      {
         bool startMinimized = false;
         string startUrl = null;
         if (context.Arguments.Length == 2 && context.Arguments[1] == "-u")
         {
            return new LaunchOptions(LaunchOptions.LaunchMode.Unregister, null);
         }
         else if (context.Arguments.Length == 2 && context.Arguments[1] == "-m")
         {
            startMinimized = true;
         }
         else if (context.Arguments.Length == 2 && context.Arguments[1] == "-r")
         {
            return new LaunchOptions(LaunchOptions.LaunchMode.Register, null);
         }
         else if (context.Arguments.Length == 2)
         {
            startUrl = context.Arguments[1];
         }
         else if (context.Arguments.Length > 3 && context.Arguments[1] == "diff")
         {
            LaunchOptions.DiffToolModeOptions diffOptions = new LaunchOptions.DiffToolModeOptions(context);
            return new LaunchOptions(LaunchOptions.LaunchMode.DiffTool, diffOptions);
         }

         LaunchOptions.NormalModeOptions options = new LaunchOptions.NormalModeOptions(startMinimized, startUrl);
         return new LaunchOptions(LaunchOptions.LaunchMode.Normal, options);
      }
   }
}

