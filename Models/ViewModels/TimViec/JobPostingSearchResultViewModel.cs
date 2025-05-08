using HeThongTimViec.Models;
using System.Collections.Generic;

namespace HeThongTimViec.ViewModels.TimViec
{
    public class JobPostingSearchResultViewModel
    {
        public int Id { get; set; }
        public string TieuDe { get; set; } = null!;
        public string? TenCongTy { get; set; } // Có thể là tên cá nhân nếu NTD là cá nhân
        public string? UrlLogoCongTy { get; set; } // Lấy từ HoSoDoanhNghiep
        public string DiaDiem { get; set; } = null!; // VD: "Quận 7, Hồ Chí Minh"
        public string ThongTinLuong { get; set; } = null!; // VD: "30-40 nghìn/giờ", "Thỏa thuận"
        public LoaiHinhCongViec LoaiHinhCongViec { get; set; }
        public List<string> Tags { get; set; } = new List<string>(); // VD: ["Người nội trợ", "Lái xe an toàn"]
        public int PhuHopPercent { get; set; } // Phần trăm phù hợp
        public bool DaLuu { get; set; } // Người dùng hiện tại đã lưu tin này chưa?
        public bool DaUngTuyen { get; set; } // Người dùng hiện tại đã ứng tuyển tin này chưa?
        public DateTime NgayDang { get; set; } // Thêm ngày đăng để sort
    }
}