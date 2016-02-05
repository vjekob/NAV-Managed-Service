using System;
using System.Threading;
using System.Threading.Tasks;
using ContosoDemoNAV.ApplicationTenant;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process.Workflow;
using ContosoDemoNAV.WebService;

namespace ContosoDemoNAV.Process.Provisioners
{
    [Serializable]
    public class TenantProvisioner : WorkflowTaskGroup
    {
        private readonly Operation.Operation _operation = WebServiceFactory.Operation();
        private string _operationId;

        public TenantProvisioner()
        {
            Ordinal = 1000000;
            HighLevelTask = true;
            Description = "Provisioning a new tenant";
        }

        protected override void RequestTasks()
        {
            var service = WebServiceFactory.ApplicationTenant();

            RegisterTask(
                new AsyncWorkflowTask("Creating a new tenant", (state, task) =>
                {
                    service.CreateCompleted += (sender, args) =>
                    {
                        task.CompleteAsyncOperation(args, () => state.Set(args.ApplicationTenant));
                    };
                    service.CreateAsync(state.Get<TenantModel>().ToApplicationTenant());
                })
                    .Then(
                        new AsyncWorkflowTask("Starting provisioning a new Microsoft Dynamics NAV tenant",
                            (state, task) =>
                            {
                                service.BeginProvisionCompleted += (sender, args) =>
                                {
                                    task.CompleteAsyncOperation(args, () => _operationId = args.Result);
                                };
                                service.BeginProvisionAsync(state.Get<ApplicationTenant.ApplicationTenant>().Key);
                            })
                            .Then(
                                new AsyncWorkflowTask(
                                    "Waiting for provisioning a new Microsoft Dynamics NAV tenant to complete",
                                    (state, task) =>
                                    {
                                        Task.Factory.StartNew(() =>
                                        {
                                            var status = @"provisioning";
                                            var errorCount = 0;
                                            while (status == "provisioning")
                                            {
                                                try
                                                {
                                                    status = _operation.GetOperationStatus(_operationId).ToLower();
                                                    Thread.Sleep(5000);
                                                }
                                                catch (Exception e)
                                                {
                                                    if (errorCount++ > 5)
                                                    {
                                                        task.Fail(e);
                                                        return;
                                                    }
                                                }
                                            }
                                            task.Complete();
                                        });
                                    })
                                    .Then(
                                        new AsyncWorkflowTask("Retrieving provisioned tenant information",
                                            (state, task) =>
                                            {
                                                service.ReadCompleted += (sender, args) =>
                                                {
                                                    task.CompleteAsyncOperation(args, () =>
                                                    {
                                                        state.Set(args.Result);
                                                        state.Get<TenantModel>().Id = args.Result.ID;
                                                        state.Get<TenantModel>().Url = args.Result.URL;
                                                        switch (args.Result.Provisioning_Status)
                                                        {
                                                            case Provisioning_Status.Active:
                                                                task.Complete();
                                                                break;
                                                            case Provisioning_Status.Provisioning_Failed:
                                                                task.Fail(
                                                                    new Exception(
                                                                        $"Provisioning of tenant {args.Result.Name} failed."));
                                                                break;
                                                        }
                                                    });
                                                };
                                                service.ReadAsync(state.Get<ApplicationTenant.ApplicationTenant>().ID);
                                            })
                                    ))));
        }
    }
}