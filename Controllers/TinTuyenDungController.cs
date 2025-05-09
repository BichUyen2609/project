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
using System.Security.Claims; // Required for ClaimTypes
// using Microsoft.Extensions.Logging; // If logging is needed

namespace HeThongTimViec.Controllers.Admin
{
    [Authorize(Roles = nameof(LoaiTaiKhoan.quantrivien))]
    [Route("admin/tintuyendung")]
    public class TinTuyenDungController : Controller
    {
        private readonly ApplicationDbContext _context;
        // private readonly ILogger<TinTuyenDungController> _logger;
        private const int PageSize = 10;

        public TinTuyenDungController(ApplicationDbContext context) //, ILogger<TinTuyenDungController> logger)
        {
            _context = context;
            // _logger = logger;
        }

        private int? GetCurrentAdminId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            // Log warning or handle error if admin ID cannot be retrieved
            // _logger?.LogWarning("Could not retrieve current admin ID. User.FindFirstValue(ClaimTypes.NameIdentifier) was '{UserIdString}'.", userIdString);
            return null;
        }

        [HttpGet] // Default action for the controller route
        [HttpGet("index")] // Explicit route for Index
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? selectedNganhNgheId,
            int? selectedThanhPhoId,
            TrangThaiTinTuyenDung? selectedTrangThai,
            string? activeTab,
            string? viewMode,
            int pageIndex = 1)
        {
            var viewModel = new AdminJobPostIndexViewModel
            {
                SearchTerm = searchTerm,
                SelectedNganhNgheId = selectedNganhNgheId,
                SelectedThanhPhoId = selectedThanhPhoId,
                SelectedTrangThai = selectedTrangThai,
                ActiveTab = activeTab ?? "all",
                ViewMode = viewMode ?? "list", // Default to "list" view
                PageIndex = pageIndex
            };

            // Populate filter dropdowns
            viewModel.NganhNgheOptions.Add(new SelectListItem("Tất cả ngành nghề", string.Empty));
            viewModel.NganhNgheOptions.AddRange(await _context.NganhNghes
                .OrderBy(n => n.Ten)
                .Select(n => new SelectListItem(n.Ten, n.Id.ToString()))
                .ToListAsync());

            viewModel.ThanhPhoOptions.Add(new SelectListItem("Tất cả thành phố", string.Empty));
            viewModel.ThanhPhoOptions.AddRange(await _context.ThanhPhos
                .OrderBy(tp => tp.Ten)
                .Select(tp => new SelectListItem(tp.Ten, tp.Id.ToString()))
                .ToListAsync());
            
            // TrangThaiOptions are populated in AdminJobPostIndexViewModel's constructor

            var query = _context.TinTuyenDungs
                                .Include(t => t.NguoiDang)
                                    .ThenInclude(nd => nd.HoSoDoanhNghiep) // For TenCongTy, Logo
                                .Include(t => t.ThanhPho) // For DiaDiem
                                .Include(t => t.QuanHuyen) // Potentially useful, though not in ItemViewModel
                                .Include(t => t.TinTuyenDungNganhNghes)
                                    .ThenInclude(tnn => tnn.NganhNghe) // For NganhNghes list
                                .Include(t => t.UngTuyens) // For SoUngVien count
                                .Include(t => t.AdminDuyet) // For admin who approved/rejected
                                .OrderByDescending(t => t.NgayDang) // Default sort
                                .AsQueryable();

            // --- Calculate Statistics ---
            // Ensure all statistics queries respect the same base conditions (e.g., not deleted, unless specified)
            var allJobsQueryForStats = _context.TinTuyenDungs.AsQueryable();
            
            // Total jobs (excluding explicitly deleted ones, unless the view is for deleted items)
            viewModel.Statistics.TotalJobs = await allJobsQueryForStats
                .CountAsync(t => t.TrangThai != TrangThaiTinTuyenDung.daxoa);
            viewModel.Statistics.TotalJobsChangeText = "N/A"; // Placeholder

            DateTime now = DateTime.UtcNow; // Or DateTime.Now if server/data is local time based
                                            // Using .Date for comparisons effectively treats NgayHetHan as EOD
            
            viewModel.Statistics.ActiveJobs = await allJobsQueryForStats
                .CountAsync(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet && 
                                 (t.NgayHetHan == null || t.NgayHetHan.Value.Date >= now.Date) &&
                                 t.TrangThai != TrangThaiTinTuyenDung.daxoa);
            viewModel.Statistics.ActiveJobsChangeText = "N/A";

            viewModel.Statistics.PendingJobs = await allJobsQueryForStats
                .CountAsync(t => t.TrangThai == TrangThaiTinTuyenDung.choduyet &&
                                 t.TrangThai != TrangThaiTinTuyenDung.daxoa);
            viewModel.Statistics.PendingJobsChangeText = "N/A";

            viewModel.Statistics.ExpiredOrRejectedJobs = await allJobsQueryForStats
                .CountAsync(t => (t.TrangThai == TrangThaiTinTuyenDung.hethan ||
                                  t.TrangThai == TrangThaiTinTuyenDung.bituchoi ||
                                  (t.TrangThai == TrangThaiTinTuyenDung.daduyet && t.NgayHetHan != null && t.NgayHetHan.Value.Date < now.Date)) &&
                                 t.TrangThai != TrangThaiTinTuyenDung.daxoa);
            viewModel.Statistics.ExpiredOrRejectedJobsChangeText = "N/A";


            // --- Apply Filters based on Active Tab ---
            // This logic assumes activeTab filters first, then other filters are applied on top.
            switch (viewModel.ActiveTab.ToLower())
            {
                case "pending":
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.choduyet);
                    viewModel.SelectedTrangThai = TrangThaiTinTuyenDung.choduyet; // Reflect in dropdown
                    break;
                case "active":
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet && 
                                             (t.NgayHetHan == null || t.NgayHetHan.Value.Date >= now.Date));
                    // Do not set viewModel.SelectedTrangThai for "active" tab directly, 
                    // as it's a computed state. User might still filter by "Đã duyệt" in dropdown.
                    break;
                case "expired": // This tab shows items matching the "ExpiredOrRejectedJobs" stat
                    query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.hethan ||
                                             t.TrangThai == TrangThaiTinTuyenDung.bituchoi ||
                                             (t.TrangThai == TrangThaiTinTuyenDung.daduyet && t.NgayHetHan != null && t.NgayHetHan.Value.Date < now.Date));
                    break;
                case "all":
                default:
                    // For "all" tab, if a specific status is selected in the dropdown, apply it.
                    // This overrides the tab if a dropdown filter is chosen.
                    if (selectedTrangThai.HasValue)
                    {
                        // Handle special dropdown values that map to computed states
                        if (selectedTrangThai.Value == TrangThaiTinTuyenDung.daduyet) // Interpreted as "Đang hoạt động"
                        {
                             query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.daduyet && 
                                                      (t.NgayHetHan == null || t.NgayHetHan.Value.Date >= now.Date));
                        }
                        else if (selectedTrangThai.Value == TrangThaiTinTuyenDung.hethan) // Interpreted as "Đã hết hạn"
                        {
                            query = query.Where(t => t.TrangThai == TrangThaiTinTuyenDung.hethan ||
                                                     (t.TrangThai == TrangThaiTinTuyenDung.daduyet && t.NgayHetHan != null && t.NgayHetHan.Value.Date < now.Date));
                            // If "Đã hết hạn" from dropdown should also include "Bị từ chối", add:
                            // || t.TrangThai == TrangThaiTinTuyenDung.bituchoi
                        }
                        else // Standard enum value from dropdown
                        {
                            query = query.Where(t => t.TrangThai == selectedTrangThai.Value);
                        }
                    }
                    break;
            }

            // Apply general search and filters (these apply on top of tab filters or 'all' with dropdown)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string st = searchTerm.ToLower();
                query = query.Where(t => (t.TieuDe != null && t.TieuDe.ToLower().Contains(st)) ||
                                         (t.NguoiDang.HoSoDoanhNghiep != null && t.NguoiDang.HoSoDoanhNghiep.TenCongTy != null && t.NguoiDang.HoSoDoanhNghiep.TenCongTy.ToLower().Contains(st)) ||
                                         (t.MoTa != null && t.MoTa.ToLower().Contains(st)) ||
                                         (t.Id.ToString() == st) // Allow searching by ID
                                         );
            }

            if (selectedNganhNgheId.HasValue && selectedNganhNgheId > 0)
            {
                query = query.Where(t => t.TinTuyenDungNganhNghes.Any(tnn => tnn.NganhNgheId == selectedNganhNgheId.Value));
            }

            if (selectedThanhPhoId.HasValue && selectedThanhPhoId > 0)
            {
                query = query.Where(t => t.ThanhPhoId == selectedThanhPhoId.Value);
            }
            
            // By default, exclude "daxoa" status unless a specific tab or filter implies showing them (not implemented here)
            if (viewModel.ActiveTab.ToLower() != "archived") // Assuming no "archived" tab for now
            {
                 query = query.Where(t => t.TrangThai != TrangThaiTinTuyenDung.daxoa);
            }


            // --- Pagination ---
            viewModel.TotalItems = await query.CountAsync();
            viewModel.TotalPages = (int)Math.Ceiling(viewModel.TotalItems / (double)PageSize);
            // Ensure pageIndex is valid
            pageIndex = Math.Max(1, Math.Min(pageIndex, viewModel.TotalPages == 0 ? 1 : viewModel.TotalPages));
            viewModel.PageIndex = pageIndex;

            var jobPostsData = await query
                                .Skip((pageIndex - 1) * PageSize)
                                .Take(PageSize)
                                .ToListAsync();

            viewModel.JobPosts = jobPostsData.Select(t => new AdminJobPostItemViewModel
            {
                Id = t.Id,
                TieuDe = t.TieuDe,
                SoUngVien = t.UngTuyens?.Count ?? 0,
                TenCongTy = t.NguoiDang?.HoSoDoanhNghiep?.TenCongTy ?? (t.NguoiDang?.HoTen ?? "N/A"),
                LogoCongTyUrl = t.NguoiDang?.HoSoDoanhNghiep?.UrlLogo ?? t.NguoiDang?.UrlAvatar,
                NganhNghes = t.TinTuyenDungNganhNghes?.Select(tnn => tnn.NganhNghe.Ten).ToList() ?? new List<string>(),
                DiaDiem = t.ThanhPho?.Ten ?? "N/A",
                MucLuongText = FormatMucLuong(t.LuongToiThieu, t.LuongToiDa, t.LoaiLuong),
                TrangThai = t.TrangThai,
                NgayDang = t.NgayDang,
                NgayHetHan = t.NgayHetHan,
                TinGap = t.TinGap
            }).ToList();

            return View(viewModel);
        }

        private string FormatMucLuong(ulong? min, ulong? max, LoaiLuong loai)
        {
            if (loai == LoaiLuong.thoathuan) return "Thỏa thuận";

            string unit = " VNĐ";
            bool isMillion = false;
            
            // Check if values are large enough to be displayed as millions
            // Convert to "triệu" only if it makes sense for the range
            bool minIsMillionRange = min.HasValue && min >= 1000000;
            bool maxIsMillionRange = max.HasValue && max >= 1000000;

            if ((min.HasValue && minIsMillionRange) || (max.HasValue && maxIsMillionRange))
            {
                // If one is in millions, and the other exists but is not, it's messy.
                // Prefer to show both in VNĐ or both in triệu.
                // If both exist, they both need to be in million range to use "triệu"
                if (min.HasValue && max.HasValue) {
                    if (minIsMillionRange && maxIsMillionRange) isMillion = true;
                } else if (min.HasValue && minIsMillionRange) { // Only min exists
                    isMillion = true;
                } else if (max.HasValue && maxIsMillionRange) { // Only max exists
                    isMillion = true;
                }
            }
            
            if (isMillion) unit = " triệu";

            Func<ulong?, string> formatVal = v =>
            {
                if (!v.HasValue) return "?";
                // Using "vi-VN" culture for dot as thousands separator
                var culture = new System.Globalization.CultureInfo("vi-VN"); 
                return isMillion ? (v.Value / 1000000.0).ToString("0.#", culture) : v.Value.ToString("N0", culture);
            };

            string suffix = $"/{GetLoaiLuongSuffix(loai)}";
            
            if (min.HasValue && max.HasValue)
            {
                if (min == max) return $"{formatVal(min)}{unit}{suffix}";
                return $"{formatVal(min)} - {formatVal(max)}{unit}{suffix}";
            }
            if (min.HasValue) return $"Từ {formatVal(min)}{unit}{suffix}";
            if (max.HasValue) return $"Đến {formatVal(max)}{unit}{suffix}";
            
            return $"Theo {GetLoaiLuongSuffix(loai)}"; // Fallback
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
                _ => loai.ToString().ToLower() // Fallback, though ThoaThuan is handled earlier
            };
        }


        [HttpPost("approve/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var tinTuyenDung = await _context.TinTuyenDungs.FindAsync(id);
            var adminId = GetCurrentAdminId();

            if (tinTuyenDung == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }
            if (!adminId.HasValue)
            {
                TempData["ErrorMessage"] = "Không thể xác định quản trị viên.";
                return RedirectToAction(nameof(Index));
            }

            if (tinTuyenDung.TrangThai == TrangThaiTinTuyenDung.choduyet)
            {
                tinTuyenDung.TrangThai = TrangThaiTinTuyenDung.daduyet;
                tinTuyenDung.AdminDuyetId = adminId.Value;
                tinTuyenDung.NgayDuyet = DateTime.UtcNow;
                tinTuyenDung.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tin tuyển dụng ID {id} đã được duyệt thành công.";
            }
            else
            {
                TempData["WarningMessage"] = $"Tin tuyển dụng ID {id} không ở trạng thái 'Chờ duyệt'.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("reject/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var tinTuyenDung = await _context.TinTuyenDungs.FindAsync(id);
            var adminId = GetCurrentAdminId();

            if (tinTuyenDung == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }
            if (!adminId.HasValue)
            {
                TempData["ErrorMessage"] = "Không thể xác định quản trị viên.";
                return RedirectToAction(nameof(Index));
            }

            if (tinTuyenDung.TrangThai == TrangThaiTinTuyenDung.choduyet || tinTuyenDung.TrangThai == TrangThaiTinTuyenDung.daduyet)
            {
                tinTuyenDung.TrangThai = TrangThaiTinTuyenDung.bituchoi;
                tinTuyenDung.AdminDuyetId = adminId.Value; // Admin who rejected
                tinTuyenDung.NgayDuyet = DateTime.UtcNow; // Or a new field NgayTuChoi
                tinTuyenDung.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tin tuyển dụng ID {id} đã bị từ chối.";
            }
            else
            {
                TempData["WarningMessage"] = $"Không thể từ chối tin tuyển dụng ID {id} ở trạng thái hiện tại.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id) // Soft delete
        {
            var tinTuyenDung = await _context.TinTuyenDungs.FindAsync(id);
             var adminId = GetCurrentAdminId(); // For audit log if needed, or to set AdminDuyetId

            if (tinTuyenDung == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }
            if (!adminId.HasValue && (tinTuyenDung.TrangThai == TrangThaiTinTuyenDung.choduyet || tinTuyenDung.TrangThai == TrangThaiTinTuyenDung.daduyet))
            {
                // Only strictly require adminId if the delete implies an admin action like overriding a pending/approved state
                // For already expired/rejected, adminId might not be strictly needed for just marking as 'daxoa'
                // However, for consistency, it's good to record who performed the soft delete.
                // TempData["ErrorMessage"] = "Không thể xác định quản trị viên.";
                // return RedirectToAction(nameof(Index));
            }


            if (tinTuyenDung.TrangThai != TrangThaiTinTuyenDung.daxoa)
            {
                tinTuyenDung.TrangThai = TrangThaiTinTuyenDung.daxoa;
                // Optionally, set AdminDuyetId to the admin who performed the delete
                // tinTuyenDung.AdminDuyetId = adminId.HasValue ? adminId.Value : (int?)null;
                // tinTuyenDung.NgayDuyet = DateTime.UtcNow; // Or a specific NgayXoa field
                tinTuyenDung.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Tin tuyển dụng ID {id} đã được xóa (ẩn).";
            }
            else
            {
                TempData["WarningMessage"] = $"Tin tuyển dụng ID {id} đã ở trạng thái xóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Placeholder for other actions if needed
        // [HttpGet("details/{id:int}")]
        // public async Task<IActionResult> Details(int id) { /* ... */ return View(); }

        // [HttpGet("edit/{id:int}")]
        // public async Task<IActionResult> Edit(int id) { /* ... */ return View(); }

        // [HttpPost("edit/{id:int}")]
        // [ValidateAntiForgeryToken]
        // public async Task<IActionResult> Edit(int id, YourEditViewModel vm) { /* ... */ return RedirectToAction(nameof(Index)); }
    }
}