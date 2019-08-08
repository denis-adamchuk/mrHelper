namespace mrHelper.Common
{
   /// <summary>
   /// Checks for updates
   /// </summary>
   public interface IUpdateChecker
   {
      async public Task<bool> AreAnyUpdatesAsync(DateTime timestamp);
   }
}
