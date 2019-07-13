using System;
using System.Windows.Forms;
using mrCore;

namespace mrHelperUI
{
   static class Program
   {
      /// <summary>
      /// The main entry point for the application.
      /// </summary>
      [STAThread]
      static void Main()
      {
         Application.EnableVisualStyles();
         Application.SetCompatibleTextRenderingDefault(false);
         var arguments = Environment.GetCommandLineArgs();
         try
         {
            if (arguments.Length < 2)
            {
               // Ordinary launch
               Application.Run(new mrHelperForm());
            }
            else if (arguments[1] == "diff")
            {
               DiffArgumentsParser argumentsParser = new DiffArgumentsParser(arguments);
               DiffToolInfo diffToolInfo = argumentsParser.Parse();

               InterprocessSnapshotSerializer serializer = new InterprocessSnapshotSerializer();
               var snapshot = serializer.DeserializeFromDisk();
               if (!snapshot.HasValue)
               {
                  throw new ArgumentException("To create a discussion you need to start tracking time and have a running diff tool");
               }
               Application.Run(new NewDiscussionForm(snapshot.Value, diffToolInfo));
            }
            else
            {
               throw new ArgumentException("Unexpected argument");
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }
   }
}
