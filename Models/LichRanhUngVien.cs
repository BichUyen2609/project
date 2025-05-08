// File: Models/LichRanhUngVien.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("LichRanhUngVien")]
public class LichRanhUngVien
{
    [Key]
    public int Id { get; set; }

    public int NguoiDungId { get; set; }

    [Required]
    [EnumDataType(typeof(NgayTrongTuan))]
    public NgayTrongTuan NgayTrongTuan { get; set; }

    [Required]
    [EnumDataType(typeof(BuoiLamViec))]
    public BuoiLamViec BuoiLamViec { get; set; }

    [ForeignKey("NguoiDungId")]
    public virtual NguoiDung NguoiDung { get; set; } = null!;
}