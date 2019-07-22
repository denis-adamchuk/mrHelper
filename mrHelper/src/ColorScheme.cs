using System;
using System.Collections.Generic;
using System.Drawing;
using System.Web.Script.Serialization;

namespace mrHelperUI
{
   public class ColorScheme
   {
      public ColorScheme()
      {
         resetToDefault();
      }

      public ColorScheme(string filename)
      {
         resetToDefault();

         if (filename == null || filename.Length == 0 || !System.IO.File.Exists(filename))
         {
            return;
         }

         try
         {
            string json = System.IO.File.ReadAllText(filename);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic colors = serializer.DeserializeObject(json);
            foreach (var record in colors)
            {
               string[] rgbs = record.Value.ToString().Split(',');
               if (rgbs.Length != 3)
               {
                  continue;
               }

               int r = 0, g = 0, b = 0;
               if (!int.TryParse(rgbs[0], out r) || !int.TryParse(rgbs[1], out g) || !int.TryParse(rgbs[2], out b))
               {
                  continue;
               }

               setColor(record.Key, Color.FromArgb(r, g, b));
            }
         }
         catch (Exception)
         {
            // Bad JSON
            throw new ApplicationException("Unexpected format or color scheme file");
         }
      }

      public Color GetColor(string name)
      {
         if (!_colors.ContainsKey(name))
         {
            return System.Drawing.Color.Black;
         }

         return _colors[name];
      }

      private void resetToDefault()
      {
         setColor("Discussions_Comments", Color.FromArgb(202,199,243));
         setColor("Discussions_Author_Notes_Resolved", Color.FromArgb(192,235,230));
         setColor("Discussions_NonAuthor_Notes_Resolved", Color.FromArgb(247,249,202));
         setColor("Discussions_Author_Notes_Unresolved", Color.FromArgb(148,218,207));
         setColor("Discussions_NonAuthor_Notes_Unresolved", Color.FromArgb(233,210,122));
      }

      private void setColor(string name, Color color)
      {
         _colors[name] = color;
      }

      private Dictionary<string, Color> _colors = new Dictionary<string, Color>();
   }
}

