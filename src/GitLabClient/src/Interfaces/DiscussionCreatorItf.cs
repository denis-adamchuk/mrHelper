using System;
using System.Threading.Tasks;
using GitLabSharp.Accessors;
using mrHelper.Common.Exceptions;

namespace mrHelper.GitLabClient
{
   public class DiscussionCreatorException : ExceptionEx
   {
      public DiscussionCreatorException(bool handled, Exception innerException)
         : base("Discussion creation failed", innerException)
      {
         Handled = handled;
      }

      public bool Handled { get; }
   }

   public interface IDiscussionCreator
   {
      Task CreateNoteAsync(CreateNewNoteParameters parameters);

      Task CreateDiscussionAsync(NewDiscussionParameters parameters, bool revertOnError);
   }
}

