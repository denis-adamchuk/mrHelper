using mrHelper.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace mrHelper.GitClient
{
   public interface ILocalGitRepositoryData : IGitRepositoryData
   {
      Task Update(IEnumerable<GitShortStatArguments> arguments);
      Task Update(IEnumerable<GitDiffArguments> arguments);
      Task Update(IEnumerable<GitRevisionArguments> arguments);
      Task Update(IEnumerable<GitNumStatArguments> arguments);
   }
}

