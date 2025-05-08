// File: Models/UngTuyen.cs
namespace HeThongTimViec.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("UngTuyen")]
public class UngTuyen
{
    [Key]
    public int Id { get; set; }

    public int TinTuyenDungId { get; set; }
    public int UngVienId { get; set; }

    [StringLength(255)]
    [Url]
    public string? UrlCvDaNop { get; set; }

    public string? ThuGioiThieu { get; set; }

    [Required]
    [EnumDataType(typeof(TrangThaiUngTuyen))]
    public TrangThaiUngTuyen TrangThai { get; set; }

    public DateTime NgayNop { get; set; }
    public DateTime? NgayCapNhatTrangThai { get; set; }

    // Navigation Properties
    [ForeignKey("TinTuyenDungId")]
    public virtual TinTuyenDung TinTuyenDung { get; set; } = null!;
    [ForeignKey("UngVienId")]
    public virtual NguoiDung UngVien { get; set; } = null!;
    public virtual ICollection<TinNhan> TinNhansLienQuan { get; set; } = new List<TinNhan>();
}