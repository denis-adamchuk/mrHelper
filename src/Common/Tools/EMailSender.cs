using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Diagnostics;
using Microsoft.Win32;
using Outlook = Microsoft.Office.Interop.Outlook;
using mrHelper.Common.Exceptions;

namespace mrHelper.Common.Tools
{
   public class EMailSenderException : ExceptionEx
   {
      public EMailSenderException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

   public static class EMailSender
   {
      public static void Send(string logarchivepath, string dumparchivepath,
         string sender, string recipient, string body, string subject)
      {
         if (!new DesktopBridge.Helpers().IsRunningAsUwp())
         {
            try
            {
               sendFromOutlook(logarchivepath, dumparchivepath, recipient, body, subject);
               return;
            }
            catch (EMailSenderException ex)
            {
               // not a fatal exception
               ExceptionHandlers.Handle("Cannot send e-mail from Outlook application", ex);
            }
         }

         // TODO Add support of dumparchivepath
         try
         {
            sendFromEmlAssociation(logarchivepath, sender, recipient, body, subject);
            return;
         }
         catch (EMailSenderException ex)
         {
            // not a fatal exception
            ExceptionHandlers.Handle("Cannot send e-mail from .eml-associated application", ex);
         }

         sendFromDefaultEmailClient(logarchivepath, recipient, subject, body);
      }

      private static void sendFromOutlook(string logarchivepath, string dumparchivepath,
         string recipient, string body, string subject)
      {
         try
         {
            Outlook.Application app = new Outlook.Application();

            Outlook.MailItem message = (Outlook.MailItem)app.CreateItem(Outlook.OlItemType.olMailItem);
            message.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
            message.Body = body;
            message.Subject = subject;
            message.Recipients.Add(recipient).Resolve();

            void addAttachment(string filePath)
            {
               if (!String.IsNullOrWhiteSpace(filePath))
               {
                  string filename = Path.GetFileName(filePath);
                  int position = message.Body.Length + 1;
                  int attachmentType = (int)Outlook.OlAttachmentType.olByValue;
                  message.Attachments.Add(filePath, attachmentType, position, filename);
               }
            }

            addAttachment(logarchivepath);
            addAttachment(dumparchivepath);

            message.Display();
         }
         catch (Exception ex) // Any exception from Outlook API code
         {
            throw new EMailSenderException("Cannot connect to Outlook", ex);
         }
      }

      private static bool sendFromEmlAssociation(
         string logarchivepath, string sender, string recipient, string body, string subject)
      {
         sender = String.IsNullOrWhiteSpace(sender) ? recipient : sender;
         MailMessage message = new MailMessage(sender, recipient, subject, body);
         if (!String.IsNullOrWhiteSpace(logarchivepath))
         {
            message.Attachments.Insert(0, new Attachment(logarchivepath));
         }
         message.Headers.Add("X-Unsent", "1");

         string emailDirectory = PathFinder.NewEMailDirectory;
         Directory.CreateDirectory(emailDirectory);

         try
         {
            SmtpClient client = new SmtpClient
            {
               DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
               PickupDirectoryLocation = emailDirectory
            };
            client.Send(message);

            if (!Directory.GetFiles(emailDirectory).Any())
            {
               throw new EMailSenderException("Cannot create .eml file", null);
            }

            string emailFilepath = Path.Combine(emailDirectory, Directory.GetFiles(emailDirectory).Single());
            try
            {
               Process p = Process.Start(emailFilepath);
               // TODO It is better to use ExternalProcessManager here but it requires some extra changes
               p.WaitForExit(5000); // should be enough. we will delete file and directory in "finally" block.
               return true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
               throw new EMailSenderException("Cannot open .eml file", ex);
            }
            finally
            {
               if (File.Exists(emailFilepath))
               {
                  File.Delete(emailFilepath);
               }
            }
         }
         finally
         {
            Directory.Delete(emailDirectory);
         }
      }

      private static void sendFromDefaultEmailClient(string logarchivepath,
         string recipient, string subject, string body)
      {
         string filename = Path.GetFileName(logarchivepath);
         string directory = Path.GetDirectoryName(logarchivepath);
         string msg = String.Format(
            "Please attach {0} from {1} (it is opened in Explorer for you) to this e-mail.", filename, directory);
         string command = String.Format("mailto:{0}?subject={1}&body={2} {3}", recipient, subject, msg, body);

         try
         {
            Process.Start(directory);
            Process.Start(command);
         }
         catch (System.ComponentModel.Win32Exception ex)
         {
            throw new EMailSenderException("Cannot open default e-mail client", ex);
         }
      }
   }
}

