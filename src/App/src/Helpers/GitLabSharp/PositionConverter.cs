using GitLabSharp.Entities;
using mrHelper.Core.Matching;

namespace mrHelper.App.Helpers
{
   public static class PositionConverter
   {
      public static DiffPosition Convert(Position position)
      {
         return new DiffPosition
         {
            LeftLine = position.Old_Line,
            LeftPath = position.Old_Path,
            RightLine = position.New_Line,
            RightPath = position.New_Path,
            Refs = new Core.Matching.DiffRefs
            {
               LeftSHA = position.Base_SHA,
               RightSHA = position.Head_SHA
            }
         };
      }
   }
}

