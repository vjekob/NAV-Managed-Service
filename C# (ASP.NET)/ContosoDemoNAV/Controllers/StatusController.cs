using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Process;
using ContosoDemoNAV.Process.Workflow;

namespace ContosoDemoNAV.Controllers
{
    public class StatusController : Controller
    {
        private State State
        {
            get { return Session[@"provisioning_state"] as State; }
            set { Session[@"provisioning_state"] = value; }
        }

        // GET: Status
        public ActionResult Index()
        {
            return View(State.Get<StatusModel>());
        }

        [HttpPost]
        public ActionResult GetStatus()
        {
            var result = Json(new
            {
                Steps = State.Get<List<ITask>>().OrderBy(t => t.Ordinal),
                State.Get<StatusModel>().Workflow.Status,
                State.Get<StatusModel>().Workflow.Error
            });
            return result;
        }
    }
}