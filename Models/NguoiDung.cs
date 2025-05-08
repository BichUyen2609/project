// File: Models/NguoiDung.cs
namespace HeThongTimViec.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("NguoiDung")]
public class NguoiDung // KHÔNG kế thừa IdentityUser
{
    [Key]
    public int Id { get; set; }

    // ... (Các thuộc tính Email, MatKhauHash, HoTen, Sdt,... giữ nguyên) ...
    [Required][StringLength(255)][EmailAddress] public string Email { get; set; } = null!;
    [Required][StringLength(255)] public string MatKhauHash { get; set; } = null!;
    [Required][StringLength(150)] public string HoTen { get; set; } = null!;
    [StringLength(20)] public string? Sdt { get; set; }
    [Required] public LoaiTaiKhoan LoaiTk { get; set; }
    [StringLength(255)] public string? UrlAvatar { get; set; }
    public GioiTinhNguoiDung? GioiTinh { get; set; }
    [Column(TypeName = "date")] public DateTime? NgaySinh { get; set; }
    [StringLength(255)] public string? DiaChiChiTiet { get; set; }
    public int? QuanHuyenId { get; set; }
    public int? ThanhPhoId { get; set; }
    [Required] public TrangThaiTaiKhoan TrangThaiTk { get; set; } = TrangThaiTaiKhoan.kichhoat;//public TrangThaiTaiKhoan? TrangThaiTk { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }


    // --- Navigation Properties ---
    [ForeignKey("QuanHuyenId")] public virtual QuanHuyen? QuanHuyen { get; set; }
    [ForeignKey("ThanhPhoId")] public virtual ThanhPho? ThanhPho { get; set; }
    public virtual HoSoDoanhNghiep? HoSoDoanhNghiep { get; set; }
    public virtual HoSoUngVien? HoSoUngVien { get; set; }

    // ... (Các ICollection khác giữ nguyên) ...
    public virtual ICollection<LichRanhUngVien> LichRanhUngViens { get; set; } = new List<LichRanhUngVien>();
    public virtual ICollection<DiaDiemMongMuon> DiaDiemMongMuons { get; set; } = new List<DiaDiemMongMuon>();
    public virtual ICollection<UngTuyen> UngTuyens { get; set; } = new List<UngTuyen>();
    public virtual ICollection<TinDaLuu> TinDaLuus { get; set; } = new List<TinDaLuu>();
    public virtual ICollection<BaoCaoViPham> BaoCaoViPhamsDaGui { get; set; } = new List<BaoCaoViPham>();
    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
    public virtual ICollection<HoSoDoanhNghiep> HoSoDoanhNghiepDaXacMinh { get; set; } = new List<HoSoDoanhNghiep>();
    public virtual ICollection<BaoCaoViPham> BaoCaoViPhamDaXuLy { get; set; } = new List<BaoCaoViPham>();


    // --- THAY ĐỔI Ở ĐÂY: Mối quan hệ với TinTuyenDung ---
    // Collection này liên kết với thuộc tính NguoiDang trong TinTuyenDung
    [InverseProperty("NguoiDang")]
    public virtual ICollection<TinTuyenDung> TinTuyenDungsDaDang { get; set; } = new List<TinTuyenDung>();

    // Collection này liên kết với thuộc tính AdminDuyet trong TinTuyenDung
    [InverseProperty("AdminDuyet")]
    public virtual ICollection<TinTuyenDung> TinTuyenDungDaDuyet { get; set; } = new List<TinTuyenDung>();


    // --- Mối quan hệ với TinNhan (Giữ nguyên từ lần sửa trước) ---
    [InverseProperty("NguoiGui")]
    public virtual ICollection<TinNhan> TinNhansDaGui { get; set; } = new List<TinNhan>();

    [InverseProperty("NguoiNhan")]
    public virtual ICollection<TinNhan> TinNhansDaNhan { get; set; } = new List<TinNhan>();
}