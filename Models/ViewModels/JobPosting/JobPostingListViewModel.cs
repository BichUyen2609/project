using HeThongTimViec.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.ViewModels.JobPosting
{
    // ViewModel dùng để hiển thị danh sách tin đăng trong trang quản lý
    public class JobPostingListViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề")]
        public string TieuDe { get; set; } = string.Empty;

        [Display(Name = "Trạng thái")]
        public TrangThaiTinTuyenDung TrangThai { get; set; }

        [Display(Name = "Ngày đăng")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime NgayDang { get; set; }

        [Display(Name = "Ngày hết hạn")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", NullDisplayText = "Không giới hạn")]
        public DateTime? NgayHetHan { get; set; }

        // Ví dụ thêm các thông tin khác nếu muốn hiển thị
        [Display(Name = "Lượt xem")]
        public int LuotXem { get; set; } = 0; // Giả sử bạn có trường này trong Model

        [Display(Name = "Số ứng viên")]
        public int SoUngVien { get; set; } = 0; // Sẽ được tính toán
    }
}