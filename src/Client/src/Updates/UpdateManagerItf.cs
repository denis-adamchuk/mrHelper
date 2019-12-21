﻿using mrHelper.Client.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Updates
{
   public interface IUpdateManager
   {
      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IInstantProjectChecker GetLocalProjectChecker(MergeRequestKey mrk);

      /// <summary>
      /// Checks local cache to detect if there are project changes caused by new versions of any merge request
      /// </summary>
      IInstantProjectChecker GetLocalProjectChecker(ProjectKey pk);

      /// <summary>
      /// Makes a request to GitLab to detect if there are project changes caused by new versions of a merge request
      /// </summary>
      IInstantProjectChecker GetRemoteProjectChecker(MergeRequestKey mrk);

      /// <summary>
      /// Request Update Manager to update the specified MR after the specified time period (in milliseconds)
      /// </summary>
      void RequestOneShotUpdate(MergeRequestKey mrk, int delay);
   }
}
