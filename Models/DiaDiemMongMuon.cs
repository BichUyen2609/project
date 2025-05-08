// File: Models/DiaDiemMongMuon.cs
namespace HeThongTimViec.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("DiaDiemMongMuon")]
public class DiaDiemMongMuon
{
    [Key]
    public int Id { get; set; }

    public int NguoiDungId { get; set; }
    public int QuanHuyenId { get; set; }
    public int ThanhPhoId { get; set; }

    [ForeignKey("NguoiDungId")]
    public virtual NguoiDung NguoiDung { get; set; } = null!;
    [ForeignKey("QuanHuyenId")]
    public virtual QuanHuyen QuanHuyen { get; set; } = null!;
    [ForeignKey("ThanhPhoId")]
    public virtual ThanhPho ThanhPho { get; set; } = null!;
}