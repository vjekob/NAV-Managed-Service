namespace ContosoDemoNAV.WebService
{
    public static class Configuration
    {
        public static string UrlBase
            => @""; // TODO: put your Management Portal url here
        public static string UserName => @""; // TODO: put your username here
        public static string Password => @""; // TODO: put your password here
        public static string ApplicationServiceName = @""; // TODO: put your application service here
        public static string DefaultCompanyName = @"CRONUS International Ltd.";
        public static string GetUrl(string service) => string.Format(UrlBase, service);
    }
}