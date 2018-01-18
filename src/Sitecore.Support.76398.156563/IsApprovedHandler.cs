using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Workflows;
using System;
using System.Reflection;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Utilites
{
  public static class IsApprovedHandler
  {
    public static readonly BindingFlags BFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static bool Process(Sitecore.Workflows.WorkflowContext workflow, Item item, Database targetDatabase)
    {
      Error.AssertObject(item, "item");
      IWorkflow _workflow = GetWorkflow(workflow, item);
      if (_workflow != null)
      {
        return _workflow.IsApproved(item, targetDatabase);
      }
      return true;
    }

    private static IWorkflow GetWorkflow(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      Error.AssertObject(item, "item");
      if (IsWorkflowEnabled(workflow))
      {
        IWorkflowProvider workflowProvider = item.Database.WorkflowProvider;
        if (workflowProvider != null)
        {
          return workflowProvider.GetWorkflow(item);
        }
      }
      return null;
    }

    private static bool IsWorkflowEnabled(Sitecore.Workflows.WorkflowContext workflow)
    {
      switch (Switcher<WorkflowContextState, WorkflowContextState>.CurrentValue)
      {
        case WorkflowContextState.Default:
          {
            var _contextField = workflow.GetType().GetField("_context", BFlags);
            var _contextValue = (Sitecore.Context.ContextData)_contextField.GetValue(workflow);
            return ((_contextValue.Site != null) && (_contextValue.Site.EnableWorkflow || IsReferrerInEditMode()));
          }
        case WorkflowContextState.Disabled:
          return false;
      }
      return true;
    }

    private static bool IsReferrerInEditMode()
    {
      const string editModePattern = "mode=edit";
      const string urlEditModePattern = "sc_mode=edit";
      var referrer = HttpContext.Current.Request.UrlReferrer;
      if (referrer != null && !string.IsNullOrWhiteSpace(referrer.Query))
      {
        string innerQuery = null;
        bool res = IsInQuery(editModePattern, referrer.Query, out innerQuery);
        if (res)
        {
          return res;
        }
        else if (!res && !string.IsNullOrWhiteSpace(innerQuery))
        {
          string innerQueryStub = null;
          return IsInQuery(urlEditModePattern, innerQuery, out innerQueryStub);
        }
      }
      return false;
    }

    private static bool IsInQuery(string param, string query, out string innerQuery)
    {
      innerQuery = null;
      var queryParts = query.Split(new char[] { '?', '&', '/' }, StringSplitOptions.RemoveEmptyEntries);
      string urlParam = string.Empty;
      foreach (string queryPart in queryParts)
      {
        string q = HttpContext.Current.Server.UrlDecode(queryPart);
        if (param.Equals(q, StringComparison.InvariantCultureIgnoreCase))
        {
          return true;
        }
        if (q.StartsWith("url=", StringComparison.InvariantCultureIgnoreCase))
        {
          innerQuery = q;
        }
      }
      return false;
    }

    public static bool HasWorkflow(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      if (!Settings.Workflows.Enabled)
      {
        return false;
      }
      return (GetWorkflow(workflow, item) != null);
    }
  }
}