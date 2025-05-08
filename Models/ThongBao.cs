// File: Models/ThongBao.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("ThongBao")]
public class ThongBao
{
    [Key]
    public int Id { get; set; }

    public int NguoiDungId { get; set; }

    [Required]
    [StringLength(100)]
    public string LoaiThongBao { get; set; } = null!;

    [Required]
    public string DuLieu { get; set; } = null!; // Thường là JSON

    [StringLength(50)]
    public string? LoaiLienQuan { get; set; }

    public int? IdLienQuan { get; set; }

    public bool DaDoc { get; set; }
    public DateTime? NgayDoc { get; set; }
    public DateTime NgayTao { get; set; }

    [ForeignKey("NguoiDungId")]
    public virtual NguoiDung NguoiDung { get; set; } = null!;
}