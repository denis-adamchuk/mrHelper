using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface IProjectUpdate {}

   public class FullProjectUpdate : IProjectUpdate
   {
      public DateTime LatestChange;
      public IEnumerable<string> Sha;
   }

   public class PartialProjectUpdate : IProjectUpdate
   {
      public IEnumerable<string> Sha;
   }

   public interface IProjectUpdateContext
   {
      Task<IProjectUpdate> GetUpdate();
   }
}

