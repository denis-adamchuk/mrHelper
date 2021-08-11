namespace mrHelper.App.Helpers
{
   internal struct CommandState
   {
      public CommandState(bool enabled, bool visible)
      {
         Enabled = enabled;
         Visible = visible;
      }

      internal bool Enabled { get; }
      internal bool Visible { get; }
   }
}

