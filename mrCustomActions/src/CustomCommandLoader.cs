using System.Collections.Generic;
using System.Xml;

namespace mrCustomActions
{
   public class CustomCommandLoader
   {
      public CustomCommandLoader(ICommandCallback callback)
      {
         _callback = callback;
      }

      public List<ICommand> LoadCommands(string filename)
      {
         if (!File.Exists(CustomActionsFileName))
         {
            throw new ArgumentException(String.Format("Cannot find file \"{0}\"", filename));
         }

         List<ICommand> results = new List<ICommand>();

         XmlDocument document = new XmlDocument();
         document.Load(filename);
         XmlNode commands = document.SelectSingleNode("Commands");
         foreach (XmlNode child in commands.ChildNodes)
         {
            XmlNode command = child.SelectSingleNode("Command");
            if (command == null)
            {
               // TODO Log warning
               continue;
            }

            XmlNode name = command.Attributes.GetNamedItem("Name");
            if (name == null)
            {
               // TODO Log warning
               continue;
            }

            XmlNode obj = command.FirstChild;
            if (obj == null)
            {
               // TODO Log warning
               continue;
            }

            if (obj.Name == "SendNote")
            {
               XmlNode body = obj.Attributes.GetNamedItem("Body");
               results.Add(new SendNoteCommand(_callback, name.Value, body.Value));
            }
            else
            {
               // TODO Log warning
            }
         }

         return results;
      }

      private readonly ICommandCallback _callback;
   }
}

