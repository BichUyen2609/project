using HeThongTimViec.Data;
using HeThongTimViec.Models;
using HeThongTimViec.Views.ViewModels.Admin.JobPosts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HeThongTimViec.Controllers.Admin // Adjust namespace if you have an Admin area
{
    // [Area("Admin")] // If using areas
    // [Authorize(Roles = "quantrivien")] // Add authorization
    [Authorize(Roles = nameof(LoaiTaiKhoan.quantrivien))] // Chỉ Admin mới vào được
    [Route("admin/tintuyendung")] // Định tuyến base cho Controller
    public class TinTuyenDungController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminUserController> _logger;
        private const int PageSize = 10; // Số lượng user trên mỗi trang
       public TinTuyenDungController(ApplicationDbContext context) 
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? searchTerm,
            int? selectedNganhNgheId,
            int? selectedThanhPhoId,
            TrangThaiTinTuyenDung? selectedTrangThai,
            string? activeTab,
            int pageIndex = 1)
        {
            var viewModel = new AdminJobPostIndexViewModel
            {
                SearchTerm = searchTerm,
                SelectedNganhNgheId = selectedNganhNgheId,
                SelectedThanhPhoId = selectedThanhPhoId,
                SelectedTrangThai = selectedTrangThai,
                ActiveTab = activeTab ?? "all", // Default to "all"
                PageIndex = pageIndex
            };

            // Populate filter dropdowns
            viewModel.NganhNgheOptions.Add(new SelectListItem("Tất cả danh mục", ""));
            viewModel.NganhNgheOptions.AddRange(await _context.NganhNghes
                .OrderBy(n => n.Ten)
                .Select(n => new SelectListItem(n.Ten, n.Id.ToString()))
                .ToListAsync());

            viewModel.ThanhPhoOptions.Add(new SelectListItem("Tất cả địa điểm", ""));
            viewModel.ThanhPhoOptions.AddRange(await _context.ThanhPhos
                .OrderBy(tp => tp.Ten)
                .Select(tp => new SelectListItem(tp.Ten, tp.Id.ToString()))
                .ToListAsync());

            // Base query
            var query = _context.TinTuyenDungs
                                .Include(t => t.NguoiDang)
                                    .ThenInclude(nd => nd.HoSoDoanhNghiep) // For TenCongTy, Logo
                                .Include(t => t.ThanhPho)
                                .Include(t => t.TinTuyenDungNganhNghes)
                                    .ThenInclude(tnn => tnn.NganhNghe)
                                .Include(t => t.UngTuyens) // For SoUngVien
                                .OrderByDescending(t => t.NgayDang)
                                .AsQueryable();

            // --- Calculate Statistics (before specific tab/status filtering for some stats) ---
            var allJobsQuery = _context.TinTuyenDungs.AsQueryable(); // Separate query for broad stats
            viewModel.Statistics.TotalJobs = await allJobsQuery.CountAsync();
            // Placeholder for change text - implement actual logic if needed
            viewModel.Statistics.TotalJobsChangeText = "+0% so với tháng trước";


            DateTime now = DateTime.UtcNow;
            viewModel.Statistics.ActiveJobs = await allJobsQuery
                .CountAsync(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet && (t.NgayHetHan == null || t.NgayHetHan >= now));
            viewModel.Statistics.ActiveJobsChangeText = "+0% so với tháng trước";

            viewModel.Statistics.PendingJobs = await allJobsQuery
                .CountAsync(t => t.TrangThai == TrangThaiTinTuyenDung.choduyet);
            viewModel.Statistics.PendingJobsChangeText = "+0% so với tháng trước";

            viewModel.Statistics.ExpiredOrRejectedJobs = await allJobsQuery
                .CountAsync(t => t.TrangThai == TrangThaiTinTuyenDung.hethan ||
                                 t.TrangThai == TrangThaiTinTuyenDung.bituchoi ||
                                 (t.TrangThai == TrangThaiTinTuyenDung.daduyet && t.NgayHetHan != null && t.NgayHetHan < now));
            viewModel.Statistics.ExpiredOrRejectedJobsChangeText = "+0% so với tháng trước";


            // --- Apply Filters ---
            // Tab-based filtering takes precedence for status
            switch (viewModel.ActiveTab?.ToLower())
            {
                case "pending":
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.choduyet);
                    viewModel.SelectedTrangThai = TrangThaiTinTuyenDung.choduyet; // Reflect in dropdown
                    break;
                case "active":
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet && (t.NgayHetHan == null || t.NgayHetHan >= now));
                    // viewModel.SelectedTrangThai = TrangThaiTinTuyenDung.daduyet; // Don't set this as 'daduyet' also includes expired
                    break;
                case "expired":
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.hethan ||
                                             t.TrangThai == TrangThaiTinTuyenDung.bituchoi ||
                                             (t.TrangThai == TrangThaiTinTuyenDung.daduyet && t.NgayHetHan != null && t.NgayHetHan < now));
                    // viewModel.SelectedTrangThai = TrangThaiTinTuyenDung.hethan; // Or a custom "expired" value
                    break;
                case "all":
                default:
                    if (selectedTrangThai.HasValue)
                    {
                        query = query.Where(t => t.TrangThai == selectedTrangThai.Value);
                    }
                    break;
            }


            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.TieuDe.Contains(searchTerm) ||
                                         (t.NguoiDang.HoSoDoanhNghiep != null && t.NguoiDang.HoSoDoanhNghiep.TenCongTy.Contains(searchTerm)) ||
                                         t.MoTa.Contains(searchTerm));
            }

            if (selectedNganhNgheId.HasValue)
            {
                query = query.Where(t => t.TinTuyenDungNganhNghes.Any(tnn => tnn.NganhNgheId == selectedNganhNgheId.Value));
            }

            if (selectedThanhPhoId.HasValue)
            {
                query = query.Where(t => t.ThanhPhoId == selectedThanhPhoId.Value);
            }

            // --- Pagination ---
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalItems / (double)PageSize);
            pageIndex = Math.Max(1, Math.Min(pageIndex, viewModel.TotalPages == 0 ? 1 : viewModel.TotalPages)); // Ensure pageIndex is valid
            viewModel.PageIndex = pageIndex;


            var jobPostsData = await query
                                .Skip((pageIndex - 1) * PageSize)
                                .Take(PageSize)
                                .ToListAsync();

            viewModel.JobPosts = jobPostsData.Select(t => new AdminJobPostItemViewModel
            {
                Id = t.Id,
                TieuDe = t.TieuDe,
                SoUngVien = t.UngTuyens.Count,
                TenCongTy = t.NguoiDang?.HoSoDoanhNghiep?.TenCongTy ?? "N/A",
                LogoCongTyUrl = t.NguoiDang?.HoSoDoanhNghiep?.UrlLogo,
                NganhNghes = t.TinTuyenDungNganhNghes.Select(tnn => tnn.NganhNghe.Ten).ToList(),
                DiaDiem = t.ThanhPho.Ten,
                MucLuongText = FormatMucLuong(t.LuongToiThieu, t.LuongToiDa, t.LoaiLuong),
                TrangThai = t.TrangThai, // Store original status
                NgayDang = t.NgayDang,
                NgayHetHan = t.NgayHetHan,
                TinGap = t.TinGap
            }).ToList();

            return View(viewModel);
        }

        private string FormatMucLuong(ulong? min, ulong? max, LoaiLuong loai)
        {
            if (loai == LoaiLuong.thoathuan) return "Thỏa thuận";

            string unit = " VNĐ"; // Or " triệu" if numbers are large
            bool isMillion = false;

            if ((min.HasValue && min >= 1000000) || (max.HasValue && max >= 1000000))
            {
                unit = " triệu";
                isMillion = true;
            }

            Func<ulong?, string> formatVal = v =>
            {
                if (!v.HasValue) return "?";
                return isMillion ? (v.Value / 1000000.0).ToString("0.#") : v.Value.ToString("N0");
            };

            if (min.HasValue && max.HasValue)
            {
                if (min == max) return $"{formatVal(min)}{unit}/{GetLoaiLuongSuffix(loai)}";
                return $"{formatVal(min)} - {formatVal(max)}{unit}/{GetLoaiLuongSuffix(loai)}";
            }
            if (min.HasValue) return $"Từ {formatVal(min)}{unit}/{GetLoaiLuongSuffix(loai)}";
            if (max.HasValue) return $"Đến {formatVal(max)}{unit}/{GetLoaiLuongSuffix(loai)}";

            return $"Theo {GetLoaiLuongSuffix(loai)}";
        }

        private string GetLoaiLuongSuffix(LoaiLuong loai)
        {
            return loai switch
            {
                LoaiLuong.theogio => "giờ",
                LoaiLuong.theongay => "ngày",
                LoaiLuong.theoca => "ca",
                LoaiLuong.theothang => "tháng",
                LoaiLuong.theoduan => "dự án",
                _ => "thỏa thuận"
            };
        }

        // Placeholder for other actions like Edit, Delete, Approve, Reject
        // [HttpPost]
        // public async Task<IActionResult> Approve(int id) { ... }
    }
}