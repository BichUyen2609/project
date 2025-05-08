// File: Models/QuanHuyen.cs
namespace HeThongTimViec.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("QuanHuyen")]
public class QuanHuyen
{
    [Key]
    public int Id { get; set; }

    public int ThanhPhoId { get; set; }

    [Required]
    [StringLength(100)]
    public string Ten { get; set; } = null!;

    [ForeignKey("ThanhPhoId")]
    public virtual ThanhPho ThanhPho { get; set; } = null!;

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
    public virtual ICollection<DiaDiemMongMuon> DiaDiemMongMuons { get; set; } = new List<DiaDiemMongMuon>();
    public virtual ICollection<TinTuyenDung> TinTuyenDungs { get; set; } = new List<TinTuyenDung>();
}