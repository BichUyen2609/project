using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HeThongTimViec.Models; // Đảm bảo namespace này đúng

namespace HeThongTimViec.ViewModels.Admin
{
    /// <summary>
    /// ViewModel sử dụng cho việc chỉnh sửa thông tin người dùng (Cá nhân hoặc Doanh nghiệp) từ giao diện Admin.
    /// </summary>
    public class AdminUserEditViewModel
    {
        public int Id { get; set; } // Không thay đổi

        // =========================================
        // == THÔNG TIN NGƯỜI DÙNG CHUNG (NguoiDung) ==
        // =========================================

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(150, ErrorMessage = "Họ và tên không được vượt quá {1} ký tự.")]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng địa chỉ email không hợp lệ.")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá {1} ký tự.")]
        [Display(Name = "Địa chỉ Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá {1} ký tự.")]
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^(\+84|0)\d{9,10}$", ErrorMessage = "Số điện thoại phải theo định dạng Việt Nam hợp lệ (VD: 09..., +849...).")]
        public string? Sdt { get; set; }

        // LoaiTk được giữ lại để biết đang sửa loại người dùng nào, nhưng không nên cho phép sửa đổi trong form.
        // Thuộc tính này quan trọng để hiển thị/ẩn các trường dành riêng cho Doanh nghiệp.
        [Required]
        [Display(Name = "Loại tài khoản")]
        public LoaiTaiKhoan LoaiTk { get; set; } // ReadOnly trong View

        // TrangThaiTk KHÔNG được sửa trực tiếp ở đây. Nó được quản lý qua các action Ban/Unban/Verify.
        // private TrangThaiTaiKhoan TrangThaiTk { get; set; } // Bỏ đi khỏi ViewModel Edit

        [Display(Name = "Giới tính")]
        public GioiTinhNguoiDung? GioiTinh { get; set; }

        [Column(TypeName = "date")] // Giữ lại để biết DB type
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date, ErrorMessage = "Định dạng ngày không hợp lệ.")]
        public DateTime? NgaySinh { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ chi tiết không được vượt quá {1} ký tự.")]
        [Display(Name = "Địa chỉ chi tiết (Số nhà, đường)")]
        public string? DiaChiChiTiet { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public int? ThanhPhoId { get; set; }

        [Display(Name = "Quận/Huyện")]
        public int? QuanHuyenId { get; set; }


        // ====================================================
        // == THÔNG TIN HỒ SƠ DOANH NGHIỆP (HoSoDoanhNghiep) ==
        // == Chỉ áp dụng nếu LoaiTk == doanhnghiep        ==
        // ====================================================

        // Controller sẽ kiểm tra Required nếu LoaiTk là doanhnghiep
        [StringLength(255, ErrorMessage = "Tên công ty không được vượt quá {1} ký tự.")]
        [Display(Name = "Tên công ty")]
        public string? TenCongTy { get; set; } // Bắt buộc nếu là Doanh nghiệp (kiểm tra trong Controller)

        [StringLength(20, ErrorMessage = "Mã số thuế không được vượt quá {1} ký tự.")]
        [Display(Name = "Mã số thuế")]
        // Kiểm tra unique trong Controller nếu thay đổi
        public string? MaSoThue { get; set; }

        [StringLength(255, ErrorMessage = "URL Website không được vượt quá {1} ký tự.")]
        [Url(ErrorMessage = "Định dạng URL Website không hợp lệ.")]
        [Display(Name = "Website")]
        public string? UrlWebsite { get; set; }

        [Display(Name = "Mô tả công ty")]
        [DataType(DataType.MultilineText)]
        public string? MoTaCongTy { get; set; } // Tên thuộc tính khớp với Controller

        [StringLength(255, ErrorMessage = "Địa chỉ đăng ký không được vượt quá {1} ký tự.")]
        [Display(Name = "Địa chỉ Đăng ký Kinh doanh")]
        public string? DiaChiDangKy { get; set; }

        [StringLength(100, ErrorMessage = "Quy mô công ty không được vượt quá {1} ký tự.")]
        [Display(Name = "Quy mô công ty")]
        public string? QuyMoCongTy { get; set; }

        // Không bao gồm mật khẩu ở đây. Việc đổi mật khẩu nên là một chức năng riêng biệt.
        // Các trường của HoSoUngVien có thể thêm vào đây nếu cần chỉnh sửa từ Admin.
    }
}