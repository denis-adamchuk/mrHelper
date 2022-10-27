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
      public KeywordCollection(KeywordCollection collection) => _data = collection._data.ToArray();

      public bool IsExcluded(string text) => containsKeyword(createExcludedKeyword(text));

      public IEnumerable<string> GetExcluded() => getExcluded(includePrefix: false);

      public KeywordCollection CloneWithToggledExclusion(string text)
      {
         string keyword = createExcludedKeyword(text);
         IEnumerable<string> keywords = containsKeyword(keyword) ?
            removeKeyword(_data, keyword) : addKeyword(_data, keyword);
         return new KeywordCollection(keywords);
      }

      public bool IsPinned(string text) => containsKeyword(createPinnedKeyword(text));

      public IEnumerable<string> GetPinned() => getPinned();

      public KeywordCollection CloneWithToggledPinned(string text)
      {
         string keyword = createPinnedKeyword(text);
         IEnumerable<string> keywords = containsKeyword(keyword) ?
            removeKeyword(_data, keyword) : addKeyword(_data, keyword);
         return new KeywordCollection(keywords);
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

      public override string ToString() => String.Join(", ", _data);

      public string[] ToArray() => _data.ToArray();

      public static MergeRequestKey? KeywordToMergeRequestKey(string keyword, string hostname)
      {
         string[] parts = keyword.Split(':').ToArray();
         return new MergeRequestKey(new Common.Interfaces.ProjectKey(hostname, parts[0]), int.Parse(parts[1]));
      }

      public static string KeywordFromMergeRequestKey(MergeRequestKey mergeRequestKey)
      {
         return mergeRequestKey.ProjectKey.ProjectName + ":" + mergeRequestKey.IId.ToString();
      }

      internal bool DoesMatchFilter(FullMergeRequestKey fmk)
      {
         if (!_data.Any() || (_data.Length == 1 && _data[0] == String.Empty))
         {
            return true;
         }

         MergeRequestKey mrk = new MergeRequestKey(fmk.ProjectKey, fmk.MergeRequest.IId);
         if (IsPinned(KeywordFromMergeRequestKey(mrk)))
         {
            return true;
         }

         if (IsExcluded(fmk.MergeRequest.Id.ToString()))
         {
            return false;
         }

         string[] notExcluded = _data.Except(getExcluded(includePrefix: true)).ToArray();
         if (!notExcluded.Any() || (notExcluded.Length == 1 && notExcluded[0] == String.Empty))
         {
            return true;
         }

         foreach (string item in notExcluded)
         {
            if (item.StartsWith(Constants.AuthorLabelPrefix))
            {
               if (fmk.MergeRequest.Author.Username.StartsWith(item.Substring(1),
                     StringComparison.CurrentCultureIgnoreCase))
               {
                  return true;
               }
            }
            else if (item.StartsWith(Constants.GitLabLabelPrefix))
            {
               if (fmk.MergeRequest.Labels.Any(x => x.StartsWith(item,
                     StringComparison.CurrentCultureIgnoreCase)))
               {
                  return true;
               }
            }
            else if (item != String.Empty)
            {
               if (fmk.MergeRequest.IId.ToString() == item
                || StringUtils.ContainsNoCase(fmk.MergeRequest.Author.Username, item)
                || StringUtils.ContainsNoCase(fmk.MergeRequest.Title, item)
                || StringUtils.ContainsNoCase(fmk.MergeRequest.Source_Branch, item)
                || StringUtils.ContainsNoCase(fmk.MergeRequest.Target_Branch, item)
                || fmk.MergeRequest.Labels.Any(x => StringUtils.ContainsNoCase(x, item)))
               {
                  return true;
               }
            }
         }

         return false;
      }

      private KeywordCollection(IEnumerable<string> data) => _data = data.ToArray();

      private IEnumerable<string> getExcluded(bool includePrefix)
      {
         IEnumerable<string> keywords = getKeywordsWithPrefix(Constants.ExcludeLabelPrefix);
         return includePrefix ? keywords : trimPrefix(keywords, Constants.ExcludeLabelPrefix);
      }

      private IEnumerable<string> getPinned()
      {
         IEnumerable<string> keywords = getKeywordsWithPrefix(Constants.PinLabelPrefix);
         return trimPrefix(keywords, Constants.ExcludeLabelPrefix);
      }

      private bool containsKeyword(string keyword) => _data.Any(kw => String.Compare(kw, keyword) == 0);

      private IEnumerable<string> getKeywordsWithPrefix(string prefix) =>
         _data.Where(keyword => keyword.StartsWith(prefix));

      private static IEnumerable<string> trimPrefix(IEnumerable<string> keywords, string prefix) =>
         keywords
            .Select(keyword => keyword.Substring(prefix.Length))
            .Where(keyword => !String.IsNullOrWhiteSpace(keyword));

      private static IEnumerable<string> removeKeyword(IEnumerable<string> keywords, string keyword) =>
         keywords.Where(kw => kw != keyword).ToArray();

      private static IEnumerable<string> addKeyword(IEnumerable<string> keywords, string keyword) =>
         keywords.Append(keyword).ToArray();

      private static string createExcludedKeyword(string text) =>
         String.Format("{0}{1}", Constants.ExcludeLabelPrefix, text);

      private static string createPinnedKeyword(string text) =>
         String.Format("{0}{1}", Constants.PinLabelPrefix, text);

      private readonly string[] _data;
   }

   public enum FilterState
   {
      Disabled,
      Enabled,
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

      public bool DoesMatchFilter(FullMergeRequestKey fmk)
      {
         switch (Filter.State)
         {
            case FilterState.Enabled:
               return Filter.Keywords.DoesMatchFilter(fmk);

            case FilterState.Disabled:
               return true;

            case FilterState.ShowHiddenOnly:
               return Filter.Keywords.IsExcluded(fmk.MergeRequest.Id.ToString());

            default:
               Debug.Assert(false);
               return true;
         }
      }

      private MergeRequestFilterState _filter;
   }
}

