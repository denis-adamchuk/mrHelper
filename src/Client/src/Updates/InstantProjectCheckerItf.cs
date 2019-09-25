using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GitLabSharp.Entities;
using mrHelper.Client.Tools;
using mrHelper.Client.Git;

namespace mrHelper.Client.Updates
{
   public interface IInstantProjectChecker
   {
      Task<DateTime> GetLatestChangeTimestampAsync();
   }
}

