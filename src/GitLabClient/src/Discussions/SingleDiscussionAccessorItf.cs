using System;
using System.Threading.Tasks;
using mrHelper.Common.Exceptions;
using GitLabSharp.Accessors;

namespace mrHelper.Client.Discussions
{
   public interface ISingleDiscussionAccessor
   {
      IDiscussionEditor GetDiscussionEditor();
   }
}

