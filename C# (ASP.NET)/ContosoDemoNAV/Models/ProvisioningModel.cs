using System;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class ProvisioningModel
    {
        public TenantModel Tenant { get; set; }
        public UserModel User { get; set; }
        public FeaturesModel Features { get; set; }
        public int CurrentStep { get; set; }
    }
}