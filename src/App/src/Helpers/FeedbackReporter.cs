using System;
using System.IO;
using System.IO.Compression;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;

namespace mrHelper.App.Helpers
{
   public class FeedbackReporterException : ExceptionEx
   {
      public FeedbackReporterException(string message, Exception ex)
         : base(message, ex)
      {
      }
   }

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
         _preCollectLogFiles = preCollectLogFiles;
         _postCollectLogFiles = postCollectLogFiles;
         _logPath = logPath;
      }

      public void SetUserEMail(string email)
      {
         _email = email;
      }

      public void SendEMail(string subject, string body, string recipient, string logarchivename)
      {
         if (!Directory.Exists(PathFinder.LogArchiveStorage))
         {
            try
            {
               Directory.CreateDirectory(PathFinder.LogArchiveStorage);
            }
            catch (Exception ex) // Any exception from Directory.CreateDirectory()
            {
               throw new LogCollectException(ex);
            }
         }

         string logarchivepath = String.IsNullOrEmpty(logarchivename) ?
            String.Empty : Path.Combine(PathFinder.LogArchiveStorage, logarchivename);

         try
         {
            createLogArchive(logarchivepath);
         }
         catch (Exception ex) // Any exception from ZipFile.CreateFromDirectory()
         {
            throw new LogCollectException(ex);
         }

         try
         {
            EMailSender.Send(logarchivepath, _email, recipient, body, subject);
         }
         catch (Exception ex) // Any exception from external API
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

         _preCollectLogFiles?.Invoke();
         try
         {
            ZipFile.CreateFromDirectory(_logPath, logarchivepath);
         }
         finally
         {
            _postCollectLogFiles?.Invoke();
         }
      }

      private readonly string _logPath;
      private readonly Action _preCollectLogFiles;
      private readonly Action _postCollectLogFiles;
      private string _email;
   }
}

