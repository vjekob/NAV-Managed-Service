namespace ContosoDemoNAV.Models
{
    public class UserListModel
    {
        public TenantModel Tenant { get; set; }
        public UserModel[] Users { get; set; }
        public string SelectedCompany { get; set; }
    }
}
