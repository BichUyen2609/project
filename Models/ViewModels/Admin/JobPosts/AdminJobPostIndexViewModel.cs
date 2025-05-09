using Microsoft.AspNetCore.Mvc.Rendering;
using HeThongTimViec.Models; // For Enum TrangThaiTinTuyenDung
using System; // For DateTime, Enum.GetValues
using System.Linq; // For Enum.GetValues

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
        public string ActiveTab { get; set; } = "all"; // "all", "pending", "active", "expired"
        public string ViewMode { get; set; } = "list"; // "list", "card"


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
            // Initialize TrangThaiOptions for filter dropdown
            // Based on screenshot, filter dropdown has specific items
            TrangThaiOptions.Add(new SelectListItem("Tất cả trạng thái", ""));
            TrangThaiOptions.Add(new SelectListItem(GetTrangThaiDisplayName(TrangThaiTinTuyenDung.daduyet), TrangThaiTinTuyenDung.daduyet.ToString())); // "Đang hoạt động"
            TrangThaiOptions.Add(new SelectListItem(GetTrangThaiDisplayName(TrangThaiTinTuyenDung.choduyet), TrangThaiTinTuyenDung.choduyet.ToString())); // "Chờ duyệt"
            TrangThaiOptions.Add(new SelectListItem(GetTrangThaiDisplayName(TrangThaiTinTuyenDung.hethan), TrangThaiTinTuyenDung.hethan.ToString())); // "Đã hết hạn"
            // Add more if other specific filterable states are needed, e.g., "Bị từ chối"
            // TrangThaiOptions.Add(new SelectListItem(GetTrangThaiDisplayName(TrangThaiTinTuyenDung.bituchoi), TrangThaiTinTuyenDung.bituchoi.ToString()));
        }

        public static string GetTrangThaiDisplayName(TrangThaiTinTuyenDung status)
        {
            // This helper is good for general display, not for "Đang hoạt động" / "Đã hết hạn" computed states
            return status switch
            {
                TrangThaiTinTuyenDung.choduyet => "Chờ duyệt",
                TrangThaiTinTuyenDung.daduyet => "Đang hoạt động", // Note: This is the "base" status. Computed status uses "ĐANG HOẠT ĐỘNG"
                TrangThaiTinTuyenDung.taman => "Tạm ẩn",
                TrangThaiTinTuyenDung.hethan => "Hết hạn", // Note: This is the "base" status. Computed status uses "ĐÃ HẾT HẠN"
                TrangThaiTinTuyenDung.datuyen => "Đã tuyển",
                TrangThaiTinTuyenDung.bituchoi => "Bị từ chối",
                TrangThaiTinTuyenDung.daxoa => "Đã xóa",
                _ => status.ToString(),
            };
        }

        // Use DateTime.Today for date-only comparisons as UI likely implies this
        // Ensure NgayHetHan in DB is consistently handled (e.g., stored as Date or UTC start/end of day)
        public static string GetTrangThaiCssClass(TrangThaiTinTuyenDung status, DateTime? ngayHetHan)
        {
            DateTime today = DateTime.Today; // Use local date for comparison
            if (status == TrangThaiTinTuyenDung.daduyet && (ngayHetHan == null || ngayHetHan.Value.Date >= today))
            {
                return "badge bg-success"; // Đang hoạt động
            }
            if (status == TrangThaiTinTuyenDung.hethan || (status == TrangThaiTinTuyenDung.daduyet && ngayHetHan != null && ngayHetHan.Value.Date < today))
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
            DateTime today = DateTime.Today;
            if (status == TrangThaiTinTuyenDung.daduyet && (ngayHetHan == null || ngayHetHan.Value.Date >= today))
            {
                return "ĐANG HOẠT ĐỘNG";
            }
            if (status == TrangThaiTinTuyenDung.hethan || (status == TrangThaiTinTuyenDung.daduyet && ngayHetHan != null && ngayHetHan.Value.Date < today))
            {
                return "ĐÃ HẾT HẠN";
            }
            // For other statuses, use their direct display name
            return GetTrangThaiDisplayName(status).ToUpper();
        }
    }
}