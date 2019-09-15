using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mrHelper.Client.Tools
{
   public class ExpressionResolver
   {
      public ExpressionResolver(Workflow.Workflow workflow)
      {
         Workflow = workflow;
      }

      public string Resolve(string expression)
      {
         return expression.Replace("%CurrentUsername%", Workflow.State.CurrentUser.Username);
      }

      private Workflow.Workflow Workflow { get; }
   }
}

