using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.StorageSupport
{
   internal static class DiffTreeBuilder
   {
      internal static DiffTree Build(IEnumerable<DiffStruct> diffStructs)
      {
         return convertToTree(diffStructs);
      }

      private static DiffTree convertToTree(IEnumerable<DiffStruct> diffStructs)
      {
         DiffTree root = new DiffTree();
         foreach (DiffStruct diffStruct in diffStructs)
         {
            DiffStructCounter.Count(diffStruct, out DiffSize? diffSize);
            if (diffStruct.Deleted_File)
            {
               string[] splittedPath = splitPath(diffStruct.Old_Path);
               FileDiffDescription data = new FileDiffDescription(diffSize, DiffKind.Deleted, null);
               createMissingChilds(root, splittedPath, data);
            }
            else if (diffStruct.New_File)
            {
               string[] splittedPath = splitPath(diffStruct.New_Path);
               FileDiffDescription data = new FileDiffDescription(diffSize, DiffKind.New, null);
               createMissingChilds(root, splittedPath, data);
            }
            else if (diffStruct.Renamed_File)
            {
               string oldName = System.IO.Path.GetFileName(diffStruct.Old_Path);
               string newName = System.IO.Path.GetFileName(diffStruct.New_Path);
               bool isMoved = oldName == newName;
               {
                  string[] splittedPath = splitPath(diffStruct.Old_Path);
                  var kind = isMoved ? DiffKind.MovedFrom : DiffKind.RenamedFrom;
                  FileDiffDescription data = new FileDiffDescription(diffSize, kind, newName);
                  createMissingChilds(root, splittedPath, data);
               }
               {
                  string[] splittedPath = splitPath(diffStruct.New_Path);
                  var kind = isMoved ? DiffKind.MovedTo : DiffKind.RenamedTo;
                  FileDiffDescription data = new FileDiffDescription(diffSize, kind, oldName);
                  createMissingChilds(root, splittedPath, data);
               }
            }
            else
            {
               Debug.Assert(diffStruct.Old_Path == diffStruct.New_Path);
               string[] splittedPath = splitPath(diffStruct.New_Path);
               string anotherName = System.IO.Path.GetFileName(diffStruct.Old_Path);
               FileDiffDescription data = new FileDiffDescription(diffSize, DiffKind.Modified, anotherName);
               createMissingChilds(root, splittedPath, data);
            }
         }
         return root;
      }

      private static string[] splitPath(string path)
      {
         char pathSeparator = System.IO.Path.AltDirectorySeparatorChar;
         return path.Split(pathSeparator);
      }

      private static void createMissingChilds(
         CompositeItem item, IEnumerable<string> remainingPath, FileDiffDescription data)
      {
         if (!remainingPath.Any())
         {
            return;
         }

         string currentPathElement = remainingPath.First();
         BaseItem child = item.ChildItems.FirstOrDefault(childItem => childItem.Name == currentPathElement);
         if (child == null)
         {
            bool isFile = remainingPath.First() == remainingPath.Last(); // as path cannot end with a directory
            if (isFile)
            {
               item.ChildItems.Add(new FileDiffItem(currentPathElement, data));
            }
            else
            {
               item.ChildItems.Add(new FolderItem(currentPathElement));
            }
            child = item.ChildItems.Last();
         }

         if (child is CompositeItem)
         {
            createMissingChilds(child as CompositeItem, remainingPath.Skip(1), data);
         }
      }
   }
}

