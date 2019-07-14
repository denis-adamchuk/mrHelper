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
         List<ICommand> results = new List<ICommand>();
         
         XmlDocument document = new XmlDocument();
         document.Load(filename);
         XmlNode commands = document.SelectSingleNode("Commands");
         List<int> ids = new List<int>();
         foreach (XmlNode child in commands.ChildNodes)
         {
            XmlNode command = child.SelectSingleNode("Command");
            XmlNode name = command.Attributes.GetNamedItem("Name");
            XmlNode obj = command.FirstChild;
            if (obj.Name == "SendNote")
            {
               XmlNode body = obj.Attributes.GetNamedItem("Body");
               results.Add(new SendNoteCommand(_callback, name.Value, body.Value));
            }
         }

         return results;
      }

      readonly ICommandCallback _callback;
   }
}
