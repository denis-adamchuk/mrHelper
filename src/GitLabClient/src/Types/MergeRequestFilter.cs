using System;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using System.Collections.Generic;

namespace mrHelper.GitLabClient
{
   public struct MergeRequestFilterState : IEquatable<MergeRequestFilterState>
   {
      public MergeRequestFilterState(string[] keywords, bool enabled)
      {
         Keywords = keywords;
         Enabled = enabled;
      }

      public string[] Keywords { get; }
      public bool Enabled { get; }

      public override bool Equals(object obj)
      {
         return obj is MergeRequestFilterState state && Equals(state);
      }

      public bool Equals(MergeRequestFilterState other)
      {
         return EqualityComparer<string[]>.Default.Equals(Keywords, other.Keywords) &&
                Enabled == other.Enabled;
      }

      public override int GetHashCode()
      {
         int hashCode = 1563885307;
         hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(Keywords);
         hashCode = hashCode * -1521134295 + Enabled.GetHashCode();
         return hashCode;
      }
   }

   public class MergeRequestFilter : IMergeRequestFilterChecker
   {
      public MergeRequestFilter(MergeRequestFilterState initialState)
      {
         Filter = initialState;
      }

      public MergeRequestFilterState Filter
      {
         get
         {
            return _filter;
         }
         set
         {
            MergeRequestFilterState oldFilter = _filter;
            _filter = value;

            bool needUpdate =
                  oldFilter.Enabled != _filter.Enabled
               || oldFilter.Enabled && _filter.Enabled && !oldFilter.Keywords.SequenceEqual(_filter.Keywords);

            if (needUpdate)
            {
               FilterChanged?.Invoke();
            }
         }
      }

      public event Action FilterChanged;

      public bool DoesMatchFilter(MergeRequest mergeRequest)
      {
         if (!Filter.Enabled)
         {
            return true;
         }

         if (Filter.Keywords == null || (Filter.Keywords.Length == 1 && Filter.Keywords[0] == String.Empty))
         {
            return true;
         }

         foreach (string item in Filter.Keywords)
         {
            if (item.StartsWith(Constants.AuthorLabelPrefix))
            {
               if (mergeRequest.Author.Username.StartsWith(item.Substring(1),
                     StringComparison.CurrentCultureIgnoreCase))
               {
                  return true;
               }
            }
            else if (item.StartsWith(Constants.GitLabLabelPrefix))
            {
               if (mergeRequest.Labels.Any(x => x.StartsWith(item,
                     StringComparison.CurrentCultureIgnoreCase)))
               {
                  return true;
               }
            }
            else if (item != String.Empty)
            {
               if (mergeRequest.IId.ToString() == item
                || StringUtils.ContainsNoCase(mergeRequest.Author.Username, item)
                || StringUtils.ContainsNoCase(mergeRequest.Title, item)
                || StringUtils.ContainsNoCase(mergeRequest.Source_Branch, item)
                || StringUtils.ContainsNoCase(mergeRequest.Target_Branch, item)
                || mergeRequest.Labels.Any(x => StringUtils.ContainsNoCase(x, item)))
               {
                  return true;
               }
            }
         }

         return false;
      }

      private MergeRequestFilterState _filter;
   }
}

