// File: Models/TinTuyenDung_NganhNghe.cs
namespace HeThongTimViec.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TinTuyenDung_NganhNghe")]
public class TinTuyenDung_NganhNghe
{
    // Khóa chính phức hợp - Cần Fluent API trong DbContext
    public int TinTuyenDungId { get; set; }
    public int NganhNgheId { get; set; }

    // Navigation Properties
    [ForeignKey("TinTuyenDungId")]
    public virtual TinTuyenDung TinTuyenDung { get; set; } = null!;
    [ForeignKey("NganhNgheId")]
    public virtual NganhNghe NganhNghe { get; set; } = null!;
}