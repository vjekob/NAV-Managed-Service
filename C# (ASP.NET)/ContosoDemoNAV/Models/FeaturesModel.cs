using System;
using System.ComponentModel.DataAnnotations;

namespace ContosoDemoNAV.Models
{
    [Serializable]
    public class FeaturesModel
    {
        [Required]
        [Display(Name = "Local Currency Code")]
        [StringLength(3, MinimumLength = 3)]
        public string LocalCurrencyCode { get; set; }

        [Display(Name = "Register time (the users spend in the application)")]
        public bool RegisterTime { get; set; }

        [Required]
        [Display(Name = "Company Address")]
        public string CompanyAddress { get; set; }

        [Display(Name = "Company Address 2")]
        public string CompanyAddress2 { get; set; }

        [Required]
        [Display(Name = "Company Post Code")]
        public string CompanyPostCode { get; set; }

        [Required]
        [Display(Name = "Company City")]
        public string CompanyCity { get; set; }

        [Required]
        [Display(Name = "Company Country/Region Code")]
        [StringLength(2, MinimumLength = 2)]
        public string CompanyCountryCode { get; set; }

        [Required]
        [Display(Name = "VAT Registration Number")]
        public string VatRegistrationNumber { get; set; }
    }
}