using mrHelper.Client.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Projects
{
   public interface ISingleProjectAccessor
   {
      IRepositoryAccessor RepositoryAccessor { get; }
   }
}

