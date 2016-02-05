using System.Linq;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process.Workflow;
using ContosoDemoNAV.Tenant.User;
using ContosoDemoNAV.WebService;
using Newtonsoft.Json;

namespace ContosoDemoNAV.Process.Provisioners
{
    public class NewUserProvisioner : WorkflowTaskGroup
    {
        protected override void RequestTasks()
        {
            var service = WebServiceFactory.ApplicationTenantUser();
            RegisterTask(
                new AsyncWorkflowTask("Creating the user account", (state, action) =>
                {
                    state.Get<NewUserModel>().User.TenantId = state.Get<TenantModel>().Id;
                    service.CreateCompleted += (sender, args) =>
                    {
                        action.CompleteAsyncOperation(args, () => state.Set(args.ApplicationTenantUser));
                    };
                    service.CreateAsync(state.Get<NewUserModel>().User.ToApplicationTenantUser());
                })
                    .Then(
                        new AsyncWorkflowTask("Enabling the user account to access the tenant", (state, action) =>
                        {
                            service.NewCompleted += (sender, args) =>
                            {
                                action.CompleteAsyncOperation(args, null);
                            };
                            service.NewAsync(state.Get<ApplicationTenantUser.ApplicationTenantUser>().Key, true);
                        }).Then(
                            new AsyncWorkflowTask("Setting user permission sets", (state, action) =>
                            {
                                var userSvc = WebServiceFactory.Tenant.User(state);
                                var newUser = state.Get<NewUserModel>();
                                var user = userSvc.ReadMultiple(
                                    new[]
                                    {
                                        new User_Filter
                                        {
                                            Field = User_Fields.User_Name,
                                            Criteria = state.Get<NewUserModel>().User.UserName
                                        }
                                    }, null, 1)[0];
                                user.Permissions =
                                    JsonConvert.DeserializeObject<string[]>(
                                        newUser.SelectedPermissionSets).Select(p => new User_Line
                                        {
                                            Company = newUser.User.Company,
                                            Permission_Set = p
                                        }).ToArray();
                                userSvc.Update(ref user);
                                action.Complete();
                            }))));
        }
    }
}