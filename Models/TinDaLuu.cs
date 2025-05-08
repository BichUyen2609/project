// File: Models/TinDaLuu.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TinDaLuu")]
public class TinDaLuu
{
    [Key]
    public int Id { get; set; }

    public int NguoiDungId { get; set; }
    public int TinTuyenDungId { get; set; }

    public DateTime NgayLuu { get; set; }

    [ForeignKey("NguoiDungId")]
    public virtual NguoiDung NguoiDung { get; set; } = null!;
    [ForeignKey("TinTuyenDungId")]
    public virtual TinTuyenDung TinTuyenDung { get; set; } = null!;
}