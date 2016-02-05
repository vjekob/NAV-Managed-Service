using System.Threading;
using System.Threading.Tasks;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process.Workflow;
using ContosoDemoNAV.Tenant.User;
using ContosoDemoNAV.WebService;

namespace ContosoDemoNAV.Process.Provisioners
{
    public class UserProvisioner : WorkflowTaskGroup
    {
        public UserProvisioner()
        {
            Ordinal = 3000000;
            HighLevelTask = true;
            Description = "Provisioning a new user";
        }

        protected override void RequestTasks()
        {
            var service = WebServiceFactory.ApplicationTenantUser();
            RegisterTask(
                new AsyncWorkflowTask("Creating the administrative user account", (state, action) =>
                {
                    state.Get<UserModel>().TenantId = state.Get<TenantModel>().Id;
                    service.CreateCompleted += (sender, args) =>
                    {
                        action.CompleteAsyncOperation(args, () => state.Set(args.ApplicationTenantUser));
                    };
                    service.CreateAsync(state.Get<UserModel>().ToApplicationTenantUser());
                })
                    .Then(
                        new AsyncWorkflowTask("Enabling the user account to access the tenant", (state, action) =>
                        {
                            service.NewCompleted += (sender, args) =>
                            {
                                action.CompleteAsyncOperation(args,
                                    () =>
                                    {
                                        state.Get<UserModel>().Password = args.Result;
                                        state["UserCreated"] = true;
                                    });
                            };
                            service.NewAsync(state.Get<ApplicationTenantUser.ApplicationTenantUser>().Key, true);
                        }).Then(
                            new AsyncWorkflowTask("Activating security system for the tenant", (state, action) =>
                            {
                                Task.Factory.StartNew(() =>
                                {
                                    while (!state["CompanyRenamed"])
                                        Thread.Sleep(1000);

                                    var svcUser = WebServiceFactory.Tenant.User(state);
                                    var user = svcUser.ReadMultiple(null, null, 1)?[0];
                                    if (user != null)
                                    {
                                        user.Permissions = new[] {new User_Line {Permission_Set = "SUPER"}};
                                        svcUser.UpdateCompleted += (sender, args) =>
                                        {
                                            action.CompleteAsyncOperation(args, null);
                                        };
                                        svcUser.UpdateAsync(user);
                                    }
                                });
                            }))));
        }
    }
}