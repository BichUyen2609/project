using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using HeThongTimViec.Models; // For Enum TrangThaiTinTuyenDung

namespace HeThongTimViec.Views.ViewModels.Admin.JobPosts
{
    public class AdminJobPostIndexViewModel
    {
        public List<AdminJobPostItemViewModel> JobPosts { get; set; } = new List<AdminJobPostItemViewModel>();
        public AdminJobPostStatisticsViewModel Statistics { get; set; } = new AdminJobPostStatisticsViewModel();

        // Filtering & Search
        public string? SearchTerm { get; set; }
        public int? SelectedNganhNgheId { get; set; }
        public int? SelectedThanhPhoId { get; set; }
        public TrangThaiTinTuyenDung? SelectedTrangThai { get; set; }
        public string? ActiveTab { get; set; } // "all", "pending", "active", "expired"

        public List<SelectListItem> NganhNgheOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ThanhPhoOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> TrangThaiOptions { get; set; } = new List<SelectListItem>();


        // Pagination
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
        public int TotalItems { get; set; }

        public AdminJobPostIndexViewModel()
        {
            // Initialize TrangThaiOptions for all statuses, including a "Tất cả" option
            TrangThaiOptions.Add(new SelectListItem("Tất cả trạng thái", ""));
            foreach (TrangThaiTinTuyenDung status in Enum.GetValues(typeof(TrangThaiTinTuyenDung)))
            {
                TrangThaiOptions.Add(new SelectListItem(GetTrangThaiDisplayName(status), status.ToString()));
            }
        }

        // Helper to get display names for TrangThaiTinTuyenDung (could use DisplayAttribute too)
        public static string GetTrangThaiDisplayName(TrangThaiTinTuyenDung status)
        {
            return status switch
            {
                TrangThaiTinTuyenDung.choduyet => "Chờ duyệt",
                TrangThaiTinTuyenDung.daduyet => "Đã duyệt", // UI uses "Đang hoạt động" for daduyet and not expired
                TrangThaiTinTuyenDung.taman => "Tạm ẩn",
                TrangThaiTinTuyenDung.hethan => "Hết hạn",
                TrangThaiTinTuyenDung.datuyen => "Đã tuyển",
                TrangThaiTinTuyenDung.bituchoi => "Bị từ chối",
                TrangThaiTinTuyenDung.daxoa => "Đã xóa",
                _ => status.ToString(),
            };
        }
        public static string GetTrangThaiCssClass(TrangThaiTinTuyenDung status, DateTime? ngayHetHan)
        {
            if (status == TrangThaiTinTuyenDung.daduyet && (ngayHetHan == null || ngayHetHan >= DateTime.Today))
            {
                return "badge bg-success"; // Đang hoạt động
            }
            if (status == TrangThaiTinTuyenDung.hethan || (status == TrangThaiTinTuyenDung.daduyet && ngayHetHan < DateTime.Today))
            {
                return "badge bg-danger"; // Đã hết hạn
            }
            return status switch
            {
                TrangThaiTinTuyenDung.choduyet => "badge bg-warning text-dark",
                TrangThaiTinTuyenDung.bituchoi => "badge bg-secondary",
                TrangThaiTinTuyenDung.taman => "badge bg-info text-dark",
                TrangThaiTinTuyenDung.datuyen => "badge bg-primary",
                TrangThaiTinTuyenDung.daxoa => "badge bg-dark",
                _ => "badge bg-light text-dark",
            };
        }
         public static string GetTrangThaiDisplayForTable(TrangThaiTinTuyenDung status, DateTime? ngayHetHan)
        {
            if (status == TrangThaiTinTuyenDung.daduyet && (ngayHetHan == null || ngayHetHan >= DateTime.Today))
            {
                return "ĐANG HOẠT ĐỘNG";
            }
            if (status == TrangThaiTinTuyenDung.hethan || (status == TrangThaiTinTuyenDung.daduyet && ngayHetHan < DateTime.Today))
            {
                return "ĐÃ HẾT HẠN";
            }
            return GetTrangThaiDisplayName(status).ToUpper();
        }
    }
}