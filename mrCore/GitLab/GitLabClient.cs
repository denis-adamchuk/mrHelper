using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace mrCore
{
   public enum ApiVersion
   {
      v3,
      v4
   }

   public enum StateFilter
   {
      Open,
      Closed,
      Merged,
      All
   }

   public enum WorkInProgressFilter
   {
      Yes,
      No,
      All
   }

   public class GitLabClient
   {
      private string protocol = "https://";

      public GitLabClient(string host, string token, ApiVersion version = ApiVersion.v4)
      {
         _host = protocol + host;
         _token = token;
         _version = version;

         ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
         _client = new WebClient();
         _client.BaseAddress = _host;
         _client.Headers.Add("Content-Type:application/json");
         _client.Headers.Add("Accept:application/json");
         _client.Headers["Private-Token"] = _token;
      }

      public User GetCurrentUser()
      {
         string url = makeCommonUrl() + "/user";
         string response = get(url);
         dynamic json = deserializeJson(response);
         return readUser(json);
      }

      public List<Project> GetAllProjects(bool publicOnly)
      {
         string url = makeUrlForAllProjects(publicOnly);
         string response = get(url);

         dynamic s = deserializeJson(response);
         List<Project> projects = new List<Project>();
         foreach (dynamic item in (s as Array))
         {
            Project project = new Project();
            project.Id = item["id"];
            project.NameWithNamespace = item["path_with_namespace"];
            projects.Add(project);
         }
         return projects;
      }

      public MergeRequest GetSingleMergeRequest(string project, int id)
      {
         string url = makeUrlForSingleMergeRequest(project, id);
         string response = get(url);

         dynamic s = deserializeJson(response);
         return readMergeRequest(s);
      }

      public List<MergeRequest> GetAllMergeRequests(StateFilter state, string labels, string author,
         WorkInProgressFilter wip)
      {
         string url = makeUrlForAllMergeRequests(state, labels, author, wip);
         string response = get(url);

         dynamic json = deserializeJson(response);
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (dynamic item in (json as Array))
         {
            mergeRequests.Add(readMergeRequest(item));
         }
         return mergeRequests;
      }

      public List<MergeRequest> GetAllProjectMergeRequests(string project)
      {
         string url = makeUrlForAllProjectMergeRequests(project, StateFilter.Open, WorkInProgressFilter.Yes, 100);
         string response = get(url);

         dynamic json = deserializeJson(response);
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (dynamic item in (json as Array))
         {
            mergeRequests.Add(readMergeRequest(item));
         }
         return mergeRequests;
      }

      public List<Commit> GetMergeRequestCommits(string project, int id)
      {
         string url = makeUrlForMergeRequestCommits(project, id);
         string response = get(url);

         dynamic json = deserializeJson(response);
         List<Commit> commits = new List<Commit>();
         foreach (dynamic item in (json as Array))
         {
            Commit commit;
            commit.Id = item["id"];
            commit.ShortId = item["short_id"];
            commit.Title = item["title"];
            commit.Message = item["message"];
            commit.CommitedDate = DateTimeOffset.Parse(item["committed_date"]).DateTime;
            commits.Add(commit);
         }
         return commits;
      }

      public List<Version> GetMergeRequestVersions(string project, int id)
      {
         string url = makeUrlForMergeRequestVersions(project, id);
         string response = get(url);

         dynamic json = deserializeJson(response);
         List<Version> versions = new List<Version>();
         foreach (dynamic item in (json as Array))
         {
            Version version = new Version();
            version.Id = item["id"];
            version.Refs.HeadSHA = item["head_commit_sha"];
            version.Refs.BaseSHA = item["base_commit_sha"];
            version.Refs.StartSHA = item["start_commit_sha"];
            version.CreatedAt = DateTimeOffset.Parse(item["created_at"]).DateTime;
            versions.Add(version);
         }
         return versions;
      }

      public List<Discussion> GetMergeRequestDiscussions(string project, int id)
      {
         // evaluate total number of items
         get(makeUrlForMergeRequestDiscussions(project, id, 1, 1));
         int total = int.Parse(_client.ResponseHeaders["X-Total"]);
         int perPage = 100;
         int pages = total / perPage + (total % perPage > 0 ? 1 : 0);

         // load all discussions page by page
         List<Discussion> discussions = new List<Discussion>();
         for (int iPage = 0; iPage < pages; ++iPage)
         {
            string url = makeUrlForMergeRequestDiscussions(project, id, iPage + 1, perPage);
            string response = get(url);

            dynamic json = deserializeJson(response);
            foreach (dynamic item in (json as Array))
            {
               discussions.Add(readDiscussion(item));
            }
         }
         return discussions;
      }

      public void AddSpentTimeForMergeRequest(string project, int id, ref TimeSpan span)
      {
         string url = makeUrlForAddSpentTime(project, id, span);
         post(url);
      }

      public void CreateNewMergeRequestDiscussion(string project, int id, DiscussionParameters parameters)
      {
         string url = makeUrlForNewDiscussion(project, id, parameters);
         post(url);
      }

      public void CreateNewMergeRequestNote(string project, int id, string body)
      {
         string url = makeUrlForNewNote(project, id, body);
         post(url);
      }

      private static MergeRequest readMergeRequest(dynamic json)
      {
         MergeRequest mr = new MergeRequest();
         mr.Id = json["iid"];
         mr.Title = json["title"];
         mr.Description = json["description"];
         mr.SourceBranch = json["source_branch"];
         mr.TargetBranch = json["target_branch"];
         Enum.TryParse(json["state"], true, out mr.State);
         dynamic jsonLables = json["labels"];
         mr.Labels = new List<string>();
         foreach (dynamic item in (jsonLables as Array))
         {
            mr.Labels.Add(item);
         }
         mr.WebUrl = json["web_url"];
         mr.WorkInProgress = json["work_in_progress"];
         mr.Author = readUser(json["author"]);

         if (json.ContainsKey("diff_refs"))
         {
            dynamic jsonDiffRefs = json["diff_refs"];
            mr.Refs.BaseSHA = jsonDiffRefs["base_sha"];
            mr.Refs.HeadSHA = jsonDiffRefs["head_sha"];
            mr.Refs.StartSHA = jsonDiffRefs["start_sha"];
         }
         return mr;
      }

      private static User readUser(dynamic jsonUser)
      {
         User user = new User();
         user.Id = jsonUser["id"];
         user.Name = jsonUser["name"];
         user.Username = jsonUser["username"];
         return user;
      }

      private static Discussion readDiscussion(dynamic json)
      {
         Discussion discussion = new Discussion();
         discussion.Id = json["id"];
         discussion.IndividualNote = json["individual_note"];
         dynamic jsonNotes = json["notes"];
         discussion.Notes = new List<DiscussionNote>();
         foreach (dynamic item in (jsonNotes as Array))
         {
            DiscussionNote discussionNote = new DiscussionNote();
            discussionNote.Id = item["id"];
            discussionNote.Body = item["body"];
            discussionNote.Author = readUser(item["author"]);
            discussionNote.Type = convertDiscussionNoteTypeFromJson(item["type"]);
            discussionNote.System = item["system"];
            discussionNote.Resolvable = item["resolvable"];
            discussionNote.CreatedAt = DateTimeOffset.Parse(item["created_at"]).DateTime;
            if (item.ContainsKey("resolved"))
            {
               discussionNote.Resolved = item["resolved"];
            }
            if (item.ContainsKey("position"))
            {
               discussionNote.Position = readposition(item["position"]);
            }
            discussion.Notes.Add(discussionNote);
         }
         return discussion;
      }

      private static Position readposition(dynamic json)
      {
         Position position;
         position.Refs.HeadSHA = json["head_sha"];
         position.Refs.BaseSHA = json["base_sha"];
         position.Refs.StartSHA = json["start_sha"];
         position.OldLine = json["old_line"] != null ? json["old_line"].ToString() : null;
         position.OldPath = json["old_path"];
         position.NewLine = json["new_line"] != null ? json["new_line"].ToString() : null;
         position.NewPath = json["new_path"];
         return position;
      }

      private string post(string data)
      {
         return _client.UploadString(data, "");
      }

      private string get(string request)
      {
         return _client.DownloadString(request);
      }

      private string makeCommonUrl()
      {
         string commonUrlPart = _host + "/api/" + _version.ToString();
         return commonUrlPart;
      }

      private string makeUrlForAllProjects(bool publicOnly)
      {
         return makeCommonUrl()
            + "/projects"
            + query("?with_merge_requests_enabled", "true")
            + query("&per_page", "100")
            + query("&simple", "true")
            + (publicOnly ? query("&visibility", "public") : "");
      }
        
      private string makeUrlForSingleProject(string project, int id)
      {
         return makeCommonUrl() + "/projects" + "/" + WebUtility.UrlEncode(project);
      }

      private string makeUrlForSingleMergeRequest(string project, int id)
      {
         return makeUrlForSingleProject(project, id) + "/merge_requests/" + id.ToString();
      }

      private string makeUrlForAllProjectMergeRequests(string project, StateFilter state, WorkInProgressFilter wip,
         int perPage)
      {
         return makeCommonUrl()
            + "/projects/" + WebUtility.UrlEncode(project)
            + "/merge_requests"
            + query("?per_page", perPage.ToString())
            + query("&order_by", "updated_at")
            + query("&wip", workInProgressToString(wip))
            + query("&state", stateFilterToString(state)); 
      }

      private string makeUrlForAllMergeRequests(StateFilter state, string labels, string author, WorkInProgressFilter wip)
      {
         return makeCommonUrl()
            + "/merge_requests?scope=all"
            + query("&wip", workInProgressToString(wip))
            + query("&state", stateFilterToString(state))
            + query("&labels", labels)
            + query("&author", author);
      }

      private string makeUrlForMergeRequestCommits(string project, int id)
      {
         return makeUrlForSingleMergeRequest(project, id) + "/commits";
      }

      private string makeUrlForMergeRequestVersions(string project, int id)
      {
         return makeUrlForSingleMergeRequest(project, id) + "/versions";
      }

      private string makeUrlForAddSpentTime(string project, int id, TimeSpan span)
      {
         string duration = convertTimeSpanToGitlabDuration(span);
         return makeUrlForSingleMergeRequest(project, id) + "/add_spent_time?duration=" + duration;
      }

      private string makeUrlForNewDiscussion(string project, int id, DiscussionParameters parameters)
      {
         string url = makeUrlForSingleMergeRequest(project, id)
            + "/discussions"
            + "?body=" + WebUtility.UrlEncode(parameters.Body);

         var pos = parameters.Position;
         if (pos.HasValue)
         {
            url += "&" + WebUtility.UrlEncode("position[position_type]") + "=text";
            url += "&" + WebUtility.UrlEncode("position[old_path]") + "=" + WebUtility.UrlEncode(pos.Value.OldPath);
            url += "&" + WebUtility.UrlEncode("position[new_path]") + "=" + WebUtility.UrlEncode(pos.Value.NewPath);
            url += "&" + WebUtility.UrlEncode("position[base_sha]") + "=" + pos.Value.Refs.BaseSHA;
            url += "&" + WebUtility.UrlEncode("position[start_sha]") + "=" + pos.Value.Refs.StartSHA;
            url += "&" + WebUtility.UrlEncode("position[head_sha]") + "=" + pos.Value.Refs.HeadSHA;
            if (pos.Value.OldLine != null)
            {
               url += "&" + WebUtility.UrlEncode("position[old_line]") + "=" + pos.Value.OldLine;
            }
            if (pos.Value.NewLine != null)
            {
               url += "&" + WebUtility.UrlEncode("position[new_line]") + "=" + pos.Value.NewLine;
            }
         }

         return url;
      }

      private string makeUrlForMergeRequestDiscussions(string project, int id, int pageNumber, int perPage)
      {
         string url = makeUrlForSingleMergeRequest(project, id)
            + "/discussions"
            + query("?page", pageNumber.ToString())
            + query("&per_page", perPage.ToString());
         return url;
      }

      private string makeUrlForNewNote(string project, int id, string body)
      {
         string url = makeUrlForSingleMergeRequest(project, id)
            + "/notes"
            + "?body=" + WebUtility.UrlEncode(body);
         return url;
      }

      private string convertTimeSpanToGitlabDuration(TimeSpan span)
      {
         return span.ToString("hh") + "h" + span.ToString("mm") + "m" + span.ToString("ss") + "s";
      }

      private static DiscussionNoteType convertDiscussionNoteTypeFromJson(string type)
      {
         if (type == "DiffNote")
         {
            return DiscussionNoteType.DiffNote;
         }
         else if (type == "DiscussionNote")
         {
            return DiscussionNoteType.DiscussionNote;
         } 
         return DiscussionNoteType.Default;
      }

      private string stateFilterToString(StateFilter state)
      {
         switch (state)
         {
            case StateFilter.All: return "";
            case StateFilter.Closed: return "closed";
            case StateFilter.Merged: return "merged";
            case StateFilter.Open: return "opened"; }
         return "";
      }

      private string workInProgressToString(WorkInProgressFilter wip)
      {
         switch (wip)
         {
            case WorkInProgressFilter.All: return "";
            case WorkInProgressFilter.No: return "no";
            case WorkInProgressFilter.Yes: return "yes";
         }
         return "";         
      }

      private string query(string query, string value)
      {
         if (value.Length > 0)
         {
            return query + "=" + value;
         }
         return "";
      }

      private static object deserializeJson(string Json)
      {
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         return serializer.DeserializeObject(Json);
      }

      private readonly string _host;
      private readonly string _token;
      private readonly ApiVersion _version;
      private WebClient _client;
   }
}
