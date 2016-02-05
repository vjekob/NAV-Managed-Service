using System;
using System.ComponentModel.DataAnnotations;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class UserModel
    {
        [Required]
        [Display(Name = "Full name")]
        public string FullName { get; set; }
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; }
        [Required]
        [Display(Name = "Contact e-mail")]
        public string ContactEmail { get; set; }

        public bool Administrator { get; set; }
        public string Password { get; set; }

        public string TenantId { get; set; }
        public string Company { get; set; }
        public UsePermissionSetModel[] PermissionSets { get; set; }
    }
}