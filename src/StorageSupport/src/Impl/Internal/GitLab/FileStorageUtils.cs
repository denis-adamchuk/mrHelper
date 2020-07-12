using System;
using System.Collections.Generic;
using mrHelper.Common.Tools;
using mrHelper.Common.Exceptions;
using mrHelper.Common.Interfaces;
using GitLabSharp.Entities;

namespace mrHelper.StorageSupport
{
   internal static class FileStorageUtils
   {
      internal static void InitalizeFileStorage(string path, ProjectKey projectKey)
      {
         string descriptionFilepath = System.IO.Path.Combine(path, FileStorageConfig);
         if (System.IO.File.Exists(descriptionFilepath))
         {
            return;
         }

         if (!System.IO.Directory.Exists(path))
         {
            System.IO.Directory.CreateDirectory(path);
         }

         FileStorageDescription fileStorageDescription = new FileStorageDescription
         {
            HostName = projectKey.HostName,
            ProjectName = projectKey.ProjectName
         };

         try
         {
            JsonUtils.SaveToFile(descriptionFilepath, fileStorageDescription);
         }
         catch (Exception ex)
         {
            ExceptionHandlers.Handle("Cannot serialize FileStorageDescription object", ex);
         }
      }

      internal static ProjectKey? GetFileStorageProjectKey(string path)
      {
         string descriptionFilename = System.IO.Path.Combine(path, FileStorageConfig);
         if (System.IO.File.Exists(descriptionFilename))
         {
            try
            {
               FileStorageDescription x = JsonUtils.LoadFromFile<FileStorageDescription>(descriptionFilename);
               return new ProjectKey(x.HostName, x.ProjectName);
            }
            catch (Exception ex)
            {
               ExceptionHandlers.Handle("Cannot read serialized FileStorageDescription object", ex);
            }
         }
         return null;
      }

      internal static IEnumerable<FileRevision> CreateFileRevisions(IEnumerable<DiffStruct> diffs, string sha, bool old)
      {
         List<FileRevision> revisions = new List<FileRevision>();
         foreach (DiffStruct diff in diffs)
         {
            if (old && !String.IsNullOrWhiteSpace(diff.Old_Path) && !diff.New_File)
            {
               revisions.Add(new FileRevision(diff.Old_Path, sha));
            }
            else if (!old && !String.IsNullOrWhiteSpace(diff.New_Path) && !diff.Deleted_File)
            {
               revisions.Add(new FileRevision(diff.New_Path, sha));
            }
         }
         return revisions;
      }

      private class FileStorageDescription
      {
         // Don't make it immutable to avoid adding JsonProperty tags to avoid dependency from Newtonsoft.Json
         public string HostName { get; set; }
         public string ProjectName { get; set; }
      }

      private static readonly string FileStorageConfig = "mrHelper.filestorage.json";
   }
}

