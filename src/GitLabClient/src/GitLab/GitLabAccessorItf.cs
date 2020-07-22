namespace mrHelper.Client.Common
{
   public interface IGitLabAccessor
   {
      IGitLabInstanceAccessor GetInstanceAccessor(string hostname);
   }
}

