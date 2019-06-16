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
         string url = makeRestApiUrl(project, id, RestApiUrlType.SingleMergeRequest);
         string response = get(url);

         dynamic s = deserializeJson(response);
         MergeRequest mr = new MergeRequest();
         mr.Id = s["id"];
         mr.Title = s["title"];
         mr.Description = s["description"];
         mr.SourceBranch = s["source_branch"];
         mr.TargetBranch = s["target_branch"];
         return mr;
      }

      public List<Commit> GetMergeRequestCommits(string project, int id)
      {
         string url = makeRestApiUrl(project, id, RestApiUrlType.MergeRequestCommits);
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
         string url = makeRestApiUrl(project, id, RestApiUrlType.AddSpentTimeForMergeRequest, span);
         post(url);
      }

      private enum RestApiUrlType
      {
         SingleProject,
         SingleMergeRequest,
         MergeRequestCommits,
         AddSpentTimeForMergeRequest
      }

      private string post(string data)
      {
         return _client.UploadString(data, "");
      }

      private string get(string request)
      {
         return _client.DownloadString(request);
      }

      private string makeRestApiUrl(string project, int id, RestApiUrlType type, params object[] parameters)
      {
         string commonUrlPart = _host + "/api/" + _version.ToString();

         switch (type)
         {
            case RestApiUrlType.SingleProject:
               return commonUrlPart + "/projects" + "/" + WebUtility.UrlEncode(project);

            case RestApiUrlType.SingleMergeRequest:
               return makeRestApiUrl(project, id, RestApiUrlType.SingleProject) + "/merge_requests/" + id.ToString();

            case RestApiUrlType.MergeRequestCommits:
               return makeRestApiUrl(project, id, RestApiUrlType.SingleMergeRequest) + "/commits";

            case RestApiUrlType.AddSpentTimeForMergeRequest:
               TimeSpan span = (TimeSpan)parameters[0];
               string duration = convertTimeSpanToGitlabDuration(span);
               return makeRestApiUrl(project, id, RestApiUrlType.SingleMergeRequest) + "/add_spent_time?duration=" + duration;
         }

         throw new NotImplementedException();
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
