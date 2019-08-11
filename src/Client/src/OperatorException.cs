using System;

namespace mrHelper.Client
{
   internal class OperatorException
   {
      internal OperatorException(GitLabRequestException ex) {}
      internal GitLabRequestException GitLabRequestException;
   }
}

