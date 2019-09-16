using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Web.Script.Serialization;
using mrHelper.Client.Tools;

namespace mrHelper.App.Helpers
{
   /// <summary>
   /// Represents a color scheme used in the application
   /// </summary>
   internal class ColorScheme : IEnumerable<KeyValuePair<string, Color>>
   {
      /// <summary>
      /// Create an empty scheme
      /// </summary>
      internal ColorScheme()
      {
      }

      /// <summary>
      /// Read scheme from file
      /// Throws ArgumentException
      /// Throws ArgumentNullException
      /// Throws InvalidOperationException
      /// </summary>
      internal ColorScheme(string filename, ExpressionResolver expressionResolver)
      {
         ExpressionResolver = expressionResolver;

         if (!System.IO.File.Exists(filename))
         {
            throw new ArgumentException(String.Format("Cannot find file \"{0}\"", filename));
         }

         string json = System.IO.File.ReadAllText(filename);

         JavaScriptSerializer serializer = new JavaScriptSerializer();

         Dictionary<string, object> colors;
         try
         {
            colors = (Dictionary<string, object>)serializer.DeserializeObject(json);
         }
         catch (Exception) // whatever JSON exception
         {
            throw;
         }

         foreach (var record in colors)
         {
            string[] rgbs = record.Value.ToString().Split(',');
            if (rgbs.Length != 3)
            {
               continue;
            }

            if (!int.TryParse(rgbs[0], out int r)
             || !int.TryParse(rgbs[1], out int g)
             || !int.TryParse(rgbs[2], out int b))
            {
               continue;
            }

            setColor(record.Key, Color.FromArgb(r, g, b));
         }
      }

      internal Color GetColorOrDefault(string name, Color defaultColor)
      {
         return findColor<Color>(name, (x) => x, () => defaultColor);
      }

      internal bool HasColor(string name)
      {
         return findColor<bool>(name, (x) => true, () => false);
      }

      public IEnumerator<KeyValuePair<string, Color>> GetEnumerator()
      {
         foreach (KeyValuePair<string, Color> color in _colors)
         {
            string resolvedKey = ExpressionResolver.Resolve(color.Key);
            KeyValuePair<string, Color> keyValuePair =
               new KeyValuePair<string, Color>(resolvedKey, color.Value);
            yield return keyValuePair;
         }
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return GetEnumerator();
      }

      private T findColor<T>(string name, Func<Color, T> found, Func<T> notFound)
      {
         foreach (KeyValuePair<string, Color> color in _colors)
         {
            string resolvedKey = ExpressionResolver.Resolve(color.Key);
            if (name == resolvedKey)
            {
               return found(color.Value);
            }
         }

         return notFound();
      }

      private void setColor(string name, Color color)
      {
         _colors[name] = color;
      }

      private readonly Dictionary<string, Color> _colors = new Dictionary<string, Color>();

      private ExpressionResolver ExpressionResolver { get; }
   }
}

