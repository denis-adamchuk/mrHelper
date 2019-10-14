﻿using System.Threading.Tasks;

namespace mrHelper.Common.Interfaces
{
   public interface ICommand
   {
      string GetName();

      string GetDependency();

      Task Run();
   }
}
