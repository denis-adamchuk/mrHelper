﻿using System.Collections.Generic;

namespace mrHelper.Common.Interfaces
{
   public interface IHostProperties
   {
      string GetAccessToken(string host);

      IEnumerable<string> GetEnabledProjects(string host);
   }
}

