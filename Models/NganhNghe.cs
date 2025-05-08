// File: Models/NganhNghe.cs
namespace HeThongTimViec.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("NganhNghe")]
public class NganhNghe
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Ten { get; set; } = null!;

    public string? MoTa { get; set; }

    public virtual ICollection<TinTuyenDung_NganhNghe> TinTuyenDungNganhNghes { get; set; } = new List<TinTuyenDung_NganhNghe>();
}