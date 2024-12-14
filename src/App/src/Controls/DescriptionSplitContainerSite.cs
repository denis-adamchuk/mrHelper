using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.App.Helpers;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Controls
{
   internal partial class DescriptionSplitContainerSite : UserControl
   {
      internal DescriptionSplitContainerSite()
      {
         InitializeComponent();
         ColorScheme.Modified += onColorSchemeModified;
      }

      internal void Initialize(IEnumerable<string> keywords, Markdig.MarkdownPipeline mdPipeline)
      {
         _keywords = keywords;
         _mdPipeline = mdPipeline;
         startRedrawTimer();
         applyFontAndColors();
      }

      internal void UpdateData(FullMergeRequestKey fmk, DataCache dataCache)
      {
         _currentMergeRequest = fmk;
         _currentDataCache = dataCache;

         updateMergeRequestDescription();
         updateAuthorComments();
      }

      internal void ClearData()
      {
         _currentMergeRequest = null;
         _currentDataCache = null;

         updateMergeRequestDescription();
         updateAuthorComments();
      }

      internal SplitContainer SplitContainer => splitContainer;

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);
         applyFontAndColors();
      }

      private void onColorSchemeModified()
      {
         applyFontAndColors();
      }

      private void applyFontAndColors()
      {
         applyFontAndColors(richTextBoxMergeRequestDescription);
         applyFontAndColors(htmlPanelAuthorComments);
      }

      private void updateMergeRequestDescription()
      {
         if (!_currentMergeRequest.HasValue)
         {
            richTextBoxMergeRequestDescription.Text = String.Empty;
         }
         else
         {
            FullMergeRequestKey fmk = _currentMergeRequest.Value;

            string rawTitle = !String.IsNullOrEmpty(fmk.MergeRequest.Title) ? fmk.MergeRequest.Title : "Title is empty";
            string title = MarkDownUtils.ConvertToHtml(rawTitle, String.Empty, _mdPipeline,
               richTextBoxMergeRequestDescription);

            string rawDescription = !String.IsNullOrEmpty(fmk.MergeRequest.Description)
               ? fmk.MergeRequest.Description : "Description is empty";
            string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey.HostName, fmk.MergeRequest.Project_Id);
            string description = MarkDownUtils.ConvertToHtml(rawDescription, uploadsPrefix, _mdPipeline,
               richTextBoxMergeRequestDescription);

            string body = String.Format("<b>Title</b><br>{0}<br><b>Description</b><br>{1}", title, description);
            richTextBoxMergeRequestDescription.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         }
         richTextBoxMergeRequestDescription.Update();
      }

      private void updateAuthorComments()
      {
         if (!_currentMergeRequest.HasValue)
         {
            splitContainer.Panel2Collapsed = true;
            return;
         }

         FullMergeRequestKey fmk = _currentMergeRequest.Value;
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         DiscussionCount.EStatus? status = _currentDataCache.DiscussionCache?.GetDiscussionCount(mrk).Status;
         if (status == DiscussionCount.EStatus.NotAvailable)
         {
            splitContainer.Panel2Collapsed = true;
         }
         else if (status == DiscussionCount.EStatus.Loading)
         {
            splitContainer.Panel2Collapsed = true;
         }
         else if (status == DiscussionCount.EStatus.Ready)
         {
            IEnumerable<Discussion> filteredDiscussions = getFilteredDiscussions(_currentDataCache, fmk);
            splitContainer.Panel2Collapsed = !filteredDiscussions.Any();
            if (filteredDiscussions.Any())
            {
               string htmlText = getCommentsAsHtmlTable(fmk, filteredDiscussions);
               htmlPanelAuthorComments.Text = String.Format(MarkDownUtils.HtmlPageTemplate, htmlText);
               htmlPanelAuthorComments.Update();
            }
         }
      }

      private string getCommentsAsHtmlTable(FullMergeRequestKey fmk, IEnumerable<Discussion> discussions)
      {
         string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey.HostName, fmk.MergeRequest.Project_Id);
         StringBuilder builder = new StringBuilder();
         builder.Append("<b>Notes from author</b><br>");
         foreach (Discussion discussion in discussions)
         {
            DiscussionNote note = discussion.Notes.First();
            string noteBody = MarkDownUtils.ConvertToHtml(note.Body, uploadsPrefix, _mdPipeline, htmlPanelAuthorComments);
            string noteRow = String.Format("<p><i>{0} ({1})</i></p><br><p>{2}</p><br>",
               TimeUtils.DateTimeToStringAgo(note.Created_At), TimeUtils.DateTimeToString(note.Created_At), noteBody);
            builder.Append(noteRow);
         }
         return builder.ToString();
      }

      private IEnumerable<Discussion> getFilteredDiscussions(DataCache dataCache, FullMergeRequestKey fmk)
      {
         return dataCache.DiscussionCache?.GetDiscussions(new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId))
            .Where(discussion => discussion.Notes.Any())
            .Where(discussion => discussion.Notes.First().Author.Id == fmk.MergeRequest.Author.Id)
            .Where(discussion => !discussion.Notes.First().System)
            .Where(discussion => !String.IsNullOrEmpty(discussion.Notes.First().Body))
            .Where(discussion =>
            {
               if (_keywords == null)
               {
                  return true;
               }
               foreach (var keyword in _keywords)
               {
                  if (discussion.Notes.First().Body.Trim().StartsWith(
                     keyword, StringComparison.CurrentCultureIgnoreCase))
                  {
                     return false;
                  }
               }
               return true;
            })
            .OrderByDescending(discussion => discussion.Notes.First().Created_At).ToArray() ?? Array.Empty<Discussion>();
      }

      private void startRedrawTimer()
      {
         _redrawTimer.Tick += onRedrawTimer;
         _redrawTimer.Start();
      }

      private void stopRedrawTimer()
      {
         _redrawTimer.Tick -= onRedrawTimer;
         _redrawTimer.Stop();
      }

      private void onRedrawTimer(object sender, EventArgs e)
      {
         updateAuthorComments();
      }

      static private void applyFontAndColors(HtmlPanel htmlPanel)
      {
         htmlPanel.BaseStylesheet = ResourceHelper.ApplyFontSizeAndColorsToCSS(htmlPanel);
      }

      private static readonly int RedrawTimerInterval = 1000 * 30; // 0.5 minute
      private readonly Timer _redrawTimer = new Timer
      {
         // This timer is needed to update "ago" timestamps
         Interval = RedrawTimerInterval
      };

      private Markdig.MarkdownPipeline _mdPipeline;
      private IEnumerable<string> _keywords;
      private FullMergeRequestKey? _currentMergeRequest;
      private DataCache _currentDataCache;
   }
}

