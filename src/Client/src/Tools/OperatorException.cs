using System;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Tools
{
   internal class OperatorException : Exception
   {
      internal OperatorException(GitLabRequestException ex) {}
      internal GitLabRequestException GitLabRequestException;
   }
}

