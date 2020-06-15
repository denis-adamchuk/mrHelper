﻿#pragma warning disable 67  // Event never used

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using Aga.Controls.Tree;

namespace mrHelper.App.Helpers
{
   internal class RevisionBrowserModelData
   {
      internal RevisionBrowserModelData()
      {
      }

      internal RevisionBrowserModelData(string baseSha,
         IEnumerable<Commit> commits, IEnumerable<Version> versions, IEnumerable<string> reviewedRevisions)
      {
         BaseSha = baseSha;
         Revisions = new Dictionary<RevisionType, IEnumerable<object>>();
         Revisions.Add(RevisionType.Version, versions);
         Revisions.Add(RevisionType.Commit, commits);
         ReviewedRevisions = reviewedRevisions;
      }

      internal string BaseSha { get; }
      internal Dictionary<RevisionType, IEnumerable<object>> Revisions { get; }
      internal IEnumerable<string> ReviewedRevisions { get; }
   }

   internal class RevisionBrowserModel : ITreeModel
   {
      internal RevisionBrowserModel()
      {
      }

      internal RevisionBrowserModelData Data
      {
         get
         {
            return _data;
         }
         set
         {
            _data = value;
            StructureChanged?.Invoke(this, new TreePathEventArgs()); // refresh the view
         }
      }

      public System.Collections.IEnumerable GetChildren(TreePath treePath)
      {
         if (!treePath.IsEmpty() && !(treePath.LastNode is RevisionBrowserBaseItem))
         {
            return null;
         }

         List<RevisionBrowserBaseItem> items = new List<RevisionBrowserBaseItem>();
         if (treePath.IsEmpty())
         {
            items.Add(new RevisionBrowserTypeItem(RevisionType.Version, this));
            items.Add(new RevisionBrowserTypeItem(RevisionType.Commit, this));
         }
         else
         {
            if (treePath.LastNode is RevisionBrowserTypeItem parent
             && Data?.Revisions != null
             && Data.Revisions.TryGetValue(parent.Type, out IEnumerable<object> objects))
            {
               int iItem = objects.Count();
               foreach (object item in sortByTimestamp(objects))
               {
                  getItemProperties(iItem, item,
                     out string fullSha, out string name, out DateTime timestamp, out string tooltipText);
                  bool isReviewed = _data.ReviewedRevisions.Contains(fullSha);
                  items.Add(new RevisionBrowserItem(name, timestamp, fullSha, parent, this, tooltipText, isReviewed));
                  --iItem;
               }
            }
         }
         return items;
      }

      public bool IsLeaf(TreePath treePath)
      {
         return treePath.LastNode is RevisionBrowserItem;
      }

      public event EventHandler<TreeModelEventArgs> NodesChanged;
      public event EventHandler<TreeModelEventArgs> NodesInserted;
      public event EventHandler<TreeModelEventArgs> NodesRemoved;
      public event EventHandler<TreePathEventArgs> StructureChanged;

      private IEnumerable<object> sortByTimestamp(IEnumerable<object> objects)
      {
         return objects.OrderByDescending(
            x =>
         {
            if (x is Commit c)
            {
               return c.Created_At;
            }
            else if (x is Version v)
            {
               return v.Created_At;
            }
            else
            {
               throw new NotImplementedException();
            }
         });
      }

      private void getItemProperties(int iItem, object item,
         out string fullSha, out string name, out DateTime timestamp, out string tooltipText)
      {
         if (item is Version version)
         {
            fullSha = String.IsNullOrEmpty(version.Head_Commit_SHA) ? "N/A" : version.Head_Commit_SHA;
            timestamp = version.Created_At;

            string sha = fullSha.Substring(0, Math.Min(10, fullSha.Length));
            Commit versionCommit = version.Commits?.FirstOrDefault();
            tooltipText = versionCommit == null ? String.Empty : versionCommit.Message;

            name = String.Format("Version #{0} ({1})", iItem, sha);
         }
         else if (item is Commit commit)
         {
            name = commit.Title;
            fullSha = commit.Id;
            timestamp = commit.Created_At;
            tooltipText = commit.Message;
         }
         else
         {
            throw new NotImplementedException();
         }
      }

      private RevisionBrowserModelData _data;
   }
}

