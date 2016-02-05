namespace ContosoDemoNAV.WebService
{
    public static class ServiceNames
    {
        public static string ApplicationTenant => "Page/ApplicationTenant";
        public static string ApplicationTenantCompany => "Page/ApplicationTenantCompany";
        public static string ApplicationTenantUser => "Page/ApplicationTenantUser";
        public static string Operation => "Codeunit/Operation";
        public static class Tenant
        {
            public static string CompanyInformation => "Page/CompanyInformation";
            public static string GeneralLedgerSetup => "Page/GLSetup";
            public static string User => "Page/User";
            public static string UserPermissionSet => "Page/UserPermissionSet";
            public static string PermissionSet => "Page/PermissionSet";
        }
    }
}