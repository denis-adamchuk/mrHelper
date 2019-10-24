using System;
using System.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Client.Git;
using mrHelper.Client.Tools;
using mrHelper.Client.Workflow;
using mrHelper.Common.Interfaces;

namespace mrHelper.Client.Git
{
   /// <summary>
   /// Pre-loads file revisions into git repository cache
   /// </summary>
   public class RevisionCacher
   {
      public RevisionCacher(DiscussionManager discussionManager, ISynchronizeInvoke synchronizeInvoke,
         Func<ProjectKey, IGitRepository> getRepository)
      {
         discussionManager.PostLoadDiscusions += (mrk, discussions) => processDiscussions(mrk, discussions);
         SynchronizeInvoke = synchronizeInvoke;
         GetRepository = getRepository;
      }

      public void Cache(GitClient gitClient)
      {
         ProjectKey projectKey = gitClient.ProjectKey;

         if (ReadyProjects.Add(projectKey))
         {
            Trace.TraceInformation(String.Format( "[RevisionCacher] Project {0} at host {1} is ready",
               projectKey.ProjectName, projectKey.HostName));
         }

         if (PostponedNotes.Any(x => x.Key.ProjectKey.Equals(projectKey)))
         {
            Dictionary<MergeRequestKey, List<Note>> toCache =
               PostponedNotes.Where(pair => pair.Key.ProjectKey.Equals(projectKey)).ToDictionary(
                  pair => pair.Key, pair => pair.Value);
            foreach (KeyValuePair<MergeRequestKey, List<Note>> notes in toCache)
            {
               scheduleCache(projectKey, notes.Value);
               PostponedNotes.Remove(notes.Key);
            }
         }
      }

      private void processDiscussions(MergeRequestKey mrk, List<Discussion> discussions)
      {
         Trace.TraceInformation(String.Format(
            "[RevisionCacher] Got {0} discussions for MRK: HostName={0}, ProjectName={1}, IId={2}",
            discussions.Count, mrk.ProjectKey.HostName, mrk.ProjectKey.ProjectName, mrk.IId));
         List<Note> notesFiltered = new List<Note>();
         foreach (Discussions discussion in discussions)
         {
            notesFiltered.AddRange(discussions.Notes.Where(x =>
               x.Ix.Type == "DiffNote" && !x.Position.Equals(default(Position))).ToList());
         }

         if (ReadyProjects.Contains(mrk.ProjectKey))
         {
            scheduleCache(mrk.ProjectKey, notesFiltered);
         }
         else
         {
            PostponedNotes[mrk] = notesFiltered;
         }
      }

      private void scheduleCache(ProjectKey projectKey, List<Note> notes)
      {
         if (notes.Count == 0)
         {
            return;
         }

         Note[] notesCopy = new Note[notes.Count];
         notes.CopyTo(notesCopy);
         SynchronizeInvoke.BeginInvoke(new Action<IGitRepository, Note[]>(
            async (repository, notesInternal) =>
         {
            Trace.TraceInformation(String.Format(
               "[RevisionCacher] Caching revisions for {0} notes in project {1} at host {2}",
               notes.Count(), projectKey.ProjectName, projectKey.HostName));

            await doCacheAsync(repository, notesInternal);
         }), new object[] { GetRepository(projectKey), notesCopy });
      }

      async private Task doCacheAsync(IGitRepository gitRepository, Note[] notes)
      {
         foreach (Note note in notes)
         {
            await gitRepository.DiffAsync(note.Position.Base_SHA, note.Position.Head_SHA,
               note.Position.Old_Path, note.Position.New_Path, 0);
            await gitRepository.DiffAsync(note.Position.Base_SHA, note.Position.Head_SHA,
               note.Position.Old_Path, note.Position.New_Path, mrHelper.Common.Constants.Constants.FullContextSize);
            if (note.Position.Old_Line != null)
            {
               await gitRepository.ShowFileByRevisionAsync(note.Position.Old_Path, note.Position.Base_SHA);
            }
            if (note.Position.New_Line != null)
            {
               await gitRepository.ShowFileByRevisionAsync(note.Position.New_Path, note.Position.Head_SHA);
            }
         }
      }

      private Dictionary<MergeRequestKey, List<Note>> PostponedNotes = new Dictionary<MergeRequestKey, List<Note>>();
      private HashSet<ProjectKey> ReadyProjects = new HashSet<ProjectKey>();
      private ISynchronizeInvoke SynchronizeInvoke { get; }
      private Func<ProjectKey, IGitRepository> GetRepository { get; }
   }
}

