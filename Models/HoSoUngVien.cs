// File: Models/HoSoUngVien.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HeThongTimViec.Models
{
    [Table("HoSoUngVien")]
    public class HoSoUngVien
    {
        [Key]
        [ForeignKey("NguoiDung")]
        [Required(ErrorMessage = "ID người dùng không được để trống.")]
        public int NguoiDungId { get; set; }

        [StringLength(255)]
        [Display(Name = "Tiêu đề hồ sơ")] // <<< THÊM/SỬA DISPLAY NAME
        public string? TieuDeHoSo { get; set; }

        [Display(Name = "Giới thiệu bản thân")] // <<< THÊM/SỬA DISPLAY NAME
        public string? GioiThieuBanThan { get; set; }

        [StringLength(255)]
        [Display(Name = "Vị trí mong muốn")] // <<< THÊM/SỬA DISPLAY NAME
        public string? ViTriMongMuon { get; set; }

        [EnumDataType(typeof(LoaiLuong))]
        [Display(Name = "Loại lương mong muốn")] // <<< THÊM/SỬA DISPLAY NAME
        public LoaiLuong? LoaiLuongMongMuon { get; set; }

        [Display(Name = "Mức lương mong muốn")] // <<< Sửa lại cho gọn hơn nếu muốn
        [Range(0, double.MaxValue, ErrorMessage = "Mức lương phải là số không âm.")] // Có thể dùng ulong thay vì double?
        public ulong? MucLuongMongMuon { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái tìm việc.")]
        [EnumDataType(typeof(TrangThaiTimViec))]
        [Display(Name = "Trạng thái tìm việc")] // <<< Đã có
        public TrangThaiTimViec TrangThaiTimViec { get; set; }

        [Display(Name = "Cho phép tìm kiếm")] // <<< Đã có
        public bool ChoPhepTimKiem { get; set; }

        [StringLength(255)]
        [Url]
        [Display(Name = "CV mặc định")] // <<< Sửa lại cho gọn hơn nếu muốn
        public string? UrlCvMacDinh { get; set; }

        [ValidateNever]
        public virtual NguoiDung NguoiDung { get; set; } = null!;
    }
}