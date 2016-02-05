using System;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class NewUserModel
    {
        public TenantModel Tenant { get; set; }
        public UserModel User { get; set; }
        public PermissionSetModel[] PermissionSets { get; set; }
        public string SelectedPermissionSets { get; set; }
    }
}
