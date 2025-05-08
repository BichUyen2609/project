// File: ViewModels/Admin/AdminUserDetailsViewModel.cs
using HeThongTimViec.Models; // Namespace chứa các Model NguoiDung, UngTuyen, TinDaLuu, etc.
using System.Collections.Generic;

// Đổi namespace thành ViewModels.Admin
namespace HeThongTimViec.ViewModels.Admin
{
    /// <summary>
    /// ViewModel chứa tất cả dữ liệu cần thiết để hiển thị trang chi tiết người dùng trong giao diện Admin.
    /// Bao gồm thông tin người dùng cơ bản và dữ liệu cho các tab chức năng.
    /// </summary>
    public class AdminUserDetailsViewModel
    {
        /// <summary>
        /// Đối tượng NguoiDung chứa thông tin chi tiết của người dùng đang xem.
        /// </summary>
        public NguoiDung User { get; set; } = null!;

        /// <summary>
        /// Danh sách các hoạt động gần đây của người dùng (log hoạt động).
        /// </summary>
        public List<ActivityLogItem> ActivityLog { get; set; } = new List<ActivityLogItem>();

        /// <summary>
        /// Danh sách các đơn ứng tuyển của người dùng (nếu là Ứng viên).
        /// </summary>
        public List<UngTuyen>? CandidateApplications { get; set; } // Nullable nếu không phải ứng viên

        /// <summary>
        /// Danh sách các tin tuyển dụng đã lưu của người dùng (nếu là Ứng viên).
        /// </summary>
        public List<TinDaLuu>? CandidateSavedJobs { get; set; } // Nullable nếu không phải ứng viên

        /// <summary>
        /// Danh sách các tin tuyển dụng đã đăng của người dùng (nếu là Nhà tuyển dụng).
        /// </summary>
        public List<TinTuyenDung>? EmployerPostedJobs { get; set; } // Nullable nếu không phải NTD

        /// <summary>
        /// Danh sách các báo cáo vi phạm nhắm vào các tin tuyển dụng của người dùng này (nếu là Nhà tuyển dụng).
        /// </summary>
        public List<BaoCaoViPham> ReportsAboutUser { get; set; } = new List<BaoCaoViPham>();

        /// <summary>
        /// Danh sách các báo cáo vi phạm mà người dùng này đã gửi đi.
        /// </summary>
        public List<BaoCaoViPham> ReportsByUser { get; set; } = new List<BaoCaoViPham>();
    }
}