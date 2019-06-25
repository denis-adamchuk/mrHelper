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

   struct DiscussionParameters
   {
      public string Body;

      public struct PositionDetails
      {
         public string OldPath;
         public string NewPath;
         public string OldLine;
         public string NewLine;
         public string BaseSHA;
         public string HeadSHA;
         public string StartSHA;
      }

      public PositionDetails? Position;
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
         List<Version> commits = new List<Version>();
         foreach (dynamic item in (json as Array))
         {
            Version version;
            version.Id = item["id"];
            version.HeadSHA = item["head_commit_sha"];
            version.BaseSHA = item["base_commit_sha"];
            version.StartSHA = item["start_commit_sha"];
            version.CreatedAt = DateTimeOffset.Parse(item["created_at"]).DateTime;
            commits.Add(version);
         }
         return commits;
      }


      public void AddSpentTimeForMergeRequest(string project, int id, ref TimeSpan span)
      {
         // TODO Add handling of responses
         string url = makeUrlForAddSpentTime(project, id, span);
         post(url);
      }

      public bool CreateNewMergeRequestDiscussion(string project, int id, DiscussionParameters parameters)
      {
         string url = makeUrlForNewDiscussion(project, id, parameters);
         string response = post(url);

         dynamic json = deserializeJson(response);
         if (json.ContainsKey("message"))
         {
            // TODO Anything else?
            return false;
         }
         return true;
      }

      private static MergeRequest readMergeRequest(dynamic json)
      {
         MergeRequest mr = new MergeRequest();
         mr.Id = json["id"];
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

         dynamic jsonAuthor = json["author"];
         mr.Author.Id = jsonAuthor["id"];
         mr.Author.Name = jsonAuthor["name"];
         mr.Author.Username = jsonAuthor["username"];

         if (json.ContainsKey("diff_refs"))
         {
            dynamic jsonDiffRefs = json["diff_refs"];
            mr.BaseSHA = jsonDiffRefs["base_sha"];
            mr.HeadSHA = jsonDiffRefs["head_sha"];
            mr.StartSHA = jsonDiffRefs["start_sha"];
         }
         return mr;
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
            + "?" + WebUtility.UrlEncode("body") + "=" + WebUtility.UrlEncode(parameters.Body);

         var pos = parameters.Position;
         if (pos.HasValue)
         {
            url += "&" + WebUtility.UrlEncode("position[position_type]") + "=text";
            url += "&" + WebUtility.UrlEncode("position[old_path]") + "=" + WebUtility.UrlEncode(pos.Value.OldPath);
            url += "&" + WebUtility.UrlEncode("position[new_path]") + "=" + WebUtility.UrlEncode(pos.Value.NewPath);
            url += "&" + WebUtility.UrlEncode("position[base_sha]") + "=" + pos.Value.BaseSHA;
            url += "&" + WebUtility.UrlEncode("position[start_sha]") + "=" + pos.Value.StartSHA;
            url += "&" + WebUtility.UrlEncode("position[head_sha]") + "=" + pos.Value.HeadSHA;
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
         JavaScriptSerializer serializer = new JavaScriptSerializer();
         return serializer.DeserializeObject(Json);
      }

      private readonly string _host;
      private readonly string _token;
      private readonly ApiVersion _version;
      private WebClient _client;
   }
}
