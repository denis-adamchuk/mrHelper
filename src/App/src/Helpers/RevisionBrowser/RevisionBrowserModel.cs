#pragma warning disable 67  // Event never used

using System;
using System.Linq;
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
             && Data.Revisions != null
             && Data.Revisions.TryGetValue(parent.Type, out IEnumerable<object> objects))
            {
               int iObject = objects.Count();
               foreach (object o in objects)
               {
                  if (o is Version version)
                  {
                     string fullSha = String.IsNullOrEmpty(version.Head_Commit_SHA) ? "N/A" : version.Head_Commit_SHA;
                     string sha = fullSha.Substring(0, Math.Min(10, fullSha.Length));
                     string name = String.Format("Version #{0} ({1})", iObject, sha);
                     string tooltipText = String.Empty; // Not implemented
                     items.Add(new RevisionBrowserItem(name, version.Created_At, fullSha, parent, this, tooltipText,
                        _data.ReviewedRevisions.Contains(fullSha)));
                     --iObject;
                  }
                  else if (o is Commit commit)
                  {
                     string name = commit.Title;
                     string fullSha = commit.Id;
                     string tooltipText = commit.Message;
                     items.Add(new RevisionBrowserItem(name, commit.Created_At, fullSha, parent, this, tooltipText,
                        _data.ReviewedRevisions.Contains(fullSha)));
                  }
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

      private RevisionBrowserModelData _data;
   }
}

