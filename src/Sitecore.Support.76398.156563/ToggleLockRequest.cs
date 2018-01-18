using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Web;
using System;
using Sitecore.Data.Managers;
using Sitecore.Support.ExperienceEditor.Utilites;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.LockItem
{
  public class ToggleLockRequest : PipelineProcessorRequest<ItemContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      base.RequestContext.ValidateContextItem();
      Item item = this.SwitchLock(base.RequestContext.Item);
      this.HandleVersionCreating(item);
      return new PipelineProcessorResponseValue
      {
        Value = new
        {
          Locked = item.Locking.IsLocked(),
          Version = item.Version.Number,
          Revision = item[FieldIDs.Revision]
        }
      };
    }

    protected Item SwitchLock(Item item)
    {
      if (item.Locking.IsLocked())
      {
        item.Locking.Unlock();
        return item;
      }
      if (Sitecore.Context.User.IsAdministrator)
      {
        item.Locking.Lock();
        return item;
      }
      return StartEditing(Context.Workflow, item);
    }

    private void HandleVersionCreating(Item finalItem)
    {
      if (base.RequestContext.Item.Version.Number != finalItem.Version.Number)
      {
        WebUtil.SetCookieValue(base.RequestContext.Site.GetCookieKey("sc_date"), string.Empty, DateTime.MinValue);
      }
    }

    protected virtual Item StartEditing(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      Error.AssertObject(item, "item");
      if (!Settings.RequireLockBeforeEditing || Context.User.IsAdministrator)
      {
        return item;
      }

      var _contextField = workflow.GetType().GetField("_context", IsApprovedHandler.BFlags);
      var _contextValue = (Sitecore.Context.ContextData)_contextField.GetValue(workflow);
      if (_contextValue.IsAdministrator)
      {
        return this.Lock(item);
      }
      if (StandardValuesManager.IsStandardValuesHolder(item))
      {
        return this.Lock(item);
      }
      if (!IsApprovedHandler.HasWorkflow(workflow, item) && !workflow.HasDefaultWorkflow(item))
      {
        return this.Lock(item);
      }
      if (!IsApproved(workflow, item))
      {
        return this.Lock(item);
      }
      Item item2 = item.Versions.AddVersion();
      if (item2 != null)
      {
        return this.Lock(item2);
      }
      return null;
    }

    protected virtual Item Lock(Item item)
    {
      if (TemplateManager.IsFieldPartOfTemplate(FieldIDs.Lock, item) && !item.Locking.Lock())
      {
        return null;
      }
      return item;
    }

    protected virtual bool IsApproved(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      return IsApprovedHandler.Process(workflow, item, null);
    }
  }
}