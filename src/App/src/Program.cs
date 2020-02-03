using System;
using System.Windows.Forms;
using mrHelper.App.Forms;

namespace mrHelper.App
{
   internal static class Program
   {
      [STAThread]
      private static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         Application.Run(new MainForm());
      }
   }
}

