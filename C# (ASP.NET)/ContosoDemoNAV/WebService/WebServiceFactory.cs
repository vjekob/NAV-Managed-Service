using System;
using System.Net;
using System.Web.Services.Protocols;
using ContosoDemoNAV.ApplicationTenant;
using ContosoDemoNAV.ApplicationTenantCompany;
using ContosoDemoNAV.ApplicationTenantUser;
using ContosoDemoNAV.Models;
using ContosoDemoNAV.Tenant.CompanyInformation;
using ContosoDemoNAV.Tenant.GLSetup;
using ContosoDemoNAV.Tenant.PermissionSet;
using ContosoDemoNAV.Tenant.User;
using ContosoDemoNAV.Tenant.UserPermissionSet;
using State = ContosoDemoNAV.Process.State;

namespace ContosoDemoNAV.WebService
{
    public static class WebServiceFactory
    {
        private static T NewService<T>(string serviceName) where T : SoapHttpClientProtocol, new()
        {
            return new T()
            {
                Url = Configuration.GetUrl(serviceName),
                Credentials = new NetworkCredential(Configuration.UserName, Configuration.Password)
            };
        }

        public static ApplicationTenant_Service ApplicationTenant()
            => NewService<ApplicationTenant_Service>(ServiceNames.ApplicationTenant);

        public static ApplicationTenantCompany_Service ApplicationTenantCompany()
            => NewService<ApplicationTenantCompany_Service>(ServiceNames.ApplicationTenantCompany);

        public static ApplicationTenantUser_Service ApplicationTenantUser()
            => NewService<ApplicationTenantUser_Service>(ServiceNames.ApplicationTenantUser);

        public static Operation.Operation Operation() => NewService<Operation.Operation>(ServiceNames.Operation);

        public static class Tenant
        {
            private static T NewTenantService<T>(string serviceName, State state) where T : SoapHttpClientProtocol, new()
            {
                return new T()
                {
                    Url = $"{state.Get<TenantModel>().Url}:7047/NAV/WS/{Uri.EscapeUriString(state.Get<TenantModel>().CompanyName)}/{serviceName}",
                    Credentials = new NetworkCredential(state.Get<UserModel>().UserName, state.Get<UserModel>().Password)
                };
            }

            public static CompanyInformation_Service CompanyInformation(State state)
                => NewTenantService<CompanyInformation_Service>(ServiceNames.Tenant.CompanyInformation, state);

            public static GLSetup_Service GeneralLedgerSetup(State state)
                => NewTenantService<GLSetup_Service>(ServiceNames.Tenant.GeneralLedgerSetup, state);

            public static User_Service User(State state)
                => NewTenantService<User_Service>(ServiceNames.Tenant.User, state);

            public static UserPermissionSet_Service UserPermissionSet(State state)
                => NewTenantService <UserPermissionSet_Service>(ServiceNames.Tenant.UserPermissionSet, state);
            public static PermissionSet_Service PermissionSet(State state)
                => NewTenantService<PermissionSet_Service>(ServiceNames.Tenant.PermissionSet, state);
        }
    }
}