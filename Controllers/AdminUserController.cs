// File: Controllers/AdminUserController.cs
using HeThongTimViec.Data;
using HeThongTimViec.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System.Text.Json;
using HeThongTimViec.ViewModels.Admin; // Đảm bảo using ViewModel Admin
using System.Text;
using HeThongTimViec.ViewModels;       // Đảm bảo using ViewModel chung
// using MySqlConnector; // Bỏ comment nếu cần xử lý Exception của MySQL

namespace HeThongTimViec.Controllers
{
    [Authorize(Roles = nameof(LoaiTaiKhoan.quantrivien))] // Chỉ Admin mới vào được
    [Route("admin/users")] // Định tuyến base cho Controller
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminUserController> _logger;
        private const int PageSize = 10; // Số lượng user trên mỗi trang

        public AdminUserController(ApplicationDbContext context, ILogger<AdminUserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /admin/users
        [HttpGet("")] // Route cho action Index (trang danh sách)
        public async Task<IActionResult> Index(
            string? searchString,           // Từ khóa tìm kiếm
            LoaiTaiKhoan? roleFilter,       // Lọc theo vai trò
            TrangThaiTaiKhoan? statusFilter, // Lọc theo trạng thái
            DateTime? startDate,            // Lọc ngày tạo từ
            DateTime? endDate,              // Lọc ngày tạo đến
            string? viewMode,               // Chế độ xem (table/card)
            int pageNumber = 1)             // Số trang hiện tại
        {
            _logger.LogInformation("Index Quản lý Người dùng - Đang tải danh sách và thống kê. Trang: {Page}, Chế độ xem: {ViewMode}", pageNumber, viewMode ?? "table");
            try
            {
                // --- Lấy dữ liệu thống kê (Dashboard Stats) ---
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
                var startOfThisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // Query cơ sở cho người dùng HOẠT ĐỘNG (không phải admin, không bị cấm)
                var baseUserQueryActive = _context.NguoiDungs
                                           .Where(u => u.LoaiTk != LoaiTaiKhoan.quantrivien
                                                    && u.TrangThaiTk != TrangThaiTaiKhoan.bidinhchi);

                // Query cơ sở cho TẤT CẢ người dùng không phải admin (bao gồm cả bị cấm, chờ xác minh,...)
                var baseUserQueryAllNonAdmin = _context.NguoiDungs
                                            .Where(u => u.LoaiTk != LoaiTaiKhoan.quantrivien);

                // Đếm số lượng
                int totalActiveUsers = await baseUserQueryActive.CountAsync();
                int totalActiveCandidates = await baseUserQueryActive.CountAsync(u => u.LoaiTk == LoaiTaiKhoan.canhan);
                int totalActiveEmployers = await baseUserQueryActive.CountAsync(u => u.LoaiTk == LoaiTaiKhoan.doanhnghiep);
                int totalBanned = await baseUserQueryAllNonAdmin.CountAsync(u => u.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi);
                int newlyBannedThisMonth = await baseUserQueryAllNonAdmin.CountAsync(u => u.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi && u.NgayCapNhat >= startOfThisMonth);
                int totalPendingVerification = await baseUserQueryAllNonAdmin.CountAsync(u => u.LoaiTk == LoaiTaiKhoan.doanhnghiep && u.TrangThaiTk == TrangThaiTaiKhoan.choxacminh);

                // Tính toán tăng trưởng (dựa trên người dùng hoạt động)
                int newActiveUsersLastMonth = await baseUserQueryActive.CountAsync(u => u.NgayTao >= oneMonthAgo);
                int totalActiveUsersBeforeLastMonth = totalActiveUsers - newActiveUsersLastMonth;
                double userGrowthPercentage = (totalActiveUsersBeforeLastMonth > 0) ? ((double)newActiveUsersLastMonth / totalActiveUsersBeforeLastMonth) * 100 : (newActiveUsersLastMonth > 0 ? 100.0 : 0.0);
                // Tương tự cho ứng viên và nhà tuyển dụng...
                int newCandidatesLastMonth = await baseUserQueryActive.CountAsync(u => u.LoaiTk == LoaiTaiKhoan.canhan && u.NgayTao >= oneMonthAgo);
                int totalCandidatesBeforeLastMonth = totalActiveCandidates - newCandidatesLastMonth;
                double candidateGrowthPercentage = (totalCandidatesBeforeLastMonth > 0) ? ((double)newCandidatesLastMonth / totalCandidatesBeforeLastMonth) * 100 : (newCandidatesLastMonth > 0 ? 100.0 : 0.0);
                int newEmployersLastMonth = await baseUserQueryActive.CountAsync(u => u.LoaiTk == LoaiTaiKhoan.doanhnghiep && u.NgayTao >= oneMonthAgo);
                int totalEmployersBeforeLastMonth = totalActiveEmployers - newEmployersLastMonth;
                double employerGrowthPercentage = (totalEmployersBeforeLastMonth > 0) ? ((double)newEmployersLastMonth / totalEmployersBeforeLastMonth) * 100 : (newEmployersLastMonth > 0 ? 100.0 : 0.0);

                // Gán dữ liệu thống kê vào ViewBag
                ViewBag.TotalUserCount = totalActiveUsers;
                ViewBag.UserGrowthPercentage = userGrowthPercentage;
                ViewBag.CandidateCount = totalActiveCandidates;
                ViewBag.CandidateGrowthPercentage = candidateGrowthPercentage;
                ViewBag.EmployerCount = totalActiveEmployers;
                ViewBag.EmployerGrowthPercentage = employerGrowthPercentage;
                ViewBag.BannedCount = totalBanned;
                ViewBag.NewlyBannedCount = newlyBannedThisMonth;
                ViewBag.PendingVerificationCount = totalPendingVerification;

                // --- Lấy danh sách người dùng để hiển thị ---
                var usersQuery = _context.NguoiDungs
                                         .Where(u => u.LoaiTk != LoaiTaiKhoan.quantrivien) // Loại trừ admin
                                         .Include(u => u.ThanhPho)       // Include để hiển thị tên TP
                                         .Include(u => u.QuanHuyen)      // Include để hiển thị tên QH
                                         .Include(u => u.HoSoDoanhNghiep) // Include để lấy tên công ty (nếu là NTD) cho danh sách
                                         .AsNoTracking() // Dùng AsNoTracking cho query chỉ đọc để tăng hiệu suất
                                         .AsQueryable();

                // Áp dụng bộ lọc
                if (!string.IsNullOrEmpty(searchString))
                {
                    var searchLower = searchString.ToLower();
                    usersQuery = usersQuery.Where(u => (u.HoTen != null && u.HoTen.ToLower().Contains(searchLower))
                                                    || (u.Email != null && u.Email.ToLower().Contains(searchLower)));
                }
                if (roleFilter.HasValue)
                {
                    // Trường hợp đặc biệt: lọc NTD đang chờ xác minh
                    if (roleFilter == LoaiTaiKhoan.doanhnghiep && statusFilter == TrangThaiTaiKhoan.choxacminh)
                    {
                        usersQuery = usersQuery.Where(u => u.LoaiTk == LoaiTaiKhoan.doanhnghiep && u.TrangThaiTk == TrangThaiTaiKhoan.choxacminh);
                        statusFilter = null; // Xóa bộ lọc trạng thái vì đã xử lý ở đây
                    }
                    else
                    {
                        usersQuery = usersQuery.Where(u => u.LoaiTk == roleFilter.Value);
                    }
                }
                if (statusFilter.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.TrangThaiTk == statusFilter.Value);
                }
                if (startDate.HasValue)
                {
                    var startOfDayUtc = startDate.Value.Date.ToUniversalTime();
                    usersQuery = usersQuery.Where(u => u.NgayTao >= startOfDayUtc);
                }
                if (endDate.HasValue)
                {
                    var endOfDayUtc = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
                    usersQuery = usersQuery.Where(u => u.NgayTao <= endOfDayUtc);
                }

                // Sắp xếp (mới nhất lên đầu)
                usersQuery = usersQuery.OrderByDescending(u => u.Id);

                // Phân trang
                var totalUsersInList = await usersQuery.CountAsync();
                var users = await usersQuery
                                    .Skip((pageNumber - 1) * PageSize)
                                    .Take(PageSize)
                                    .ToListAsync();
                var totalPages = (int)Math.Ceiling(totalUsersInList / (double)PageSize);

                // Truyền dữ liệu phân trang và bộ lọc vào View
                ViewData["CurrentSearch"] = searchString;
                ViewData["CurrentRole"] = roleFilter;
                ViewData["CurrentStatus"] = statusFilter; // Truyền bộ lọc trạng thái ban đầu (nếu không bị xóa)
                ViewData["CurrentStartDate"] = startDate?.ToString("yyyy-MM-dd");
                ViewData["CurrentEndDate"] = endDate?.ToString("yyyy-MM-dd");
                ViewData["PageNumber"] = pageNumber;
                ViewData["TotalPages"] = totalPages;
                ViewData["TotalUsers"] = totalUsersInList;
                ViewData["PageSize"] = PageSize;
                ViewBag.ViewMode = string.IsNullOrEmpty(viewMode) ? "table" : viewMode;

                _logger.LogInformation("Index: Đã tải {UserCount} người dùng cho trang {PageNumber}/{TotalPages}. Tổng số khớp bộ lọc: {TotalMatching}", users.Count, pageNumber, totalPages, totalUsersInList);

                // Chọn view dựa trên viewMode
                string viewPath = (viewMode == "card")
                                ? "~/Views/AdminUser/IndexCards.cshtml" // Giả sử có view card
                                : "~/Views/AdminUser/Index.cshtml";    // View table mặc định
                return View(viewPath, users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index: Lỗi khi tải trang Quản lý Người dùng.");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải dữ liệu người dùng. Vui lòng thử lại.";
                // Trả về view mặc định với danh sách rỗng khi có lỗi
                return View("~/Views/AdminUser/Index.cshtml", new List<NguoiDung>());
            }
        }

        // GET: /admin/users/details/5
        [HttpGet("details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            _logger.LogInformation("Details: Lấy chi tiết cho Người dùng ID: {UserId}", id);

            // --- Lấy Người dùng với dữ liệu liên quan chi tiết ---
            var user = await _context.NguoiDungs
                // Thông tin cơ bản & Địa điểm
                .Include(u => u.ThanhPho)
                .Include(u => u.QuanHuyen)
                // Dữ liệu hồ sơ cụ thể
                .Include(u => u.HoSoDoanhNghiep)
                    .ThenInclude(hsdn => hsdn.AdminXacMinh) // Admin đã xác minh
                .Include(u => u.HoSoUngVien)
                // Dữ liệu Hoạt động/Tương tác
                .Include(u => u.TinTuyenDungsDaDang) // Các tin NTD này đã đăng
                    .ThenInclude(ttd => ttd.UngTuyens) // Các ứng tuyển vào tin của họ
                .Include(u => u.TinTuyenDungsDaDang) // Include lại nếu cần thuộc tính khác
                    .ThenInclude(ttd => ttd.AdminDuyet) // Admin đã duyệt tin
                .Include(u => u.BaoCaoViPhamsDaGui) // Các báo cáo người dùng này GỬI ĐI
                    .ThenInclude(bcp => bcp.TinTuyenDung) // Tin tuyển dụng liên quan đến báo cáo họ gửi
                // Lọc bỏ Admin
                .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Details: Không tìm thấy người dùng ID {UserId} hoặc là Admin.", id);
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hoặc bạn không có quyền xem chi tiết.";
                return RedirectToAction(nameof(Index)); // Chuyển hướng về trang danh sách
            }

            // --- Chuẩn bị ViewModel ---
            var viewModel = new AdminUserDetailsViewModel { User = user };

            // --- Xây dựng Nhật ký Hoạt động ---
            var activityLog = new List<ActivityLogItem>();
            activityLog.Add(new ActivityLogItem { Timestamp = user.NgayTao, Description = "Tạo tài khoản", IconClass = "fas fa-user-plus text-success" });
            if (user.LanDangNhapCuoi.HasValue) activityLog.Add(new ActivityLogItem { Timestamp = user.LanDangNhapCuoi.Value, Description = "Đăng nhập lần cuối", IconClass = "fas fa-sign-in-alt text-info" });
            if (user.NgayCapNhat > user.NgayTao) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Cập nhật thông tin tài khoản", IconClass = "fas fa-user-edit text-primary" });

            // Thay đổi trạng thái tài khoản
            if (user.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi && user.NgayCapNhat > user.NgayTao) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Tài khoản bị đình chỉ", IconClass = "fas fa-user-lock text-danger" });
            else if (user.TrangThaiTk == TrangThaiTaiKhoan.kichhoat && user.NgayCapNhat > user.NgayTao && activityLog.LastOrDefault(a => a.Description.Contains("bị đình chỉ"))?.Timestamp < user.NgayCapNhat) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Tài khoản được kích hoạt lại", IconClass = "fas fa-user-check text-success" });
            else if (user.TrangThaiTk == TrangThaiTaiKhoan.choxacminh && user.NgayCapNhat > user.NgayTao) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Tài khoản chờ xác minh", IconClass = "fas fa-hourglass-half text-warning" });

            // Hoạt động theo loại hồ sơ
            if (user.LoaiTk == LoaiTaiKhoan.doanhnghiep && user.HoSoDoanhNghiep != null)
            {
                if (user.HoSoDoanhNghiep.NgayXacMinh.HasValue) activityLog.Add(new ActivityLogItem { Timestamp = user.HoSoDoanhNghiep.NgayXacMinh.Value, Description = $"Hồ sơ doanh nghiệp được xác minh bởi {user.HoSoDoanhNghiep.AdminXacMinh?.HoTen ?? "Admin"}", IconClass = "fas fa-building-check text-success" });
                else if (user.NgayCapNhat > user.NgayTao) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Cập nhật hồ sơ doanh nghiệp (ước tính)", IconClass = "fas fa-edit text-primary" }); // Thời gian cập nhật ước tính

                foreach (var job in user.TinTuyenDungsDaDang.OrderByDescending(j => j.NgayDang).Take(10)) // Giới hạn hoạt động liên quan đến tin đăng
                {
                    activityLog.Add(new ActivityLogItem { Timestamp = job.NgayDang, Description = $"Đăng tin: '{job.TieuDe}'", IconClass = "fas fa-bullhorn text-info" });
                    if (job.NgayDuyet.HasValue) activityLog.Add(new ActivityLogItem { Timestamp = job.NgayDuyet.Value, Description = $"Tin '{job.TieuDe}' được duyệt bởi {job.AdminDuyet?.HoTen ?? "Admin"}", IconClass = "fas fa-check-circle text-success" });
                    if (job.NgayCapNhat > job.NgayDang) activityLog.Add(new ActivityLogItem { Timestamp = job.NgayCapNhat, Description = $"Cập nhật tin: '{job.TieuDe}'", IconClass = "fas fa-edit text-primary" });
                }
            }
            else if (user.LoaiTk == LoaiTaiKhoan.canhan) // Hoạt động của ứng viên
            {
                 if (user.NgayCapNhat > user.NgayTao) activityLog.Add(new ActivityLogItem { Timestamp = user.NgayCapNhat, Description = "Cập nhật hồ sơ ứng viên (ước tính)", IconClass = "fas fa-file-alt text-primary" }); // Thời gian cập nhật ước tính

                 // Lấy riêng dữ liệu ứng tuyển và tin đã lưu nếu cần cho log (nếu chưa include hoặc cần sắp xếp/giới hạn khác)
                 var applications = await _context.UngTuyens
                                                .Where(a => a.UngVienId == id)
                                                .Include(a => a.TinTuyenDung) // Include Tin để lấy TieuDe
                                                .OrderByDescending(a => a.NgayNop).Take(10)
                                                .ToListAsync();
                 foreach (var app in applications)
                 {
                     activityLog.Add(new ActivityLogItem { Timestamp = app.NgayNop, Description = $"Ứng tuyển vào: '{app.TinTuyenDung?.TieuDe ?? "N/A"}'", IconClass = "fas fa-file-signature text-info" });
                     if (app.NgayCapNhatTrangThai.HasValue && app.NgayCapNhatTrangThai > app.NgayNop) activityLog.Add(new ActivityLogItem { Timestamp = app.NgayCapNhatTrangThai.Value, Description = $"Trạng thái ứng tuyển đổi thành '{app.TrangThai}' cho '{app.TinTuyenDung?.TieuDe ?? "N/A"}'", IconClass = "fas fa-tasks text-secondary" });
                 }
                 var savedJobs = await _context.TinDaLuus
                                             .Where(s => s.NguoiDungId == id)
                                             .Include(s => s.TinTuyenDung) // Include Tin để lấy TieuDe
                                             .OrderByDescending(s => s.NgayLuu).Take(5)
                                             .ToListAsync();
                 foreach (var saved in savedJobs) activityLog.Add(new ActivityLogItem { Timestamp = saved.NgayLuu, Description = $"Lưu tin: '{saved.TinTuyenDung?.TieuDe ?? "N/A"}'", IconClass = "fas fa-save text-warning" });
            }

            // Báo cáo người dùng này đã gửi đi
            foreach (var reportSent in user.BaoCaoViPhamsDaGui.OrderByDescending(r => r.NgayBaoCao).Take(5)) activityLog.Add(new ActivityLogItem { Timestamp = reportSent.NgayBaoCao, Description = $"Gửi báo cáo cho tin: '{reportSent.TinTuyenDung?.TieuDe ?? "N/A"}' (Lý do: {reportSent.LyDo})", IconClass = "fas fa-flag text-warning" });

            viewModel.ActivityLog = activityLog.OrderByDescending(a => a.Timestamp).ToList();

            // --- Điền dữ liệu cho các Tab (Dựa trên Loại Tài khoản) ---
            if (user.LoaiTk == LoaiTaiKhoan.canhan)
            {
                // Lấy dữ liệu nếu chưa include hoặc cần lọc/sắp xếp cụ thể cho tabs
                viewModel.CandidateApplications = await _context.UngTuyens
                                                        .Where(ut => ut.UngVienId == id)
                                                        .Include(ut => ut.TinTuyenDung).ThenInclude(ttd => ttd.NguoiDang).ThenInclude(nd => nd.HoSoDoanhNghiep) // Include thông tin NTD
                                                        .OrderByDescending(ut => ut.NgayNop)
                                                        .AsNoTracking() // Dùng NoTracking cho dữ liệu chỉ đọc
                                                        .ToListAsync();
                viewModel.CandidateSavedJobs = await _context.TinDaLuus
                                                    .Where(tdl => tdl.NguoiDungId == id)
                                                    .Include(tdl => tdl.TinTuyenDung).ThenInclude(ttd => ttd.NguoiDang).ThenInclude(nd => nd.HoSoDoanhNghiep) // Include thông tin NTD
                                                    .OrderByDescending(tdl => tdl.NgayLuu)
                                                    .AsNoTracking() // Dùng NoTracking
                                                    .ToListAsync();
                viewModel.EmployerPostedJobs = new List<TinTuyenDung>(); // Rỗng cho ứng viên
                viewModel.ReportsAboutUser = new List<BaoCaoViPham>(); // Không áp dụng trực tiếp cho ứng viên
            }
            else // Doanh nghiệp (Employer)
            {
                viewModel.EmployerPostedJobs = user.TinTuyenDungsDaDang.OrderByDescending(ttd => ttd.NgayDang).ToList(); // Đã lấy ở trên
                viewModel.CandidateApplications = new List<UngTuyen>(); // Rỗng cho NTD
                viewModel.CandidateSavedJobs = new List<TinDaLuu>(); // Rỗng cho NTD

                // Lấy Báo cáo VỀ các tin đăng của NTD này
                var userJobIds = user.TinTuyenDungsDaDang.Select(j => j.Id).ToList();
                if (userJobIds.Any())
                {
                    viewModel.ReportsAboutUser = await _context.BaoCaoViPhams
                                                        .Include(b => b.NguoiBaoCao) // Ai báo cáo?
                                                        .Include(b => b.TinTuyenDung) // Tin nào?
                                                        .Where(b => userJobIds.Contains(b.TinTuyenDungId))
                                                        .OrderByDescending(b => b.NgayBaoCao)
                                                        .AsNoTracking() // Dùng NoTracking
                                                        .ToListAsync();
                }
                else
                {
                    viewModel.ReportsAboutUser = new List<BaoCaoViPham>();
                }
            }
            // Báo cáo do người dùng này gửi đi (áp dụng cho cả hai)
            viewModel.ReportsByUser = user.BaoCaoViPhamsDaGui.OrderByDescending(b => b.NgayBaoCao).ToList(); // Đã lấy ở trên

            _logger.LogInformation("Details: Đã tải chi tiết và dữ liệu liên quan cho Người dùng ID: {UserId}. Vai trò: {UserRole}. Hoạt động: {ActivityCount}, BC đã gửi: {ReportsByCount}, BC về tin: {ReportsAboutCount}",
                id, user.LoaiTk, viewModel.ActivityLog.Count, viewModel.ReportsByUser.Count, viewModel.ReportsAboutUser?.Count ?? 0);

            // Sử dụng view cụ thể cho trang chi tiết
            return View("~/Views/AdminUser/Details.cshtml", viewModel);
        }


        // GET: /admin/users/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            _logger.LogInformation("[AdminUser/Create] GET: Hiển thị biểu mẫu tạo người dùng.");
            await PopulateDropdownsForCreateEditView(); // Chuẩn bị dropdown
            return View(new NguoiDungCreateViewModel()); // Trả về view với ViewModel rỗng
        }

        // POST: /admin/users/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken] // Chống CSRF
        public async Task<IActionResult> Create(NguoiDungCreateViewModel model)
        {
            _logger.LogInformation("[AdminUser/Create] POST: Bắt đầu xử lý tạo người dùng mới. Email: {Email}, Vai trò: {Role}", model.Email, model.LoaiTk);

            // Luôn load lại dropdown phòng trường hợp validation fail và cần hiển thị lại form
            await PopulateDropdownsForCreateEditView(model.ThanhPhoId, model.QuanHuyenId);

            // --- Validation tùy chỉnh ---
            if (model.LoaiTk == LoaiTaiKhoan.quantrivien)
            {
                ModelState.AddModelError(nameof(NguoiDungCreateViewModel.LoaiTk), "Không thể tạo tài khoản Quản trị viên qua giao diện này.");
            }
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(nameof(NguoiDungCreateViewModel.Password), "Mật khẩu là bắt buộc khi tạo người dùng mới.");
            }
            // Kiểm tra Email duy nhất
            if (!string.IsNullOrEmpty(model.Email) && await _context.NguoiDungs.AnyAsync(u => u.Email == model.Email.Trim()))
            {
                ModelState.AddModelError(nameof(NguoiDungCreateViewModel.Email), "Địa chỉ email này đã được sử dụng.");
            }
            // Kiểm tra SĐT duy nhất (nếu có)
            if (!string.IsNullOrEmpty(model.Sdt) && await _context.NguoiDungs.AnyAsync(u => u.Sdt == model.Sdt.Trim()))
            {
                ModelState.AddModelError(nameof(NguoiDungCreateViewModel.Sdt), "Số điện thoại này đã được sử dụng.");
            }
            // --- Kết thúc Validation ---

            if (ModelState.IsValid) // Nếu dữ liệu từ form hợp lệ
            {
                var newUser = new NguoiDung
                {
                    Email = model.Email.Trim(),
                    HoTen = model.HoTen?.Trim(),
                    Sdt = model.Sdt?.Trim(),
                    LoaiTk = model.LoaiTk,
                    // Trạng thái mặc định: Chờ xác minh cho DN, Kích hoạt cho Cá nhân
                    TrangThaiTk = (model.LoaiTk == LoaiTaiKhoan.doanhnghiep) ? TrangThaiTaiKhoan.choxacminh : TrangThaiTaiKhoan.kichhoat,
                    GioiTinh = model.GioiTinh,
                    NgaySinh = model.NgaySinh,
                    DiaChiChiTiet = model.DiaChiChiTiet?.Trim(),
                    QuanHuyenId = model.QuanHuyenId,
                    ThanhPhoId = model.ThanhPhoId,
                    NgayTao = DateTime.UtcNow,
                    NgayCapNhat = DateTime.UtcNow,
                    MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password) // Băm mật khẩu
                };

                // --- Tạo Hồ sơ Liên quan Tự động ---
                if (newUser.LoaiTk == LoaiTaiKhoan.doanhnghiep)
                {
                    newUser.HoSoDoanhNghiep = new HoSoDoanhNghiep
                    {
                        // NguoiDungId sẽ được EF Core tự động gán
                        TenCongTy = newUser.HoTen ?? "Công ty chưa đặt tên", // Lấy tên từ HoTen hoặc mặc định
                        DaXacMinh = false
                        // Thêm các trường mặc định khác nếu cần
                    };
                    _logger.LogInformation("[AdminUser/Create] POST: Chuẩn bị tạo HoSoDoanhNghiep cho {Email}", newUser.Email);
                }
                else if (newUser.LoaiTk == LoaiTaiKhoan.canhan)
                {
                    newUser.HoSoUngVien = new HoSoUngVien
                    {
                        // NguoiDungId tự động gán
                        TieuDeHoSo = $"Hồ sơ của {newUser.HoTen ?? "Người dùng chưa đặt tên"}", // Tiêu đề mặc định
                        TrangThaiTimViec = TrangThaiTimViec.dangtimtichcuc // Trạng thái mặc định
                        // Thêm các trường mặc định khác nếu cần
                    };
                    _logger.LogInformation("[AdminUser/Create] POST: Chuẩn bị tạo HoSoUngVien cho {Email}", newUser.Email);
                }
                // --- Kết thúc Tạo Hồ sơ ---

                _context.NguoiDungs.Add(newUser); // Thêm người dùng mới vào DbContext

                try
                {
                    await _context.SaveChangesAsync(); // Lưu người dùng và hồ sơ liên quan (nếu có) vào DB
                    _logger.LogInformation("[AdminUser/Create] POST: Đã lưu thành công người dùng mới ID: {UserId}, Email: {Email}", newUser.Id, newUser.Email);
                    TempData["SuccessMessage"] = $"Đã tạo thành công người dùng '{newUser.HoTen}'.";
                    return RedirectToAction(nameof(Index)); // Chuyển hướng về trang danh sách
                }
                catch (DbUpdateException ex) // Bắt lỗi DB cụ thể (VD: UNIQUE constraint)
                {
                    _logger.LogError(ex, "[AdminUser/Create] POST: Lỗi DbUpdateException khi lưu người dùng mới (Email: {Email}). Inner: {InnerMessage}", model.Email, ex.InnerException?.Message);
                    HandleDbUpdateException(ex, model); // Sử dụng helper để thêm lỗi vào ModelState
                }
                catch (Exception ex) // Bắt các lỗi khác
                {
                    _logger.LogError(ex, "[AdminUser/Create] POST: Lỗi không xác định khi lưu người dùng mới (Email: {Email}).", model.Email);
                    ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống không mong muốn khi tạo người dùng.");
                }
            }
            else // Nếu ModelState không hợp lệ
            {
                _logger.LogWarning("[AdminUser/Create] POST: ModelState không hợp lệ. Quay lại view tạo.");
                LogModelStateErrors(); // Ghi log chi tiết lỗi validation
            }

            // Nếu ModelState không hợp lệ hoặc lưu DB lỗi, hiển thị lại form với dữ liệu đã nhập
            return View(model);
        }

        // GET: /admin/users/edit/5
        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation("Edit GET: Đang tải người dùng ID: {UserId} để chỉnh sửa.", id);

            // Lấy người dùng và hồ sơ liên quan (Doanh nghiệp hoặc Ứng viên)
            // Quan trọng: Include() hồ sơ liên quan để hiển thị và cập nhật
            var user = await _context.NguoiDungs
                                     .Include(u => u.HoSoDoanhNghiep) // Include HoSoDoanhNghiep
                                     .Include(u => u.HoSoUngVien)     // Include HoSoUngVien (mặc dù chưa dùng trong ViewModel)
                                     .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                                     .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Edit GET: Không tìm thấy người dùng ID {UserId} hoặc là Admin.", id);
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hoặc không được phép sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Map dữ liệu từ User và Profile (nếu có) sang ViewModel
            var viewModel = new AdminUserEditViewModel
            {
                Id = user.Id,
                // Thông tin NguoiDung chung
                HoTen = user.HoTen,
                Email = user.Email,
                Sdt = user.Sdt,
                LoaiTk = user.LoaiTk, // Giữ nguyên loại TK, không cho sửa
                GioiTinh = user.GioiTinh,
                NgaySinh = user.NgaySinh,
                DiaChiChiTiet = user.DiaChiChiTiet,
                ThanhPhoId = user.ThanhPhoId,
                QuanHuyenId = user.QuanHuyenId,
                // Thông tin HoSoDoanhNghiep (nếu là doanh nghiệp)
                TenCongTy = user.HoSoDoanhNghiep?.TenCongTy,
                MaSoThue = user.HoSoDoanhNghiep?.MaSoThue,
                UrlWebsite = user.HoSoDoanhNghiep?.UrlWebsite,
                MoTaCongTy = user.HoSoDoanhNghiep?.MoTa, // Đã đổi tên trong ViewModel
                DiaChiDangKy = user.HoSoDoanhNghiep?.DiaChiDangKy,
                QuyMoCongTy = user.HoSoDoanhNghiep?.QuyMoCongTy
                // Mật khẩu không được tải để sửa
                // Thêm các trường HoSoUngVien nếu cần
            };

            // Chuẩn bị dropdown địa điểm
            await PopulateDropdownsForCreateEditView(user.ThanhPhoId, user.QuanHuyenId);

            // Truyền trạng thái và vai trò hiện tại (chỉ đọc) sang View
            ViewBag.CurrentStatusText = GetTrangThaiDisplay(user.TrangThaiTk).Text;
            ViewBag.CurrentStatusBadge = GetTrangThaiDisplay(user.TrangThaiTk).BadgeClass;
            ViewBag.CurrentRoleText = GetVaiTroDisplay(user.LoaiTk);
            // Truyền trạng thái xác minh nếu là DN
            ViewBag.IsBusinessVerified = user.HoSoDoanhNghiep?.DaXacMinh;

            return View(viewModel); // Trả về view Edit với ViewModel đã có dữ liệu
        }

        // POST: /admin/users/edit/5
        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminUserEditViewModel model)
        {
            _logger.LogInformation("Edit POST: Đang xử lý cập nhật cho người dùng ID: {UserId}", id);

            if (id != model.Id)
            {
                _logger.LogWarning("Edit POST: ID không khớp giữa route ({RouteId}) và model ({ModelId}).", id, model.Id);
                TempData["ErrorMessage"] = "Yêu cầu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            // Lấy lại người dùng cần cập nhật từ DB, **Include cả hồ sơ liên quan**
            var userToUpdate = await _context.NguoiDungs
                                         .Include(u => u.HoSoDoanhNghiep) // Quan trọng để cập nhật HoSoDoanhNghiep
                                         .Include(u => u.HoSoUngVien)     // Include nếu cần cập nhật HoSoUngVien
                                         .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                                         .FirstOrDefaultAsync();

            if (userToUpdate == null)
            {
                _logger.LogWarning("Edit POST: Không tìm thấy người dùng ID {UserId} hoặc là Admin khi cố gắng cập nhật.", id);
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hoặc không được phép sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Lưu trạng thái và vai trò gốc để hiển thị lại nếu validation fail
            var originalStatus = userToUpdate.TrangThaiTk;
            var originalRole = userToUpdate.LoaiTk;
            var originalVerification = userToUpdate.HoSoDoanhNghiep?.DaXacMinh;

            // --- Custom Validation cho Employer Fields ---
            if(userToUpdate.LoaiTk == LoaiTaiKhoan.doanhnghiep)
            {
                if (string.IsNullOrWhiteSpace(model.TenCongTy))
                {
                    ModelState.AddModelError(nameof(AdminUserEditViewModel.TenCongTy), "Tên công ty là bắt buộc.");
                }
                // Kiểm tra Mã số thuế duy nhất *nếu thay đổi* và không rỗng
                string? trimmedMST = model.MaSoThue?.Trim();
                if (!string.IsNullOrEmpty(trimmedMST) && userToUpdate.HoSoDoanhNghiep?.MaSoThue != trimmedMST)
                {
                    bool mstExists = await _context.HoSoDoanhNghieps.AnyAsync(h => h.MaSoThue == trimmedMST && h.NguoiDungId != id);
                    if (mstExists)
                    {
                         ModelState.AddModelError(nameof(AdminUserEditViewModel.MaSoThue), "Mã số thuế này đã được sử dụng bởi doanh nghiệp khác.");
                         _logger.LogWarning("Edit POST: Cập nhật thất bại - MST {MST} đã tồn tại.", trimmedMST);
                    }
                }
            }
            // --- Kết thúc Custom Validation ---


            if (ModelState.IsValid) // Kiểm tra validation của ViewModel và validation tùy chỉnh ở trên
            {
                bool emailChanged = userToUpdate.Email != model.Email.Trim();
                bool sdtChanged = userToUpdate.Sdt != (model.Sdt ?? "").Trim(); // Xử lý SĐT null

                // Kiểm tra Email duy nhất *chỉ khi* thay đổi
                if (emailChanged)
                {
                    bool emailExists = await _context.NguoiDungs.AnyAsync(u => u.Email == model.Email.Trim() && u.Id != id);
                    if (emailExists)
                    {
                        ModelState.AddModelError(nameof(AdminUserEditViewModel.Email), "Địa chỉ email này đã được sử dụng bởi người dùng khác.");
                    }
                }

                // Kiểm tra SĐT duy nhất *chỉ khi* thay đổi và không rỗng
                string trimmedSdt = (model.Sdt ?? "").Trim();
                if (sdtChanged && !string.IsNullOrEmpty(trimmedSdt))
                {
                    bool sdtExists = await _context.NguoiDungs.AnyAsync(u => u.Sdt == trimmedSdt && u.Id != id);
                    if (sdtExists)
                    {
                        ModelState.AddModelError(nameof(AdminUserEditViewModel.Sdt), "Số điện thoại này đã được sử dụng bởi người dùng khác.");
                    }
                }

                 // Nếu có lỗi validation (unique check hoặc custom validation), trả về ngay
                 if (!ModelState.IsValid)
                 {
                     LogModelStateErrors();
                     await PopulateDropdownsForCreateEditView(model.ThanhPhoId, model.QuanHuyenId);
                     ViewBag.CurrentStatusText = GetTrangThaiDisplay(originalStatus).Text;
                     ViewBag.CurrentStatusBadge = GetTrangThaiDisplay(originalStatus).BadgeClass;
                     ViewBag.CurrentRoleText = GetVaiTroDisplay(originalRole);
                     ViewBag.IsBusinessVerified = originalVerification;
                     return View(model);
                 }

                // --- Cập nhật thuộc tính NguoiDung ---
                userToUpdate.HoTen = model.HoTen?.Trim();
                userToUpdate.Email = model.Email.Trim();
                userToUpdate.Sdt = string.IsNullOrEmpty(trimmedSdt) ? null : trimmedSdt; // Lưu null nếu rỗng
                userToUpdate.GioiTinh = model.GioiTinh;
                userToUpdate.NgaySinh = model.NgaySinh;
                userToUpdate.DiaChiChiTiet = model.DiaChiChiTiet?.Trim();
                userToUpdate.QuanHuyenId = model.QuanHuyenId;
                userToUpdate.ThanhPhoId = model.ThanhPhoId;
                userToUpdate.NgayCapNhat = DateTime.UtcNow; // Cập nhật thời gian sửa đổi

                // --- Cập nhật thuộc tính Hồ sơ liên quan (Quan trọng!) ---
                if (userToUpdate.LoaiTk == LoaiTaiKhoan.doanhnghiep)
                {
                    if (userToUpdate.HoSoDoanhNghiep == null)
                    {
                        // Trường hợp hiếm gặp: NTD không có hồ sơ -> tạo mới hoặc báo lỗi
                        _logger.LogWarning("Edit POST: Người dùng Doanh nghiệp ID {UserId} bị thiếu HoSoDoanhNghiep. Tạo mới hồ sơ.", id);
                        userToUpdate.HoSoDoanhNghiep = new HoSoDoanhNghiep { NguoiDungId = id, DaXacMinh = false };
                        // Gán các giá trị mặc định khác nếu cần
                    }
                    // Cập nhật các trường từ ViewModel vào HoSoDoanhNghiep
                    userToUpdate.HoSoDoanhNghiep.TenCongTy = model.TenCongTy!.Trim(); // Đã kiểm tra not null ở custom validation
                    userToUpdate.HoSoDoanhNghiep.MaSoThue = string.IsNullOrWhiteSpace(model.MaSoThue) ? null : model.MaSoThue.Trim();
                    userToUpdate.HoSoDoanhNghiep.UrlWebsite = string.IsNullOrWhiteSpace(model.UrlWebsite) ? null : model.UrlWebsite.Trim();
                    userToUpdate.HoSoDoanhNghiep.MoTa = model.MoTaCongTy?.Trim();
                    userToUpdate.HoSoDoanhNghiep.DiaChiDangKy = model.DiaChiDangKy?.Trim();
                    userToUpdate.HoSoDoanhNghiep.QuyMoCongTy = model.QuyMoCongTy?.Trim();
                     // Không cập nhật trạng thái xác minh ở đây, chỉ cập nhật thông tin
                }
                // else if (userToUpdate.LoaiTk == LoaiTaiKhoan.canhan)
                // {
                //     if (userToUpdate.HoSoUngVien == null) { /* Xử lý thiếu hồ sơ ứng viên nếu cần */ }
                //     // Cập nhật các trường HoSoUngVien từ model nếu có
                // }

                // _context.NguoiDungs.Update(userToUpdate); // Không cần gọi Update rõ ràng nếu entity đã được tracked

                try
                {
                    await _context.SaveChangesAsync(); // Lưu thay đổi cho cả NguoiDung và HoSo...
                    _logger.LogInformation("Edit POST: Đã cập nhật thành công thông tin người dùng và hồ sơ liên quan (nếu có) cho ID: {UserId}", id);
                    TempData["SuccessMessage"] = $"Đã cập nhật thông tin người dùng '{userToUpdate.HoTen}' thành công.";
                    return RedirectToAction(nameof(Details), new { id = id }); // Chuyển hướng về trang chi tiết sau khi sửa thành công
                }
                catch (DbUpdateConcurrencyException ex) // Lỗi trùng khớp dữ liệu
                {
                     _logger.LogError(ex, "Edit POST: Lỗi concurrency khi cập nhật người dùng ID {UserId}.", id);
                     ModelState.AddModelError("", "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.");
                }
                catch (DbUpdateException ex) // Lỗi DB khác (VD: unique constraint khi lưu)
                {
                    _logger.LogError(ex, "Edit POST: Lỗi DB khi cập nhật người dùng ID {UserId}. Inner: {Inner}", id, ex.InnerException?.Message);
                    HandleDbUpdateException(ex, model); // Sử dụng helper để thêm lỗi cụ thể
                }
                 catch (Exception ex) // Lỗi không xác định khác
                {
                    _logger.LogError(ex, "Edit POST: Lỗi không xác định khi cập nhật người dùng ID {UserId}.", id);
                    ModelState.AddModelError("", "Đã xảy ra lỗi không xác định khi lưu thay đổi.");
                }
            }

            // Nếu ModelState không hợp lệ hoặc lưu thất bại, chuẩn bị lại dropdown và trả về view Edit
            LogModelStateErrors();
            await PopulateDropdownsForCreateEditView(model.ThanhPhoId, model.QuanHuyenId);
            // Khôi phục trạng thái/vai trò/xác minh gốc để hiển thị lại trên form lỗi
            ViewBag.CurrentStatusText = GetTrangThaiDisplay(originalStatus).Text;
            ViewBag.CurrentStatusBadge = GetTrangThaiDisplay(originalStatus).BadgeClass;
            ViewBag.CurrentRoleText = GetVaiTroDisplay(originalRole);
            ViewBag.IsBusinessVerified = originalVerification;
            return View(model);
        }

        // GET: /admin/users/delete/5
        [HttpGet("delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
             _logger.LogInformation("Delete GET: Đang tải người dùng ID: {UserId} để xác nhận XÓA VĨNH VIỄN.", id);

            var user = await _context.NguoiDungs
                                     .Include(u => u.ThanhPho) // Include để hiển thị thêm thông tin trên trang xác nhận
                                     .Where(m => m.Id == id && m.LoaiTk != LoaiTaiKhoan.quantrivien)
                                     .AsNoTracking() // Không cần track entity chỉ để hiển thị xác nhận
                                     .FirstOrDefaultAsync();

            if (user == null)
            {
                _logger.LogWarning("Delete GET: Không tìm thấy người dùng ID {UserId} để xóa hoặc là Admin.", id);
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hoặc không thể xóa tài khoản này.";
                return RedirectToAction(nameof(Index));
            }

            // Truyền người dùng sang view xác nhận xóa (Views/AdminUser/Delete.cshtml)
            // View này nên có cảnh báo rõ ràng về việc xóa vĩnh viễn.
            return View(user);
        }

        // POST: /admin/users/delete/5 (Thực hiện Xóa Vĩnh Viễn)
        [HttpPost("delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string? viewMode) // Thêm viewMode để redirect về đúng view
        {
            _logger.LogWarning("DeleteConfirmed POST (Xóa Vĩnh Viễn): Bắt đầu xử lý xóa vĩnh viễn cho người dùng ID: {UserId}", id);

            // Lấy người dùng cần xóa (Không include nhiều nếu đã cấu hình Cascade Delete đúng)
            // ** QUAN TRỌNG: Nếu Cascade Delete không được thiết lập đúng trong DB, bạn cần Include và xóa thủ công các bản ghi liên quan TRƯỚC KHI xóa NguoiDung **
            // Ví dụ: .Include(u => u.ThongBaos).Include(u => u.TinDaLuus)...
            var userToDelete = await _context.NguoiDungs
                                           .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                                           .FirstOrDefaultAsync(); // Cần entity để Remove

            if (userToDelete == null)
            {
                _logger.LogWarning("DeleteConfirmed POST: Xóa vĩnh viễn thất bại - Không tìm thấy người dùng ID {UserId} hoặc là Admin.", id);
                TempData["ErrorMessage"] = "Không tìm thấy người dùng để xóa hoặc không thể xóa tài khoản này.";
                return RedirectToAction(nameof(Index), new { viewMode });
            }

            string userName = userToDelete.HoTen ?? userToDelete.Email; // Dùng cho log/thông báo

            using var transaction = await _context.Database.BeginTransactionAsync(); // Bắt đầu transaction
            try
            {
                // --- Xóa Thủ công Dữ liệu Liên quan (NẾU CẦN THIẾT) ---
                // Ví dụ: Xóa thông báo của người dùng này
                // var notifications = await _context.ThongBaos.Where(n => n.NguoiDungId == id).ToListAsync();
                // if(notifications.Any()) _context.ThongBaos.RemoveRange(notifications);
                // Ví dụ: Xóa các tin đã lưu
                // var savedJobs = await _context.TinDaLuus.Where(s => s.NguoiDungId == id).ToListAsync();
                // if(savedJobs.Any()) _context.TinDaLuus.RemoveRange(savedJobs);
                // Cần xử lý cẩn thận các mối quan hệ khác (UngTuyen, TinTuyenDung...) dựa trên cấu hình ON DELETE của bạn.
                // -------------------------------------------------------

                // Thực hiện Xóa Vĩnh Viễn
                _context.NguoiDungs.Remove(userToDelete);
                await _context.SaveChangesAsync(); // Lưu thay đổi (xóa)

                await transaction.CommitAsync(); // Commit transaction nếu mọi thứ thành công

                _logger.LogWarning("DeleteConfirmed POST: Đã XÓA VĨNH VIỄN thành công người dùng ID {UserId} ('{UserName}') khỏi cơ sở dữ liệu.", id, userName);
                TempData["SuccessMessage"] = $"Đã xóa vĩnh viễn thành công người dùng '{userName}'.";

            }
            catch (DbUpdateException ex) // Bắt lỗi ràng buộc khóa ngoại (FK) ngăn xóa
            {
                await transaction.RollbackAsync(); // Rollback transaction
                _logger.LogError(ex, "DeleteConfirmed POST: Lỗi DB khi xóa vĩnh viễn người dùng ID {UserId} ('{UserName}'). Có thể do ràng buộc khóa ngoại. Inner: {Inner}", id, userName, ex.InnerException?.Message);
                // Cung cấp thông báo lỗi cụ thể hơn nếu có thể
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi khi xóa người dùng '{userName}'. Dữ liệu liên quan (ví dụ: tin đã đăng, ứng tuyển đã nhận) có thể ngăn chặn việc xóa. Lỗi: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex) // Bắt các lỗi không xác định khác
            {
                 await transaction.RollbackAsync(); // Rollback transaction
                _logger.LogError(ex, "DeleteConfirmed POST: Lỗi không xác định khi xóa vĩnh viễn người dùng ID {UserId} ('{UserName}').", id, userName);
                TempData["ErrorMessage"] = $"Đã xảy ra lỗi không xác định khi xóa người dùng '{userName}'.";
            }

            // Chuyển hướng về trang Index, giữ nguyên chế độ xem nếu có
            return RedirectToAction(nameof(Index), new { viewMode });
        }


        // POST: /admin/users/ban/5
        [HttpPost("ban/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ban(int id)
        {
             _logger.LogInformation("Ban POST: Đang cố gắng đình chỉ người dùng ID: {UserId}", id);
             var user = await _context.NguoiDungs
                                     .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                                     .FirstOrDefaultAsync();

             string referer = Request.Headers["Referer"].ToString(); // Lấy referer để quay lại trang trước đó

             if (user == null)
             {
                 _logger.LogWarning("Ban POST: Đình chỉ thất bại - Không tìm thấy người dùng ID {UserId} hoặc là Admin.", id);
                 TempData["ErrorMessage"] = "Không tìm thấy người dùng để đình chỉ.";
                 return RedirectToRefererOrDefault(referer, id); // Chuyển hướng về trang trước hoặc Index
             }

            if (user.TrangThaiTk != TrangThaiTaiKhoan.bidinhchi)
            {
                user.TrangThaiTk = TrangThaiTaiKhoan.bidinhchi; // Đánh dấu là bị đình chỉ
                user.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Ban POST: Người dùng ID {UserId} ('{UserEmail}') đã bị đình chỉ (trạng thái: bidinhchi) bởi admin.", id, user.Email);
                TempData["SuccessMessage"] = $"Người dùng '{user.HoTen}' đã bị đình chỉ thành công.";
            }
            else
            {
                 _logger.LogInformation("Ban POST: Người dùng ID {UserId} ('{UserEmail}') đã ở trạng thái bị đình chỉ.", id, user.Email);
                 TempData["WarningMessage"] = $"Người dùng '{user.HoTen}' đã ở trạng thái bị đình chỉ.";
            }

            return RedirectToRefererOrDefault(referer, id); // Chuyển hướng về trang trước hoặc Index
        }

        // POST: /admin/users/unban/5
        [HttpPost("unban/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unban(int id)
        {
             _logger.LogInformation("Unban POST: Đang cố gắng kích hoạt lại người dùng ID: {UserId}", id);
            var user = await _context.NguoiDungs
                                     .Include(u => u.HoSoDoanhNghiep) // Cần để xác định trạng thái đúng sau khi bỏ cấm
                                     .Where(u => u.Id == id && u.LoaiTk != LoaiTaiKhoan.quantrivien)
                                     .FirstOrDefaultAsync();

            string referer = Request.Headers["Referer"].ToString(); // Lấy referer

            if (user == null)
            {
                 _logger.LogWarning("Unban POST: Kích hoạt lại thất bại - Không tìm thấy người dùng ID {UserId} hoặc là Admin.", id);
                 TempData["ErrorMessage"] = "Không tìm thấy người dùng để kích hoạt lại.";
                 return RedirectToRefererOrDefault(referer, id);
            }

            if (user.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi) // Chỉ bỏ cấm nếu đang bị cấm
            {
                // Xác định trạng thái đúng để quay về:
                // - Nếu là Doanh nghiệp VÀ CHƯA xác minh -> Chờ xác minh
                // - Các trường hợp khác -> Kích hoạt
                user.TrangThaiTk = (user.LoaiTk == LoaiTaiKhoan.doanhnghiep && user.HoSoDoanhNghiep?.DaXacMinh == false)
                                   ? TrangThaiTaiKhoan.choxacminh
                                   : TrangThaiTaiKhoan.kichhoat;

                user.NgayCapNhat = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Unban POST: Người dùng ID {UserId} ('{UserEmail}') đã được kích hoạt lại. Trạng thái mới: {NewStatus}", id, user.Email, user.TrangThaiTk);
                TempData["SuccessMessage"] = $"Người dùng '{user.HoTen}' đã được kích hoạt lại.";
            }
             else
             {
                  _logger.LogInformation("Unban POST: Người dùng ID {UserId} ('{UserEmail}') hiện không ở trạng thái bị đình chỉ. Trạng thái hiện tại: {CurrentStatus}", id, user.Email, user.TrangThaiTk);
                  TempData["WarningMessage"] = $"Người dùng '{user.HoTen}' không ở trạng thái bị đình chỉ.";
             }

            return RedirectToRefererOrDefault(referer, id);
        }

        // POST: /admin/users/verify/5
        [HttpPost("verify/{userId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyBusiness(int userId)
        {
             _logger.LogInformation("VerifyBusiness POST: Đang cố gắng xác minh hồ sơ doanh nghiệp cho Người dùng ID: {UserId}", userId);

            string referer = Request.Headers["Referer"].ToString(); // Lấy referer

            var user = await _context.NguoiDungs
                                    .Include(u => u.HoSoDoanhNghiep)
                                    .Where(u => u.Id == userId
                                                && u.LoaiTk == LoaiTaiKhoan.doanhnghiep // Phải là doanh nghiệp
                                                && u.LoaiTk != LoaiTaiKhoan.quantrivien) // Không phải admin
                                    .FirstOrDefaultAsync();

            if (user == null)
            {
                 _logger.LogWarning("VerifyBusiness POST: Xác minh thất bại - Không tìm thấy NTD ID {UserId} hoặc không phải loại doanh nghiệp.", userId);
                 TempData["ErrorMessage"] = "Không tìm thấy Nhà tuyển dụng hoặc người dùng không phải loại Doanh nghiệp.";
                  return RedirectToRefererOrDefault(referer, userId, true); // Ép về Index nếu không tìm thấy user
            }

            if (user.HoSoDoanhNghiep == null)
            {
                 // Trường hợp này cho thấy dữ liệu không nhất quán. NTD phải luôn có hồ sơ.
                 _logger.LogError("VerifyBusiness POST: Xác minh thất bại - NTD ID {UserId} ('{UserEmail}') bị thiếu bản ghi HoSoDoanhNghiep.", userId, user.Email);
                 TempData["ErrorMessage"] = "Lỗi dữ liệu: Hồ sơ doanh nghiệp bị thiếu. Không thể xác minh.";
                 return RedirectToRefererOrDefault(referer, userId);
            }

            // Lấy ID của Admin hiện tại từ Claims
            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(adminIdStr, out int currentAdminId))
            {
                 _logger.LogError("VerifyBusiness POST: Xác minh thất bại - Không thể lấy ID của Admin hiện tại từ Claims.");
                 TempData["ErrorMessage"] = "Lỗi hệ thống: Không thể xác định quản trị viên thực hiện.";
                 return RedirectToRefererOrDefault(referer, userId);
            }

            // Kiểm tra nếu đã xác minh và đang hoạt động
            if (user.HoSoDoanhNghiep.DaXacMinh && user.TrangThaiTk == TrangThaiTaiKhoan.kichhoat)
            {
                  _logger.LogInformation("VerifyBusiness POST: Bỏ qua - NTD ID {UserId} ('{UserEmail}') đã được xác minh và đang hoạt động.", userId, user.Email);
                  TempData["WarningMessage"] = $"Doanh nghiệp '{user.HoSoDoanhNghiep.TenCongTy}' đã được xác minh trước đó.";
                  return RedirectToRefererOrDefault(referer, userId);
            }

            // Thực hiện Xác minh
            user.HoSoDoanhNghiep.DaXacMinh = true;
            user.HoSoDoanhNghiep.AdminXacMinhId = currentAdminId;
            user.HoSoDoanhNghiep.NgayXacMinh = DateTime.UtcNow;
            user.TrangThaiTk = TrangThaiTaiKhoan.kichhoat; // Kích hoạt tài khoản khi xác minh
            user.NgayCapNhat = DateTime.UtcNow; // Cập nhật cả timestamp của người dùng

            await _context.SaveChangesAsync();

            _logger.LogInformation("VerifyBusiness POST: Hồ sơ Doanh nghiệp của NTD ID {UserId} ('{UserEmail}') đã được xác minh bởi Admin ID {AdminId}. Trạng thái người dùng: {Status}.", userId, user.Email, currentAdminId, user.TrangThaiTk);
            TempData["SuccessMessage"] = $"Doanh nghiệp '{user.HoSoDoanhNghiep.TenCongTy}' đã được xác minh và kích hoạt thành công.";

            return RedirectToRefererOrDefault(referer, userId); // Chuyển hướng về trang trước đó (thường là Details)
        }


        // POST: /admin/users/bulk-action
        [HttpPost("bulk-action")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(List<int> userIds, string action, string? notificationMessage, string? notificationType)
        {
            string actionDisplay = GetActionDisplayName(action); // Lấy tên hành động hiển thị tiếng Việt
            _logger.LogInformation("BulkAction POST: Thực hiện hành động '{ActionName}' cho {Count} ID người dùng được chọn.", actionDisplay, userIds?.Count ?? 0);

            if (userIds == null || !userIds.Any())
            {
                TempData["WarningMessage"] = "Vui lòng chọn ít nhất một người dùng để thực hiện hành động.";
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            int failCount = 0;
            var processedUserIdentifiers = new List<string>(); // Log email/ID của user đã xử lý thành công

            // Lấy danh sách người dùng cần xử lý (loại trừ admin, include dữ liệu cần thiết)
            var usersToProcess = await _context.NguoiDungs
                                         .Include(u => u.HoSoDoanhNghiep) // Cần cho logic Unban
                                         // Include thêm nếu cần cho các hành động khác (VD: xóa cascade thủ công)
                                         .Where(u => userIds.Contains(u.Id)
                                                    && u.LoaiTk != LoaiTaiKhoan.quantrivien) // Loại trừ Admin
                                         .ToListAsync();

            var foundUserIds = usersToProcess.Select(u => u.Id).ToList();
            var invalidOrSkippedIds = userIds.Except(foundUserIds).ToList();

            if (invalidOrSkippedIds.Any())
            {
                failCount += invalidOrSkippedIds.Count;
                _logger.LogWarning("BulkAction '{ActionName}': {InvalidCount} ID không hợp lệ, không tìm thấy hoặc là Admin và sẽ được bỏ qua: {SkippedIds}",
                                   actionDisplay, invalidOrSkippedIds.Count, string.Join(", ", invalidOrSkippedIds));
            }

            // Chuẩn bị danh sách cho các thao tác CSDL hàng loạt
            var usersToUpdate = new List<NguoiDung>(); // Cho Ban/Unban (EF Core tự track, nhưng có thể dùng nếu cần UpdateRange)
            var usersToDelete = new List<NguoiDung>(); // Cho Xóa Vĩnh Viễn
            var notificationsToAdd = new List<ThongBao>(); // Cho Gửi Thông báo

            bool requiresSaveChanges = false; // Cờ để tránh gọi SaveChangesAsync không cần thiết

            // Xử lý từng người dùng hợp lệ
            foreach (var user in usersToProcess)
            {
                string userIdentifier = user.Email ?? $"ID:{user.Id}";
                try
                {
                    switch (action.ToLowerInvariant()) // Sử dụng ToLowerInvariant để so sánh không phân biệt hoa thường
                    {
                        case "activate": // Alias cho unban
                        case "unban":
                            if (user.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi) // Chỉ bỏ cấm nếu đang bị cấm
                            {
                                // Xác định trạng thái đúng (logic như unban đơn lẻ)
                                TrangThaiTaiKhoan newStatus = (user.LoaiTk == LoaiTaiKhoan.doanhnghiep && user.HoSoDoanhNghiep?.DaXacMinh == false)
                                                               ? TrangThaiTaiKhoan.choxacminh
                                                               : TrangThaiTaiKhoan.kichhoat;
                                if (user.TrangThaiTk != newStatus) // Kiểm tra xem trạng thái có thực sự thay đổi không
                                {
                                    user.TrangThaiTk = newStatus;
                                    user.NgayCapNhat = DateTime.UtcNow;
                                    // EF Core theo dõi thay đổi, không cần add vào list update tường minh
                                    requiresSaveChanges = true;
                                    successCount++;
                                    processedUserIdentifiers.Add(userIdentifier);
                                } else {
                                     _logger.LogInformation("BulkAction '{ActionName}': Người dùng {UserIdentifier} đang bị cấm nhưng trạng thái đích giống nhau ({NewStatus}). Bỏ qua cập nhật.", actionDisplay, userIdentifier, newStatus);
                                }
                            }
                            else {
                                 _logger.LogInformation("BulkAction '{ActionName}': Bỏ qua người dùng {UserIdentifier} vì không ở trạng thái bị đình chỉ.", actionDisplay, userIdentifier);
                            }
                            break;

                        case "ban":
                            if (user.TrangThaiTk != TrangThaiTaiKhoan.bidinhchi) // Chỉ cấm nếu chưa bị cấm
                            {
                                user.TrangThaiTk = TrangThaiTaiKhoan.bidinhchi;
                                user.NgayCapNhat = DateTime.UtcNow;
                                // EF Core theo dõi thay đổi
                                requiresSaveChanges = true;
                                successCount++;
                                processedUserIdentifiers.Add(userIdentifier);
                            }
                             else {
                                 _logger.LogInformation("BulkAction '{ActionName}': Bỏ qua người dùng {UserIdentifier} vì đã ở trạng thái bị đình chỉ.", actionDisplay, userIdentifier);
                            }
                            break;

                        case "notify":
                            if (!string.IsNullOrWhiteSpace(notificationMessage))
                            {
                                notificationsToAdd.Add(new ThongBao
                                {
                                    NguoiDungId = user.Id,
                                    LoaiThongBao = string.IsNullOrWhiteSpace(notificationType) ? "Thông báo từ Admin" : notificationType.Trim(),
                                    DuLieu = JsonSerializer.Serialize(new { message = notificationMessage.Trim() }), // Lưu message vào JSON
                                    DaDoc = false,
                                    NgayTao = DateTime.UtcNow
                                });
                                requiresSaveChanges = true; // Cần lưu thông báo
                                successCount++; // Đếm việc *tạo* thông báo thành công
                                processedUserIdentifiers.Add(userIdentifier);
                            }
                            else
                            {
                                 _logger.LogWarning("BulkAction '{ActionName}': Bỏ qua người dùng {UserIdentifier} vì nội dung thông báo trống.", actionDisplay, userIdentifier);
                                 failCount++; // Đếm là thất bại nếu nội dung trống cho hành động notify
                            }
                            break;

                        case "delete": // Xóa Vĩnh Viễn
                            // --- Xóa Thủ công Dữ liệu Liên quan (NẾU CẦN) ---
                            // Thêm logic ở đây nếu cascade delete không đủ
                            // await DeleteRelatedUserDataAsync(user.Id); // Ví dụ gọi hàm xóa riêng
                            // ------------------------------------------------
                            usersToDelete.Add(user); // Thêm vào danh sách chờ xóa
                            requiresSaveChanges = true;
                            successCount++;
                            processedUserIdentifiers.Add(userIdentifier);
                            break;

                        default:
                            _logger.LogWarning("BulkAction: Hành động '{Action}' không được hỗ trợ.", action);
                            TempData["ErrorMessage"] = $"Hành động '{actionDisplay}' không hợp lệ.";
                            // Chuyển hướng ngay lập tức nếu hành động không hợp lệ
                            return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex) // Bắt lỗi khi xử lý từng người dùng trong vòng lặp
                {
                    _logger.LogError(ex, "BulkAction '{ActionName}': Lỗi khi xử lý người dùng {UserIdentifier}.", actionDisplay, userIdentifier);
                    failCount++;
                    // Cân nhắc xóa user khỏi danh sách nếu họ lỗi trước khi lưu (ví dụ: khỏi usersToDelete)
                    if (usersToDelete.Contains(user)) usersToDelete.Remove(user);
                    // Khó xử lý list notification/update nếu lỗi ở đây, nên để SaveChanges báo lỗi sau.
                }
            } // Kết thúc vòng lặp foreach

            // --- Thực hiện Thao tác CSDL ---
            if (requiresSaveChanges)
            {
                 // Sử dụng transaction để đảm bảo tính toàn vẹn, đặc biệt khi có cả Thêm/Sửa/Xóa
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Thêm Thông báo (nếu có) - Thực hiện trước
                    if (notificationsToAdd.Any())
                    {
                        await _context.ThongBaos.AddRangeAsync(notificationsToAdd);
                        _logger.LogInformation("BulkAction '{ActionName}': Đã chuẩn bị {NotificationCount} thông báo để thêm.", actionDisplay, notificationsToAdd.Count);
                    }

                    // 2. Xóa Người dùng (nếu có) - Thực hiện trước SaveChanges nếu là Xóa Vĩnh Viễn
                    if (usersToDelete.Any())
                    {
                        _context.NguoiDungs.RemoveRange(usersToDelete);
                        _logger.LogWarning("BulkAction '{ActionName}': Đã chuẩn bị {DeleteCount} người dùng để XÓA VĨNH VIỄN.", actionDisplay, usersToDelete.Count);
                    }

                    // 3. Lưu Tất cả Thay đổi (bao gồm cả các update được EF Core theo dõi)
                    int affectedRows = await _context.SaveChangesAsync();

                    await transaction.CommitAsync(); // Commit transaction nếu SaveChanges thành công

                    _logger.LogInformation("BulkAction '{ActionName}': Đã lưu thành công thay đổi vào CSDL. Số dòng ảnh hưởng: {AffectedRows}. Số lượng thành công: {SuccessCount}. Danh sách đã xử lý: {UserIdentifiers}",
                                           actionDisplay, affectedRows, successCount, string.Join(", ", processedUserIdentifiers));

                    if (successCount > 0)
                    {
                        TempData["SuccessMessage"] = $"Đã {actionDisplay.ToLower()} thành công cho {successCount} người dùng/thông báo.";
                    }

                }
                catch (DbUpdateException ex) // Bắt lỗi FK, concurrency,... trong SaveChanges
                {
                    await transaction.RollbackAsync(); // Rollback transaction
                    _logger.LogError(ex, "BulkAction '{ActionName}': Lỗi CSDL nghiêm trọng khi lưu thay đổi. Inner: {Inner}", actionDisplay, ex.InnerException?.Message);
                    TempData["ErrorMessage"] = $"Đã xảy ra lỗi CSDL khi lưu thay đổi (có thể do ràng buộc dữ liệu liên quan khi xóa). Một số hành động có thể chưa hoàn tất. Lỗi: {ex.InnerException?.Message ?? ex.Message}";
                    // Reset số lượng thành công vì batch đã lỗi
                    successCount = 0;
                    // Cập nhật số lượng thất bại gần đúng (khó biết chính xác cái nào lỗi trong batch)
                    failCount = userIds.Count - invalidOrSkippedIds.Count;
                }
                catch (Exception ex) // Bắt các lỗi không xác định khác trong quá trình lưu
                {
                    await transaction.RollbackAsync(); // Rollback transaction
                    _logger.LogError(ex, "BulkAction '{ActionName}': Lỗi không xác định khi lưu thay đổi vào CSDL.", actionDisplay);
                    TempData["ErrorMessage"] = $"Đã xảy ra lỗi không xác định khi lưu thay đổi. Một số hành động có thể chưa hoàn tất.";
                    successCount = 0;
                    failCount = userIds.Count - invalidOrSkippedIds.Count;
                }
            }
            else if (successCount == 0 && failCount == 0 && !invalidOrSkippedIds.Any()) // Không có thay đổi nào cần thực hiện hoặc được thử
            {
               // Cung cấp phản hồi nếu không có gì xảy ra nhưng không phải do lỗi/bỏ qua
               if(action.ToLowerInvariant() == "notify" && string.IsNullOrWhiteSpace(notificationMessage)) {
                    TempData["WarningMessage"] = "Nội dung thông báo trống. Không có thông báo nào được gửi.";
               } else {
                    TempData["InfoMessage"] = $"Không có người dùng nào cần {actionDisplay.ToLower()} hoặc không có người dùng hợp lệ nào được chọn.";
               }
            }

            // Gắn thêm thông báo thất bại/bỏ qua nếu cần
            if (failCount > 0 || invalidOrSkippedIds.Any())
            {
                string existingError = TempData["ErrorMessage"] as string ?? "";
                 int totalFailures = failCount + invalidOrSkippedIds.Count; // Tổng số lỗi + bỏ qua
                TempData["ErrorMessage"] = $"{existingError} Không thể {actionDisplay.ToLower()} cho {totalFailures} mục (do lỗi, ID không hợp lệ/Admin, hoặc không cần thay đổi).".Trim();
            }

            return RedirectToAction(nameof(Index)); // Luôn chuyển hướng về Index sau khi xử lý xong
        }


        // --- Phương thức Helper Riêng ---

        // Chuẩn bị dropdown Tỉnh/Thành phố và Quận/Huyện cho view Create/Edit
        private async Task PopulateDropdownsForCreateEditView(int? selectedCityId = null, int? selectedDistrictId = null)
        {
            // Lấy danh sách Tỉnh/TP sắp xếp theo tên
            var cities = await _context.ThanhPhos
                                     .OrderBy(t => t.Ten)
                                     .Select(t => new { t.Id, t.Ten })
                                     .AsNoTracking()
                                     .ToListAsync();
            ViewBag.ThanhPhoId = new SelectList(cities, "Id", "Ten", selectedCityId);

            // Lấy danh sách Quận/Huyện dựa trên TP đã chọn (hoặc rỗng ban đầu)
            var districts = new List<object>(); // Dùng List<object> để linh hoạt
            if (selectedCityId.HasValue && selectedCityId.Value > 0)
            {
                districts.AddRange(await _context.QuanHuyens
                                            .Where(q => q.ThanhPhoId == selectedCityId.Value)
                                            .OrderBy(q => q.Ten)
                                            .Select(q => new { Id = q.Id, Ten = q.Ten }) // Chọn kiểu ẩn danh
                                            .AsNoTracking()
                                            .ToListAsync());
            }
            // Trường hợp: Validation fail, quận đã được chọn nhưng TP có thể chưa đúng
            else if (selectedDistrictId.HasValue && selectedDistrictId > 0)
            {
                 var selectedDistrictInfo = await _context.QuanHuyens
                                                  .Where(q => q.Id == selectedDistrictId.Value)
                                                  .Select(q => new { q.Id, q.Ten, q.ThanhPhoId })
                                                  .AsNoTracking()
                                                  .FirstOrDefaultAsync();
                 if (selectedDistrictInfo != null)
                 {
                     // Nếu TP chưa được chọn hoặc cần sửa, đặt lại và lấy QH của TP đó
                     if (!selectedCityId.HasValue || selectedCityId != selectedDistrictInfo.ThanhPhoId) {
                          selectedCityId = selectedDistrictInfo.ThanhPhoId;
                           // Chọn lại TP trong dropdown
                           ViewBag.ThanhPhoId = new SelectList(cities, "Id", "Ten", selectedCityId);
                           // Lấy lại danh sách QH cho TP mới xác định này
                           districts.AddRange(await _context.QuanHuyens
                                             .Where(q => q.ThanhPhoId == selectedCityId.Value)
                                             .OrderBy(q => q.Ten)
                                             .Select(q => new { Id = q.Id, Ten = q.Ten })
                                             .AsNoTracking()
                                             .ToListAsync());
                     }
                     // Đảm bảo quận đã chọn có trong danh sách (trường hợp hiếm)
                     if (!districts.Any(d => ((dynamic)d).Id == selectedDistrictId.Value)) {
                          districts.Add(new { Id = selectedDistrictInfo.Id, Ten = selectedDistrictInfo.Ten });
                          districts = districts.OrderBy(d => ((dynamic)d).Ten).ToList(); // Giữ sắp xếp
                     }
                 }
            }

            ViewBag.QuanHuyenId = new SelectList(districts, "Id", "Ten", selectedDistrictId);
        }

        // Helper lấy text và class badge cho trạng thái tài khoản (tiếng Việt)
        public static (string Text, string BadgeClass) GetTrangThaiDisplay(TrangThaiTaiKhoan status)
        {
             return status switch
            {
                TrangThaiTaiKhoan.kichhoat => ("Hoạt động", "badge bg-success"),
                TrangThaiTaiKhoan.bidinhchi => ("Bị đình chỉ", "badge bg-danger"),
                TrangThaiTaiKhoan.choxacminh => ("Chờ xác minh", "badge bg-warning text-dark"),
                TrangThaiTaiKhoan.tamdung => ("Tạm dừng", "badge bg-secondary"), // Có thể đổi thành "Tạm dừng"
                _ => ("Không rõ", "badge bg-light text-dark")
            };
        }

        // Helper lấy text hiển thị cho vai trò người dùng (tiếng Việt)
        public static string GetVaiTroDisplay(LoaiTaiKhoan role)
        {
            return role switch
            {
                LoaiTaiKhoan.canhan => "Cá nhân", // Đổi thành Ứng viên
                LoaiTaiKhoan.doanhnghiep => "Doanh nghiệp", // Đổi thành Nhà tuyển dụng
                LoaiTaiKhoan.quantrivien => "Quản trị viên",
                _ => "Không xác định"
            };
        }

         // Helper lấy tên hiển thị cho hành động hàng loạt (tiếng Việt)
         private string GetActionDisplayName(string action)
         {
             return action?.ToLowerInvariant() switch // Dùng InvariantCulture
             {
                 "activate" => "Kích hoạt/Bỏ cấm",
                 "ban" => "Đình chỉ (Cấm)",
                 "unban" => "Bỏ đình chỉ/Kích hoạt",
                 "notify" => "Gửi thông báo",
                 "delete" => "Xóa vĩnh viễn",
                 _ => action ?? "Hành động không rõ"
             };
         }

        // Helper ghi log lỗi ModelState
        private void LogModelStateErrors()
        {
            if (!ModelState.IsValid)
            {
                // Lấy lỗi dưới dạng Dictionary để dễ đọc hơn trong log JSON
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                )
                .Where(kvp => kvp.Value != null && kvp.Value.Length > 0); // Chỉ lấy các key có lỗi

                _logger.LogWarning("ModelState không hợp lệ. Lỗi: {ModelErrors}", JsonSerializer.Serialize(errors));
            }
        }

        // Helper xử lý DbUpdateException (VD: lỗi UNIQUE) và thêm lỗi vào ModelState
        private void HandleDbUpdateException(DbUpdateException ex, object? model = null)
        {
             // Log lỗi gốc và InnerException
             _logger.LogError(ex, "DbUpdateException xảy ra. InnerException: {InnerMessage}", ex.InnerException?.Message);

             // Kiểm tra lỗi UNIQUE constraint (từ khóa có thể thay đổi tùy DB - VD: MySQL dùng 'Duplicate entry')
             if (ex.InnerException != null &&
                 (ex.InnerException.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                  ex.InnerException.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)))
             {
                 // Cố gắng xác định trường bị lỗi dựa trên thông báo lỗi
                 if (ex.InnerException.Message.ToLower().Contains("email"))
                 {
                      // Thêm lỗi vào đúng trường trong ViewModel (cần khớp tên thuộc tính)
                      // Sử dụng nameof để tránh lỗi chính tả
                      ModelState.AddModelError(nameof(NguoiDungCreateViewModel.Email), "Lỗi DB: Địa chỉ email này đã tồn tại.");
                 }
                 else if (ex.InnerException.Message.ToLower().Contains("sdt")) // Giả sử cột SĐT là 'Sdt'
                 {
                     ModelState.AddModelError(nameof(NguoiDungCreateViewModel.Sdt), "Lỗi DB: Số điện thoại này đã tồn tại.");
                 }
                 else if (model is AdminUserEditViewModel && ex.InnerException.Message.ToLower().Contains("masothue")) // Kiểm tra MST nếu là form Edit DN
                 {
                     ModelState.AddModelError(nameof(AdminUserEditViewModel.MaSoThue), "Lỗi DB: Mã số thuế này đã tồn tại.");
                 }
                 else
                 {
                     // Lỗi UNIQUE không xác định được trường cụ thể
                     ModelState.AddModelError("", $"Lỗi DB: Vi phạm ràng buộc UNIQUE. {ex.InnerException.Message}");
                 }
             }
             // Kiểm tra lỗi khóa ngoại (FOREIGN KEY)
             else if (ex.InnerException != null && ex.InnerException.Message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
             {
                  ModelState.AddModelError("", $"Lỗi DB: Vi phạm ràng buộc khóa ngoại. {ex.InnerException.Message}");
             }
             // Kiểm tra lỗi trường bắt buộc (cannot be null) từ DB (thường nên được bắt bởi ModelState trước)
             else if (ex.InnerException != null && ex.InnerException.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase))
             {
                  ModelState.AddModelError("", $"Lỗi DB: Một trường bắt buộc bị bỏ trống. {ex.InnerException.Message}. Vui lòng kiểm tra hồ sơ liên quan nếu có.");
             }
             else // Lỗi DB chung khác
             {
                 ModelState.AddModelError("", $"Đã xảy ra lỗi CSDL khi lưu: {ex.InnerException?.Message ?? ex.Message}");
             }
        }

        // Helper chuyển hướng: ưu tiên quay lại trang referer (details/edit) hoặc về trang Index
        private IActionResult RedirectToRefererOrDefault(string? referer, int entityId, bool forceIndex = false)
        {
            // Kiểm tra xem có ép về Index không, referer có tồn tại và có chứa trang details/edit của entity này không
            if (!forceIndex && !string.IsNullOrEmpty(referer) &&
                (referer.Contains($"/admin/users/details/{entityId}", StringComparison.OrdinalIgnoreCase) ||
                 referer.Contains($"/admin/users/edit/{entityId}", StringComparison.OrdinalIgnoreCase)))
            {
                // An toàn hơn là chuyển hướng tường minh về action Details thay vì dùng referer trực tiếp
                _logger.LogDebug("Chuyển hướng về trang Details cho ID: {EntityId}", entityId);
                return RedirectToAction(nameof(Details), new { id = entityId });
                // Hoặc nếu muốn dùng referer (cẩn thận security): return Redirect(referer);
            }
            // Nếu không thỏa mãn các điều kiện trên, chuyển về trang Index
             _logger.LogDebug("Referer không phù hợp hoặc không tìm thấy ('{Referer}'). Chuyển hướng về Index.", referer ?? "NULL");
            return RedirectToAction(nameof(Index));
        }

    }
}