using System.Linq;
using ContosoDemoNAV.WebService;

namespace ContosoDemoNAV.Models
{
    public static class Extensions
    {
        public static ApplicationTenant.ApplicationTenant ToApplicationTenant(this TenantModel tenant)
        {
            return new ApplicationTenant.ApplicationTenant
            {
                ApplicationServiceName = Configuration.ApplicationServiceName,
                Name = tenant.TenantName,
                Country = tenant.Country,
                ID = tenant.Id
            };
        }

        public static TenantModel ToTenantModel(this ApplicationTenant.ApplicationTenant tenant)
        {
            return new TenantModel
            {
                CompanyName = tenant.App_Tenant_Subpage_Companies.FirstOrDefault()?.Name,
                Companies = tenant.App_Tenant_Subpage_Companies.Select(c => c.Name).ToArray(),
                TenantName = tenant.Name,
                Country = tenant.Country,
                Id = tenant.ID,
                Url = tenant.URL
            };
        }

        public static ApplicationTenantUser.ApplicationTenantUser ToApplicationTenantUser(this UserModel user)
        {
            return new ApplicationTenantUser.ApplicationTenantUser
            {
                User_Name = user.UserName,
                Contact_Email = user.ContactEmail,
                Full_Name = user.FullName,
                Application_Tenant_ID = user.TenantId,

                Administrator = user.Administrator,
                AdministratorSpecified = true,
            };
        }
    }
}