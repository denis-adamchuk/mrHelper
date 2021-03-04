using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal struct ColorSchemeItem
   {
      internal ColorSchemeItem(string name, string displayName, IEnumerable<string> conditions, Color color)
      {
         Name = name;
         DisplayName = displayName;
         Conditions = conditions;
         Color = color;
      }

      internal string Name { get; }
      internal string DisplayName { get; }
      internal IEnumerable<string> Conditions { get; }
      internal Color Color { get; }
   }

   /// <summary>
   /// Represents a color scheme used in the application
   /// </summary>
   internal class ColorScheme
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
      /// </summary>
      internal ColorScheme(string filename, ExpressionResolver expressionResolver)
      {
         _expressionResolver = expressionResolver;
         initializeFromFile(filename);
      }

      internal ColorSchemeItem? GetColor(string name)
      {
         return _colors.SelectMany(g => g.Value).FirstOrDefault(i => i.Name == name);
      }

      internal ColorSchemeItem[] GetColors(string groupName)
      {
         if (!_colors.ContainsKey(groupName))
         {
            return Array.Empty<ColorSchemeItem>();
         }
         return _colors[groupName]
            .Select(item =>
            {
               IEnumerable<string> resolvedConditions = item.Conditions
                  .Select(condition => _expressionResolver.Resolve(condition));
               return new ColorSchemeItem(item.Name, item.DisplayName, resolvedConditions, item.Color);
            })
            .ToArray();
      }

      private void setColor(string groupName, string name, string displayName,
         IEnumerable<string> conditions, Color color)
      {
         ColorSchemeItem newItem = new ColorSchemeItem(name, displayName, conditions, color);
         if (!_colors.ContainsKey(groupName))
         {
            _colors.Add(groupName, new List<ColorSchemeItem>());
         }
         _colors[groupName].Add(newItem);
      }

      private void initializeFromFile(string filename)
      {
         if (!System.IO.File.Exists(filename))
         {
            throw new ArgumentException(String.Format("Cannot find file \"{0}\"", filename));
         }

         ColorGroup[] groups = JsonUtils.LoadFromFile<ColorGroup[]>(filename);
         foreach (ColorGroup g in groups)
         {
            foreach (ColorItem i in g.Colors)
            {
               Color? colorOpt = readColorFromText(i.Factory);
               if (colorOpt.HasValue)
               {
                  setColor(g.Group, i.Name, i.DisplayName, i.Conditions, colorOpt.Value);
               }
            }
         }
      }

      private Color? readColorFromText(string text)
      {
         string[] rgbs = text.Split(',');
         if (rgbs.Length == 1)
         {
            try
            {
               return Color.FromName(rgbs[0]);
            }
            catch (ArgumentException)
            {
               return null;
            }
         }
         else if (rgbs.Length == 3
            && int.TryParse(rgbs[0], out int r)
            && int.TryParse(rgbs[1], out int g)
            && int.TryParse(rgbs[2], out int b))
         {
            return Color.FromArgb(r, g, b);
         }
         return null;
      }

      private readonly Dictionary<string, List<ColorSchemeItem>> _colors =
         new Dictionary<string, List<ColorSchemeItem>>();

      internal class ColorItem
      {
         [JsonProperty]
         public string Name { get; protected set; }

         [JsonProperty]
         public string DisplayName { get; protected set; }

         [JsonProperty]
         public IEnumerable<string> Conditions { get; protected set; }

         [JsonProperty]
         public string Factory { get; protected set; }
      }

      private class ColorGroup
      {
         [JsonProperty]
         public string Group { get; protected set; }

         [JsonProperty]
         public IEnumerable<ColorItem> Colors { get; protected set; }
      }

      private readonly ExpressionResolver _expressionResolver;
   }
}

