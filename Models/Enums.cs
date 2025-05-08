// File: Models/DomainEnums.cs (hoặc tên file tương tự)
using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.Models
{
    #region Enums with Display Names

    public enum LoaiTaiKhoan
    {
        [Display(Name = "Quản trị viên")]
        quantrivien,
        [Display(Name = "Cá nhân")]
        canhan,
        [Display(Name = "Doanh nghiệp")]
        doanhnghiep
    }

    public enum GioiTinhNguoiDung
    {
        [Display(Name = "Nam")]
        nam,
        [Display(Name = "Nữ")]
        nu
    }

    public enum TrangThaiTaiKhoan
    {
        [Display(Name = "Chờ xác minh")]
        choxacminh,
        [Display(Name = "Kích hoạt")]
        kichhoat,
        [Display(Name = "Tạm dừng")]
        tamdung,
        [Display(Name = "Bị đình chỉ")]
        bidinhchi
    }

    public enum LoaiLuong
    {
        [Display(Name = "Theo giờ")]
        theogio,
        [Display(Name = "Theo ngày")]
        theongay,
        [Display(Name = "Theo ca")]
        theoca,
        [Display(Name = "Theo tháng")]
        theothang,
        [Display(Name = "Thỏa thuận")]
        thoathuan,
        [Display(Name = "Theo dự án")]
        theoduan
    }

    public enum TrangThaiTimViec
    {
        [Display(Name = "Đang tìm tích cực")]
        dangtimtichcuc,
        [Display(Name = "Đang tìm thụ động")]
        dangtimthudong,
        [Display(Name = "Không tìm kiếm")]
        khongtimkiem
    }

    public enum NgayTrongTuan
    {
        [Display(Name = "Thứ 2")]
        thu2,
        [Display(Name = "Thứ 3")]
        thu3,
        [Display(Name = "Thứ 4")]
        thu4,
        [Display(Name = "Thứ 5")]
        thu5,
        [Display(Name = "Thứ 6")]
        thu6,
        [Display(Name = "Thứ 7")]
        thu7,
        [Display(Name = "Chủ nhật")]
        chunhat,
        [Display(Name = "Ngày linh hoạt")]
        ngaylinhhoat
    }

    public enum BuoiLamViec
    {
        [Display(Name = "Buổi sáng")]
        sang,
        [Display(Name = "Buổi chiều")]
        chieu,
        [Display(Name = "Buổi tối")]
        toi,
        [Display(Name = "Cả ngày")]
        cangay,
        [Display(Name = "Linh hoạt")]
        linhhoat
    }

    public enum LoaiHinhCongViec
    {
        [Display(Name = "Bán thời gian")]
        banthoigian,
        [Display(Name = "Thời vụ")]
        thoivu,
        [Display(Name = "Linh hoạt khác")]
        linhhoatkhac
    }

    public enum TrangThaiTinTuyenDung
    {
        [Display(Name = "Chờ duyệt")]
        choduyet,
        [Display(Name = "Đã duyệt")]
        daduyet,
        [Display(Name = "Tạm ẩn")]
        taman,
        [Display(Name = "Hết hạn")]
        hethan,
        [Display(Name = "Đã tuyển")]
        datuyen,
        [Display(Name = "Bị từ chối")]
        bituchoi,
        [Display(Name = "Đã xóa")]
        daxoa
    }

    public enum TrangThaiUngTuyen
    {
        [Display(Name = "Đã nộp")]
        danop,
        [Display(Name = "NTD đã xem")]
        ntddaxem,
        [Display(Name = "Bị từ chối")]
        bituchoi,
        [Display(Name = "Đã duyệt (Phù hợp)")]
        daduyet,
        [Display(Name = "Đã rút")]
        darut
    }

    public enum LyDoBaoCao
    {
        [Display(Name = "Lừa đảo/Thu phí")]
        luadaophi,
        [Display(Name = "Thông tin sai sự thật")]
        saisuthat,
        [Display(Name = "Nội dung không phù hợp")]
        noidungkhongphuhop,
        [Display(Name = "Không phải Part-time/Thời vụ")]
        khongphaiparttimethoivu,
        [Display(Name = "Tin trùng lặp")]
        trunglap,
        [Display(Name = "Spam/Quảng cáo")]
        spamquangcao,
        [Display(Name = "Lý do khác")]
        khac
    }

    public enum TrangThaiXuLyBaoCao
    {
        [Display(Name = "Mới")]
        moi,
        [Display(Name = "Đã xem xét")]
        daxemxet,
        [Display(Name = "Đã xử lý")]
        daxuly,
        [Display(Name = "Bỏ qua")]
        boqua
    }

    #endregion
}