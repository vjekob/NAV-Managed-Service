using System;
using System.Web.Mvc;
using ContosoDemoNAV.ApplicationTenant;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.WebService;

namespace ContosoDemoNAV.Controllers
{
    public class ValidationController : Controller
    {
        public JsonResult IsTenantAvailable(TenantModel tenant)
        {
            try
            {
                var svc = WebServiceFactory.ApplicationTenant();
                var result = svc.ReadMultiple(new []
                {
                    new ApplicationTenant_Filter
                    {
                        Field = ApplicationTenant_Fields.Name,
                        Criteria = "@" + tenant.TenantName
                    },
                    new ApplicationTenant_Filter
                    {
                        Field = ApplicationTenant_Fields.Application_Service_Name_Web_Service_Filter_Field,
                        Criteria = Configuration.ApplicationServiceName
                    }
                }, null, 1);
                return Json(result.Length == 0, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(e.Message, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult IsUserAvailable(UserModel user)
        {
            return Json("");
        }
    }
}