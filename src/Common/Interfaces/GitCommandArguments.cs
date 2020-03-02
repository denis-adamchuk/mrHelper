using System;
using System.Diagnostics;
using mrHelper.Common.Tools;

namespace mrHelper.Common.Interfaces
{
   public struct GitDiffArguments
   {
      public enum DiffMode
      {
         Context,
         ShortStat,
         NumStat
      }

      public struct CommonArguments
      {
         public string Sha1;
         public string Sha2;
         public string Filename1;
         public string Filename2;
         public string Filter;

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
      }

      public struct DiffContextArguments
      {
         public int Context;

         public override string ToString()
         {
            return String.Format("-U{0}", Context);
         }

         public bool IsValid()
         {
            return Context >= 0;
         }
      }

      public DiffMode Mode;
      public CommonArguments CommonArgs;
      public object SpecialArgs;

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
   }

   public struct GitShowRevisionArguments
   {
      public string Sha;
      public string Filename;

      public override string ToString()
      {
         return String.Format("show {0}:{1}", Sha, StringUtils.EscapeSpaces(Filename));
      }

      public bool IsValid()
      {
         return !String.IsNullOrEmpty(Sha) && !String.IsNullOrEmpty(Filename);
      }
   }
}

