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
            else if (arguments[1] == "diff" && arguments.Length == 6)
            {
               // Launch from diff tool. Trim two first arguments and pass exactly four to the parser.
               string[] diffArgs = new string[4];
               Array.Copy(arguments, 2, diffArgs, 0, 4);
               DiffArgumentsParser argumentsParser = new DiffArgumentsParser(diffArgs);
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
