// File: Models/LichLamViecCongViec.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("LichLamViecCongViec")]
public class LichLamViecCongViec
{
    [Key]
    public int Id { get; set; }

    public int TinTuyenDungId { get; set; }

    [Required]
    [EnumDataType(typeof(NgayTrongTuan))]
    public NgayTrongTuan NgayTrongTuan { get; set; }

    public TimeSpan? GioBatDau { get; set; } // TIME
    public TimeSpan? GioKetThuc { get; set; } // TIME

    [EnumDataType(typeof(BuoiLamViec))]
    public BuoiLamViec? BuoiLamViec { get; set; }

    [ForeignKey("TinTuyenDungId")]
    public virtual TinTuyenDung TinTuyenDung { get; set; } = null!;
}