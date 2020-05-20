using System;
using System.Collections.Generic;

namespace mrHelper.Client.Types
{
   public class SearchCriteria
   {
      public SearchCriteria(IEnumerable<object> criteria)
      {
         Criteria = criteria;
      }

      public IEnumerable<object> Criteria;

      public new string ToString()
      {
         return String.Join(";", Criteria.ToString());
      }
   }

   public class SearchByProject
   {
      public SearchByProject(string projectName)
      {
         ProjectName = projectName;
      }

      public string ProjectName { get; }

      public override string ToString()
      {
         return String.Format("Project: {0}", ProjectName);
      }
   }

   public class SearchByIId
   {
      public SearchByIId(string projectName, int iId)
      {
         ProjectName = projectName;
         IId = iId;
      }

      public string ProjectName { get; }
      public int IId { get; }

      public override string ToString()
      {
         return String.Format("Project: {0}, IId: {1}", ProjectName, IId);
      }
   }

   public class SearchByTargetBranch
   {
      public SearchByTargetBranch(string targetBranchName)
      {
         TargetBranchName = targetBranchName;
      }

      public string TargetBranchName { get; }

      public override string ToString()
      {
         return String.Format("TargetBranch: {0}", TargetBranchName);
      }
   }

   public class SearchByText
   {
      public SearchByText(string text)
      {
         Text = text;
      }

      public string Text { get; }

      public override string ToString()
      {
         return String.Format("Text: {0}", Text);
      }
   }

   public class SearchByUsername
   {
      public SearchByUsername(string username)
      {
         Username = username;
      }

      public string Username { get; }

      public override string ToString()
      {
         return String.Format("Username: {0}", Username);
      }
   }
}

