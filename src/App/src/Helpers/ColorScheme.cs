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
         IEnumerable<string> conditions, Color color, Color factoryColor, bool affectSummary)
      {
         Name = name;
         DisplayName = displayName;
         Conditions = conditions;
         Color = color;
         FactoryColor = factoryColor;
         AffectSummary = affectSummary;
      }

      internal ColorSchemeItem CloneWithDifferentColor(Color color)
      {
         return new ColorSchemeItem(Name, DisplayName, Conditions, color, Color, AffectSummary);
      }

      internal string Name { get; }
      internal string DisplayName { get; }
      internal IEnumerable<string> Conditions { get; }
      internal Color Color { get; }
      internal Color FactoryColor { get; }
      internal bool AffectSummary { get; }
   }

   /// <summary>
   /// Represents a color scheme used in the application
   /// </summary>
   public class ColorScheme
   {
      /// <summary>
      /// Read scheme from file
      /// Throws ArgumentException
      /// </summary>
      internal void LoadFromFile(string filename)
      {
         initializeFromFile(filename);
      }

      internal event Action Changed;

      internal ColorSchemeItem GetColor(string name)
      {
         ColorSchemeItem item = _colors.SelectMany(g => g.Value).FirstOrDefault(i => i.Name == name);
         return item?.CloneWithDifferentColor(getCustomColor(name) ?? item.Color);
      }

      internal ColorSchemeItem[] GetColors(string groupName)
      {
         if (!_colors.ContainsKey(groupName))
         {
            return Array.Empty<ColorSchemeItem>();
         }
         return _colors[groupName]
            .Select(item => item.CloneWithDifferentColor(getCustomColor(item.Name) ?? item.Color))
            .ToArray();
      }

      internal void ResetToDefault()
      {
         Program.Settings.CustomColors = new Dictionary<string, string>();
         Changed?.Invoke();
      }

      internal void SetColor(string name, Color color)
      {
         ColorSchemeItem colorSchemeItem = name != null ? GetColor(name) : null;
         if (colorSchemeItem == null || color.Equals(colorSchemeItem.Color))
         {
            return;
         }

         Dictionary<string, string> dict = Program.Settings.CustomColors;
         if (colorSchemeItem.FactoryColor.Equals(color))
         {
            dict.Remove(colorSchemeItem.Name);
         }
         else
         {
            string colorAsText = color.IsNamedColor
               ? color.Name : String.Format("{0},{1},{2},{3}", color.A, color.R, color.G, color.B);
            dict[colorSchemeItem.Name] = colorAsText;
         }
         Program.Settings.CustomColors = dict;

         Changed?.Invoke();
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
         IEnumerable<string> conditions, Color color, bool affectSummary)
      {
         ColorSchemeItem newItem = new ColorSchemeItem(name, displayName, conditions,
            color, color, affectSummary);
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

         _colors.Clear();
         foreach (ColorGroup g in groups)
         {
            foreach (ColorItem i in g.Colors)
            {
               Color? colorOpt = readColorFromText(i.Factory);
               if (colorOpt.HasValue)
               {
                  initializeColor(g.Group, i.Name, i.Display_Name, i.Conditions, colorOpt.Value, i.AffectSummary);
               }
            }
         }
         Changed?.Invoke();
      }

      private static Color? readColorFromText(string text)
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

         [JsonProperty]
         public bool AffectSummary { get; protected set; }
      }

      private class ColorGroup
      {
         [JsonProperty]
         public string Group { get; protected set; }

         [JsonProperty]
         public IEnumerable<ColorItem> Colors { get; protected set; }
      }
   }
}

