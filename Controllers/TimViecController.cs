using HeThongTimViec.Data;
using HeThongTimViec.Models;
using HeThongTimViec.ViewModels.TimViec;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HeThongTimViec.Controllers
{
    public class TimViecController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimViecController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TimViecController(ApplicationDbContext context, ILogger<TimViecController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: /TimViec or /TimViec/Index
        public async Task<IActionResult> Index(TimViecSearchViewModel searchModel)
        {
            await PrepareFilterDataAsync(searchModel);

            int? currentUserId = null;
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int id))
            {
                currentUserId = id;
            }

            var query = BuildSearchQuery(searchModel, currentUserId);

            searchModel.TotalCount = await query.CountAsync();

            // Sắp xếp theo DB trước khi tính toán phù hợp
            // Clone query trước khi lấy count và phân trang nếu logic sort ảnh hưởng
            // Hoặc đảm bảo ApplyDbSorting trả về IQueryable mới

            query = ApplyDbSorting(query, searchModel.SortOrder);

            var tinTuyenDungs = await query
                                    .Skip((searchModel.PageIndex - 1) * searchModel.PageSize)
                                    .Take(searchModel.PageSize)
                                    .ToListAsync();

            // Chỉ lấy thông tin hồ sơ nếu người dùng đăng nhập
            HoSoUngVien? userProfile = null;
            List<LichRanhUngVien>? userSchedule = null;
            List<DiaDiemMongMuon>? userLocations = null;
            HashSet<int> savedJobIds = new HashSet<int>();
            HashSet<int> appliedJobIds = new HashSet<int>();

            if (currentUserId.HasValue)
            {
                userProfile = await _context.HoSoUngViens.AsNoTracking()
                                       .FirstOrDefaultAsync(uv => uv.NguoiDungId == currentUserId.Value);

                // Chỉ lấy lịch rảnh và địa điểm mong muốn nếu hồ sơ ứng viên tồn tại
                if (userProfile != null)
                {
                    userSchedule = await _context.LichRanhUngViens.AsNoTracking()
                                            .Where(lr => lr.NguoiDungId == currentUserId.Value).ToListAsync();
                    userLocations = await _context.DiaDiemMongMuons.AsNoTracking()
                                             .Where(dd => dd.NguoiDungId == currentUserId.Value).ToListAsync();
                }

                savedJobIds = (await _context.TinDaLuus.AsNoTracking()
                                        .Where(l => l.NguoiDungId == currentUserId.Value)
                                        .Select(l => l.TinTuyenDungId).ToListAsync()).ToHashSet();

                // FIX: Lấy tất cả ID tin đã ứng tuyển bởi user
                appliedJobIds = (await _context.UngTuyens.AsNoTracking()
                                        .Where(u => u.UngVienId == currentUserId.Value)
                                        .Select(u => u.TinTuyenDungId).ToListAsync()).ToHashSet();
            }

            // Map kết quả và tính phù hợp (nếu có userProfile và dữ liệu liên quan)
            searchModel.Results = tinTuyenDungs.Select(t => MapToSearchResultViewModel(t, userProfile, userSchedule, userLocations, savedJobIds, appliedJobIds)).ToList();

            // Sắp xếp lại kết quả theo phù hợp sau khi đã tính toán (chỉ khi đăng nhập)
            if (searchModel.SortOrder == "phu_hop" && currentUserId.HasValue && searchModel.Results.Any())
            {
                searchModel.Results = searchModel.Results.OrderByDescending(r => r.PhuHopPercent)
                                                     .ThenByDescending(r => r.NgayDang)
                                                     .ToList();
            }


            return View(searchModel);
        }

        // --- Helper Methods ---

        private async Task PrepareFilterDataAsync(TimViecSearchViewModel model)
        {
            model.ThanhPhoList = new SelectList(await _context.ThanhPhos.AsNoTracking().OrderBy(tp => tp.Ten).ToListAsync(), "Id", "Ten", model.SelectedThanhPhoId);
            model.QuanHuyenList = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Ten");
            if (model.SelectedThanhPhoId.HasValue && model.SelectedThanhPhoId > 0)
            {
                model.QuanHuyenList = new SelectList(await _context.QuanHuyens.AsNoTracking()
                                                                    .Where(qh => qh.ThanhPhoId == model.SelectedThanhPhoId)
                                                                    .OrderBy(qh => qh.Ten).ToListAsync(),
                                                        "Id", "Ten", model.SelectedQuanHuyenId);
            }

            model.NganhNgheList = await _context.NganhNghes.AsNoTracking().OrderBy(n => n.Ten)
                                        .Select(n => new SelectListItem
                                        {
                                            Value = n.Id.ToString(),
                                            Text = n.Ten,
                                            Selected = model.SelectedNganhNgheIds != null && model.SelectedNganhNgheIds.Contains(n.Id)
                                        }).ToListAsync();

            model.LoaiCongViecList = new SelectList(Enum.GetValues(typeof(LoaiHinhCongViec)).Cast<LoaiHinhCongViec>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text", model.SelectedLoaiCongViec);
            model.NgayTrongTuanList = new SelectList(Enum.GetValues(typeof(NgayTrongTuan)).Cast<NgayTrongTuan>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text", model.SelectedNgayTrongTuan);
            model.BuoiLamViecList = new SelectList(Enum.GetValues(typeof(BuoiLamViec)).Cast<BuoiLamViec>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text", model.SelectedBuoi);
            model.LoaiLuongList = new SelectList(Enum.GetValues(typeof(LoaiLuong)).Cast<LoaiLuong>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text", model.SelectedLoaiLuong);
        }

        private IQueryable<TinTuyenDung> BuildSearchQuery(TimViecSearchViewModel filters, int? currentUserId)
        {
            var query = _context.TinTuyenDungs
                              .Include(t => t.NguoiDang)
                                .ThenInclude(nd => nd.HoSoDoanhNghiep)
                              .Include(t => t.ThanhPho)
                              .Include(t => t.QuanHuyen)
                              .Include(t => t.TinTuyenDungNganhNghes)
                                .ThenInclude(tnn => tnn.NganhNghe)
                              .Include(t => t.LichLamViecCongViecs)
                              .Where(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet)
                              .AsQueryable();

            // Lọc theo Từ khóa (bao gồm cả tên công ty/cá nhân)
            if (!string.IsNullOrWhiteSpace(filters.TuKhoa))
            {
                string keyword = filters.TuKhoa.ToLower().Trim();
                query = query.Where(t => t.TieuDe.ToLower().Contains(keyword)
                                      || t.MoTa.ToLower().Contains(keyword)
                                      // Tìm theo tên công ty (nếu là DN) hoặc tên cá nhân (nếu là CN)
                                      || (t.NguoiDang != null && t.NguoiDang.LoaiTk == LoaiTaiKhoan.doanhnghiep && t.NguoiDang.HoSoDoanhNghiep != null && t.NguoiDang.HoSoDoanhNghiep.TenCongTy.ToLower().Contains(keyword))
                                      || (t.NguoiDang != null && t.NguoiDang.LoaiTk == LoaiTaiKhoan.canhan && t.NguoiDang.HoTen.ToLower().Contains(keyword))
                                      || (t.YeuCau != null && t.YeuCau.ToLower().Contains(keyword))
                                      || (t.QuyenLoi != null && t.QuyenLoi.ToLower().Contains(keyword)));
            }

            // Lọc Loại công việc
            if (filters.SelectedLoaiCongViec.HasValue) { query = query.Where(t => t.LoaiHinhCongViec == filters.SelectedLoaiCongViec.Value); }

            // Lọc Địa điểm
            if (filters.SelectedQuanHuyenId.HasValue && filters.SelectedQuanHuyenId > 0) { query = query.Where(t => t.QuanHuyenId == filters.SelectedQuanHuyenId.Value); }
            else if (filters.SelectedThanhPhoId.HasValue && filters.SelectedThanhPhoId > 0) { query = query.Where(t => t.ThanhPhoId == filters.SelectedThanhPhoId.Value); }

            // Lọc Ngành nghề (ANY match)
            if (filters.SelectedNganhNgheIds != null && filters.SelectedNganhNgheIds.Any()) { query = query.Where(t => t.TinTuyenDungNganhNghes.Any(tnn => filters.SelectedNganhNgheIds.Contains(tnn.NganhNgheId))); }

            // Lọc Lịch làm việc (Ngày và Buổi)
            if (filters.SelectedNgayTrongTuan.HasValue) { query = query.Where(t => t.LichLamViecCongViecs.Any(l => l.NgayTrongTuan == filters.SelectedNgayTrongTuan.Value || l.NgayTrongTuan == NgayTrongTuan.ngaylinhhoat)); }
            if (filters.SelectedBuoi.HasValue) { query = query.Where(t => t.LichLamViecCongViecs.Any(l => l.BuoiLamViec == filters.SelectedBuoi.Value || l.BuoiLamViec == BuoiLamViec.linhhoat || l.BuoiLamViec == BuoiLamViec.cangay)); }

            // Lọc Loại lương
            if (filters.SelectedLoaiLuong.HasValue) { query = query.Where(t => t.LoaiLuong == filters.SelectedLoaiLuong.Value); }

            // Lọc Mức lương tối thiểu
            if (filters.MucLuongMin.HasValue && filters.MucLuongMin > 0) { query = query.Where(t => t.LoaiLuong != LoaiLuong.thoathuan && t.LuongToiThieu.HasValue && t.LuongToiThieu.Value >= filters.MucLuongMin.Value); }

            // Lọc Kinh nghiệm (tìm text đơn giản)
            if (!string.IsNullOrWhiteSpace(filters.SelectedKinhNghiem)) { query = query.Where(t => t.YeuCauKinhNghiemText != null && t.YeuCauKinhNghiemText.ToLower().Contains(filters.SelectedKinhNghiem.ToLower())); }

            // Lọc Kỹ năng (tìm text đơn giản trong Yêu cầu)
            if (!string.IsNullOrWhiteSpace(filters.KyNang)) { string skillKeyword = filters.KyNang.ToLower().Trim(); query = query.Where(t => t.YeuCau != null && t.YeuCau.ToLower().Contains(skillKeyword)); }

            // --- Lọc tùy chọn khác ---
            if (filters.ChiHienThiThoiVu) { query = query.Where(t => t.LoaiHinhCongViec == LoaiHinhCongViec.thoivu); }

            if (currentUserId.HasValue)
            {
                if (filters.ChiHienThiDaLuu)
                {
                    query = query.Where(t => _context.TinDaLuus.Any(l => l.TinTuyenDungId == t.Id && l.NguoiDungId == currentUserId.Value));
                }
                if (filters.ChiHienThiDaUngTuyen)
                {
                    query = query.Where(t => _context.UngTuyens.Any(u => u.TinTuyenDungId == t.Id && u.UngVienId == currentUserId.Value));
                }
                // Lọc theo phù hợp (> ngưỡng) sẽ thực hiện sau khi tính toán trong bộ nhớ
            }

            return query;
        }

        // Sắp xếp thực hiện trên DB (trừ phù hợp)
        private IQueryable<TinTuyenDung> ApplyDbSorting(IQueryable<TinTuyenDung> query, string sortOrder)
        {
            switch (sortOrder)
            {
                // "phu_hop" sẽ được sắp xếp sau khi map và tính phù hợp
                case "luong_cao":
                    return query.OrderByDescending(t => t.LuongToiDa ?? t.LuongToiThieu ?? 0).ThenByDescending(t => t.NgayDang);
                case "luong_thap":
                    return query.OrderBy(t => t.LuongToiThieu == null).ThenBy(t => t.LuongToiThieu ?? 0).ThenByDescending(t => t.NgayDang);
                case "moi_nhat":
                default:
                    return query.OrderByDescending(t => t.NgayDang);
            }
        }

        private JobPostingSearchResultViewModel MapToSearchResultViewModel(
            TinTuyenDung t,
            HoSoUngVien? userProfile,
            List<LichRanhUngVien>? userSchedule,
            List<DiaDiemMongMuon>? userLocations,
            HashSet<int> savedJobIds,
            HashSet<int> appliedJobIds)
        {
            int suitability = 0;
            // Chỉ tính phù hợp nếu userProfile và các dữ liệu liên quan tồn tại
            if (userProfile != null && userLocations != null && userSchedule != null)
            {
                suitability = CalculateSuitability(t, userProfile, userSchedule, userLocations);
            }

            return new JobPostingSearchResultViewModel
            {
                Id = t.Id,
                TieuDe = t.TieuDe,
                // Lấy tên công ty nếu là doanh nghiệp, ngược lại lấy họ tên người đăng nếu là cá nhân
                TenCongTy = (t.NguoiDang?.LoaiTk == LoaiTaiKhoan.doanhnghiep ? t.NguoiDang?.HoSoDoanhNghiep?.TenCongTy : t.NguoiDang?.HoTen) ?? "Nhà tuyển dụng",
                UrlLogoCongTy = t.NguoiDang?.HoSoDoanhNghiep?.UrlLogo, // Chỉ có logo cho DN
                DiaDiem = $"{(t.QuanHuyen?.Ten ?? "")}, {(t.ThanhPho?.Ten ?? "")}".Trim(',', ' '),
                ThongTinLuong = GetFormattedSalary(t),
                LoaiHinhCongViec = t.LoaiHinhCongViec,
                Tags = GenerateTags(t),
                PhuHopPercent = suitability, // Đã tính toán ở trên
                DaLuu = savedJobIds.Contains(t.Id),
                DaUngTuyen = appliedJobIds.Contains(t.Id),
                NgayDang = t.NgayDang
            };
        }

        // Hàm tính điểm phù hợp (Ví dụ Đơn giản)
        private int CalculateSuitability(TinTuyenDung job, HoSoUngVien userProfile, List<LichRanhUngVien> userSchedule, List<DiaDiemMongMuon> userLocations)
        {
            // Logic tính phù hợp dựa trên Địa điểm, Lịch làm việc, Mức lương, Vị trí mong muốn
            // KHÔNG sử dụng NganhNgheMongMuonIds ở đây.

            int score = 0;
            int currentMaxScore = 0; // Tổng điểm tối đa có thể đạt dựa trên thông tin user cung cấp

            // 1. Địa điểm (Trọng số: 30) - Chỉ tính nếu user có Địa điểm mong muốn
            if (userLocations.Any()) // Chỉ xét nếu user có địa điểm mong muốn
            {
                currentMaxScore += 30;
                bool locationMatch = userLocations.Any(loc => loc.QuanHuyenId == job.QuanHuyenId);
                if (locationMatch) { score += 30; }
                else { bool cityMatch = userLocations.Any(loc => loc.ThanhPhoId == job.ThanhPhoId); if (cityMatch) { score += 15; } }
            }


            // 2. Lịch làm việc (Trọng số: 30) - Chỉ tính nếu user có Lịch rảnh
            if (userSchedule.Any()) // Chỉ xét nếu user có lịch rảnh
            {
                currentMaxScore += 30;
                if (job.LichLamViecCongViecs.Any()) // Chỉ so khớp nếu Job có lịch cụ thể
                {
                    if (job.LichLamViecCongViecs.Any(l => l.NgayTrongTuan == NgayTrongTuan.ngaylinhhoat || l.BuoiLamViec == BuoiLamViec.linhhoat)) { score += 25; } // Job linh hoạt
                    else
                    {
                        int matchingSlots = 0;
                        foreach (var jobSlot in job.LichLamViecCongViecs)
                        {
                            bool canWorkSlot = userSchedule.Any(userSlot =>
                               (userSlot.NgayTrongTuan == jobSlot.NgayTrongTuan || userSlot.NgayTrongTuan == NgayTrongTuan.ngaylinhhoat)
                               && (userSlot.BuoiLamViec == jobSlot.BuoiLamViec || userSlot.BuoiLamViec == BuoiLamViec.cangay || userSlot.BuoiLamViec == BuoiLamViec.linhhoat || jobSlot.BuoiLamViec == BuoiLamViec.linhhoat || jobSlot.BuoiLamViec == BuoiLamViec.cangay)
                           );
                            if (canWorkSlot) { matchingSlots++; }
                        }
                        if (matchingSlots > 0) { score += Math.Min(30, (int)Math.Round((double)matchingSlots / job.LichLamViecCongViecs.Count * 30)); }
                    }
                }
                else { score += 5; } // Job không có lịch cụ thể
            }


            // 3. Mức lương (Trọng số: 20) - Chỉ tính nếu user có Mức lương mong muốn
            if (userProfile.MucLuongMongMuon.HasValue && userProfile.MucLuongMongMuon > 0 && userProfile.LoaiLuongMongMuon.HasValue)
            {
                currentMaxScore += 20;
                if (job.LoaiLuong == userProfile.LoaiLuongMongMuon.Value)
                {
                    if (job.LuongToiThieu.HasValue && job.LuongToiThieu.Value >= userProfile.MucLuongMongMuon.Value) { score += 20; }
                    else if (job.LuongToiDa.HasValue && job.LuongToiDa.Value >= userProfile.MucLuongMongMuon.Value) { score += 10; }
                }
                else if (job.LoaiLuong == LoaiLuong.thoathuan) { score += 5; }
            }

            // 4. Vị trí mong muốn / Tiêu đề tin (Trọng số: 20) - Chỉ tính nếu user có Vị trí mong muốn
            if (!string.IsNullOrWhiteSpace(userProfile.ViTriMongMuon))
            {
                currentMaxScore += 20;
                string desiredPosLower = userProfile.ViTriMongMuon.ToLower();
                if (job.TieuDe.ToLower().Contains(desiredPosLower)) { score += 20; }
            }

            // Chuẩn hóa về thang điểm 100 dựa trên các tiêu chí thực sự được tính
            if (currentMaxScore == 0) return 0; // Tránh chia cho 0

            int finalScore = (score * 100) / currentMaxScore;

            return Math.Min(100, finalScore);
        }

        private string GetFormattedSalary(TinTuyenDung t)
        {
            switch (t.LoaiLuong)
            {
                case LoaiLuong.theogio:
                case LoaiLuong.theongay:
                case LoaiLuong.theoca:
                case LoaiLuong.theothang:
                    string unit = t.LoaiLuong == LoaiLuong.theogio ? "giờ" : t.LoaiLuong == LoaiLuong.theongay ? "ngày" : t.LoaiLuong == LoaiLuong.theoca ? "ca" : "tháng";
                    string prefix = (t.LoaiLuong == LoaiLuong.theogio || t.LoaiLuong == LoaiLuong.theongay || t.LoaiLuong == LoaiLuong.theoca) ? " nghìn" : " triệu";
                    string format = (t.LoaiLuong == LoaiLuong.theogio || t.LoaiLuong == LoaiLuong.theongay || t.LoaiLuong == LoaiLuong.theoca) ? "N0" : "N1";

                    ulong factor = (t.LoaiLuong == LoaiLuong.theogio || t.LoaiLuong == LoaiLuong.theongay || t.LoaiLuong == LoaiLuong.theoca) ? 1UL : 1000UL;

                    double? luongMinFormatted = t.LuongToiThieu.HasValue ? (double?)t.LuongToiThieu.Value / factor : null;
                    double? luongMaxFormatted = t.LuongToiDa.HasValue ? (double?)t.LuongToiDa.Value / factor : null;

                    if (luongMinFormatted.HasValue && luongMaxFormatted.HasValue && luongMaxFormatted > luongMinFormatted)
                        // FIX CS0103: Sửa luangMaxFormatted -> luongMaxFormatted
                        return $"{luongMinFormatted.Value.ToString(format)} - {luongMaxFormatted.Value.ToString(format)}{prefix}/{unit}";
                    if (luongMinFormatted.HasValue)
                        return $"{luongMinFormatted.Value.ToString(format)}{prefix}/{unit}";
                    // FIX CS0103 & CS8629: Thêm kiểm tra HasValue và sửa lỗi chính tả
                    if (luongMaxFormatted.HasValue)
                        return $"Lên đến {luongMaxFormatted.Value.ToString(format)}{prefix}/{unit}";
                    return $"Theo {unit} (Chưa rõ)";
                case LoaiLuong.thoathuan: return "Lương thỏa thuận";
                case LoaiLuong.theoduan: return "Lương theo dự án";
                default: return "Chưa cập nhật";
            }
        }

        private List<string> GenerateTags(TinTuyenDung t)
        {
            var tags = new List<string>();
            if (t.YeuCauKinhNghiemText != null && (t.YeuCauKinhNghiemText.ToLower().Contains("sinh viên") || t.YeuCauKinhNghiemText.ToLower().Contains("không yêu cầu"))) tags.Add("Không yêu cầu kinh nghiệm");
            if (t.YeuCau != null)
            {
                if (t.YeuCau.ToLower().Contains("lái xe")) tags.Add("Cần bằng lái");
                if (t.YeuCau.ToLower().Contains("ngoại hình")) tags.Add("Ưa nhìn");
                if (t.YeuCau.ToLower().Contains("giao tiếp")) tags.Add("Giao tiếp tốt");
            }
            // Tags từ Ngành nghề (có thể tinh chỉnh)
            if (t.TinTuyenDungNganhNghes.Any(nn => nn.NganhNghe?.Ten != null && nn.NganhNghe.Ten.ToLower().Contains("bán hàng"))) tags.Add("Bán hàng");
            if (t.TinTuyenDungNganhNghes.Any(nn => nn.NganhNghe?.Ten != null && nn.NganhNghe.Ten.ToLower().Contains("phục vụ"))) tags.Add("Phục vụ / F&B");
            if (t.TinTuyenDungNganhNghes.Any(nn => nn.NganhNghe?.Ten != null && nn.NganhNghe.Ten.ToLower().Contains("nhập liệu"))) tags.Add("Nhập liệu");
            if (t.TinGap) tags.Add("Tuyển gấp");

            return tags.Distinct().Take(4).ToList();
        }

        private string GetEnumDisplayName(Enum enumValue)
        {
            switch (enumValue)
            {
                case LoaiHinhCongViec.banthoigian: return "Bán thời gian";
                case LoaiHinhCongViec.thoivu: return "Thời vụ";
                case LoaiHinhCongViec.linhhoatkhac: return "Linh hoạt khác";
                case LoaiLuong.theogio: return "Theo giờ";
                case LoaiLuong.theongay: return "Theo ngày";
                case LoaiLuong.theoca: return "Theo ca";
                case LoaiLuong.theothang: return "Theo tháng";
                case LoaiLuong.thoathuan: return "Thỏa thuận";
                case LoaiLuong.theoduan: return "Theo dự án";
                case NgayTrongTuan.thu2: return "Thứ 2";
                case NgayTrongTuan.thu3: return "Thứ 3";
                case NgayTrongTuan.thu4: return "Thứ 4";
                case NgayTrongTuan.thu5: return "Thứ 5";
                case NgayTrongTuan.thu6: return "Thứ 6";
                case NgayTrongTuan.thu7: return "Thứ 7";
                case NgayTrongTuan.chunhat: return "Chủ Nhật";
                case NgayTrongTuan.ngaylinhhoat: return "Ngày linh hoạt";
                case BuoiLamViec.sang: return "Buổi Sáng";
                case BuoiLamViec.chieu: return "Buổi Chiều";
                case BuoiLamViec.toi: return "Buổi Tối";
                case BuoiLamViec.cangay: return "Cả ngày";
                case BuoiLamViec.linhhoat: return "Buổi linh hoạt";
                default: return enumValue.ToString();
            }
        }
        // *** ACTION MỚI ĐỂ XỬ LÝ LƯU/BỎ LƯU TIN ***
        [HttpPost] // Chỉ cho phép POST
        [Authorize] // Yêu cầu đăng nhập
        [ValidateAntiForgeryToken] // Kiểm tra token bảo mật
        public async Task<IActionResult> ToggleSaveJob(int tinTuyenDungId)
        {
            // 1. Lấy UserId của người dùng hiện tại
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return Json(new { success = false, message = "Lỗi xác thực người dùng." });
            }

            // 2. Kiểm tra xem Tin Tuyển Dụng có tồn tại và đang hiển thị không
            var jobExists = await _context.TinTuyenDungs.AnyAsync(t => t.Id == tinTuyenDungId && t.TrangThai == TrangThaiTinTuyenDung.daduyet);
            if (!jobExists)
            {
                return Json(new { success = false, message = "Tin tuyển dụng không tồn tại hoặc đã bị ẩn." });
            }

            // 3. Tìm bản ghi TinDaLuu hiện có
            var existingSave = await _context.TinDaLuus
                                        .FirstOrDefaultAsync(l => l.NguoiDungId == userId && l.TinTuyenDungId == tinTuyenDungId);

            bool isCurrentlySaved;

            try
            {
                if (existingSave != null)
                {
                    // Đã lưu -> Thực hiện Bỏ lưu
                    _context.TinDaLuus.Remove(existingSave);
                    await _context.SaveChangesAsync();
                    isCurrentlySaved = false; // Trạng thái mới là CHƯA LƯU
                    _logger.LogInformation("User {UserId} đã bỏ lưu TinTuyenDung {TinTuyenDungId}", userId, tinTuyenDungId);
                }
                else
                {
                    // Chưa lưu -> Thực hiện Lưu mới
                    var newSave = new TinDaLuu
                    {
                        NguoiDungId = userId,
                        TinTuyenDungId = tinTuyenDungId,
                        NgayLuu = DateTime.UtcNow // Ghi lại ngày lưu
                    };
                    _context.TinDaLuus.Add(newSave);
                    await _context.SaveChangesAsync();
                    isCurrentlySaved = true; // Trạng thái mới là ĐÃ LƯU
                    _logger.LogInformation("User {UserId} đã lưu TinTuyenDung {TinTuyenDungId}", userId, tinTuyenDungId);
                }

                // Trả về kết quả thành công và trạng thái lưu mới cho JavaScript
                return Json(new { success = true, isSaved = isCurrentlySaved });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi User {UserId} thực hiện lưu/bỏ lưu TinTuyenDung {TinTuyenDungId}", userId, tinTuyenDungId);
                return Json(new { success = false, message = "Đã có lỗi xảy ra phía máy chủ, vui lòng thử lại." });
            }
        }
    }
}