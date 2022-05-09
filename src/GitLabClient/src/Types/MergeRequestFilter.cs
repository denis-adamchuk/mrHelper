using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;

namespace mrHelper.GitLabClient
{
   public struct KeywordCollection
   {
      public KeywordCollection(KeywordCollection collection)
      {
         _data = collection._data.ToArray();
      }

      public bool IsExcluded(string text)
      {
         string exclusionRule = getExclusionRule(text);
         return _data.Any(keyword => String.Compare(keyword, exclusionRule) == 0);
      }

      public IEnumerable<string> GetExcluded()
      {
         return _data
            .Where(keyword => keyword.StartsWith(Constants.ExcludeLabelPrefix))
            .Select(keyword => keyword.Substring(Constants.ExcludeLabelPrefix.Length))
            .Where(keyword => !String.IsNullOrWhiteSpace(keyword));
      }

      public KeywordCollection CloneWithToggledExclusion(string text)
      {
         string exclusionRule = getExclusionRule(text);
         return IsExcluded(text)
            ? new KeywordCollection(_data.Where(rule => rule != exclusionRule).ToArray())
            : new KeywordCollection(_data.Append(exclusionRule).ToArray());
      }

      public static KeywordCollection FromString(string text)
      {
         if (String.IsNullOrWhiteSpace(text))
         {
            return new KeywordCollection(Array.Empty<string>());
         }

         return new KeywordCollection(text
            .Split(',')
            .Select(x => x.Trim(' '))
            .ToArray());
      }

      public override string ToString() 
      {
         return String.Join(", ", _data);
      }

      public string[] ToArray()
      {
         return _data.ToArray();
      }

      internal bool DoesMatchFilter(MergeRequest mergeRequest)
      {
         if (!_data.Any() || (_data.Length == 1 && _data[0] == String.Empty))
         {
            return true;
         }

         if (IsExcluded(mergeRequest.Id.ToString()))
         {
            return false;
         }

         IEnumerable<string> nonExclusions = _data
            .Where(keyword => !keyword.StartsWith(Constants.ExcludeLabelPrefix));
         if (!nonExclusions.Any())
         {
            return true;
         }

         foreach (string item in nonExclusions)
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

      private static string getExclusionRule(string text)
      {
         return String.Format("{0}{1}", Constants.ExcludeLabelPrefix, text);
      }

      private KeywordCollection(string[] data)
      {
         _data = data.ToArray();
      }

      private readonly string[] _data;
   }

   public enum FilterState
   {
      Enabled,
      Disabled,
      ShowHiddenOnly
   }

   public struct MergeRequestFilterState
   {
      public MergeRequestFilterState(string keywords, FilterState state)
      {
         Keywords = KeywordCollection.FromString(keywords);
         State = state;
      }

      public KeywordCollection Keywords { get; }
      public FilterState State { get; }
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
            FilterChanged?.Invoke();
         }
      }

      public event Action FilterChanged;

      public bool DoesMatchFilter(MergeRequest mergeRequest)
      {
         switch (Filter.State)
         {
            case FilterState.Enabled:
               return Filter.Keywords.DoesMatchFilter(mergeRequest);

            case FilterState.Disabled:
               return true;

            case FilterState.ShowHiddenOnly:
               return Filter.Keywords.IsExcluded(mergeRequest.Id.ToString());

            default:
               Debug.Assert(false);
               return true;
         }
      }

      private MergeRequestFilterState _filter;
   }
}

