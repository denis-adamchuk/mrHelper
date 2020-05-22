using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitLabSharp;
using GitLabSharp.Accessors;
using GitLabSharp.Entities;
using mrHelper.Common.Constants;

namespace mrHelper.Client.Common
{
   internal class MergeRequestSearchByIIdProcessor : SingleProjectMergeRequestSearchProcessor
   {
      internal MergeRequestSearchByIIdProcessor(int iid, string projectName, bool onlyOpen)
         : base(onlyOpen)
      {
         _iid = iid;
         _projectname = projectName;
      }

      public async override Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? _)
      {
         MergeRequest mergeRequest = await gl.Projects.Get(_projectname).MergeRequests.Get(_iid).LoadTaskAsync();
         return new MergeRequest[] { mergeRequest };
      }

      private int _iid;
      private string _projectname;
   }

   internal class MergeRequestSearchByProjectProcessor : SingleProjectMergeRequestSearchProcessor
   {
      internal MergeRequestSearchByProjectProcessor(string projectName, bool onlyOpen)
         : base(onlyOpen)
      {
         _projectname = projectName;
      }

      public override Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults)
      {
         return load(gl, _projectname, maxResults, new MergeRequestsFilter(
            null, _wipFilter, _stateFilter, false, null, null, null, null, null));
      }

      private string _projectname;
   }

   internal class MergeRequestSearchByTargetBranchProcessor : CrossProjectMergeRequestSearchProcessor
   {
      internal MergeRequestSearchByTargetBranchProcessor(string branchname, bool onlyOpen)
         : base(onlyOpen)
      {
         _branchname = branchname;
      }

      public override Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults)
      {
         return load(gl, maxResults, new MergeRequestsFilter(
            null, _wipFilter, _stateFilter, false, null, _branchname, null, null, null));
      }

      private string _branchname;
   }

   internal class MergeRequestSearchByTextProcessor : CrossProjectMergeRequestSearchProcessor
   {
      internal MergeRequestSearchByTextProcessor(string text, bool onlyOpen)
         : base(onlyOpen)
      {
         _text = text;
      }

      public override Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? maxResults)
      {
         return load(gl, maxResults, new MergeRequestsFilter(
            null, _wipFilter, _stateFilter, false, _text, null, null, null, null));
      }

      private string _text;
   }

   internal class MergeRequestSearchByUsernameProcessor : CrossProjectMergeRequestSearchProcessor
   {
      internal MergeRequestSearchByUsernameProcessor(string username, bool onlyOpen)
         : base(onlyOpen)
      {
         _username = username;
      }

      public async override Task<IEnumerable<MergeRequest>> Process(GitLab gl, int? _)
      {
         IEnumerable<MergeRequest> byLabel = await loadByLabel(gl, Constants.GitLabLabelPrefix + _username);
         IEnumerable<MergeRequest> byAuthor = await loadByAuthor(gl, _username);
         return byLabel
            .Concat(byAuthor)
            .GroupBy(x => x.Id) // filter out non-unique, important to use Id because search is cross-project
            .Select(group => group.First());
      }

      private Task<IEnumerable<MergeRequest>> loadByLabel(GitLab gl, string label)
      {
         return load(gl, null, new MergeRequestsFilter(
            label, _wipFilter, _stateFilter, false, null, null, null, null, null));
      }

      async private Task<IEnumerable<MergeRequest>> loadByAuthor(GitLab gl, string username)
      {
         User user = GlobalCache.GetUser(gl.Host, username);
         if (user == null)
         {
            IEnumerable<User> users = await gl.Users.SearchByUsernameTaskAsync(username);
            if (users == null || !users.Any())
            {
               return System.Array.Empty<MergeRequest>();
            }
            user = users.First();
            GlobalCache.AddUser(gl.Host, user);
         }

         return await load(gl, null, new MergeRequestsFilter(
            null, _wipFilter, _stateFilter, false, null, null, null, null, user.Id));
      }

      private string _username;
   }
}

