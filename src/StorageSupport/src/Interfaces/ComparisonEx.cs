using System.Linq;
using System.Collections.Generic;
using GitLabSharp.Entities;

namespace mrHelper.StorageSupport
{
   public class ComparisonEx : Comparison
   {
      internal ComparisonEx(Comparison comparison)
      {
         this.Commit = comparison.Commit;
         this.Commits = comparison.Commits;
         this.Compare_Timeout = comparison.Compare_Timeout;
         this.Diffs = comparison.Diffs;
      }

      public class Statistic
      {
         public Statistic(IEnumerable<DiffStruct> diffs)
         {
            fillData(diffs);
            Tree = DiffTreeBuilder.Build(diffs);
         }

         public class Item
         {
            public Item(string old_Path, string new_Path, DiffSize? diffSize)
            {
               Old_Path = old_Path;
               New_Path = new_Path;
               DiffSize = diffSize;
            }

            public string Old_Path { get; }
            public string New_Path { get; }
            public DiffSize? DiffSize { get; }
         }

         public IEnumerable<Item> Data => _data;

         public DiffTree Tree { get; }

         private void fillData(IEnumerable<DiffStruct> diffs)
         {
            _data.Clear();
            foreach (DiffStruct diff in diffs)
            {
               DiffStructCounter.Count(diff, out DiffSize? diffSize);
               _data.Add(new Item(diff.Old_Path, diff.New_Path, diffSize));
            }
         }

         private readonly List<Item> _data = new List<Item>();
      }

      public Statistic GetStatistic()
      {
         if (_statistic == null)
         {
            _statistic = new Statistic(this.Diffs);
         }
         return _statistic;
      }

      private Statistic _statistic;
   }
}

