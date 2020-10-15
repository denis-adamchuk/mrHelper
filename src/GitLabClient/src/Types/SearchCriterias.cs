﻿using System;
using System.Collections.Generic;
using mrHelper.Common.Interfaces;

namespace mrHelper.GitLabClient
{
   public class SearchCriteria
   {
      public SearchCriteria(IEnumerable<object> criteria, bool onlyOpen)
      {
         Criteria = criteria;
         OnlyOpen = onlyOpen;
      }

      public IEnumerable<object> Criteria;
      public bool OnlyOpen;

      public new string ToString()
      {
         return String.Join(";", Criteria) + "_OnlyOpen=" + OnlyOpen.ToString();
      }

      public override bool Equals(object obj)
      {
         return obj is SearchCriteria criteria &&
                EqualityComparer<IEnumerable<object>>.Default.Equals(Criteria, criteria.Criteria) &&
                OnlyOpen == criteria.OnlyOpen;
      }

      public override int GetHashCode()
      {
         int hashCode = -854807273;
         hashCode = hashCode * -1521134295 + EqualityComparer<IEnumerable<object>>.Default.GetHashCode(Criteria);
         hashCode = hashCode * -1521134295 + OnlyOpen.GetHashCode();
         return hashCode;
      }
   }

   public class SearchByProject
   {
      public SearchByProject(ProjectKey projectKey)
      {
         ProjectKey = projectKey;
      }

      public ProjectKey ProjectKey { get; }

      public override bool Equals(object obj)
      {
         var project = obj as SearchByProject;
         return project != null &&
                ProjectKey.Equals(project.ProjectKey);
      }

      public override int GetHashCode()
      {
         return -108071763 + EqualityComparer<ProjectKey>.Default.GetHashCode(ProjectKey);
      }

      public override string ToString()
      {
         return String.Format("Project: {0}, Host: {1}", ProjectKey.ProjectName, ProjectKey.HostName);
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

      public override bool Equals(object obj)
      {
         return obj is SearchByIId id &&
                ProjectName == id.ProjectName &&
                IId == id.IId;
      }

      public override int GetHashCode()
      {
         int hashCode = 69353006;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProjectName);
         hashCode = hashCode * -1521134295 + IId.GetHashCode();
         return hashCode;
      }

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

      public override bool Equals(object obj)
      {
         return obj is SearchByTargetBranch branch &&
                TargetBranchName == branch.TargetBranchName;
      }

      public override int GetHashCode()
      {
         return -519884205 + EqualityComparer<string>.Default.GetHashCode(TargetBranchName);
      }

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

      public override bool Equals(object obj)
      {
         return obj is SearchByText text &&
                Text == text.Text;
      }

      public override int GetHashCode()
      {
         return 1249999374 + EqualityComparer<string>.Default.GetHashCode(Text);
      }

      public override string ToString()
      {
         return String.Format("Text: \"{0}\"", Text);
      }
   }

   public class SearchByUsername
   {
      public SearchByUsername(string username)
      {
         Username = username;
      }

      public string Username { get; }

      public override bool Equals(object obj)
      {
         return obj is SearchByUsername username &&
                Username == username.Username;
      }

      public override int GetHashCode()
      {
         return -182246463 + EqualityComparer<string>.Default.GetHashCode(Username);
      }

      public override string ToString()
      {
         return String.Format("Username: {0}", Username);
      }
   }
}

