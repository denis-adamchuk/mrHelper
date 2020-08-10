using GitLabSharp.Entities;
using mrHelper.Core.Matching;

namespace mrHelper.App.Helpers
{
   public static class PositionConverter
   {
      public static DiffPosition Convert(Position position)
      {
         return new DiffPosition(
            position.Old_Path, position.New_Path,
            position.Old_Line, position.New_Line,
            new Core.Matching.DiffRefs(position.Base_SHA, position.Head_SHA));
      }
   }
}

