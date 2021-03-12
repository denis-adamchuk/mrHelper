using System;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;

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
      public IEnumerable<ICommand> LoadCommands(string filename)
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

      private IEnumerable<ICommand> doLoad(string filename)
      {
         if (!System.IO.File.Exists(filename))
         {
            throw new ArgumentException(String.Format("File is missing \"{0}\"", filename));
         }

         List<ICommand> results = new List<ICommand>();

         XmlDocument document = new XmlDocument();
         document.Load(filename);
         XmlNode xmlNodeCommands = document.SelectSingleNode("Commands");
         foreach (XmlNode child in xmlNodeCommands.ChildNodes)
         {
            XmlNode xmlNodeCommand = child.SelectSingleNode("Command");
            if (xmlNodeCommand == null)
            {
               Trace.TraceInformation(String.Format("Missing \"Command\" node in node {0}, ignoring it", child.Name));
               continue;
            }

            XmlNode xmlNodeName = xmlNodeCommand.Attributes.GetNamedItem("Name");
            if (xmlNodeName == null)
            {
               Trace.TraceInformation(
                 String.Format("Missing \"Name\" attribute in \"Command\" node of node {0}, ignoring this command",
                 child.Name));
               continue;
            }

            List<ISubCommand> subcommands = new List<ISubCommand>();
            foreach (XmlNode obj in xmlNodeCommand.ChildNodes)
            {
               if (obj == null)
               {
                  Trace.TraceInformation(
                    String.Format("No child nodes in \"Command\" node of node {0}, ignoring this command", child.Name));
                  continue;
               }

               if (obj.Name == "SendNote")
               {
                  subcommands.Add(createSendNoteCommand(xmlNodeCommand.Attributes));
               }
               else if (obj.Name == "MergeRequestEndPointPOST")
               {
                  subcommands.Add(createEndPointPOSTCommand(xmlNodeCommand.Attributes));
               }
               else
               {
                  Trace.TraceInformation(
                    String.Format("Unknown action type \"{0}\" in node {1}, ignoring this command", obj.Name, child.Name));
               }
            }

            if (subcommands.Count > 0)
            {
               results.Add(createCompositeCommand(subcommands, xmlNodeCommand.Attributes, xmlNodeName.Value));
            }
         }

         return results;
      }

      private ISubCommand createSendNoteCommand(XmlAttributeCollection attributes)
      {
         XmlNode body = attributes.GetNamedItem("Body");
         return new SendNoteCommand(_callback, body.Value);
      }

      private ISubCommand createEndPointPOSTCommand(XmlAttributeCollection attributes)
      {
         XmlNode endpoint = attributes.GetNamedItem("EndPoint");
         return new MergeRequestEndPointPOSTCommand(_callback, endpoint.Value);
      }

      private ICommand createCompositeCommand(
         IEnumerable<ISubCommand> commands, XmlAttributeCollection attributes, string name)
      {
         XmlNode enabledIf = attributes.GetNamedItem("EnabledIf");
         XmlNode visibleIf = attributes.GetNamedItem("VisibleIf");
         XmlNode stopTimer = attributes.GetNamedItem("StopTimer");
         XmlNode reload = attributes.GetNamedItem("Reload");
         XmlNode hint = attributes.GetNamedItem("Hint");
         XmlNode initiallyVisible = attributes.GetNamedItem("InitiallyVisible");
         return new CompositeCommand(
                           commands,
                           name,
                           enabledIf?.Value ?? String.Empty,
                           visibleIf?.Value ?? String.Empty,
                           (stopTimer?.Value ?? "0") == "1",
                           (reload?.Value ?? "0") == "1",
                           hint?.Value ?? String.Empty,
                           (initiallyVisible?.Value ?? "0") == "1");
      }

      private readonly ICommandCallback _callback;
   }
}

