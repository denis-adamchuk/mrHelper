using System;
using mrHelper.Common.Exceptions;

namespace mrHelper.Core.Matching
{
   public static class PositionUtils
   {
      public static DiffPosition ScrollPosition(
         StorageSupport.IGitCommandService git, DiffPosition position, bool scrollUp, bool isLeftSideLine)
      {
         if (!Core.Context.Helpers.IsValidPosition(position))
         {
            return position;
         }

         int lineNumber = isLeftSideLine
               ? Core.Context.Helpers.GetLeftLineNumber(position)
               : Core.Context.Helpers.GetRightLineNumber(position);
         lineNumber += (scrollUp ? -1 : 1);

         string leftPath = position.LeftPath;
         string rightPath = position.RightPath;
         Core.Matching.DiffRefs refs = position.Refs;
         if (MatchLineNumber(git, leftPath, rightPath, refs, lineNumber, isLeftSideLine,
               out string leftLineNumber, out string rightLineNumber))
         {
            return new DiffPosition(leftPath, rightPath, leftLineNumber, rightLineNumber, refs);
         }
         return null;
      }

      public static bool MatchLineNumber(StorageSupport.IGitCommandService git,
         string leftPath, string rightPath, Core.Matching.DiffRefs refs,
         int lineNumber, bool isLeftSideLine, out string leftLineNumber, out string rightLineNumber)
      {
         leftLineNumber = rightLineNumber = null;
         LineNumberMatcher matcher = new LineNumberMatcher(git);
         try
         {
            matcher.Match(refs, leftPath, rightPath, lineNumber, isLeftSideLine, out leftLineNumber, out rightLineNumber);
            return true;
         }
         catch (ArgumentException ex)
         {
            ExceptionHandlers.Handle("Cannot create DiffPosition", ex);
         }
         catch (MatchingException ex)
         {
            ExceptionHandlers.Handle("Cannot create DiffPosition", ex);
         }
         return false;
      }
   }
}

