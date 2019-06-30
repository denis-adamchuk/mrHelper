using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace mrHelper
{
   class CustomCommandLoader
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
            if (obj.Name == "SendComment")
            {
               XmlNode comment = obj.Attributes.GetNamedItem("Comment");
               results.Add(new SendCommentCommand(_callback, name.Value, comment.Value));
            }
         }

         return results;
      }

      ICommandCallback _callback;
   }
}
