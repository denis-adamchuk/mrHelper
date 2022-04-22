using System;
using System.Linq;
using GitLabSharp.Entities;
using mrHelper.Common.Tools;
using mrHelper.Common.Constants;
using System.Collections.Generic;

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

      public KeywordCollection AddToExclusions(string text)
      {
         string exclusionRule = getExclusionRule(text);
         return new KeywordCollection(_data.Append(exclusionRule).ToArray() ?? new string[] { exclusionRule });
      }

      public KeywordCollection RemoveFromExclusions(string text)
      {
         string exclusionRule = getExclusionRule(text);
         return new KeywordCollection(_data.Where(rule => rule != exclusionRule).ToArray());
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
         if (!_data.Any())
         {
            return true;
         }

         if (_data.Length == 1 && _data[0] == String.Empty)
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

   public struct MergeRequestFilterState : IEquatable<MergeRequestFilterState>
   {
      public MergeRequestFilterState(KeywordCollection keywords, bool enabled)
      {
         Keywords = new KeywordCollection(keywords);
         Enabled = enabled;
      }

      public KeywordCollection Keywords { get; }
      public bool Enabled { get; }

      public override bool Equals(object obj)
      {
         throw new NotImplementedException();
      }

      public bool Equals(MergeRequestFilterState other)
      {
         throw new NotImplementedException();
      }

      public override int GetHashCode()
      {
         throw new NotImplementedException();
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
            FilterChanged?.Invoke();
         }
      }

      public event Action FilterChanged;

      public bool DoesMatchFilter(MergeRequest mergeRequest)
      {
         return !Filter.Enabled || Filter.Keywords.DoesMatchFilter(mergeRequest);
      }

      private MergeRequestFilterState _filter;
   }
}

