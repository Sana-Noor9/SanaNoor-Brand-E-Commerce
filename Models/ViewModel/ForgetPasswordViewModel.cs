using System.ComponentModel.DataAnnotations;

namespace SanaNoor_Brand.Models.ViewModel
{
    public class ForgetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
