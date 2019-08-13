using System;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Operators
{
   internal class OperatorException
   {
      internal OperatorException(GitLabRequestException ex) {}
      internal GitLabRequestException GitLabRequestException;
   }
}

