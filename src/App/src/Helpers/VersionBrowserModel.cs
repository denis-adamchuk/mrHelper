#pragma warning disable 67  // Event never used

using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using Aga.Controls.Tree;

namespace mrHelper.App.Helpers
{
   public class VersionBrowserModelData
   {
      public VersionBrowserModelData(IEnumerable<Commit> commits, IEnumerable<Version> versions,
         string baseSha, string targetBranch)
      {
         Objects = new Dictionary<string, IEnumerable<object>>();
         Objects.Add("Versions", versions);
         Objects.Add("Commits", commits);
         BaseSha = baseSha;
         TargetBranch = targetBranch;
      }

      public Dictionary<string, IEnumerable<object>> Objects { get; }
      public string BaseSha { get; }
      public string TargetBranch { get; }
   }

   public class VersionBrowserModel : ITreeModel
   {
      public VersionBrowserModel()
      {
      }

      internal VersionBrowserModelData Data
      {
         get { return _data; }
         set { _data = value; StructureChanged?.Invoke(this, new TreePathEventArgs()); }
      }

      public System.Collections.IEnumerable GetChildren(TreePath treePath)
      {
         if (!treePath.IsEmpty() && !(treePath.LastNode is BaseVersionBrowserItem))
         {
            return null;
         }

         List<BaseVersionBrowserItem> items = new List<BaseVersionBrowserItem>();
         if (treePath.IsEmpty())
         {
            items.Add(new RootVersionBrowserItem("Versions", this));
            items.Add(new RootVersionBrowserItem("Commits", this));
         }
         else
         {
            if (treePath.LastNode is RootVersionBrowserItem parent &&
                Data.Objects.TryGetValue(parent.Name, out IEnumerable<object> objects))
            {
               int iObject = objects.Count();
               foreach (object o in objects)
               {
                  if (o is Version version)
                  {
                     string name = String.Format("Version #{0}", iObject);
                     string sha = getSha(version.Head_Commit_SHA);
                     items.Add(new LeafVersionBrowserItem(name, version.Created_At, sha, parent, this));
                     --iObject;
                  }
                  else if (o is Commit commit)
                  {
                     string name = commit.Title;
                     string sha = getSha(commit.Id);
                     items.Add(new LeafVersionBrowserItem(name, commit.Created_At, sha, parent, this));
                  }
               }
            }
         }
         return items;
      }

      public bool IsLeaf(TreePath treePath)
      {
         return treePath.LastNode is LeafVersionBrowserItem;
      }

      public event EventHandler<TreeModelEventArgs> NodesChanged;
      public event EventHandler<TreeModelEventArgs> NodesInserted;
      public event EventHandler<TreeModelEventArgs> NodesRemoved;
      public event EventHandler<TreePathEventArgs> StructureChanged;

      private string getSha(string fullSha)
      {
         return fullSha.Substring(0, Math.Min(10, fullSha.Length));
      }

      private VersionBrowserModelData _data;
   }
}

