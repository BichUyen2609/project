// File: Models/HoSoDoanhNghiep.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("HoSoDoanhNghiep")]
public class HoSoDoanhNghiep
{
    [Key]
    [ForeignKey("NguoiDung")]
    public int NguoiDungId { get; set; }

    [Required]
    [StringLength(255)]
    public string TenCongTy { get; set; } = null!;

    [StringLength(20)]
    public string? MaSoThue { get; set; }

    [StringLength(255)]
    public string? UrlLogo { get; set; }

    [StringLength(255)]
    [Url]
    public string? UrlWebsite { get; set; }

    public string? MoTa { get; set; }

    [StringLength(255)]
    public string? DiaChiDangKy { get; set; }

    [StringLength(100)]
    public string? QuyMoCongTy { get; set; }

    public bool DaXacMinh { get; set; }

    public int? AdminXacMinhId { get; set; }

    public DateTime? NgayXacMinh { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    [ForeignKey("AdminXacMinhId")]
    public virtual NguoiDung? AdminXacMinh { get; set; }
}