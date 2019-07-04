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
         // TEST Code begin
         //DiffContextBuilder build = new DiffContextBuilder("d://git_temp//test_project");
         //PositionDetails pd = new PositionDetails();
         //pd.Refs.HeadSHA = "b2115fe4f350e2568515888b202bf5096e46466b";
         //pd.Refs.BaseSHA = pd.Refs.StartSHA = "84815eb18137bade6586016a0a67d727636afa1a";
         //pd.NewPath = pd.OldPath = "btsrc.hpp";
         //pd.OldLine = "19";
         //pd.NewLine = "30";
         //var context = build.GetContext(pd, 3);
         //foreach (var line in context.lines)
         //{
         //   Console.WriteLine(line.State.ToString() + "    " + line.Text);
         //}

         // return;
         // TEST Code end

         
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
               // Launch from diff tool
               DiffArgumentsParser argumentsParser = new DiffArgumentsParser(arguments);
               DiffToolInfo diffToolInfo = argumentsParser.Parse();

               DetailedSnapshotSerializer serializer = new DetailedSnapshotSerializer();
               var connectedMergeRequestDetails = serializer.DeserializeFromDisk();
               if (!connectedMergeRequestDetails.HasValue)
               {
                  throw new ArgumentException("To create a discussion you need to start tracking time and have a running diff tool");
               }
               Application.Run(new NewDiscussionForm(connectedMergeRequestDetails.Value, diffToolInfo));
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
