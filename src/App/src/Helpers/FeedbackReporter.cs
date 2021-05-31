using System;
using System.IO;
using System.Linq;
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
      public FeedbackReporter(Action preCollectLogFiles, Action postCollectLogFiles,
         string logPath, string dumpPath)
      {
         _preCollectLogFiles = preCollectLogFiles;
         _postCollectLogFiles = postCollectLogFiles;
         _logPath = logPath;
         _dumpPath = dumpPath;
      }

      public void SetUserEMail(string email)
      {
         _email = email;
      }

      public void SendEMail(string subject, string body, string recipient,
         string logarchivename, string dumparchivename)
      {
         createLogArchiveStorage();

         string logarchivepath = createLogArchive(logarchivename);
         string dumparchivepath = createDumpArchive(dumparchivename);

         sendEMail(subject, body, recipient, logarchivepath, dumparchivepath);

         cleanupDumps();
      }

      private void sendEMail(string subject, string body, string recipient, string logarchivepath, string dumparchivepath)
      {
         try
         {
            EMailSender.Send(logarchivepath, dumparchivepath, _email, recipient, body, subject);
         }
         catch (Exception ex) // Any exception from external API
         {
            throw new SendEMailException(ex);
         }
      }

      private static void createLogArchiveStorage()
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
      }

      private string createLogArchive(string logarchivename)
      {
         string logarchivepath = String.IsNullOrEmpty(logarchivename) ?
            String.Empty : Path.Combine(PathFinder.LogArchiveStorage, logarchivename);
         if (logarchivepath != String.Empty)
         {
            try
            {
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
            catch (Exception ex) // Any exception from ZipFile.CreateFromDirectory()
            {
               throw new LogCollectException(ex);
            }
         }
         return logarchivepath;
      }

      private string createDumpArchive(string dumparchivename)
      {
         bool hasDumps = Directory.Exists(_dumpPath) && Directory.EnumerateFiles(_dumpPath).Any();
         string dumparchivepath = !hasDumps || String.IsNullOrEmpty(dumparchivename) ?
            String.Empty : Path.Combine(PathFinder.LogArchiveStorage, dumparchivename);
         if (dumparchivepath != String.Empty)
         {
            try
            {
               ZipFile.CreateFromDirectory(_dumpPath, dumparchivepath);
            }
            catch (Exception ex) // Any exception from ZipFile.CreateFromDirectory()
            {
               throw new LogCollectException(ex);
            }
         }
         return dumparchivepath;
      }

      private void cleanupDumps()
      {
         if (Directory.Exists(_dumpPath))
         {
            try
            {
               Directory.Delete(_dumpPath, true);
            }
            catch (Exception) //Any exception from Directory.Delete()
            {
            }
         }
      }

      private readonly string _dumpPath;
      private readonly string _logPath;
      private readonly Action _preCollectLogFiles;
      private readonly Action _postCollectLogFiles;
      private string _email;
   }
}

