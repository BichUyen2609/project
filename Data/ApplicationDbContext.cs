// File: Data/ApplicationDbContext.cs

using HeThongTimViec.Models; // Đảm bảo using namespace chứa các Model của bạn
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // Cần cho HasConversion

namespace HeThongTimViec.Data // Đặt namespace phù hợp cho DbContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- DbSet cho các bảng Danh mục ---
        public virtual DbSet<ThanhPho> ThanhPhos { get; set; } = null!;
        public virtual DbSet<QuanHuyen> QuanHuyens { get; set; } = null!;
        public virtual DbSet<NganhNghe> NganhNghes { get; set; } = null!;

        // --- DbSet cho Người dùng và Hồ sơ ---
        public virtual DbSet<NguoiDung> NguoiDungs { get; set; } = null!;
        public virtual DbSet<HoSoDoanhNghiep> HoSoDoanhNghieps { get; set; } = null!;
        public virtual DbSet<HoSoUngVien> HoSoUngViens { get; set; } = null!;

        // --- DbSet cho Lịch rảnh và Địa điểm mong muốn ---
        public virtual DbSet<LichRanhUngVien> LichRanhUngViens { get; set; } = null!;
        public virtual DbSet<DiaDiemMongMuon> DiaDiemMongMuons { get; set; } = null!;

        // --- DbSet cho Tin tuyển dụng và liên quan ---
        public virtual DbSet<TinTuyenDung> TinTuyenDungs { get; set; } = null!;
        public virtual DbSet<TinTuyenDung_NganhNghe> TinTuyenDung_NganhNghes { get; set; } = null!; // Bảng trung gian
        public virtual DbSet<LichLamViecCongViec> LichLamViecCongViecs { get; set; } = null!;

        // --- DbSet cho Tương tác ---
        public virtual DbSet<UngTuyen> UngTuyens { get; set; } = null!;
        public virtual DbSet<TinDaLuu> TinDaLuus { get; set; } = null!;
        public virtual DbSet<BaoCaoViPham> BaoCaoViPhams { get; set; } = null!;

        // --- DbSet cho Giao tiếp ---
        public virtual DbSet<TinNhan> TinNhans { get; set; } = null!;
        public virtual DbSet<ThongBao> ThongBaos { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Quan trọng: Gọi phương thức của lớp cha

            // --- Cấu hình chi tiết bằng Fluent API ---

            // Khóa chính phức hợp cho bảng trung gian TinTuyenDung_NganhNghe
            modelBuilder.Entity<TinTuyenDung_NganhNghe>()
                .HasKey(t => new { t.TinTuyenDungId, t.NganhNgheId });

            // Định nghĩa mối quan hệ Nhiều-Nhiều giữa TinTuyenDung và NganhNghe
            modelBuilder.Entity<TinTuyenDung_NganhNghe>()
                .HasOne(tnn => tnn.TinTuyenDung)
                .WithMany(ttd => ttd.TinTuyenDungNganhNghes)
                .HasForeignKey(tnn => tnn.TinTuyenDungId);

            modelBuilder.Entity<TinTuyenDung_NganhNghe>()
                .HasOne(tnn => tnn.NganhNghe)
                .WithMany(nn => nn.TinTuyenDungNganhNghes)
                .HasForeignKey(tnn => tnn.NganhNgheId);

            // --- Cấu hình UNIQUE Constraints ---
            modelBuilder.Entity<ThanhPho>()
                .HasIndex(tp => tp.Ten)
                .IsUnique();

            modelBuilder.Entity<QuanHuyen>()
                .HasIndex(qh => new { qh.ThanhPhoId, qh.Ten })
                .IsUnique()
                .HasDatabaseName("uq_QuanHuyen_ThanhPho");

            modelBuilder.Entity<NganhNghe>()
                .HasIndex(nn => nn.Ten)
                .IsUnique();

            modelBuilder.Entity<NguoiDung>(entity => // Bắt đầu cấu hình NguoiDung
            {
                entity.HasIndex(nd => nd.Email)
                      .IsUnique();

                entity.HasIndex(nd => nd.Sdt)
                      .IsUnique();

                // *** THAY ĐỔI QUAN TRỌNG ĐỂ SỬA CẢNH BÁO EF CORE ***
                entity.Property(e => e.TrangThaiTk)
                      .HasConversion<string>() // Giữ nguyên việc lưu enum thành string
                      .ValueGeneratedNever();  // Báo EF Core: Giá trị luôn do code C# cung cấp,
                                               // không bao giờ do DB tạo (kể cả default).
                                               // Điều này loại bỏ HasDefaultValue(...)

                // Các cấu hình khác cho NguoiDung nếu có...
                entity.Property(e => e.LoaiTk).HasConversion<string>();
                entity.Property(e => e.GioiTinh).HasConversion<string>(); // Cần xem lại nếu GioiTinh không phải enum
            }); // Kết thúc cấu hình NguoiDung


            modelBuilder.Entity<HoSoDoanhNghiep>()
                .HasIndex(hsdn => hsdn.MaSoThue)
                .IsUnique();

            modelBuilder.Entity<LichRanhUngVien>()
                .HasIndex(lr => new { lr.NguoiDungId, lr.NgayTrongTuan, lr.BuoiLamViec })
                .IsUnique()
                .HasDatabaseName("uq_NguoiDung_LichRanh");

            modelBuilder.Entity<DiaDiemMongMuon>()
                .HasIndex(dd => new { dd.NguoiDungId, dd.QuanHuyenId })
                .IsUnique()
                .HasDatabaseName("uq_NguoiDung_DiaDiem");

            modelBuilder.Entity<UngTuyen>()
               .HasIndex(ut => new { ut.TinTuyenDungId, ut.UngVienId })
               .IsUnique()
               .HasDatabaseName("uq_TinTuyenDung_UngVien");

            modelBuilder.Entity<TinDaLuu>()
                .HasIndex(tdl => new { tdl.NguoiDungId, tdl.TinTuyenDungId })
                .IsUnique()
                .HasDatabaseName("uq_NguoiDung_TinDaLuu");


            // --- Cấu hình Hành vi Xóa (ON DELETE) ---
            modelBuilder.Entity<QuanHuyen>()
               .HasOne(qh => qh.ThanhPho)
               .WithMany(tp => tp.QuanHuyens)
               .HasForeignKey(qh => qh.ThanhPhoId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BaoCaoViPham>()
                .HasOne(b => b.AdminXuLy)
                .WithMany(nd => nd.BaoCaoViPhamDaXuLy)
                .HasForeignKey(b => b.AdminXuLyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HoSoDoanhNghiep>()
               .HasOne(hsdn => hsdn.NguoiDung)
               .WithOne(nd => nd.HoSoDoanhNghiep)
               .HasForeignKey<HoSoDoanhNghiep>(hsdn => hsdn.NguoiDungId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HoSoUngVien>()
                .HasOne(hsuv => hsuv.NguoiDung)
                .WithOne(nd => nd.HoSoUngVien)
                .HasForeignKey<HoSoUngVien>(hsuv => hsuv.NguoiDungId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.QuanHuyen)
                .WithMany(qh => qh.NguoiDungs)
                .HasForeignKey(nd => nd.QuanHuyenId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.ThanhPho)
                .WithMany(tp => tp.NguoiDungs)
                .HasForeignKey(nd => nd.ThanhPhoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<HoSoDoanhNghiep>()
               .HasOne(hsdn => hsdn.AdminXacMinh)
               .WithMany(nd => nd.HoSoDoanhNghiepDaXacMinh)
               .HasForeignKey(hsdn => hsdn.AdminXacMinhId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TinTuyenDung>()
               .HasOne(ttd => ttd.QuanHuyen)
               .WithMany(qh => qh.TinTuyenDungs)
               .HasForeignKey(ttd => ttd.QuanHuyenId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TinTuyenDung>()
               .HasOne(ttd => ttd.ThanhPho)
               .WithMany(tp => tp.TinTuyenDungs)
               .HasForeignKey(ttd => ttd.ThanhPhoId)
               .OnDelete(DeleteBehavior.Restrict);

            // --- Cấu hình Chuyển đổi Kiểu dữ liệu ENUM thành string (trừ TrangThaiTk đã xử lý ở trên) ---
            // Giữ nguyên các cấu hình khác cho Enum nếu bạn muốn lưu chúng thành string
            modelBuilder.Entity<HoSoUngVien>().Property(e => e.LoaiLuongMongMuon).HasConversion<string>();
            modelBuilder.Entity<HoSoUngVien>().Property(e => e.TrangThaiTimViec).HasConversion<string>().HasDefaultValue(TrangThaiTimViec.dangtimtichcuc);
            modelBuilder.Entity<LichRanhUngVien>().Property(e => e.NgayTrongTuan).HasConversion<string>();
            modelBuilder.Entity<LichRanhUngVien>().Property(e => e.BuoiLamViec).HasConversion<string>();
            modelBuilder.Entity<TinTuyenDung>().Property(e => e.LoaiHinhCongViec).HasConversion<string>();
            modelBuilder.Entity<TinTuyenDung>().Property(e => e.LoaiLuong).HasConversion<string>();
            modelBuilder.Entity<TinTuyenDung>().Property(e => e.TrangThai).HasConversion<string>().HasDefaultValue(TrangThaiTinTuyenDung.choduyet);
            modelBuilder.Entity<LichLamViecCongViec>().Property(e => e.NgayTrongTuan).HasConversion<string>();
            modelBuilder.Entity<LichLamViecCongViec>().Property(e => e.BuoiLamViec).HasConversion<string>();
            modelBuilder.Entity<UngTuyen>().Property(e => e.TrangThai).HasConversion<string>().HasDefaultValue(TrangThaiUngTuyen.danop);
            modelBuilder.Entity<BaoCaoViPham>().Property(e => e.LyDo).HasConversion<string>();
            modelBuilder.Entity<BaoCaoViPham>().Property(e => e.TrangThaiXuLy).HasConversion<string>().HasDefaultValue(TrangThaiXuLyBaoCao.moi);

            // --- Cấu hình Kiểu dữ liệu đặc biệt ---
            modelBuilder.Entity<LichLamViecCongViec>().Property(e => e.GioBatDau).HasColumnType("time");
            modelBuilder.Entity<LichLamViecCongViec>().Property(e => e.GioKetThuc).HasColumnType("time");

            // ... (Các cấu hình khác giữ nguyên) ...
        }
    }
}
