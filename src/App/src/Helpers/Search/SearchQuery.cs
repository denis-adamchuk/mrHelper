using System;
using System.Collections.Generic;

namespace mrHelper.App.Helpers
{
   public struct SearchQuery : IEquatable<SearchQuery>
   {
      public SearchQuery(string text, bool caseSensitive)
      {
         Text = text;
         CaseSensitive = caseSensitive;
      }

      public string Text { get; }
      public bool CaseSensitive { get; }

      public override bool Equals(object obj)
      {
         return obj is SearchQuery query && Equals(query);
      }

      public bool Equals(SearchQuery other)
      {
         return Text == other.Text &&
                CaseSensitive == other.CaseSensitive;
      }

      public override int GetHashCode()
      {
         int hashCode = -102066407;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text);
         hashCode = hashCode * -1521134295 + CaseSensitive.GetHashCode();
         return hashCode;
      }
   }
}

