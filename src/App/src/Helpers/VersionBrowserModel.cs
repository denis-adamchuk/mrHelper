#pragma warning disable 67  // Event never used

using System;
using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;
using Version = GitLabSharp.Entities.Version;
using Aga.Controls.Tree;

namespace mrHelper.App.Helpers
{
   public class VersionBrowseModelData
   {
      public VersionBrowseModelData(IEnumerable<Commit> commits, IEnumerable<Version> versions,
         string baseSha, string targetBranch)
      {
         Commits = commits;
         Versions = versions;
         BaseSha = baseSha;
         TargetBranch = targetBranch;
      }

      public IEnumerable<Version> Versions { get; }
      public IEnumerable<Commit> Commits { get; }
      public string BaseSha { get; }
      public string TargetBranch { get; }
   }

   public class VersionBrowserModel : ITreeModel
   {
      public VersionBrowserModel()
      {
      }

      internal void SetData(VersionBrowseModelData data)
      {
         _data = data;
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
            int iVersion = _data.Versions.Count() - 1;
            foreach (Version version in _data.Versions)
            {
               string name = String.Format("Version #{0}", iVersion);
               string sha = getSha(version.Head_Commit_SHA);
               BaseVersionBrowserItem parent = treePath.FirstNode as BaseVersionBrowserItem;
               items.Add(new LeafVersionBrowserItem(name, version.Created_At, sha, parent, this));
               --iVersion;
            }

            foreach (Commit commit in _data.Commits)
            {
               string name = commit.Title;
               string sha = getSha(commit.Id);
               BaseVersionBrowserItem parent = treePath.LastNode as BaseVersionBrowserItem;
               items.Add(new LeafVersionBrowserItem(name, commit.Created_At, sha, parent, this));
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

      private VersionBrowseModelData _data;
   }
}

