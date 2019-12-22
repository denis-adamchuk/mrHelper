using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using mrHelper.Common.Interfaces;
using mrHelper.Common.Exceptions;

namespace mrHelper.CustomActions
{
   /// <summary>
   /// Loads custom actions from XML file
   /// </summary>
   public class CustomCommandLoader
   {
      public CustomCommandLoader(ICommandCallback callback)
      {
         _callback = callback;
      }

      /// <summary>
      /// Loads custom actions from XML file
      /// Throws CustomCommandLoaderException
      /// </summary>
      public List<ICommand> LoadCommands(string filename)
      {
         try
         {
            return doLoad(filename);
         }
         catch (ArgumentException ex)
         {
            throw new CustomCommandLoaderException("Cannot load commands", ex);
         }
         catch (Exception ex) // whatever XML exception
         {
            throw new CustomCommandLoaderException("Unknown error", ex);
         }
      }

      private List<ICommand> doLoad(string filename)
      {
         if (!System.IO.File.Exists(filename))
         {
            throw new ArgumentException(String.Format("File is missing \"{0}\"", filename));
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
               Trace.TraceInformation(String.Format("Missing \"Command\" node in node {0}, ignoring it", child.Name));
               continue;
            }

            XmlNode name = command.Attributes.GetNamedItem("Name");
            if (name == null)
            {
               Trace.TraceInformation(
                 String.Format("Missing \"Name\" attribute in \"Command\" node of node {0}, ignoring this command",
                 child.Name));
               continue;
            }

            XmlNode obj = command.FirstChild;
            if (obj == null)
            {
               Trace.TraceInformation(
                 String.Format("No child nodes in \"Command\" node of node {0}, ignoring this command", child.Name));
               continue;
            }

            if (obj.Name == "SendNote")
            {
               XmlNode body = obj.Attributes.GetNamedItem("Body");
               XmlNode dependency = obj.Attributes.GetNamedItem("Dependency");
               XmlNode stopTimer = obj.Attributes.GetNamedItem("StopTimer");
               XmlNode reload = obj.Attributes.GetNamedItem("Reload");
               results.Add(new SendNoteCommand(_callback, name.Value, body.Value, dependency?.Value ?? String.Empty,
                  (stopTimer?.Value ?? "0") == "1", (reload?.Value ?? "0") == "1"));
            }
            else
            {
               Trace.TraceInformation(
                 String.Format("Unknown action type \"{0}\" in node {1}, ignoring this command", obj.Name, child.Name));
            }
         }

         return results;
      }

      private readonly ICommandCallback _callback;
   }
}

