using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}