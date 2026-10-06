using System.ComponentModel.DataAnnotations;

namespace SanaNoor_Brand.Models.ViewModel
{
        public class RegisterViewModel
        {
            [Required(ErrorMessage = "First Name Should not be Empty")]
            [MinLength(3, ErrorMessage = "FirstName Should Contain Atleast 3 Characters")]
            public string FirstName { get; set; }
            [Required(ErrorMessage = "First Name Should not be Empty")]
            [MinLength(3, ErrorMessage = "FirstName Should Contain Atleast 3 Characters")]
            public string LastName { get; set; }
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }
      
        [Phone]
        public string? PhoneNumber { get; set; }   // ? lagao, [Required] hatao
        [Required]
            [MinLength(8, ErrorMessage = "Password Should Contain 8 Characters")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            [Required]
            [MinLength(8, ErrorMessage = "Password Should Contain 8 Characters")]
            [Compare("Password", ErrorMessage = "Password and Confirm Password not Matched")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; }
        }
    }

