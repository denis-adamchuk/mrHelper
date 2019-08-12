using System;

namespace mrHelper.Client.Tools
{
   internal class OperatorException
   {
      internal OperatorException(GitLabRequestException ex) {}
      internal GitLabRequestException GitLabRequestException;
   }
}

