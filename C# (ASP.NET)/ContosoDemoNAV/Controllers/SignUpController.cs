using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Controllers
{
    public class SignUpController : Controller
    {
        private State State
        {
            get { return Session[@"provisioning_state"] as State; }
            set { Session[@"provisioning_state"] = value; }
        }

        private void InitializeWizard()
        {
            ViewData["Wizard"] = new[]
            {
                "Eula",
                "Company",
                "User",
                "Features",
                "Summary"
            };
        }

        private void InitializeState()
        {
            if (State == null)
                State = new State();
        }

        public ActionResult Index(ProvisioningModel provisioning, int id = 1, int step = 0)
        {
            InitializeWizard();
            InitializeState();

            if (step == 1)
            {
                if (provisioning.Tenant != null)
                    State.Set(provisioning.Tenant);
                if (provisioning.User != null)
                    State.Set(provisioning.User);
                if (provisioning.Features != null)
                    State.Set(provisioning.Features);
            }
            if (id + step > (ViewData["Wizard"] as string[])?.Length)
            {
                return RedirectToAction("CreateTenant");
            }

            return View(new ProvisioningModel
            {
                Tenant = State.Get<TenantModel>() ?? new TenantModel(),
                User = State.Get<UserModel>() ?? new UserModel(),
                Features = State.Get<FeaturesModel>() ?? new FeaturesModel(),
                CurrentStep = id + step
            });
        }

        public ActionResult CreateTenant(int fromStep = 0)
        {
            State.GetOrCreate<ProvisioningManager>().RunAsync(State, () =>
            {
                State.Completed = true;
            });
            State.Set(new StatusModel
            {
                Steps = State.Get<List<ITask>>(),
                Tenant = State.Get<TenantModel>(),
                Title = $"Provisioning \"{State.Get<TenantModel>().CompanyName}\"",
                Description = "We are setting you up. Sit back and relax, in a few minutes your Contoso Family Farming Cloud will be ready...",
                Redirect = Url.Action("Completed", "SignUp"),
                Workflow = State.Get<ProvisioningManager>()
            });
            return RedirectToAction("Index", "Status");
        }

        public ActionResult Completed()
        {
            var view = View(new ProvisioningModel
            {
                Tenant = State.Get<TenantModel>(),
                User = State.Get<UserModel>()
            });
            State = null;
            return view;
        }
    }
}