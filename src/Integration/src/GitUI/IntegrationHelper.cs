using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace mrHelper.Integration.GitUI
{
   internal static class IntegrationHelper
   {
      internal static void DeleteElements(XDocument document, string tag, string nameTag, IEnumerable<string> names)
      {
         document.Descendants(tag)
                 .Where(x => x.Element(nameTag) != null && names.Contains(x.Element(nameTag).Value))
                 .Remove();
      }

      internal static bool AddElements(XDocument document, string arrayTag, IEnumerable<XElement> elements)
      {
         XElement arrayOfCustomActionElement = document.Element(arrayTag);
         if (arrayOfCustomActionElement == null)
         {
            return false;
         }
         arrayOfCustomActionElement.Add(elements);
         return true;
      }
   }
}

