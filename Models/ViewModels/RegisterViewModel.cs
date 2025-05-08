// File: ViewModels/RegisterViewModel.cs

using System.ComponentModel.DataAnnotations;
using HeThongTimViec.Models; // Ensure you have the correct using for your Enums

namespace HeThongTimViec.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ và Tên")]
        [StringLength(150)]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [StringLength(20)]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? Sdt { get; set; } // Thường SĐT là bắt buộc, cân nhắc Required nếu cần

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn loại tài khoản.")]
        [Display(Name = "Loại Tài Khoản Đăng Ký")]
        public LoaiTaiKhoan LoaiTkDangKy { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        [Display(Name = "Giới tính")]
        public GioiTinhNguoiDung? GioiTinh { get; set; } // Nullable nếu có thể không áp dụng (ví dụ: tài khoản hệ thống?)

        [Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; } // Nullable phòng trường hợp không bắt buộc

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết.")]
        [Display(Name = "Địa chỉ liên hệ/trụ sở")] // Rõ ràng hơn đây là địa chỉ hoạt động/liên hệ
        [StringLength(255)]
        public string? DiaChiChiTiet { get; set; } // Nullable nếu không bắt buộc

        [Required(ErrorMessage = "Vui lòng chọn quận huyện.")]
        [Display(Name = "Quận/Huyện (Liên hệ/Trụ sở)")]
        public int? QuanHuyenId { get; set; } // Nullable nếu không bắt buộc

        [Required(ErrorMessage = "Vui lòng chọn thành phố.")]
        [Display(Name = "Tỉnh/Thành phố (Liên hệ/Trụ sở)")]
        public int? ThanhPhoId { get; set; } // Nullable nếu không bắt buộc

        // --- Các trường chỉ dành cho đăng ký Doanh nghiệp ---
        // Lưu ý: Các trường này nên được kiểm tra Required trong Controller
        // dựa trên giá trị của LoaiTkDangKy == LoaiTaiKhoan.doanhnghiep

        [Display(Name = "Tên Công Ty")]
        [StringLength(255)]
        // Cần Required nếu LoaiTkDangKy là DoanhNghiep
        public string? TenCongTy { get; set; }

        [Display(Name = "Mã Số Thuế")]
        [StringLength(20)]
        // Cần Required nếu LoaiTkDangKy là DoanhNghiep
        public string? MaSoThue { get; set; }

        // *** FIELD MỚI ĐƯỢC THÊM VÀO ***
        [Display(Name = "Địa chỉ đăng ký kinh doanh")]
        [StringLength(500)] // Địa chỉ đăng ký có thể dài
        // Cần Required nếu LoaiTkDangKy là DoanhNghiep
        public string? DiaChiDangKyKinhDoanh { get; set; } // Thêm field địa chỉ đăng ký

        [Display(Name = "Website")]
        [Url(ErrorMessage = "Địa chỉ website không hợp lệ.")]
        [StringLength(255)]
        public string? UrlWebsite { get; set; }
    }
}