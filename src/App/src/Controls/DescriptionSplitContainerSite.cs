using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.GitLabClient;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace mrHelper.App.Controls
{
   internal partial class DescriptionSplitContainerSite : UserControl
   {
      public DescriptionSplitContainerSite()
      {
         InitializeComponent();
      }

      internal void Initialize(IEnumerable<string> keywords, Markdig.MarkdownPipeline mdPipeline)
      {
         _keywords = keywords;
         _mdPipeline = mdPipeline;

         setFontSizeInHtmlPanel(richTextBoxMergeRequestDescription);
         setFontSizeInHtmlPanel(htmlPanelAuthorComments);
      }

      internal void UpdateData(FullMergeRequestKey? fmkOpt, DataCache dataCache)
      {
         updateMergeRequestDescription(fmkOpt);
         updateAuthorComments(fmkOpt, dataCache);
      }

      public SplitContainer SplitContainer => splitContainer;

      protected override void OnFontChanged(EventArgs e)
      {
         base.OnFontChanged(e);

         setFontSizeInHtmlPanel(richTextBoxMergeRequestDescription);
         setFontSizeInHtmlPanel(htmlPanelAuthorComments);
      }

      private void updateMergeRequestDescription(FullMergeRequestKey? fmkOpt)
      {
         if (!fmkOpt.HasValue)
         {
            richTextBoxMergeRequestDescription.Text = String.Empty;
         }
         else
         {
            FullMergeRequestKey fmk = fmkOpt.Value;

            string rawTitle = !String.IsNullOrEmpty(fmk.MergeRequest.Title) ? fmk.MergeRequest.Title : "Title is empty";
            string title = MarkDownUtils.ConvertToHtml(rawTitle, String.Empty, _mdPipeline);

            string rawDescription = !String.IsNullOrEmpty(fmk.MergeRequest.Description)
               ? fmk.MergeRequest.Description : "Description is empty";
            string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey);
            string description = MarkDownUtils.ConvertToHtml(rawDescription, uploadsPrefix, _mdPipeline);

            string body = String.Format("<b>Title</b><br>{0}<br><b>Description</b><br>{1}", title, description);
            richTextBoxMergeRequestDescription.Text = String.Format(MarkDownUtils.HtmlPageTemplate, body);
         }
         richTextBoxMergeRequestDescription.Update();
      }

      private void updateAuthorComments(FullMergeRequestKey? fmkOpt, DataCache dataCache)
      {
         if (!fmkOpt.HasValue)
         {
            splitContainer.Panel2Collapsed = true;
            return;
         }

         FullMergeRequestKey fmk = fmkOpt.Value;
         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         DiscussionCount.EStatus? status = dataCache.DiscussionCache?.GetDiscussionCount(mrk).Status;
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
            IEnumerable<Discussion> filteredDiscussions = getFilteredDiscussions(dataCache, fmk);
            splitContainer.Panel2Collapsed = !filteredDiscussions.Any();
            if (filteredDiscussions.Any())
            {
               string htmlText = getCommentsAsHtmlTable(fmk, filteredDiscussions);
               htmlPanelAuthorComments.Text = String.Format(MarkDownUtils.HtmlPageTemplate, htmlText);
               htmlPanelAuthorComments.Update();
            }
         }
      }

      private string getCommentsAsHtmlTable(FullMergeRequestKey fmk, IEnumerable<Discussion> filteredDiscussions)
      {
         string uploadsPrefix = StringUtils.GetUploadsPrefix(fmk.ProjectKey);
         StringBuilder builder = new StringBuilder();
         builder.Append("<b>Notes from author</b><br>");
         builder.Append("<table class=\"no-border no-bg\"><tbody>");
         foreach (Discussion discussion in filteredDiscussions)
         {
            DiscussionNote note = discussion.Notes.First();
            string noteBody = MarkDownUtils.ConvertToHtml(note.Body, uploadsPrefix, _mdPipeline);
            string noteRow = String.Format(
               "<tr><td class=\"no-border no-bg\"><div><i>{0} ({1})</i></div><div>{2}</div></td></tr><br>",
               TimeUtils.DateTimeToStringAgo(note.Created_At),
               TimeUtils.DateTimeToString(note.Created_At),
               noteBody);
            builder.Append(noteRow);
         }
         builder.Append("</table></tbody>");
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

      static private void setFontSizeInHtmlPanel(HtmlPanel htmlPanel)
      {
         string cssEx = String.Format("body div {{ font-size: {0}px; }}",
            CommonControls.Tools.WinFormsHelpers.GetFontSizeInPixels(htmlPanel));
         htmlPanel.BaseStylesheet = String.Format("{0}{1}", Properties.Resources.Common_CSS, cssEx);
      }

      private Markdig.MarkdownPipeline _mdPipeline;
      private IEnumerable<string> _keywords;
   }
}

