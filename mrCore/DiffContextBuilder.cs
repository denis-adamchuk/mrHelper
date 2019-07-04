using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrCore
{
   public struct DiffContext
   {
      public struct Line
      {
         public enum LineState
         {
            Added,
            Deleted,
            Unchanged
         }

         public LineState State;
         public string Text;
         public int Number;
      }

      public List<Line> lines;
   }

   public class DiffContextBuilder
   {
      public DiffContextBuilder(string gitRepository)
      {
         // This class expects a valid repository, it is not going to clone or fetch anything
         if (!Directory.Exists(gitRepository) || !GitClient.IsGitRepository(gitRepository))
         {
            throw new ApplicationException("Bad git repository");
         }

         _analyzers = new Dictionary<DiffContextKey, GitDiffAnalyzer>();
      }

      public DiffContext GetContext(PositionDetails positionDetails, int size)
      {
         // TODO Is it ok that we cannot handle different filenames?
         Debug.Assert(positionDetails.NewPath == positionDetails.OldPath);
         Debug.Assert(positionDetails.NewLine != null || positionDetails.OldLine != null);

         GitDiffAnalyzer analyzer = getAnalyzer(positionDetails);

         // If NewLine is valid, then it points to either added/modified or unchanged line, handle them the same way
         int lineNumber = int.Parse(positionDetails.NewLine != null ? positionDetails.NewLine : positionDetails.OldLine);
         bool added = positionDetails.NewLine != null;
         string sha = positionDetails.NewLine != null ? positionDetails.Refs.HeadSHA : positionDetails.Refs.BaseSHA;
         return createDiffContext(lineNumber, positionDetails.NewPath, sha, added, analyzer, size);
      }

      private DiffContext createDiffContext(int lineNumber, string filename, string sha, bool added, GitDiffAnalyzer analyzer, int size)
      {
         DiffContext diffContext = new DiffContext();

         DiffContext.Line currentLine = new DiffContext.Line();
         currentLine.State = DiffContext.Line.LineState.Added;
         currentLine.Text = readLine(sha, filename, lineNumber);
         currentLine.Number = lineNumber;
         diffContext.lines.Add(currentLine);

         for (int iContextLine = 0; iContextLine < size; ++iContextLine)
         {
            int offset = iContextLine + 1;
            DiffContext.Line line = new DiffContext.Line();
            if (added)
            {
               line.State = analyzer.IsLineAddedOrModified(lineNumber + offset)
                  ? DiffContext.Line.LineState.Added : DiffContext.Line.LineState.Unchanged;
            }
            else
            {
               line.State = analyzer.IsLineDeleted(lineNumber + offset)
                  ? DiffContext.Line.LineState.Deleted : DiffContext.Line.LineState.Unchanged;
            }
            line.Text = readLine(sha, filename, lineNumber + offset);
            line.Number = lineNumber + offset;
            diffContext.lines.Add(line);
         }

         return diffContext;
      }

      private GitDiffAnalyzer getAnalyzer(PositionDetails positionDetails)
      {
         DiffContextKey key = new DiffContextKey();
         key.sha1 = positionDetails.Refs.BaseSHA;
         key.sha2 = positionDetails.Refs.HeadSHA;
         key.filename = positionDetails.NewPath;
         if (_analyzers.ContainsKey(key))
         {
            return _analyzers[key];
         }

         GitDiffAnalyzer analyzer = new GitDiffAnalyzer(key.sha1, key.sha2, key.filename);
         _analyzers.Add(key, analyzer);
         return analyzer;
      }

      private string readLine(string sha, string filename, int linenumber)
      {
         // TODO Cache lines
         var lines = GitClient.ShowFileByRevision(filename, sha);
         return linenumber >= 0 && linenumber < lines.Count ? lines[linenumber] : null;
      }

      private struct DiffContextKey
      {
         public string sha1;
         public string sha2;
         public string filename;
      }

      private Dictionary<DiffContextKey, GitDiffAnalyzer> _analyzers;
   }
}
