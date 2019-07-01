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
         checkXmlNode(document);
         XmlNode commands = document.SelectSingleNode("Commands");
         checkXmlNode(commands);
         List<int> ids = new List<int>();
         foreach (XmlNode child in commands.ChildNodes)
         {
            XmlNode command = child.SelectSingleNode("Command");
            checkXmlNode(command);
            XmlNode name = command.Attributes.GetNamedItem("Name");
            checkXmlNode(name);
            XmlNode obj = command.FirstChild;
            checkXmlNode(obj);
            if (obj.Name == "SendComment")
            {
               XmlNode comment = obj.Attributes.GetNamedItem("Comment");
               checkXmlNode(comment);
               results.Add(new SendCommentCommand(_callback, name.Value, comment.Value));
            }
         }

         return results;
      }

      private void checkXmlNode(XmlNode node)
      {
         if (node == null)
         {
            throw new ApplicationException("Bad XML");
         }
      }

      ICommandCallback _callback;
   }
}
