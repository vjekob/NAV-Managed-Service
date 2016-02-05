using System.Collections.Generic;
using ContosoDemoNAV.Process;
using ContosoDemoNAV.WebService;
using System.Linq;
using System.Web.Mvc;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process.Provisioners;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Controllers
{
    public class UsersController : Controller
    {
        private void _cheat_in_place_of_sign_in()
        {
            if (State.Get<TenantModel>() == null)
            {
                var svc = WebServiceFactory.ApplicationTenant();
                State.Set(svc.ReadMultiple(new[]
                {
                    new ApplicationTenant.ApplicationTenant_Filter
                    {
                        Field = ApplicationTenant.ApplicationTenant_Fields.Name,
                        Criteria = "Gesellschaft GmbH"
                    }
                }, null, 0).FirstOrDefault()?.ToTenantModel());
                State.Set(new UserModel
                {
                    UserName = "ADMIN",
                    Password = "Ad.12345!"
                });
            }
        }

        private State State
        {
            get { return Session[@"provisioning_state"] as State; }
            set { Session[@"provisioning_state"] = value; }
        }

        private void InitializeState()
        {
            State = new State();
            _cheat_in_place_of_sign_in();
        }

        private PermissionSetModel[] GetPermissionSets()
        {
            var svc = WebServiceFactory.Tenant.PermissionSet(State);
            return svc.ReadMultiple(null, null, 0).Select(p => new PermissionSetModel { Code = p.PermissionSet1, Name = p.Name }).ToArray();
        }

        // GET: Users
        public ActionResult Index()
        {
            InitializeState();

            var svc = WebServiceFactory.ApplicationTenantUser();
            var users = svc.ReadMultiple(
                new []
                {
                    new ApplicationTenantUser.ApplicationTenantUser_Filter
                    {
                        Field = ApplicationTenantUser.ApplicationTenantUser_Fields.Application_Tenant_ID,
                        Criteria = State.Get<TenantModel>().Id
                    }
                }, null, 0);
            return View(new UserListModel
            {
                Users = users.Select(u => new UserModel
                {
                    UserName = u.User_Name,
                    FullName = u.Full_Name,
                    ContactEmail = u.Contact_Email,
                    Administrator = u.Administrator
                }).ToArray(),
                Tenant = State.Get<TenantModel>()
            });
        }

        public ActionResult New()
        {
            InitializeState();
            return View(new NewUserModel
            {
                Tenant = State.Get<TenantModel>(),
                PermissionSets = GetPermissionSets()
            });
        }

        [HttpPost]
        public ActionResult Create(NewUserModel newUser)
        {
            State.Set(newUser);
            newUser.Tenant = State.Get<TenantModel>();
            State.GetOrCreate<NewUserProvisioner>().RunAsync(State, null);
            State.Set(new StatusModel
            {
                Steps = State.Get<List<ITask>>(),
                Tenant = State.Get<TenantModel>(),
                Title = $"Provisioning \"{newUser.User.UserName}\"",
                Description = $"We are up the user account for {newUser.User.FullName}. Sit back and relax, we'll redirect you back to users list when all is ready...",
                Redirect = Url.Action("Index"),
                Workflow = State.Get<NewUserProvisioner>()
            });
            return RedirectToAction("Index", "Status");
        }

        [HttpPost]
        public ActionResult GetPermissionSets(string userName)
        {
            var svc = WebServiceFactory.Tenant.User(State);
            var user = svc.ReadMultiple(new[]
            {
                new Tenant.User.User_Filter
                {
                    Field = Tenant.User.User_Fields.User_Name,
                    Criteria = userName
                }
            }, null, 0).FirstOrDefault();
            var result = Json(new
            {
                permissionSets = user?.Permissions.Select(p => new { company = p.Company ?? "", permissionSet = p.Permission_Set} )
            });
            return result;
        }
    }
}