using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using Newtonsoft.Json;
using mrHelper.Common.Tools;

namespace mrHelper.App.Helpers
{
   internal class ColorSchemeItem
   {
      internal ColorSchemeItem(string name, string displayName, string textName,
         IEnumerable<string> conditions, Color color, Color factoryColor,
         bool useForPreview, bool useForSorting, bool useForMuted, bool useForTextColorInConfig)
      {
         Name = name;
         DisplayName = displayName;
         TextName = textName;
         Conditions = conditions;
         Color = color;
         FactoryColor = factoryColor;
         UseForPreview = useForPreview;
         UseForSorting = useForSorting;
         UseForMuted = useForMuted;
         UseForTextColorInConfig = useForTextColorInConfig;
      }

      internal ColorSchemeItem CloneWithDifferentColor(Color color)
      {
         return new ColorSchemeItem(
            Name, DisplayName, TextName, Conditions, color, Color,
            UseForPreview, UseForSorting, UseForMuted, UseForTextColorInConfig);
      }

      internal string Name { get; }
      internal string DisplayName { get; }
      internal string TextName { get; }
      internal IEnumerable<string> Conditions { get; }
      internal Color Color { get; }
      internal Color FactoryColor { get; }
      internal bool UseForPreview { get; }
      internal bool UseForSorting { get; }
      internal bool UseForMuted { get; }
      internal bool UseForTextColorInConfig { get; }
   }

   /// <summary>
   /// Represents a color scheme used in the application
   /// </summary>
   public static class ColorScheme
   {
      /// <summary>
      /// Read scheme from file
      /// Throws ArgumentException
      /// </summary>
      internal static void Initialize()
      {
         string filename = getColorSchemeFileName();
         initializeFromFile(filename);
      }

      internal static event Action Modified;

      internal static ColorSchemeItem GetColor(string name)
      {
         ColorSchemeItem item = _colors.SelectMany(g => g.Value).FirstOrDefault(i => i.Name == name);
         Debug.Assert(item != null);
         return item?.CloneWithDifferentColor(getCustomColor(name) ?? item.Color);
      }

      internal static ColorSchemeItem[] GetColors(string groupName)
      {
         if (!_colors.ContainsKey(groupName))
         {
            return Array.Empty<ColorSchemeItem>();
         }
         return _colors[groupName]
            .Select(item => item.CloneWithDifferentColor(getCustomColor(item.Name) ?? item.Color))
            .ToArray();
      }

      internal static void ResetToDefault()
      {
         setCustomColors(new Dictionary<string, string>());
         Modified?.Invoke();
      }

      internal static void SetColor(string name, Color color)
      {
         ColorSchemeItem colorSchemeItem = name != null ? GetColor(name) : null;
         if (colorSchemeItem == null || color.Equals(colorSchemeItem.Color))
         {
            return;
         }

         Dictionary<string, string> dict = getCustomColors();
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
         setCustomColors(dict);

         Modified?.Invoke();
      }

      private static Color? getCustomColor(string name)
      {
         if (getCustomColors().TryGetValue(name, out string value))
         {
            Color? color = readColorFromText(value);
            if (color.HasValue)
            {
               return color.Value;
            }
         }
         return null;
      }

      private static Dictionary<string, string> getCustomColors()
      {
         Common.Constants.Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);
         if (colorMode == Common.Constants.Constants.ColorMode.Dark)
         {
            return Program.Settings.CustomColorsDark;
         }
         else
         {
            Debug.Assert(colorMode == Common.Constants.Constants.ColorMode.Light);
            return Program.Settings.CustomColorsLight;
         }
      }

      private static void setCustomColors(Dictionary<string, string> customColors)
      {
         Common.Constants.Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);
         if (colorMode == Common.Constants.Constants.ColorMode.Dark)
         {
            Program.Settings.CustomColorsDark = customColors;
         }
         else
         {
            Debug.Assert(colorMode == Common.Constants.Constants.ColorMode.Light);
            Program.Settings.CustomColorsLight = customColors;
         }
      }

      private static string getColorSchemeFileName()
      {
         Common.Constants.Constants.ColorMode colorMode = ConfigurationHelper.GetColorMode(Program.Settings);
         return Common.Constants.Constants.GetDefaultColorSchemeFileName(colorMode);
      }

      private static void initializeColor(string groupName, string name, string displayName,
         string textName, IEnumerable<string> conditions, Color color,
         bool useForPreview, bool useForSorting, bool useForMuted, bool useForTextColorInConfig)
      {
         ColorSchemeItem newItem = new ColorSchemeItem(name, displayName, textName, conditions,
            color, color, useForPreview, useForSorting, useForMuted, useForTextColorInConfig);
         if (!_colors.ContainsKey(groupName))
         {
            _colors.Add(groupName, new List<ColorSchemeItem>());
         }
         _colors[groupName].Add(newItem);
      }

      private static void initializeFromFile(string filename)
      {
         if (!System.IO.File.Exists(filename))
         {
            throw new ArgumentException(String.Format("Cannot find file \"{0}\"", filename));
         }

         ColorItem[] items;
         try
         {
            items = JsonUtils.LoadFromFile<ColorItem[]>(filename);
         }
         catch (Exception ex) // Any exception from JsonUtils.LoadFromFile()
         {
            System.Diagnostics.Trace.WriteLine(ex);
            throw new ArgumentException(String.Format("Cannot parse file \"{0}\"", filename));
         }

         _colors.Clear();
         foreach (ColorItem i in items)
         {
            Color? colorOpt = readColorFromText(i.Factory);
            if (colorOpt.HasValue)
            {
               initializeColor(i.Group ?? String.Empty, i.Name, i.Display_Name, i.Text_Name,
                  i.Conditions, colorOpt.Value,
                  i.Use_For_Preview, i.Use_For_Sorting, i.Use_For_Muted, i.Use_For_Text_Color_In_Config);
            }
         }
         Modified?.Invoke();
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

      private static readonly Dictionary<string, List<ColorSchemeItem>> _colors =
         new Dictionary<string, List<ColorSchemeItem>>();

      internal class ColorItem
      {
         [JsonProperty]
         public string Group { get; protected set; }

         [JsonProperty]
         public string Name { get; protected set; }

         [JsonProperty]
         public string Display_Name { get; protected set; }

         [JsonProperty]
         public string Text_Name { get; protected set; }

         [JsonProperty]
         public IEnumerable<string> Conditions { get; protected set; }

         [JsonProperty]
         public string Factory { get; protected set; }

         [JsonProperty]
         public bool Use_For_Preview { get; protected set; }

         [JsonProperty]
         public bool Use_For_Sorting { get; protected set; }

         [JsonProperty]
         public bool Use_For_Muted { get; protected set; }

         [JsonProperty]
         public bool Use_For_Text_Color_In_Config { get; protected set; }
      }
   }
}

