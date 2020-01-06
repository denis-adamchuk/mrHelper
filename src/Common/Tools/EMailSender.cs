using System;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace mrHelper.Common.Tools
{
   public static class EMailSender
   {
      public static void Send(string logarchivepath, string recipient, string body, string subject)
      {
         Outlook.Application app = new Outlook.Application();

         Outlook.MailItem message = (Outlook.MailItem)app.CreateItem(Outlook.OlItemType.olMailItem);
         message.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
         message.Body = body;
         message.Subject = subject;
         message.Recipients.Add(recipient).Resolve();

         if (logarchivepath != String.Empty)
         {
            string filename = System.IO.Path.GetFileName(logarchivepath);
            int position = message.Body.Length + 1;
            int attachmentType = (int)Outlook.OlAttachmentType.olByValue;
            Outlook.Attachment oAttach = message.Attachments.Add(logarchivepath, attachmentType, position, filename);
         }

         message.Display();
      }

   }
}
