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


      public List<MergeRequest> GetAllMergeRequests(MergeRequestState[] states, string[] labels)
      {
         string url = makeUrlForAllMergeRequests(states, labels);
         string response = get(url);

         dynamic s = deserializeJson(response);
         List<MergeRequest> mergeRequests = new List<MergeRequest>();
         foreach (dynamic item in (s as Array))
         {
            mergeRequests.Add(readMergeRequest(item));
         }
         return mergeRequests;
      }

      public List<Commit> GetMergeRequestCommits(string project, int id)
      {
         string url = makeUrlForMergeRequestCommits(project, id);
         string response = get(url);

         dynamic s = deserializeJson(response);
         List<Commit> commits = new List<Commit>();
         foreach (dynamic item in (s as Array))
         {
            Commit commit;
            commit.Id = item["id"];
            commit.ShortId = item["short_id"];
            commit.Title = item["title"];
            commits.Add(commit);
         }
         return commits;
      }

      public void AddSpentTimeForMergeRequest(string project, int id, ref TimeSpan span)
      {
         string url = makeUrlForAddSpentTime(project, id, span);
         post(url);
      }

      private static MergeRequest readMergeRequest(dynamic s)
      {
         MergeRequest mr;
         mr.Id = s["id"];
         mr.Title = s["title"];
         mr.Description = s["description"];
         mr.SourceBranch = s["source_branch"];
         mr.TargetBranch = s["target_branch"];
         Enum.TryParse(s["state"], true, out mr.State);
         mr.Labels = (string[])(s["labels"] as Array);
         mr.WebUrl = s["web_url"];
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

      private string commonUrlPart()
      {
         string commonUrlPart = _host + "/api/" + _version.ToString();
         return commonUrlPart;
      }

      private string makeUrlForSingleProject(string project, int id)
      {
         return commonUrlPart() + "/projects" + "/" + WebUtility.UrlEncode(project);
      }

      private string makeUrlForSingleMergeRequest(string project, int id)
      {
         return makeUrlForSingleProject(project, id) + "/merge_requests/" + id.ToString();
      }

      private string makeUrlForAllMergeRequests(MergeRequestState[] states, string[] labels)
      {
         string url = "/merge_requests";
         for (int i = 0; i < states.Length; ++i)
         {
            url += (i == 0 ? "?" : "&") + "state=" + states[i].ToString();
         }
         for (int i = 0; i < labels.Length; ++i)
         {
            url += ((i == 0 && states.Length == 0) ? "?" : "&") + "label=" + labels[i].ToString();
         }
         return url;
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
