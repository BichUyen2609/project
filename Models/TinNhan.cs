// File: Models/TinNhan.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TinNhan")]
public class TinNhan
{
    [Key]
    public int Id { get; set; }

    public int NguoiGuiId { get; set; }
    public int NguoiNhanId { get; set; }
    public int? TinLienQuanId { get; set; }
    public int? UngTuyenLienQuanId { get; set; }

    [Required]
    public string NoiDung { get; set; } = null!;

    public DateTime? NgayDoc { get; set; }
    public DateTime NgayGui { get; set; }

    // Navigation Properties
    [ForeignKey("NguoiGuiId")]
    public virtual NguoiDung NguoiGui { get; set; } = null!;
    [ForeignKey("NguoiNhanId")]
    public virtual NguoiDung NguoiNhan { get; set; } = null!;
    [ForeignKey("TinLienQuanId")]
    public virtual TinTuyenDung? TinLienQuan { get; set; }
    [ForeignKey("UngTuyenLienQuanId")]
    public virtual UngTuyen? UngTuyenLienQuan { get; set; }
}