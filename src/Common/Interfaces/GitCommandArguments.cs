using System;
using System.Collections.Generic;
using System.Diagnostics;
using mrHelper.Common.Tools;

namespace mrHelper.Common.Interfaces
{
   public struct GitDiffArguments : IEquatable<GitDiffArguments>
   {
      public GitDiffArguments(DiffMode mode, CommonArguments commonArgs, object specialArgs)
      {
         Mode = mode;
         CommonArgs = commonArgs;
         SpecialArgs = specialArgs;
      }

      public enum DiffMode
      {
         Context,
         ShortStat,
         NumStat
      }

      public struct CommonArguments : IEquatable<CommonArguments>
      {
         public CommonArguments(string sha1, string sha2, string filename1, string filename2, string filter)
         {
            Sha1 = sha1;
            Sha2 = sha2;
            Filename1 = filename1;
            Filename2 = filename2;
            Filter = filter;
         }

         public string Sha1 { get; }
         public string Sha2 { get; }
         public string Filename1 { get; }
         public string Filename2 { get; }
         public string Filter { get; }

         public override string ToString()
         {
            string mergeTwoStrings(string string1, string string2)
            {
               if (!String.IsNullOrEmpty(string1) && !String.IsNullOrEmpty(string2))
               {
                  return StringUtils.EscapeSpaces(string1) + " " + StringUtils.EscapeSpaces(string2);
               }
               else if (!String.IsNullOrEmpty(string1))
               {
                  return StringUtils.EscapeSpaces(string1);
               }
               else if (!String.IsNullOrEmpty(string2))
               {
                  return StringUtils.EscapeSpaces(string2);
               }
               return String.Empty;
            }

            string diffFilterArg = String.IsNullOrEmpty(Filter)
               ? String.Empty
               : String.Format("--diff-filter={0}", Filter);
            return String.Format("{0} {1} -- {2}",
               diffFilterArg, mergeTwoStrings(Sha1, Sha2), mergeTwoStrings(Filename1, Filename2));
         }

         public bool IsValid()
         {
            return String.IsNullOrEmpty(Filter) || Filter == "R" || Filter == "M" || Filter == "A";
         }

         public override bool Equals(object obj)
         {
            return obj is CommonArguments arguments && Equals(arguments);
         }

         public bool Equals(CommonArguments other)
         {
            return Sha1 == other.Sha1 &&
                   Sha2 == other.Sha2 &&
                   Filename1 == other.Filename1 &&
                   Filename2 == other.Filename2 &&
                   Filter == other.Filter;
         }

         public override int GetHashCode()
         {
            int hashCode = 238868665;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Sha1);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Sha2);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename1);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename2);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filter);
            return hashCode;
         }
      }

      public struct DiffContextArguments : IEquatable<DiffContextArguments>
      {
         public int Context { get; }

         public DiffContextArguments(int context)
         {
            Context = context;
         }

         public override string ToString()
         {
            return String.Format("-U{0}", Context);
         }

         public bool IsValid()
         {
            return Context >= 0;
         }

         public override bool Equals(object obj)
         {
            return obj is DiffContextArguments arguments && Equals(arguments);
         }

         public bool Equals(DiffContextArguments other)
         {
            return Context == other.Context;
         }

         public override int GetHashCode()
         {
            return -59922564 + Context.GetHashCode();
         }
      }

      public DiffMode Mode { get; }
      public CommonArguments CommonArgs { get; }
      public object SpecialArgs { get; }

      public override string ToString()
      {
         switch (Mode)
         {
            case DiffMode.Context:
               Debug.Assert(SpecialArgs is DiffContextArguments);
               DiffContextArguments args = (DiffContextArguments)SpecialArgs;
               return String.Format("diff {0} {1}", args.ToString(), CommonArgs.ToString());

            case DiffMode.ShortStat:
               return String.Format("diff --shortstat {0}", CommonArgs.ToString());

            case DiffMode.NumStat:
               return String.Format("diff --numstat {0}", CommonArgs.ToString());
         }

         Debug.Assert(false);
         return String.Empty;
      }

      public bool IsValid()
      {
         if (!CommonArgs.IsValid())
         {
            return false;
         }

         if (Mode == DiffMode.Context)
         {
            Debug.Assert(SpecialArgs is DiffContextArguments);
            return ((DiffContextArguments)SpecialArgs).IsValid();
         }
         else
         {
            Debug.Assert(SpecialArgs == null);
         }

         return true;
      }

      public override bool Equals(object obj)
      {
         return obj is GitDiffArguments arguments && Equals(arguments);
      }

      public bool Equals(GitDiffArguments other)
      {
         return Mode == other.Mode &&
                EqualityComparer<CommonArguments>.Default.Equals(CommonArgs, other.CommonArgs) &&
                EqualityComparer<object>.Default.Equals(SpecialArgs, other.SpecialArgs);
      }

      public override int GetHashCode()
      {
         int hashCode = -2010611888;
         hashCode = hashCode * -1521134295 + Mode.GetHashCode();
         hashCode = hashCode * -1521134295 + CommonArgs.GetHashCode();
         hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(SpecialArgs);
         return hashCode;
      }
   }

   public struct GitShowRevisionArguments : IEquatable<GitShowRevisionArguments>
   {
      public GitShowRevisionArguments(string filename, string sha)
      {
         Filename = filename;
         Sha = sha;
      }

      public string Filename { get; }
      public string Sha { get; }

      public override string ToString()
      {
         return String.Format("show {0}:{1}", Sha, StringUtils.EscapeSpaces(Filename));
      }

      public bool IsValid()
      {
         return !String.IsNullOrEmpty(Sha) && !String.IsNullOrEmpty(Filename);
      }

      public override bool Equals(object obj)
      {
         return obj is GitShowRevisionArguments arguments && Equals(arguments);
      }

      public bool Equals(GitShowRevisionArguments other)
      {
         return Filename == other.Filename &&
                Sha == other.Sha;
      }

      public override int GetHashCode()
      {
         int hashCode = -1469301067;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Filename);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Sha);
         return hashCode;
      }
   }
}

