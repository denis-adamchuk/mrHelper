namespace mrHelper.Client
{
   public static class Tools
   {
      private static const string ProjectListFileName = "projects.json";
      private const string CustomActionsFileName = "CustomActions.xml";

      public static List<ICommand> LoadCustomActions()
      {
         CustomCommandLoader loader = new CustomCommandLoader(this);
         try
         {
            return loader.LoadCommands(CustomActionsFileName);
         }
         catch (CustomCommandLoaderException ex)
         {
            // If file doesn't exist the loader throws, leaving the app in an undesirable state.
            // Do not try to load custom actions if they don't exist.
            ExceptionHandlers.Handle(ex, "Cannot load custom actions", false);
         }
         return null;
      }

      public static List<Project> LoadProjectsFromFile()
      {
         // Check if file exists. If it does not, it is not an error.
         if (File.Exists(ProjectListFileName))
         {
            try
            {
               return loadProjectsFromFile(GetCurrentHostName(), ProjectListFileName);
            }
            catch (Exception ex) // whatever de-serialization exception
            {
               ExceptionHandlers.Handle(ex, "Cannot load projects from file");
            }
         }
         return null;
      }
   }
}

