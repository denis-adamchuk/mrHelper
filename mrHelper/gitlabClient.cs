using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;

namespace mrHelper
{
   enum ApiVersion
   {
      v3,
      v4
   }

   enum StateFilter
   {
      Open,
      Closed,
      Merged,
      All
   }

   enum WorkInProgressFilter
   {
      Yes,
      No,
      All
   }

   class gitlabClient
   {
      public gitlabClient(string host, string token, ApiVersion version = ApiVersion.v4)
      {
         // TODO Should not add it manually
         _host = "https://" + host;
         _token = token;
         _version = version;

         ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
         _client = new WebClient();
         _client.BaseAddress = _host;
         _client.Headers.Add("Content-Type:application/json");
         _client.Headers.Add("Accept:application/json");
         _client.Headers["Private-Token"] = _token;
      }

      public MergeRequest GetSingleMergeRequest(string project, int id)
      {
         string url = makeUrlForSingleMergeRequest(project, id);
         string response = get(url);

         dynamic s = deserializeJson(response);
         return readMergeRequest(s);
      }

      public List<MergeRequest> GetAllMergeRequests(StateFilter state, string labels, string author, WorkInProgressFilter wip)
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
            commit.CommitedDate = DateTimeOffset.Parse(item["commited_date"]);
            commits.Add(commit);
         }
         return commits;
      }

      private static MergeRequest readMergeRequest(dynamic json)
      {
         MergeRequest mr;
         mr.Id = json["id"];
         mr.Title = json["title"];
         mr.Description = json["description"];
         mr.SourceBranch = json["source_branch"];
         mr.TargetBranch = json["target_branch"];
         Enum.TryParse(json["state"], true, out mr.State);
         mr.Labels = (string[])(json["labels"] as Array);
         mr.WebUrl = json["web_url"];
         mr.WorkInProgress = json["work_in_progress"];

         dynamic jsonAuthor = json["author"];
         mr.Author.Id = jsonAuthor["id"];
         mr.Author.Name = jsonAuthor["name"];
         mr.Author.Username = jsonAuthor["username"];
         return mr;
      }

      public void AddSpentTimeForMergeRequest(string project, int id, ref TimeSpan span)
      {
         string url = makeUrlForAddSpentTime(project, id, span);
         post(url);
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

      private string makeUrlForSingleProject(string project, int id)
      {
         return makeCommonUrl() + "/projects" + "/" + WebUtility.UrlEncode(project);
      }

      private string makeUrlForSingleMergeRequest(string project, int id)
      {
         return makeUrlForSingleProject(project, id) + "/merge_requests/" + id.ToString();
      }

      private string makeUrlForAllMergeRequests(StateFilter state, string labels, string author, WorkInProgressFilter wip)
      {
         return makeCommonUrl()
            + "/merge_requests&scope=all"
            + query("wip", workInProgressToString(wip))
            + query("state", stateFilterToString(state))
            + query("labels", labels)
            + query("author", author);
      }

      private string makeUrlForMergeRequestCommits(string project, int id)
      {
         return makeUrlForSingleMergeRequest(project, id) + "/commits";
      }

      private string makeUrlForAddSpentTime(string project, int id, TimeSpan span)
      {
         string duration = convertTimeSpanToGitlabDuration(span);
         return makeUrlForSingleMergeRequest(project, id) + "/add_spent_time?duration=" + duration;
      }

      private string convertTimeSpanToGitlabDuration(TimeSpan span)
      {
         return span.ToString("hh") + "h" + span.ToString("mm") + "m" + span.ToString("ss") + "s";
      }

      private string stateFilterToString(StateFilter state)
      {
         switch (state)
         {
            case StateFilter.All: return "";
            case StateFilter.Closed: return "closed";
            case StateFilter.Merged: return "merged";
            case StateFilter.Open: return "opened";
         }
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
         JavaScriptSerializer JavaScriptSerializer = new JavaScriptSerializer();
         return JavaScriptSerializer.DeserializeObject(Json);
      }

      private readonly string _host;
      private readonly string _token;
      private readonly ApiVersion _version;
      private WebClient _client;
   }
}
