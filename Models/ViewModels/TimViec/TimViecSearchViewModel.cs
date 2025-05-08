using HeThongTimViec.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.ViewModels.TimViec
{
    public class TimViecSearchViewModel
    {
        // --- Tham số lọc ---
        [Display(Name = "Từ khóa")]
        public string? TuKhoa { get; set; }

        [Display(Name = "Loại công việc")]
        public LoaiHinhCongViec? SelectedLoaiCongViec { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public int? SelectedThanhPhoId { get; set; }

        [Display(Name = "Quận/Huyện")]
        public int? SelectedQuanHuyenId { get; set; }

        [Display(Name = "Ngành nghề")]
        public List<int>? SelectedNganhNgheIds { get; set; }

        [Display(Name = "Ngày làm việc")]
        public NgayTrongTuan? SelectedNgayTrongTuan { get; set; }

        [Display(Name = "Buổi làm việc")]
        public BuoiLamViec? SelectedBuoi { get; set; }

        // TODO: Cân nhắc chuyển Kinh nghiệm, Mức lương thành các trường cụ thể hơn thay vì text
        [Display(Name = "Kinh nghiệm")]
        public string? SelectedKinhNghiem { get; set; } // Tạm thời dùng text search

        [Display(Name = "Mức lương tối thiểu")]
        [Range(0, double.MaxValue, ErrorMessage = "Mức lương phải là số không âm.")]
        public ulong? MucLuongMin { get; set; }

        [Display(Name = "Mức lương tối đa")]
        [Range(0, double.MaxValue, ErrorMessage = "Mức lương phải là số không âm.")]
        public ulong? MucLuongMax { get; set; } // Có thể dùng để lọc range hoặc chỉ 1 mức

        [Display(Name = "Loại lương")]
        public LoaiLuong? SelectedLoaiLuong { get; set; } // Lọc theo giờ, tháng...

        [Display(Name = "Kỹ năng")]
        public string? KyNang { get; set; } // Tạm thời dùng text search

        // --- Tùy chọn khác ---
        public bool ChiHienThiThoiVu { get; set; } // Ví dụ map với LoaiHinhCongViec
        public bool ChiHienThiPhuHop { get; set; } // Cần logic tính phù hợp > ngưỡng nào đó
        public bool ChiHienThiDaLuu { get; set; }
        public bool ChiHienThiDaUngTuyen { get; set; }

        // --- Sắp xếp & Phân trang ---
        public string SortOrder { get; set; } = "moi_nhat"; // Default sort: moi_nhat, phu_hop
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10; // Số lượng item mỗi trang
        public int TotalCount { get; set; } // Tổng số kết quả

        // --- Dữ liệu cho View ---
        public List<JobPostingSearchResultViewModel> Results { get; set; } = new List<JobPostingSearchResultViewModel>();

        // SelectLists cho Dropdowns
        public SelectList? ThanhPhoList { get; set; }
        public SelectList? QuanHuyenList { get; set; } // Sẽ load bằng JS
        public List<SelectListItem>? NganhNgheList { get; set; } // Dùng List vì có thể là multi-select
        public SelectList? LoaiCongViecList { get; set; }
        public SelectList? NgayTrongTuanList { get; set; }
        public SelectList? BuoiLamViecList { get; set; }
        public SelectList? LoaiLuongList { get; set; }
    }
}