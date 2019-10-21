using mrHelper.Common.Exceptions;
using System;
using System.IO;
using System.IO.Compression;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace mrHelper.CommonTools
{
   public class LogCollectException : FeedbackReporterException
   {
      internal LogCollectException(Exception ex) : base("Failed to collect logs", ex) { }
   }

   public class SendEMailException : FeedbackReporterException
   {
      internal SendEMailException(Exception ex) : base("Failed to send e-mail", ex) { }
   }

   public class FeedbackReporter
   {
      public FeedbackReporter(Action preCollectLogFiles, Action postCollectLogFiles, string logPath)
      {
         PreCollectLogFiles = preCollectLogFiles;
         PostCollectLogFiles = postCollectLogFiles;
         LogPath = logPath;
      }

      public void SendEMail(string subject, string body, string recipient, string logarchivename)
      {
         string logarchivepath = String.IsNullOrEmpty(logarchivename) ?
            String.Empty : Path.Combine(Environment.GetEnvironmentVariable("TEMP"), logarchivename);

         try
         {
            createLogArchive(logarchivepath);
         }
         catch (Exception ex)
         {
            throw new LogCollectException(ex);
         }

         try
         {
            sendEmailFromOutlook(logarchivepath, recipient, body, subject);
         }
         catch (Exception ex)
         {
            throw new SendEMailException(ex);
         }
      }

      private void createLogArchive(string logarchivepath)
      {
         if (logarchivepath == String.Empty)
         {
            return;
         }

         PreCollectLogFiles?.Invoke();
         try
         {
            ZipFile.CreateFromDirectory(LogPath, logarchivepath);
         }
         finally
         {
            PostCollectLogFiles?.Invoke();
         }
      }

      private static void sendEmailFromOutlook(string logarchivepath, string recipient, string body, string subject)
      {
         Outlook.Application app = new Outlook.Application();

         Outlook.MailItem message = (Outlook.MailItem)app.CreateItem(Outlook.OlItemType.olMailItem);
         message.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
         message.Body = body;
         message.Subject = subject;
         message.Recipients.Add(recipient).Resolve();

         if (logarchivepath != String.Empty)
         {
            string filename = Path.GetFileName(logarchivepath);
            int position = message.Body.Length + 1;
            int attachmentType = (int)Outlook.OlAttachmentType.olByValue;
            Outlook.Attachment oAttach = message.Attachments.Add(logarchivepath, attachmentType, position, filename);
         }

         message.Display();
      }

      private string LogPath { get; }
      private readonly Action PreCollectLogFiles;
      private readonly Action PostCollectLogFiles;
   }
}

