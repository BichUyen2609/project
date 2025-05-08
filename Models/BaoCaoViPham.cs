// File: Models/BaoCaoViPham.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("BaoCaoViPham")]
public class BaoCaoViPham
{
    [Key]
    public int Id { get; set; }

    public int TinTuyenDungId { get; set; }
    public int NguoiBaoCaoId { get; set; }

    [Required]
    [EnumDataType(typeof(LyDoBaoCao))]
    public LyDoBaoCao LyDo { get; set; }

    public string? ChiTiet { get; set; }

    [Required]
    [EnumDataType(typeof(TrangThaiXuLyBaoCao))]
    public TrangThaiXuLyBaoCao TrangThaiXuLy { get; set; }

    public int? AdminXuLyId { get; set; }
    public DateTime? NgayXuLy { get; set; }
    public string? GhiChuAdmin { get; set; }

    public DateTime NgayBaoCao { get; set; }

    // Navigation Properties
    [ForeignKey("TinTuyenDungId")]
    public virtual TinTuyenDung TinTuyenDung { get; set; } = null!;
    [ForeignKey("NguoiBaoCaoId")]
    public virtual NguoiDung NguoiBaoCao { get; set; } = null!;
    [ForeignKey("AdminXuLyId")]
    public virtual NguoiDung? AdminXuLy { get; set; }
}