using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal class ColorSchemeItem
   {
      internal ColorSchemeItem(string name, string displayName,
         IEnumerable<string> conditions, Color color, Color factoryColor)
      {
         Name = name;
         DisplayName = displayName;
         Conditions = conditions;
         Color = color;
         FactoryColor = factoryColor;
      }

      internal string Name { get; }
      internal string DisplayName { get; }
      internal IEnumerable<string> Conditions { get; }
      internal Color Color { get; }
      internal Color FactoryColor { get; }
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

      internal ColorSchemeItem GetColor(string name)
      {
         var item = _colors.SelectMany(g => g.Value).FirstOrDefault(i => i.Name == name);
         if (item == null)
         {
            return null;
         }

         Color color = getCustomColor(name) ?? item.Color;
         return new ColorSchemeItem(item.Name, item.DisplayName, item.Conditions, color, item.Color);
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
               IEnumerable<string> resolvedConditions = item.Conditions?
                  .Select(condition => _expressionResolver.Resolve(condition)) ?? Array.Empty<string>();
               Color color = getCustomColor(item.Name) ?? item.Color;
               return new ColorSchemeItem(item.Name, item.DisplayName, resolvedConditions, color, item.Color);
            })
            .ToArray();
      }

      private Color? getCustomColor(string name)
      {
         if (Program.Settings.CustomColors.TryGetValue(name, out string value))
         {
            Color? color = readColorFromText(value);
            if (color.HasValue)
            {
               return color.Value;
            }
         }
         return null;
      }

      private void initializeColor(string groupName, string name, string displayName,
         IEnumerable<string> conditions, Color color)
      {
         ColorSchemeItem newItem = new ColorSchemeItem(name, displayName, conditions, color, color);
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

         ColorGroup[] groups;
         try
         {
            groups = JsonUtils.LoadFromFile<ColorGroup[]>(filename);
         }
         catch (Exception) // Any exception from JsonUtils.LoadFromFile()
         {
            throw new ArgumentException(String.Format("Cannot parse file \"{0}\"", filename));
         }

         foreach (ColorGroup g in groups)
         {
            foreach (ColorItem i in g.Colors)
            {
               Color? colorOpt = readColorFromText(i.Factory);
               if (colorOpt.HasValue)
               {
                  initializeColor(g.Group, i.Name, i.Display_Name, i.Conditions, colorOpt.Value);
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
         else if (rgbs.Length == 4
            && int.TryParse(rgbs[0], out int aa)
            && int.TryParse(rgbs[1], out int rr)
            && int.TryParse(rgbs[2], out int gg)
            && int.TryParse(rgbs[3], out int bb))
         {
            return Color.FromArgb(aa, rr, gg, bb);
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
         public string Display_Name { get; protected set; }

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

