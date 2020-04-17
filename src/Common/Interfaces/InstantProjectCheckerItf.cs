using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public class ProjectSnapshot
   {
      public DateTime LatestChange = DateTime.MinValue;
      public List<string> Sha = new List<string>();
   }

   public interface IInstantProjectChecker
   {
      Task<ProjectSnapshot> GetProjectSnapshot();
   }
}

