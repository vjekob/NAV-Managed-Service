using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class TenantModel
    {
        [Required]
        [Display(Name = "Company name")]
        [StringLength(30, MinimumLength = 3)]
        public string CompanyName { get; set; }

        [Required]
        [Remote("IsTenantAvailable", "Validation", ErrorMessage = "This tenant name is already taken.")]
        [RegularExpression(@"^[A-Za-z]+\w+$", ErrorMessage = "The tenant name must contain letters, numbers, and -_ and must start with a letter.")]
        [Display(Name = "Tenant name")]
        [StringLength(30, MinimumLength = 3)]
        public string TenantName { get; set; }

        [Required]
        [Display(Name = "Country")]
        public string Country { get; set; }

        public string Id { get; set; }
        public string Url { get; set; }
        public string[] Companies { get; set; }
    }
}