using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace mrHelper.App.Forms
{
   public partial class EditProjectsForm : CustomFontForm
   {
      public EditProjectsForm(IEnumerable<Tuple<string, bool>> projects)
      {
         Debug.Assert(projects != null);

         InitializeComponent();

         updateProjectsListView(projects);

         applyFont(Program.Settings.MainWindowFontSizeName);
      }

      public IEnumerable<Tuple<string, bool>> Projects
      {
         get
         {
            return listViewProjects.Items
               .Cast<ListViewItem>()
               .Select(x => x.Tag)
               .Cast<Tuple<string, bool>>();
         }
      }

      private void updateProjectsListView(IEnumerable<Tuple<string, bool>> projects)
      {
         listViewProjects.Items.Clear();
         projects.ToList().ForEach(x => addListViewItem(x));
      }

      private void addListViewItem(Tuple<string, bool> project)
      {
         listViewProjects.Items.Add(new ListViewItem(project.Item1) { Tag = project });
      }

      private void listViewProjects_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
      {
         if (e.Item.ListView == null)
         {
            return; // is being removed
         }

         Tuple<string, bool> tag = (Tuple<string, bool>)(e.Item.Tag);

         e.DrawBackground();

         bool isSelected = e.Item.Selected;
         if (isSelected)
         {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
         }

         string text = tag.Item1;
         Brush textBrush = isSelected ? SystemBrushes.HighlightText :
            (tag.Item2 ? SystemBrushes.ControlText : Brushes.LightGray);

         StringFormat format =
            new StringFormat
            {
               Trimming = StringTrimming.EllipsisCharacter,
               FormatFlags = StringFormatFlags.NoWrap
            };

         e.Graphics.DrawString(text, e.Item.ListView.Font, textBrush, e.Bounds, format);
      }

      private void listViewProjects_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
      {
         ListView listView = (sender as ListView);
         listView.Refresh();

         if (listView.SelectedItems.Count < 1)
         {
            buttonRemoveProject.Enabled = false;
            buttonToggleState.Enabled = false;
            return;
         }

         buttonUp.Enabled = listView.SelectedIndices[0] != 0;
         buttonDown.Enabled = listView.SelectedIndices[0] != listView.Items.Count - 1;

         buttonRemoveProject.Enabled = true;
         buttonToggleState.Enabled = true;

         Tuple<string, bool> tag = (Tuple<string, bool>)(listView.SelectedItems[0].Tag);
         buttonToggleState.Text = tag.Item2 ? "Disable" : "Enable";
      }

      private void buttonAddProject_Click(object sender, EventArgs e)
      {
         using (AddProjectForm form = new AddProjectForm())
         {
            if (form.ShowDialog() != DialogResult.OK)
            {
               return;
            }

            if (form.ProjectName.Count(x => x == '/') != 1)
            {
               MessageBox.Show("Wrong format of project name. It should include a group name too.", "Project will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            if (listViewProjects.Items
               .Cast<ListViewItem>()
               .Any(x => x.Text == form.ProjectName))
            {
               MessageBox.Show("Such project is already in the list", "Project will not be added",
                  MessageBoxButtons.OK, MessageBoxIcon.Warning);
               return;
            }

            addListViewItem(new Tuple<string, bool>(form.ProjectName, true));
         }
      }

      private void buttonRemoveProject_Click(object sender, EventArgs e)
      {
         if (listViewProjects.SelectedItems.Count > 0)
         {
            Trace.TraceInformation(String.Format("[EditProjectsForm] Removing project {0}",
               listViewProjects.SelectedItems[0].ToString()));

            listViewProjects.Items.Remove(listViewProjects.SelectedItems[0]);
         }
      }

      private void buttonToggleState_Click(object sender, EventArgs e)
      {
         if (listViewProjects.SelectedItems.Count > 0)
         {
            Trace.TraceInformation(String.Format("[EditProjectsForm] Toggle state of project {0}",
               listViewProjects.SelectedItems[0].ToString()));

            Tuple<string, bool> tag = (Tuple<string, bool>)(listViewProjects.SelectedItems[0].Tag);
            listViewProjects.SelectedItems[0].Tag = new Tuple<string, bool>(tag.Item1, !tag.Item2);

            buttonToggleState.Text = !tag.Item2 ? "Disable" : "Enable";
         }
      }

      private void buttonUp_Click(object sender, EventArgs e)
      {
         if (listViewProjects.SelectedIndices.Count > 0)
         {
            int selectedIndex = listViewProjects.SelectedIndices[0];
            Debug.Assert(selectedIndex > 0);

            moveItem(true, selectedIndex);
         }
      }

      private void buttonDown_Click(object sender, EventArgs e)
      {
         if (listViewProjects.SelectedItems.Count > 0)
         {
            int selectedIndex = listViewProjects.SelectedIndices[0];
            Debug.Assert(selectedIndex < listViewProjects.Items.Count - 1);

            moveItem(false, selectedIndex);
         }
      }

      private void moveItem(bool up, int index)
      {
         Trace.TraceInformation(String.Format("[EditProjectsForm] Moving project \"{0}\" {1}",
            listViewProjects.Items[index].ToString(), up ? "up" : "down"));

         ListViewItem selectedItem = listViewProjects.Items[index];
         listViewProjects.Items.RemoveAt(index);
         listViewProjects.Items.Insert(up ? index - 1 : index + 1, selectedItem);
      }
   }
}

