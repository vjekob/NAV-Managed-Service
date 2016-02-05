using System.Collections.Generic;
using ContosoDemoNAV.Process.Provisioners;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Process
{
    public class ProvisioningManager : WorkflowTaskGroup
    {
        protected override void RequestTasks()
        {
            RegisterTask(
                new TenantProvisioner().Then(new List<ITask>() {new CompanyProvisioner(), new UserProvisioner()}));
        }
    }
}