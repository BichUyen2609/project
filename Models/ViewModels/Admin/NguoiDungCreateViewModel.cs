// File: ViewModels/Admin/NguoiDungCreateViewModel.cs
using HeThongTimViec.Models; // Namespace chứa Enum LoaiTaiKhoan, GioiTinhNguoiDung
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần cho [Column]

// Đổi namespace thành ViewModels.Admin
namespace HeThongTimViec.ViewModels.Admin
{
    /// <summary>
    /// ViewModel sử dụng cho việc tạo mới một người dùng từ giao diện Admin.
    /// Chứa các thuộc tính cần thiết và các quy tắc xác thực (validation).
    /// </summary>
    public class NguoiDungCreateViewModel
    {
        [Required(ErrorMessage = "Địa chỉ email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng địa chỉ email không hợp lệ.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(150, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)] // Ví dụ: Yêu cầu tối thiểu 6 ký tự
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ.")]
        [RegularExpression(@"^(\+84|0)\d{9,10}$", ErrorMessage = "Số điện thoại phải theo định dạng Việt Nam hợp lệ (VD: 09..., +849...).")]
        public string? Sdt { get; set; } // Cho phép null

        [Required(ErrorMessage = "Vui lòng chọn loại tài khoản.")]
        [Display(Name = "Loại tài khoản")]
        // Có thể thêm validation để đảm bảo chỉ chọn 'canhan' hoặc 'doanhnghiep' trong giao diện Admin
        public LoaiTaiKhoan LoaiTk { get; set; }

        [Display(Name = "Giới tính")]
        public GioiTinhNguoiDung? GioiTinh { get; set; } // Cho phép null

        [Column(TypeName = "date")] // Chỉ định kiểu dữ liệu trong DB (nếu cần, thường EF tự suy)
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date, ErrorMessage = "Định dạng ngày không hợp lệ.")]
        // Có thể thêm validation kiểm tra ngày sinh hợp lệ (ví dụ: không phải ngày tương lai)
        public DateTime? NgaySinh { get; set; } // Cho phép null

        [StringLength(255, ErrorMessage = "{0} không được vượt quá {1} ký tự.")]
        [Display(Name = "Địa chỉ chi tiết (Số nhà, đường)")]
        public string? DiaChiChiTiet { get; set; } // Cho phép null

        [Display(Name = "Tỉnh/Thành phố")]
        // Thường không bắt buộc Required ở đây, xử lý logic trong Controller/View
        public int? ThanhPhoId { get; set; } // Cho phép null

        [Display(Name = "Quận/Huyện")]
        // Thường không bắt buộc Required ở đây, xử lý logic trong Controller/View
        public int? QuanHuyenId { get; set; } // Cho phép null

        // Lưu ý: TrangThaiTk không cần có trong ViewModel này
        // vì nó sẽ được Controller tự động quyết định khi tạo (dựa vào LoaiTk).
    }
}