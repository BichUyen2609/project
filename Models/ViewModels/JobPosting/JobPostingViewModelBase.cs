using HeThongTimViec.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.ViewModels.JobPosting
{
    // Lớp cơ sở chứa các thuộc tính chung cho cả hai loại form đăng tin
    public abstract class JobPostingViewModelBase
    {
        public int Id { get; set; } // Dùng cho Edit

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tin.")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        [Display(Name = "Tiêu đề công việc")]
        public string TieuDe { get; set; } = string.Empty; // Khởi tạo rỗng

        [Required(ErrorMessage = "Vui lòng nhập mô tả công việc.")]
        [Display(Name = "Mô tả công việc")]
        [DataType(DataType.MultilineText)]
        public string MoTa { get; set; } = string.Empty;

        [Display(Name = "Yêu cầu công việc")]
        [DataType(DataType.MultilineText)]
        public string? YeuCau { get; set; }

        [Display(Name = "Quyền lợi")]
        [DataType(DataType.MultilineText)]
        public string? QuyenLoi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại hình công việc.")]
        [Display(Name = "Loại hình công việc")]
        public LoaiHinhCongViec LoaiHinhCongViec { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại lương.")]
        [Display(Name = "Hình thức trả lương")]
        public LoaiLuong LoaiLuong { get; set; }

        [Display(Name = "Lương tối thiểu")]
        [Range(0, ulong.MaxValue, ErrorMessage = "Mức lương không hợp lệ.")]
        public ulong? LuongToiThieu { get; set; }

        [Display(Name = "Lương tối đa")]
        [Range(0, ulong.MaxValue, ErrorMessage = "Mức lương không hợp lệ.")]
        // Custom validation để đảm bảo lương tối đa >= lương tối thiểu (nếu cả hai cùng nhập)
        // [Compare(nameof(LuongToiThieu), Operator = ValidationCompareOperator.GreaterThanEqual, ErrorMessage="Lương tối đa phải lớn hơn hoặc bằng lương tối thiểu")] // Cần thư viện hỗ trợ hoặc custom validation attribute
        public ulong? LuongToiDa { get; set; }

        [Display(Name = "Địa chỉ làm việc chi tiết")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
        public string? DiaChiLamViec { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")] // Đảm bảo đã chọn
        [Display(Name = "Tỉnh / Thành phố")]
        public int ThanhPhoId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Quận/Huyện.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Quận/Huyện.")] // Đảm bảo đã chọn
        [Display(Name = "Quận / Huyện")]
        public int QuanHuyenId { get; set; }

        [Display(Name = "Yêu cầu kinh nghiệm")]
        [StringLength(100, ErrorMessage = "Yêu cầu kinh nghiệm không vượt quá 100 ký tự.")]
        public string YeuCauKinhNghiemText { get; set; } = "Không yêu cầu";

        [Display(Name = "Yêu cầu học vấn")]
        [StringLength(100, ErrorMessage = "Yêu cầu học vấn không vượt quá 100 ký tự.")]
        public string YeuCauHocVanText { get; set; } = "Không yêu cầu";

        [Required(ErrorMessage = "Vui lòng nhập số lượng cần tuyển.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng tuyển phải ít nhất là 1.")]
        [Display(Name = "Số lượng tuyển")]
        public int SoLuongTuyen { get; set; } = 1;

        [Display(Name = "Tin tuyển gấp")]
        public bool TinGap { get; set; }

        [Display(Name = "Ngày hết hạn (để trống nếu không giới hạn)")]
        [DataType(DataType.Date)]
        // Custom validation để đảm bảo ngày hết hạn >= ngày hiện tại (nếu có nhập)
        public DateTime? NgayHetHan { get; set; }

        // Danh sách ID các ngành nghề được chọn
        [Display(Name = "Ngành nghề liên quan")]
        public List<int> SelectedNganhNgheIds { get; set; } = new List<int>();

        // Danh sách lịch làm việc chi tiết
        [Display(Name = "Lịch làm việc")]
        public List<LichLamViecViewModel> LichLamViecItems { get; set; } = new List<LichLamViecViewModel>();
    }
}