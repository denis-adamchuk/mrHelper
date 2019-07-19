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
using TheArtOfDev.HtmlRenderer.WinForms;
using GitLabSharp;

namespace mrHelperUI
{
   public partial class DiscussionsForm : Form
   {
      private const int EM_GETLINECOUNT = 0xba;
      [DllImport("user32", EntryPoint = "SendMessageA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
      private static extern int SendMessage(int hwnd, int wMsg, int wParam, int lParam);

      private class DiscussionBox : Panel
      {
         // Widths in %
         public int HorzMarginWidth = 1;
         public int LabelAuthorWidth = 5;
         public int ContextWidth = 55;
         public int NotesWidth = 30;
         public int LabelFilenameWidth = 30;

         public Control LabelAuthor;
         public Control LabelFilename;
         public Control Context;
         public List<Control> Notes;

         public void ReplaceControl(Control oldControl, Control newControl)
         {
            if (LabelAuthor == oldControl)
            {
               LabelAuthor = newControl;
            }
            else if (LabelFilename == oldControl)
            {
               LabelFilename = newControl;
            }
            else if (Context == oldControl)
            {
               Context = newControl;
            }
            else
            {
               for (int iNote = 0; iNote < Notes.Count; ++iNote)
               {
                  if (Notes[iNote] == oldControl)
                  {
                     Notes[iNote] = newControl;
                     break;
                  }
               }
            }
         }
      }

      public DiscussionsForm(string host, string accessToken, string projectId, int mergeRequestId,
         User mrAuthor, GitRepository gitRepository, int diffContextDepth, ColorScheme colorScheme)
      {
         _host = host;
         _accessToken = accessToken;
         _projectId = projectId;
         _mergeRequestId = mergeRequestId;
         _mrAuthor = mrAuthor;
         _currentUser = getUser();
         if (gitRepository != null)
         {
            _panelContextMaker = new EnhancedContextMaker(gitRepository);
            _tooltipContextMaker = new CombinedContextMaker(gitRepository);
         }

         _diffContextDepth = new ContextDepth(0, diffContextDepth);
         _tooltipContextDepth = new ContextDepth(5, 5);
         _formatter = new DiffContextFormatter();
         _colorScheme = colorScheme;

         InitializeComponent();
         htmlToolTip.AutoPopDelay = 10000; // 10s
         htmlToolTip.BaseStylesheet = ".htmltooltip { padding: 1px; }";

         onRefresh();
      }

      private void DiscussionNoteTextBox_LostFocus(object sender, EventArgs e)
      {
         try
         {
            onEditNote((TextBox)(sender));
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void MenuItemDeleteNote_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         if (MessageBox.Show("This discussion note will be deleted. Are you sure?", "Confirm deletion",
               MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
         {
            return;
         }

         try
         {
            onDeleteNote(textBox);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void MenuItemToggleResolve_Click(object sender, EventArgs e)
      {
         MenuItem menuItem = (MenuItem)(sender);
         TextBox textBox = (TextBox)(menuItem.Tag);
         DiscussionNoteWithParentId note = (DiscussionNoteWithParentId)(textBox.Tag);
         if (!note.Note.Resolvable)
         {
            return;
         }

         try
         {
            onToggleResolveNote(textBox);
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void DiscussionsForm_KeyDown(object sender, KeyEventArgs e)
      {
         try
         {
            if (e.KeyCode == Keys.F5)
            {
               onRefresh();
            }
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
         }
      }

      private void DiscussionsForm_Resize(object sender, EventArgs e)
      {
         repositionAll();
      }

      private void onRefresh()
      {
         Controls.Clear();
         createControls(loadDiscussions());
         repositionAll();
         Focus();
      }

      private void onDeleteNote(TextBox textBox)
      {
         DiscussionNoteWithParentId note = (DiscussionNoteWithParentId)(textBox.Tag);

         GitLab gl = new GitLab(_host, _accessToken);
         var mergeRequest = gl.Projects.Get(_projectId).MergeRequests.Get(_mergeRequestId);
         mergeRequest.Notes.Get(note.Note.Id).Delete();

         // When a text box is deleted, we recreate a whole discussion box
         DiscussionBox newDiscussionBox = null;
         try
         {
            Discussion discussion = mergeRequest.Discussions.Get(note.DiscussionId).Load();
            if (!discussion.Notes[0].System)
            {
               newDiscussionBox = createDiscussionBox(discussion);
            }
         }
         catch (System.Net.WebException ex)
         {
            // Seems it was the only note in the discussion
            var response = ((System.Net.HttpWebResponse)ex.Response);
            Debug.Assert(response.StatusCode == System.Net.HttpStatusCode.NotFound);
         }

         // Replace discussion box among Form Controls
         replaceControlInParent(textBox.Parent as DiscussionBox, newDiscussionBox);

         // Let's shift everything
         repositionAll();
      }

      private void onEditNote(TextBox textBox)
      {
         DiscussionNoteWithParentId note = (DiscussionNoteWithParentId)(textBox.Tag);
         if (textBox.Text == note.Note.Body)
         {
            return;
         }

         note.Note.Body = textBox.Text;

         GitLab gl = new GitLab(_host, _accessToken);
         gl.Projects.Get(_projectId).MergeRequests.Get(_mergeRequestId).
            Discussions.Get(note.DiscussionId).ModifyNote(note.Note.Id,
               new ModifyDiscussionNoteParameters
               {
                  Type = ModifyDiscussionNoteParameters.ModificationType.Body,
                  Body = note.Note.Body
               });

         toolTipNotifier.Show("Discussion note was edited", textBox, textBox.Width + 20, 0, 2000);

         // Create a new text box
         TextBox newTextBox = createTextBox(note.DiscussionId, note.Note);

         // By default place a new textbox at the same place as the old one. It may be changed if height changed.
         // It is better to change Location right now to avoid flickering during repositionAll(). 
         newTextBox.Location = textBox.Location;

         // Measure heights
         int oldHeight = textBox.Height;
         int newHeight = getTextBoxPreferredHeight(newTextBox);

         // Replace text box in Discussion Box
         replaceControlInParent(textBox, newTextBox);

         if (oldHeight != newHeight)
         {
            // Line number changed, let's shift everything.
            repositionAll();
         }
      }

      private void onToggleResolveNote(TextBox textBox)
      {
         DiscussionNoteWithParentId note = (DiscussionNoteWithParentId)(textBox.Tag);
         note.Note.Resolved = !note.Note.Resolved;

         GitLab gl = new GitLab(_host, _accessToken);
         gl.Projects.Get(_projectId).MergeRequests.Get(_mergeRequestId).
            Discussions.Get(note.DiscussionId).ModifyNote(note.Note.Id,
               new ModifyDiscussionNoteParameters
               {
                  Type = ModifyDiscussionNoteParameters.ModificationType.Resolved,
                  Resolved = note.Note.Resolved
               });

         // Create a new text box
         TextBox newTextBox = createTextBox(note.DiscussionId, note.Note);

         // New textbox is placed at the same place as the old one
         newTextBox.Location = textBox.Location;
         newTextBox.Size = textBox.Size;
         
         // Replace text box in Discussion Box
         replaceControlInParent(textBox, newTextBox);
      }

      private static void replaceControlInParent(Control oldControl, Control newControl)
      {
         var index = oldControl.Parent.Controls.IndexOf(oldControl);
         var parent = oldControl.Parent;
         oldControl.Parent.Controls.Remove(oldControl);
         if (newControl != null)
         {
            parent.Controls.Add(newControl);
            parent.Controls.SetChildIndex(newControl, index);
         }

         if (parent is DiscussionBox)
         {
            (parent as DiscussionBox).ReplaceControl(oldControl, newControl);
         }
      }

      private List<Discussion> loadDiscussions()
      {
         GitLab gl = new GitLab(_host, _accessToken);
         return gl.Projects.Get(_projectId).MergeRequests.Get(_mergeRequestId).Discussions.LoadAll();
      }

      private User getUser()
      {
         GitLab gl = new GitLab(_host, _accessToken);
         return gl.CurrentUser.Load();
      }

      private void createControls(List<Discussion> discussions)
      {
         foreach (var discussion in discussions)
         {
            if (discussion.Notes.Count == 0 || discussion.Notes[0].System)
            {
               continue;
            }

            Controls.Add(createDiscussionBox(discussion));
         }
      }

      private void repositionAll()
      {
         int groupBoxMarginLeft = 10;
         int groupBoxMarginTop = 5;

         Point previousBoxLocation = new Point();
         Size previousBoxSize = new Size();
         foreach (Control control in Controls)
         {
            Debug.Assert(control is DiscussionBox);

            Point location = new Point
            {
               X = groupBoxMarginLeft,
               Y = previousBoxLocation.Y + previousBoxSize.Height + groupBoxMarginTop
            };

            resizeBoxContent(control as DiscussionBox);
            repositionBoxContent(control as DiscussionBox);

            control.Location = location;
            previousBoxLocation = control.Location;
            previousBoxSize = control.Size;
         }
      }

      private void resizeBoxContent(DiscussionBox box)
      {
         foreach (var textbox in box.Notes)
         {
            textbox.Width = this.Width * box.NotesWidth / 100;
            textbox.Height = getTextBoxPreferredHeight(textbox as TextBox);
         }

         if (box.Context != null)
         {
            box.Context.Width = this.Width * box.ContextWidth / 100;
         }
         box.LabelAuthor.Width = this.Width * box.LabelAuthorWidth / 100;
         if (box.LabelFilename != null)
         {
            box.LabelFilename.Width = this.Width * box.LabelFilenameWidth / 100;
         }
      }

      private void repositionBoxContent(DiscussionBox box)
      {
         int interControlVertMargin = 5;
         int interControlHorzMargin = this.Width * box.HorzMarginWidth / 100;

         // the LabelAuthor is placed at the left side
         Point labelPos = new Point(interControlHorzMargin, interControlVertMargin);
         box.LabelAuthor.Location = labelPos;

         // the Context is an optional control to the right of the Label
         Point ctxPos = new Point(box.LabelAuthor.Location.X + box.LabelAuthor.Width + interControlHorzMargin,
            interControlVertMargin);
         if (box.Context != null)
         {
            box.Context.Location = ctxPos;
         }

         // prepare initial position for controls that places to the right of the Context
         int nextNoteX = ctxPos.X + (box.Context == null ? 0 : box.Context.Width + interControlHorzMargin);
         Point nextNotePos = new Point(nextNoteX, ctxPos.Y);

         // the LabelFilename is placed to the right of the Context and vertically aligned with Notes
         if (box.LabelFilename != null)
         {
            box.LabelFilename.Location = nextNotePos;
            nextNotePos.Offset(0, box.LabelFilename.Height + interControlVertMargin);
         }

         // a list of Notes is to the right of the Context
         foreach (var note in box.Notes)
         {
            note.Location = nextNotePos;
            nextNotePos.Offset(0, note.Height + interControlVertMargin);
         }

         int lblAuthorHeight = box.LabelAuthor.Location.Y + box.LabelAuthor.PreferredSize.Height;
         int lblFNameHeight = (box.LabelFilename == null ? 0 : box.LabelFilename.Location.Y + box.LabelFilename.Height);
         int ctxHeight = (box.Context == null ? 0 : box.Context.Location.Y + box.Context.Height);
         int notesHeight = box.Notes[box.Notes.Count - 1].Location.Y + box.Notes[box.Notes.Count - 1].Height;

         int boxContentWidth = nextNoteX + box.Notes[0].Width;
         int boxContentHeight = new[] { lblAuthorHeight, lblFNameHeight, ctxHeight, notesHeight }.Max();
         box.Size = new Size(boxContentWidth + interControlHorzMargin, boxContentHeight + interControlVertMargin);
      }

      private DiscussionBox createDiscussionBox(Discussion discussion)
      {
         Debug.Assert(discussion.Notes.Count > 0);

         var firstNote = discussion.Notes[0];
         Debug.Assert(!firstNote.System);

         DiscussionBox controls = new DiscussionBox
         {
            LabelAuthor = createLabelAuthor(firstNote),
            LabelFilename = createLabelFilename(firstNote),
            Context = createDiffContext(firstNote),
            Notes = createTextBoxes(discussion.Id, discussion.Notes)
         };
         controls.Controls.Add(controls.LabelAuthor);
         controls.Controls.Add(controls.LabelFilename);
         controls.Controls.Add(controls.Context);
         foreach (var note in controls.Notes)
         {
            controls.Controls.Add(note);
         }
         return controls;
      }

      private List<Control> createTextBoxes(string discussionId, List<DiscussionNote> notes)
      {
         List<Control> boxes = new List<Control>();
         foreach (var note in notes)
         {
            if (note.System)
            {
               // skip spam
               continue;
            }

            TextBox textBox = createTextBox(discussionId, note);
            boxes.Add(textBox);
         }
         return boxes;
      }

      private TextBox createTextBox(string discussionId, DiscussionNote note)
      {
         bool canBeModified = note.Author.Id == _currentUser.Id;

         TextBox textBox = new TextBox();
         toolTip.SetToolTip(textBox, getNoteTooltipText(note));
         textBox.ReadOnly = !canBeModified;
         textBox.Text = note.Body;
         textBox.Multiline = true;
         textBox.Height = getTextBoxPreferredHeight(textBox);
         textBox.BackColor = getNoteColor(note);
         textBox.LostFocus += DiscussionNoteTextBox_LostFocus;
         textBox.Tag = new DiscussionNoteWithParentId
         {
            Note = note,
            DiscussionId = discussionId
         };

         textBox.ContextMenu = new ContextMenu();

         MenuItem menuItemToggleResolve = new MenuItem
         {
            Tag = textBox,
            Text =
            note.Resolvable && note.Resolved ? "Unresolve" : "Resolve"
         };
         menuItemToggleResolve.Click += MenuItemToggleResolve_Click;
         textBox.ContextMenu.MenuItems.Add(menuItemToggleResolve);

         MenuItem menuItemDeleteNote = new MenuItem
         {
            Tag = textBox,
            Enabled = canBeModified,
            Text = "Delete Note"
         };
         menuItemDeleteNote.Click += MenuItemDeleteNote_Click;
         textBox.ContextMenu.MenuItems.Add(menuItemDeleteNote);
         return textBox;
      }

      private static int getTextBoxPreferredHeight(TextBox textBox)
      {
         var numberOfLines = SendMessage(textBox.Handle.ToInt32(), EM_GETLINECOUNT, 0, 0);
         return textBox.Font.Height * (numberOfLines + 1);
      }

      private string getNoteTooltipText(DiscussionNote note)
      {
         string result = string.Empty;
         if (note.Resolvable)
         {
            result += note.Resolved ? "Resolved." : "Not resolved.";
         }
         result += " Created by " + note.Author.Name + " at " + note.Created_At.ToLocalTime().ToString("g");
         return result;
      }

      private Color getNoteColor(DiscussionNote note)
      {
         if (note.Resolvable)
         {
            if (note.Author.Id == _mrAuthor.Id)
            {
               return note.Resolved ? _colorScheme.GetColor("Discussions_Author_Notes_Resolved")
                                    : _colorScheme.GetColor("Discussions_Author_Notes_Unresolved");
            }
            else
            {
               return note.Resolved ? _colorScheme.GetColor("Discussions_NonAuthor_Notes_Resolved")
                                    : _colorScheme.GetColor("Discussions_NonAuthor_Notes_Unresolved");
            }
         }
         else
         {
            return _colorScheme.GetColor("Discussions_Comments");
         }
      }

      private HtmlPanel createDiffContext(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         int fontSizePx = 12;
         int rowsVPaddingPx = 2;
         int rowHeight = (fontSizePx + rowsVPaddingPx * 2 + 1 /* border of control */ + 2);
         // we're adding 2 extra pixels for each row because HtmlRenderer does not support CSS line-height property
         // this value was found experimentally

         int panelHeight = (_diffContextDepth.Size + 1) * rowHeight;

         HtmlPanel htmlPanel = new HtmlPanel
         {
            BorderStyle = BorderStyle.FixedSingle,
            Height = panelHeight
         };

         DiffPosition position = convertToDiffPosition(firstNote.Position);

         if (_panelContextMaker != null && _formatter != null)
         {
            DiffContext briefContext = _panelContextMaker.GetContext(position, _diffContextDepth);
            string briefContextText = _formatter.FormatAsHTML(briefContext, fontSizePx, rowsVPaddingPx);
            htmlPanel.Text = briefContextText;
         }
         else
         {
            htmlPanel.Text = "<html><body>Cannot access git repository and render diff context</body></html>";
         }

         if (_tooltipContextMaker != null && _formatter != null)
         {
            DiffContext fullContext = _tooltipContextMaker.GetContext(position, _tooltipContextDepth);
            string fullContextText = _formatter.FormatAsHTML(fullContext, fontSizePx, rowsVPaddingPx);
            htmlToolTip.MaximumSize = new Size(htmlPanel.Width, 0 /* auto-height */);
            htmlToolTip.SetToolTip(htmlPanel, fullContextText);
         }

         return htmlPanel;
      }

      // Create a label that shows discussion author
      private Label createLabelAuthor(DiscussionNote firstNote)
      {
         Label labelAuthor = new Label
         {
            Text = firstNote.Author.Name
         };
         return labelAuthor;
      }

      // Create a label that shows filename
      private Label createLabelFilename(DiscussionNote firstNote)
      {
         if (firstNote.Type != "DiffNote")
         {
            return null;
         }

         string oldPath = firstNote.Position.Old_Path ?? String.Empty;
         string newPath = firstNote.Position.New_Path ?? String.Empty;
         string result = oldPath == newPath ? oldPath : (newPath + "\n(was " + oldPath + ")");

         Label labelFilename = new Label
         {
            Text = result,
            AutoSize = true
         };
         return labelFilename;
      }

      private struct DiscussionNoteWithParentId
      {
         public DiscussionNote Note;
         public string DiscussionId;
      }

      private DiffPosition convertToDiffPosition(Position position)
      {
         return new DiffPosition
         {
            LeftLine = position.Old_Line,
            LeftPath = position.Old_Path,
            RightLine = position.New_Line,
            RightPath = position.New_Path,
            Refs = new mrCore.DiffRefs
            {
               LeftSHA = position.Base_SHA,
               RightSHA = position.Head_SHA
            }
         };
      }

      private readonly string _host;
      private readonly string _accessToken;
      private readonly string _projectId;
      private readonly User _mrAuthor;
      private readonly int _mergeRequestId;
      private readonly ContextDepth _diffContextDepth;
      private readonly ContextDepth _tooltipContextDepth;
      private readonly User _currentUser;
      private readonly IContextMaker _panelContextMaker;
      private readonly IContextMaker _tooltipContextMaker;
      private readonly DiffContextFormatter _formatter;
      private ColorScheme _colorScheme;
   }
}

