using HeThongTimViec.Models; // For Enum TrangThaiTinTuyenDung
using System;
using System.Collections.Generic;

namespace HeThongTimViec.Views.ViewModels.Admin.JobPosts
{
    public class AdminJobPostItemViewModel
    {
        public int Id { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public int SoUngVien { get; set; }
        public string TenCongTy { get; set; } = string.Empty;
        public string? LogoCongTyUrl { get; set; }
        public List<string> NganhNghes { get; set; } = new List<string>();
        public string DiaDiem { get; set; } = string.Empty; // e.g., "Hà Nội"
        public string MucLuongText { get; set; } = string.Empty;
        public TrangThaiTinTuyenDung TrangThai { get; set; }
        public DateTime NgayDang { get; set; }
        public DateTime? NgayHetHan { get; set; } // <<< ADD THIS PROPERTY
        public bool TinGap { get; set; }
    }
}
