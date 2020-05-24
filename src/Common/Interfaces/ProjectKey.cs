using System;
using System.Collections.Generic;

namespace mrHelper.Common.Interfaces
{
   public struct ProjectKey : IEquatable<ProjectKey>
   {
      public ProjectKey(string hostName, string projectName)
      {
         HostName = hostName;
         ProjectName = projectName;
      }

      public string HostName { get; }
      public string ProjectName { get; }

      public bool MatchProject(string projectname)
      {
         return isEqualProject(projectname);
      }

      public override bool Equals(object obj)
      {
         return obj is ProjectKey key && Equals(key);
      }

      public bool Equals(ProjectKey other)
      {
         return HostName == other.HostName && isEqualProject(other.ProjectName);
      }

      private bool isEqualProject(string projectname)
      {
         return ProjectName == projectname;
      }

      public override int GetHashCode()
      {
         int hashCode = -1910759831;
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(HostName);
         hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ProjectName);
         return hashCode;
      }
   }
}

