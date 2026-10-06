namespace SanaNoor_Brand.Models.ViewModel
{
    public class ResetPasswordViewModel
    { 
        public string Email { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }  
        public string ResetPasswordToken { get; set; }
    }
}
