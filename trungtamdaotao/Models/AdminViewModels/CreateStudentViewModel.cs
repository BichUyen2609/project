// File: Models/AdminViewModels/CreateStudentViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace trungtamdaotao.Models.AdminViewModels
{
    public class CreateStudentViewModel
    {
        // --- LOẠI BỎ THUỘC TÍNH USERNAME KHỎI VIEWMODEL ---
        // [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        // [Display(Name = "Tên đăng nhập (Username)")]
        // [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới và dấu chấm.")]
        // public string UserName { get; set; }
        // --- KẾT THÚC LOẠI BỎ ---

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; } // Giữ lại Email

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [Display(Name = "Họ và tên")]
        [StringLength(100)]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? BirthDate { get; set; }
    }
}