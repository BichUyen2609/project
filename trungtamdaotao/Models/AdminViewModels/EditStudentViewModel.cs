using System;
using System.ComponentModel.DataAnnotations;

namespace trungtamdaotao.Models.AdminViewModels
{
    public class EditStudentViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
        [Display(Name = "Tên đăng nhập (Username)")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? BirthDate { get; set; }

        // --- THÊM THUỘC TÍNH NÀY ---
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")] // Thêm validation nếu muốn
        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; } // Thêm kiểu nullable string?
        // --- HẾT PHẦN THÊM ---
    }
}