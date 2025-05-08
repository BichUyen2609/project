// File: ViewModels/Admin/ActivityLogItem.cs
using System;

// Đổi namespace thành ViewModels.Admin
namespace HeThongTimViec.ViewModels.Admin
{
    /// <summary>
    /// Đại diện cho một mục (một dòng) trong lịch sử hoạt động của người dùng.
    /// </summary>
    public class ActivityLogItem
    {
        /// <summary>
        /// Thời điểm xảy ra hoạt động.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Mô tả về hoạt động đã diễn ra.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Lớp CSS của icon (ví dụ: từ FontAwesome) để hiển thị bên cạnh mô tả.
        /// </summary>
        public string IconClass { get; set; } = "fas fa-info-circle"; // Icon mặc định

        /// <summary>
        /// (Tùy chọn) Đường dẫn URL liên quan đến hoạt động này (ví dụ: link đến tin tuyển dụng, hồ sơ,...).
        /// </summary>
        public string? Link { get; set; }
    }
}