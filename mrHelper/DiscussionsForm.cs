using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using mrCore;

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      private const int EM_GETLINECOUNT = 0xba;
      [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      private static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam); 

      public DiscussionsForm(string host, string accessToken, string projectId, int mergeRequestId,
         GitRepository gitRepository)
      {
         _host = host;
         _accessToken = accessToken;
         _projectId = projectId;
         _mergeRequestId = mergeRequestId;
         _currentUser = getUser();
         _gitRepository = gitRepository;
         _contextMaker = new CombinedContextMaker(_gitRepository);

         InitializeComponent();
      }

      private void Discussions_Load(object sender, EventArgs e)
      {
         try
         {
            this.Hide();
            renderDiscussions(loadDiscussions());
            this.Show();
            this.Invalidate();
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private List<Discussion> loadDiscussions()
      {
         GitLabClient client = new GitLabClient(_host, _accessToken);
         return client.GetMergeRequestDiscussions(_projectId, _mergeRequestId);
      }

      private User getUser()
      {
         GitLabClient client = new GitLabClient(_host, _accessToken);
         return client.GetCurrentUser();
      }

      private void renderDiscussions(List<Discussion> discussions)
      {
         int groupBoxMarginLeft = 10;
         int groupBoxMarginTop = 15;

         Point previousBoxLocation = new Point();
         Size previousBoxSize = new Size();
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0)
            {
               continue;
            }

            Point location = new Point();
            location.X = groupBoxMarginLeft;
            location.Y = previousBoxLocation.Y + previousBoxSize.Height + groupBoxMarginTop;
            var discussionBox = createDiscussionBox(discussion, location);
            if (discussionBox != null)
            {
               previousBoxLocation = discussionBox.Location;
               previousBoxSize = discussionBox.Size;
            }
            Controls.Add(discussionBox);
         }
      }

      private GroupBox createDiscussionBox(Discussion discussion, Point location)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         if (firstNote.System)
         {
            return null;
         }

         int contextSize = 4;

         // @{ Margins for each control within Discussion Box
         int discussionLabelMarginLeft = 7;
         int discussionLabelMarginTop = 9;
         int webBrowserMarginLeft = 7;
         int filenameTextBoxMarginRight = 7;
         int webBrowserMarginTop = 5;
         int noteTextBoxMarginLeft = 7;
         int noteTextBoxMarginTop = 5;
         int noteTextBoxMarginBottom = 5;
         int noteTextBoxMarginRight = 7;
         // @}

         // @{ Sizes of controls
         var noteTextBoxSize = new Size(500, 0 /* height is adjusted to line count */); 
         // @}

         // Create a discussion box, which is a container of other controls
         GroupBox groupBox = new GroupBox();

         // Create a label that shows discussion creation data and author
         Label discussionLabel = new Label();
         groupBox.Controls.Add(discussionLabel);
         discussionLabel.Text = "Discussion started at " + firstNote.CreatedAt.ToString() + " by " + firstNote.Author.Name;
         discussionLabel.Location = new Point(discussionLabelMarginLeft, discussionLabelMarginTop);
         discussionLabel.AutoSize = true;

         // Create a box that shows diff context
         Point webBrowserLocation = new Point(webBrowserMarginLeft,
            discussionLabel.Location.Y + discussionLabel.Size.Height );
         Size webBrowserSize = new Size();
         if (firstNote.Type == DiscussionNoteType.DiffNote)
         {
            WebBrowser webBrowser = new WebBrowser();
            groupBox.Controls.Add(webBrowser);
            webBrowser.ScrollBarsEnabled = false;
            webBrowser.AllowNavigation = false;
            webBrowser.WebBrowserShortcutsEnabled = false;
            webBrowser.Location = new Point(webBrowserLocation.X, webBrowserLocation.Y + webBrowserMarginTop);
            webBrowser.Size = new Size(noteTextBoxSize.Width * 2 + noteTextBoxMarginLeft, contextSize * 16);

            try
            {
               DiffContextFormatter diffContextFormatter = new DiffContextFormatter();
               Debug.Assert(firstNote.Position.HasValue);
               DiffContext context = _contextMaker.GetContext(firstNote.Position.Value, contextSize);
               string text = diffContextFormatter.FormatAsHTML(context);
               webBrowser.DocumentText = text;
            }
            catch (Exception)
            {
               webBrowser.DocumentText = "<html><body>Cannot show diff context</body></html>";
            }

            webBrowserLocation = webBrowser.Location;
            webBrowserSize = webBrowser.Size;
         }

         // Create a series of boxes which represent notes
         Point previousBoxLocation = new Point(0,
            webBrowserLocation.Y + webBrowserSize.Height + noteTextBoxMarginTop);
         Size previousBoxSize = new Size(0, 0);
         int biggestTextBoxHeight = previousBoxSize.Height;
         int shownCount = 0;
         foreach (var note in discussion.Notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            TextBox textBox = new TextBox();
            groupBox.Controls.Add(textBox);
            toolTip.SetToolTip(textBox, "Created at " + note.CreatedAt.ToString() + " by " + note.Author.Name);
            textBox.ReadOnly = note.Author.Id != _currentUser.Id;
            textBox.Size = noteTextBoxSize;
            textBox.Text = note.Body;
            textBox.Multiline = true;
            var numberOfLines = SendMessage(textBox.Handle.ToInt32(), EM_GETLINECOUNT, 0, 0);
            textBox.Height = (textBox.Font.Height + 4) * numberOfLines;
            if (note.Resolvable && note.Resolved.HasValue && note.Resolved.Value)
            {
               textBox.BackColor = Color.LightGreen;
            }

            Point textBoxLocation = new Point();
            textBoxLocation.X = previousBoxLocation.X + previousBoxSize.Width + noteTextBoxMarginLeft;
            textBoxLocation.Y = previousBoxLocation.Y;
            textBox.Location = textBoxLocation;
            previousBoxLocation = textBoxLocation;
            previousBoxSize = textBox.Size;

            ++shownCount;

            biggestTextBoxHeight = Math.Max(biggestTextBoxHeight, textBox.Height);
         }

         // Calculate discussion box location and size
         int groupBoxHeight =
            discussionLabelMarginTop
          + discussionLabel.Height
          + webBrowserMarginTop
          + webBrowserSize.Height
          + noteTextBoxMarginTop
          + biggestTextBoxHeight
          + noteTextBoxMarginBottom;

         int groupBoxWidth =
          + Math.Max(webBrowserMarginLeft + webBrowserSize.Width + filenameTextBoxMarginRight,
                     (noteTextBoxMarginLeft + noteTextBoxSize.Width) * shownCount + noteTextBoxMarginRight);

         groupBox.Location = location;
         groupBox.Size = new Size(groupBoxWidth, groupBoxHeight);
         return groupBox;
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly int _mergeRequestId;
      private readonly User _currentUser;
      private readonly GitRepository _gitRepository;
      private readonly ContextMaker _contextMaker;
   }
}

