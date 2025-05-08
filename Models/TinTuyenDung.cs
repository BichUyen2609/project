// File: Models/TinTuyenDung.cs
namespace HeThongTimViec.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("TinTuyenDung")]
public class TinTuyenDung
{
    [Key]
    public int Id { get; set; }

    public int NguoiDangId { get; set; }

    [Required]
    [StringLength(255)]
    public string TieuDe { get; set; } = null!;

    [Required]
    public string MoTa { get; set; } = null!;

    public string? YeuCau { get; set; }
    public string? QuyenLoi { get; set; }

    [Required]
    [EnumDataType(typeof(LoaiHinhCongViec))]
    public LoaiHinhCongViec LoaiHinhCongViec { get; set; }

    [Required]
    [EnumDataType(typeof(LoaiLuong))]
    public LoaiLuong LoaiLuong { get; set; }

    public ulong? LuongToiThieu { get; set; } // BIGINT UNSIGNED
    public ulong? LuongToiDa { get; set; }     // BIGINT UNSIGNED

    [StringLength(255)]
    public string? DiaChiLamViec { get; set; }

    public int QuanHuyenId { get; set; }
    public int ThanhPhoId { get; set; }

    [StringLength(100)]
    public string YeuCauKinhNghiemText { get; set; } = null!;

    [StringLength(100)]
    public string YeuCauHocVanText { get; set; } = null!;

    public int SoLuongTuyen { get; set; }

    [Required]
    [EnumDataType(typeof(TrangThaiTinTuyenDung))]
    public TrangThaiTinTuyenDung TrangThai { get; set; }

    public bool TinGap { get; set; }

    public DateTime NgayDang { get; set; }
    public DateTime? NgayHetHan { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public int? AdminDuyetId { get; set; }
    public DateTime? NgayDuyet { get; set; }

    // Navigation Properties
    [ForeignKey("NguoiDangId")]
    public virtual NguoiDung NguoiDang { get; set; } = null!;
    [ForeignKey("QuanHuyenId")]
    public virtual QuanHuyen QuanHuyen { get; set; } = null!;
    [ForeignKey("ThanhPhoId")]
    public virtual ThanhPho ThanhPho { get; set; } = null!;
    [ForeignKey("AdminDuyetId")]
    public virtual NguoiDung? AdminDuyet { get; set; }

    public virtual ICollection<TinTuyenDung_NganhNghe> TinTuyenDungNganhNghes { get; set; } = new List<TinTuyenDung_NganhNghe>();
    public virtual ICollection<LichLamViecCongViec> LichLamViecCongViecs { get; set; } = new List<LichLamViecCongViec>();
    public virtual ICollection<UngTuyen> UngTuyens { get; set; } = new List<UngTuyen>();
    public virtual ICollection<TinDaLuu> TinDaLuus { get; set; } = new List<TinDaLuu>();
    public virtual ICollection<BaoCaoViPham> BaoCaoViPhams { get; set; } = new List<BaoCaoViPham>();
    public virtual ICollection<TinNhan> TinNhansLienQuan { get; set; } = new List<TinNhan>();
    
}