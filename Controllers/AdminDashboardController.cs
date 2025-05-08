// File: Controllers/AdminDashboardController.cs
using HeThongTimViec.Data;
using HeThongTimViec.Models;
// Remove the using for MonthlyGoalProgressViewModel as it's being replaced
// using HeThongTimViec.ViewModels.Admin;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations; // Required for DisplayAttribute and EnumExtensions
using System.Text.Json; // Required for logging JSON
using System.Reflection; // Required for EnumExtensions reflection

namespace HeThongTimViec.Controllers
{
    // --- Controller Definition ---
    // Handles requests for the Admin Dashboard pages and API endpoints.
    [Route("admin/dashboard")] // Base route for all actions in this controller
    [Authorize(Roles = nameof(LoaiTaiKhoan.quantrivien))] // Restrict access to users with the 'quantrivien' role
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // Disable caching for dashboard data
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context; // Database context for data access
        private readonly ILogger<AdminDashboardController> _logger; // Logger for recording information and errors

        // Constructor for Dependency Injection
        public AdminDashboardController(ApplicationDbContext context, ILogger<AdminDashboardController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // --- Main Dashboard Overview Action ---
        // Loads the main dashboard view and populates the static overview cards.
        // Routes: /admin/dashboard and /admin/dashboard/overview
        [HttpGet("")]
        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            _logger.LogInformation("ADMIN DASHBOARD: Loading Overview data.");
            // --- DIAGNOSTIC CHECK ---
            // If counts below are 0, VERIFY DATABASE CONTENT MANUALLY.
            // Check the ACTUAL string values in LoaiTk, TrangThai (TinTuyenDung), TrangThaiXuLy (BaoCaoViPham) columns.
            // They MUST EXACTLY match the enum names (e.g., 'canhan', 'doanhnghiep', 'daduyet', 'choduyet', 'moi') including casing.
            // Check DEBUG logs below for exact queries and results.
            // The DbContext uses HasConversion<string>(), so EF Core *should* compare against these strings.
            // -------------------------
            try
            {
                // --- Fetch Core Statistics Asynchronously ---

                // Total Users (Non-Admin)
                var userTypeFilterAdmin = LoaiTaiKhoan.quantrivien;
                _logger.LogDebug("Querying Total Users where LoaiTk != '{AdminTypeString}'...", userTypeFilterAdmin.ToString());
                int totalUsers = await _context.NguoiDungs
                    .CountAsync(u => u.LoaiTk != userTypeFilterAdmin); // EF Core compares Enum value against configured string conversion
                _logger.LogInformation("Total Users count (excluding admin): {Count}", totalUsers);
                if (totalUsers == 0) _logger.LogWarning("ADMIN DASHBOARD: CHECK DB - Total Users count (excluding admin) is zero. Verify NguoiDungs table where LoaiTk column is NOT '{AdminTypeString}'. Expected values like 'canhan', 'doanhnghiep'.", userTypeFilterAdmin.ToString());

                // Total Employers
                var employerTypeFilter = LoaiTaiKhoan.doanhnghiep;
                _logger.LogDebug("Querying Total Employers where LoaiTk == '{EmployerTypeString}'...", employerTypeFilter.ToString());
                int totalEmployers = await _context.NguoiDungs
                    .CountAsync(u => u.LoaiTk == employerTypeFilter);
                _logger.LogInformation("Total Employer Accounts count: {Count}", totalEmployers);
                if (totalEmployers == 0) _logger.LogWarning("ADMIN DASHBOARD: CHECK DB - Total Employer Accounts count is zero. Verify NguoiDungs table where LoaiTk column IS '{EmployerTypeString}'.", employerTypeFilter.ToString());

                // Total Active/Pending Job Postings
                var activePostingStates = new[] { TrangThaiTinTuyenDung.daduyet, TrangThaiTinTuyenDung.choduyet };
                var activePostingStatesStrings = activePostingStates.Select(s => s.ToString()).ToArray(); // For logging
                _logger.LogDebug("Querying Total Job Postings where TrangThai is IN ['{ActiveStatesString}']...", string.Join("', '", activePostingStatesStrings));
                int totalJobPostings = await _context.TinTuyenDungs
                    .CountAsync(t => activePostingStates.Contains(t.TrangThai)); // EF Core compares Enum values
                _logger.LogInformation("Total Active/Pending Job Postings count: {Count}", totalJobPostings);
                if (totalJobPostings == 0) _logger.LogWarning("ADMIN DASHBOARD: CHECK DB - Total Active/Pending Job Postings count is zero. Verify TinTuyenDungs table where TrangThai column is '{State1}' or '{State2}'.", TrangThaiTinTuyenDung.daduyet.ToString(), TrangThaiTinTuyenDung.choduyet.ToString());

                // Pending Reports
                var pendingReportState = TrangThaiXuLyBaoCao.moi;
                 _logger.LogDebug("Querying Pending Reports where TrangThaiXuLy == '{PendingStateString}'...", pendingReportState.ToString());
                int pendingReports = await _context.BaoCaoViPhams
                    .CountAsync(b => b.TrangThaiXuLy == pendingReportState); // EF Core compares Enum value
                _logger.LogInformation("Pending Reports count: {Count}", pendingReports);
                if (pendingReports == 0) _logger.LogWarning("ADMIN DASHBOARD: CHECK DB - Pending Reports count is zero. Verify BaoCaoViPhams table where TrangThaiXuLy column is '{PendingStateString}'.", pendingReportState.ToString());

                // Log if all key stats are zero
                if (totalUsers == 0 && totalEmployers == 0 && totalJobPostings == 0 && pendingReports == 0)
                {
                    _logger.LogWarning("ADMIN DASHBOARD: All key overview statistics are zero. Database might be empty OR data does not match filter criteria (check exact string enum values in DB, including casing!) OR potential EF Core enum comparison issue.");
                }

                // --- Calculate Growth Percentages (vs Previous Month) ---
                // (Logic remains the same, relies on the counts above being correct)
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
                var startOfCurrentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                _logger.LogDebug("Calculating growth percentages compared to data before {Date}", oneMonthAgo);

                // User Growth Calculation
                _logger.LogDebug("Querying new users since {Date} (excluding admin)...", oneMonthAgo);
                int newUsersLastMonth = await _context.NguoiDungs.CountAsync(u => u.LoaiTk != userTypeFilterAdmin && u.NgayTao >= oneMonthAgo);
                int totalUsersBeforeLastMonth = totalUsers - newUsersLastMonth;
                double userGrowthPercentage = (totalUsersBeforeLastMonth > 0) ? ((double)newUsersLastMonth / totalUsersBeforeLastMonth) * 100 : (newUsersLastMonth > 0 ? 100.0 : 0.0);
                 _logger.LogDebug("User growth: New={NewCount}, TotalBefore={TotalBeforeCount}, Growth={Growth}%", newUsersLastMonth, totalUsersBeforeLastMonth, userGrowthPercentage);

                // Employer Growth Calculation
                _logger.LogDebug("Querying new employers since {Date}...", oneMonthAgo);
                int newEmployersLastMonth = await _context.NguoiDungs.CountAsync(u => u.LoaiTk == employerTypeFilter && u.NgayTao >= oneMonthAgo);
                int totalEmployersBeforeLastMonth = totalEmployers - newEmployersLastMonth;
                double employerGrowthPercentage = (totalEmployersBeforeLastMonth > 0) ? ((double)newEmployersLastMonth / totalEmployersBeforeLastMonth) * 100 : (newEmployersLastMonth > 0 ? 100.0 : 0.0);
                _logger.LogDebug("Employer growth: New={NewCount}, TotalBefore={TotalBeforeCount}, Growth={Growth}%", newEmployersLastMonth, totalEmployersBeforeLastMonth, employerGrowthPercentage);

                // Job Posting Growth Calculation
                _logger.LogDebug("Querying new job posts since {Date} with states ['{ActiveStatesString}']...", oneMonthAgo, string.Join("', '", activePostingStatesStrings));
                int newPostsLastMonth = await _context.TinTuyenDungs.CountAsync(t => t.NgayTao >= oneMonthAgo && activePostingStates.Contains(t.TrangThai));
                int totalPostsBeforeLastMonth = totalJobPostings - newPostsLastMonth;
                double postGrowthPercentage = (totalPostsBeforeLastMonth > 0) ? ((double)newPostsLastMonth / totalPostsBeforeLastMonth) * 100 : (newPostsLastMonth > 0 ? 100.0 : 0.0);
                _logger.LogDebug("Job Post growth: New={NewCount}, TotalBefore={TotalBeforeCount}, Growth={Growth}%", newPostsLastMonth, totalPostsBeforeLastMonth, postGrowthPercentage);

                // New Pending Reports This Month
                _logger.LogDebug("Querying new pending reports since {Date}...", startOfCurrentMonth);
                int newReportsThisMonthStillPending = await _context.BaoCaoViPhams.CountAsync(b => b.TrangThaiXuLy == pendingReportState && b.NgayBaoCao >= startOfCurrentMonth);
                _logger.LogDebug("New Pending Reports This Month: {Count}", newReportsThisMonthStillPending);

                _logger.LogInformation("Calculated Growth - Users: {UserGrowth}%, Employers: {EmployerGrowth}%, Posts: {PostGrowth}%, New Pending Reports Created This Month: {NewPendingReports}", userGrowthPercentage.ToString("N1"), employerGrowthPercentage.ToString("N1"), postGrowthPercentage.ToString("N1"), newReportsThisMonthStillPending);

                // --- Pass Data to View via ViewBag ---
                ViewBag.TotalUsers = totalUsers;
                ViewBag.UserGrowthPercentage = userGrowthPercentage;
                ViewBag.TotalEmployers = totalEmployers;
                ViewBag.EmployerGrowthPercentage = employerGrowthPercentage;
                ViewBag.TotalJobPostings = totalJobPostings;
                ViewBag.PostGrowthPercentage = postGrowthPercentage;
                ViewBag.PendingReports = pendingReports;
                ViewBag.NewPendingReportsThisMonth = newReportsThisMonthStillPending; // Corrected Key

                _logger.LogInformation("ADMIN DASHBOARD: Overview data loading completed successfully.");
                return View("AdminDashboard"); // Ensure Views/AdminDashboard/AdminDashboard.cshtml exists
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ADMIN DASHBOARD: CRITICAL Error loading Overview data.");
                ViewBag.ErrorMessage = "Đã xảy ra lỗi nghiêm trọng khi tải dữ liệu tổng quan. Vui lòng kiểm tra logs và liên hệ quản trị viên.";
                // Set defaults on error
                ViewBag.TotalUsers = 0; ViewBag.UserGrowthPercentage = 0.0;
                ViewBag.TotalEmployers = 0; ViewBag.EmployerGrowthPercentage = 0.0;
                ViewBag.TotalJobPostings = 0; ViewBag.PostGrowthPercentage = 0.0;
                ViewBag.PendingReports = 0; ViewBag.NewPendingReportsThisMonth = 0;
                return View("AdminDashboard");
            }
        }

        // --- API Endpoint: User Growth Chart Data ---
        // (Relies on NguoiDung counts being correct - check Overview logs first if this is wrong)
        [HttpGet("user-growth-data")]
        public async Task<IActionResult> GetUserGrowthData(string period = "week")
        {
             _logger.LogInformation("API: Fetching user growth data for period: {Period}", period);
             // *** USER GROWTH CHART DIAGNOSTICS ***
             // If chart doesn't show today's data:
             // 1. Verify NgayTao column in NguoiDung table is stored correctly (ideally UTC).
             // 2. Check server time (UtcNow) is accurate.
             // 3. Check database query times - maybe data for today hasn't propagated yet?
             // 4. Check Debug logs below for `startDate`, `endDate`, `allDatesInRange`, and `dailyData` counts.
             // The logic aims to include data *up to* the beginning of tomorrow (UTC).
             // Example: If today is May 3rd, endDate is May 4th 00:00:00 UTC.
             //          GetAllDates includes May 3rd. Query includes NgayTao < May 4th 00:00 UTC.
             // ***************************************
            try
            {
                DateTime startDate;
                var now = DateTime.UtcNow; // Use UTC for consistency
                string labelFormat;
                // End date is exclusive (up to the beginning of the *next* day UTC)
                // This ensures data created *during* the current day (e.g., May 3rd) is included
                // because the comparison is `NgayTao < May 4th 00:00:00 UTC`.
                DateTime endDate = now.Date.AddDays(1).ToUniversalTime();

                switch (period.ToLowerInvariant())
                {
                    case "month":
                        startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        labelFormat = "dd/MM";
                        _logger.LogDebug("Period 'month': Fetching data for {Month}/{Year}", now.Month, now.Year);
                        break;
                    case "year":
                        startDate = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        labelFormat = "MM/yyyy";
                        _logger.LogDebug("Period 'year': Fetching data for year {Year}", now.Year);
                        break;
                    case "week":
                    default:
                        // Start from Monday of the current week (adjust based on locale if needed)
                        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                        startDate = now.Date.AddDays(-diff).ToUniversalTime();
                        // Use a more culture-aware format if needed
                        labelFormat = CultureInfo.CurrentUICulture.Name.StartsWith("vi") ? "dd/MM (ddd)" : "ddd (MM/dd)";
                        period = "week"; // Ensure period is set correctly
                        _logger.LogDebug("Period 'week': Fetching data starting from Monday {StartDate}", startDate);
                        break;
                }
                 _logger.LogDebug("User growth data range: Period='{Period}', StartDate(UTC)='{StartDateUTC:o}', EndDate(UTC)='{EndDateUTC:o}' (exclusive)", period, startDate, endDate);

                List<string> labels;
                List<int> candidateCounts;
                List<int> employerCounts;

                // Define user types for filtering
                var candidateType = LoaiTaiKhoan.canhan;
                var employerType = LoaiTaiKhoan.doanhnghiep;
                var adminType = LoaiTaiKhoan.quantrivien;

                // *** Check Enum Names ***
                _logger.LogTrace("Enum values used for filtering: Candidate='{Candidate}', Employer='{Employer}', Admin='{Admin}'",
                    candidateType.ToString(), employerType.ToString(), adminType.ToString());

                if (period == "year")
                {
                    _logger.LogDebug("Grouping yearly data by month. Querying users where LoaiTk != '{AdminType}' AND NgayTao >= {StartDate:o} AND NgayTao < {EndDate:o}", adminType.ToString(), startDate, endDate);
                    var usersThisYear = await _context.NguoiDungs
                       .Where(u => u.LoaiTk != adminType && u.NgayTao >= startDate && u.NgayTao < endDate)
                       .Select(u => new { u.NgayTao, u.LoaiTk })
                       .ToListAsync();
                    _logger.LogDebug("Fetched {UserCount} non-admin users created between {StartDate:o} and {EndDate:o}", usersThisYear.Count, startDate, endDate);

                    var monthlyGroups = usersThisYear
                       .GroupBy(u => new { u.NgayTao.Year, u.NgayTao.Month })
                       .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                       .Select(g => new {
                           MonthDate = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc), // Ensure UTC Kind
                           CandidateCount = g.Count(u => u.LoaiTk == candidateType),
                           EmployerCount = g.Count(u => u.LoaiTk == employerType)
                       })
                       .ToDictionary(g => g.MonthDate, g => new { g.CandidateCount, g.EmployerCount });

                    labels = new List<string>();
                    candidateCounts = new List<int>();
                    employerCounts = new List<int>();

                    // Iterate through months of the year up to the current month
                    for (int month = 1; month <= 12; month++)
                    {
                        var monthStartDate = new DateTime(startDate.Year, month, 1, 0, 0, 0, DateTimeKind.Utc); // Ensure UTC Kind
                        // Only add label if the month starts *before* the endDate boundary.
                        // This correctly includes the current month.
                        if(monthStartDate < endDate)
                        {
                            labels.Add(monthStartDate.ToString(labelFormat)); // "MM/yyyy"
                            if (monthlyGroups.TryGetValue(monthStartDate, out var counts))
                            {
                                candidateCounts.Add(counts.CandidateCount);
                                employerCounts.Add(counts.EmployerCount);
                            }
                            else
                            {
                                candidateCounts.Add(0);
                                employerCounts.Add(0);
                            }
                        }
                        else
                        {
                            _logger.LogTrace("Skipping future month {Month}/{Year} for chart labels (MonthStartDate >= EndDate).", month, startDate.Year);
                        }
                    }
                     _logger.LogDebug("Prepared yearly data: {LabelCount} labels generated. First: '{FirstLabel}', Last: '{LastLabel}'. Totals: {CandidateTotal} candidates, {EmployerTotal} employers.",
                        labels.Count, labels.FirstOrDefault(), labels.LastOrDefault(), candidateCounts.Sum(), employerCounts.Sum());
                }
                else // Weekly or Monthly view - group by day
                {
                     _logger.LogDebug("Grouping daily data for period '{Period}'.", period);

                    // Helper to generate all dates in the range (inclusive start, exclusive end)
                    // This generates dates up to, but NOT including, end.Date.
                    // Example: If endDate is May 4th 00:00 UTC, it generates up to May 3rd.
                    List<DateTime> GetAllDates(DateTime start, DateTime end) {
                        var dates = new List<DateTime>();
                        // Ensure start date is DateOnly part at UTC midnight
                        var currentDt = DateTime.SpecifyKind(start.Date, DateTimeKind.Utc);
                        // Ensure end date is DateOnly part at UTC midnight for comparison
                        var endDtLimit = DateTime.SpecifyKind(end.Date, DateTimeKind.Utc);

                        while (currentDt < endDtLimit)
                        {
                            dates.Add(currentDt);
                            currentDt = currentDt.AddDays(1);
                        }
                        return dates;
                    }
                    var allDatesInRange = GetAllDates(startDate, endDate);

                    if (!allDatesInRange.Any()){
                        _logger.LogWarning("Date range for user growth chart is empty. Start: {Start:o}, End: {End:o}. Returning empty data.", startDate, endDate);
                        return Ok(new { labels = new List<string>(), datasets = new object[0] });
                    }
                    _logger.LogDebug("Generated {DateCount} dates for labels in range. First: '{FirstDate:yyyy-MM-dd}', Last: '{LastDate:yyyy-MM-dd}'.",
                         allDatesInRange.Count, allDatesInRange.FirstOrDefault(), allDatesInRange.LastOrDefault());

                    // Fetch data grouped by Date (UTC)
                    _logger.LogDebug("Querying daily users where LoaiTk IN ('{CandidateType}', '{EmployerType}') AND NgayTao >= {StartDate:o} AND NgayTao < {EndDate:o}", candidateType.ToString(), employerType.ToString(), startDate, endDate);
                    var dailyData = await _context.NguoiDungs
                        .Where(u => (u.LoaiTk == candidateType || u.LoaiTk == employerType) // Use OR for multiple types
                                    && u.NgayTao >= startDate && u.NgayTao < endDate)
                        .GroupBy(u => u.NgayTao.Date) // Group by Date part (EF Core translates this)
                        .Select(g => new {
                            // g.Key here will be a DateTime representing the Date at 00:00. Ensure UTC kind.
                            Date = DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                            CandidateCount = g.Count(u => u.LoaiTk == candidateType),
                            EmployerCount = g.Count(u => u.LoaiTk == employerType)
                        })
                        // Use the UTC DateTime Key for the dictionary
                        .ToDictionaryAsync(g => g.Date,
                                           g => new { g.CandidateCount, g.EmployerCount });

                    _logger.LogDebug("Fetched {Count} date groups with new user counts from DB.", dailyData.Count);
                    #if DEBUG
                        if (dailyData.Any()) {
                             _logger.LogTrace("Sample daily data fetched: {SampleDataJson}",
                                JsonSerializer.Serialize(dailyData.Take(5).Select(kv => new { Date = kv.Key.ToString("o"), Counts = kv.Value })));
                        }
                    #endif

                    labels = allDatesInRange.Select(d => d.ToString(labelFormat)).ToList();
                    // Use GetValueOrDefault, ensuring the lookup key (d from allDatesInRange) is already UTC kind
                    candidateCounts = allDatesInRange.Select(d => dailyData.GetValueOrDefault(d, null)?.CandidateCount ?? 0).ToList();
                    employerCounts = allDatesInRange.Select(d => dailyData.GetValueOrDefault(d, null)?.EmployerCount ?? 0).ToList();

                     _logger.LogDebug("Prepared daily data: {LabelCount} labels. First: '{FirstLabel}', Last: '{LastLabel}'. Totals: {CandidateTotal} candidates, {EmployerTotal} employers.",
                        labels.Count, labels.FirstOrDefault(), labels.LastOrDefault(), candidateCounts.Sum(), employerCounts.Sum());
                }

                var chartData = new {
                    labels = labels,
                    datasets = new[] {
                        new { label = "Ứng viên mới", data = candidateCounts, borderColor = "rgb(54, 162, 235)", backgroundColor = "rgba(54, 162, 235, 0.5)", tension = 0.1 },
                        new { label = "Doanh nghiệp mới", data = employerCounts, borderColor = "rgb(75, 192, 192)", backgroundColor = "rgba(75, 192, 192, 0.5)", tension = 0.1 }
                    }
                };
                 _logger.LogInformation("API: User growth data prepared successfully for period {Period}. Labels: {LabelCount}, Candidates: {CandCount}, Employers: {EmpCount}",
                    period, labels.Count, candidateCounts.Sum(), employerCounts.Sum());

                #if DEBUG
                try {
                    // Log smaller datasets directly, otherwise just counts
                    if (labels.Count < 50)
                       _logger.LogTrace("User Growth Chart Data (JSON): {ChartDataJson}", JsonSerializer.Serialize(chartData));
                    else
                       _logger.LogTrace("User Growth Chart Data prepared (too large to log full JSON).");
                } catch(Exception jsonEx) {_logger.LogWarning(jsonEx, "Failed to serialize user growth chart data for trace log.");}
                #endif

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting user growth data for period {Period}.", period);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy dữ liệu biểu đồ tăng trưởng." });
            }
        }


        // --- API Endpoint: Recent Activity Feed ---
        // Provides data for the "Recent Activity" list on the dashboard.
        // Route: /admin/dashboard/recent-activity?count={number}
        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity(int count = 7) // Default to 7 items
        {
            if (count <= 0) count = 7;
            if (count > 50) count = 50; // Keep limit reasonable
             _logger.LogInformation("API: Fetching top {Count} recent activities.", count);

            // --- DIAGNOSTIC CHECK ---
            // If this returns empty or "Error loading recent activity", check the DEBUG logs below.
            // It first queries 3 sources: Users, Job Postings, Reports.
            // If ANY of these queries fail OR return 0 results due to enum/string mismatches (see Overview Check),
            // the final list might be empty or missing expected items.
            // Verify enum string values in the database for LoaiTk, TrangThai (TinTuyenDung), TrangThaiXuLy (BaoCaoViPham).
            // -------------------------
            try
            {
                var now = DateTime.UtcNow;

                // --- Query 1: Recent Users (Candidates & Employers) ---
                var userTypeFilterAdmin = LoaiTaiKhoan.quantrivien;
                var candidateType = LoaiTaiKhoan.canhan;
                _logger.LogDebug("Querying recent users (excluding '{AdminTypeString}', limit {Count})...", userTypeFilterAdmin.ToString(), count);
                var recentUsersQuery = _context.NguoiDungs
                   .Where(u => u.LoaiTk != userTypeFilterAdmin)
                   .OrderByDescending(u => u.NgayTao)
                   .Take(count)
                   .Select(u => new RecentActivityItem
                   {
                       Type = u.LoaiTk == candidateType ? "Ứng viên mới" : "Doanh nghiệp mới", // Check canhan vs doanhnghiep
                       Description = $"{(string.IsNullOrEmpty(u.HoTen) ? u.Email : u.HoTen)} (ID: {u.Id})", // Use email if name is missing
                       Timestamp = u.NgayTao, // Assume UTC Kind from DB
                       Icon = u.LoaiTk == candidateType ? "fas fa-user-plus text-success" : "fas fa-building text-info"
                   });

                // --- Query 2: Recent Job Postings (Any status change or creation) ---
                _logger.LogDebug("Querying recent job postings (created/updated, limit {Count}). Including NguoiDang...", count);
                var recentPostsQuery = _context.TinTuyenDungs
                   .Include(t => t.NguoiDang) // Include NguoiDung who posted
                   .OrderByDescending(t => t.NgayCapNhat) // Order by last update time
                   .Take(count)
                   .Select(t => new RecentActivityItem
                   {
                       Type = GetJobPostingActivityType(t.TrangThai), // Helper uses enum
                       Description = $"'{t.TieuDe ?? "(Chưa có tiêu đề)"}'"
                                     + (t.NguoiDang != null ? $" bởi {(string.IsNullOrEmpty(t.NguoiDang.HoTen) ? t.NguoiDang.Email : t.NguoiDang.HoTen)}" : " bởi (Không rõ người đăng)") // Handle null NguoiDang or HoTen
                                     + $" (ID: {t.Id})",
                       Timestamp = t.NgayCapNhat, // Assume UTC Kind from DB
                       Icon = GetJobPostingActivityIcon(t.TrangThai) // Helper uses enum
                   });


                // --- Query 3: Recent Reports (Status 'moi') ---
                var pendingReportState = TrangThaiXuLyBaoCao.moi;
                _logger.LogDebug("Querying recent pending reports (state '{PendingStateString}', limit {Count}). Including TinTuyenDung...", pendingReportState.ToString(), count);
                var recentReportsQuery = _context.BaoCaoViPhams
                   .Include(b => b.TinTuyenDung) // Include related Job Posting for title
                   // No need to include NguoiDang of the TTD here unless needed for description
                   .Where(b => b.TrangThaiXuLy == pendingReportState) // Filter by enum value
                   .OrderByDescending(b => b.NgayBaoCao)
                   .Take(count)
                   .Select(b => new RecentActivityItem
                   {
                       Type = "Báo cáo mới",
                       Description = b.TinTuyenDung != null
                                        ? $"Tin: '{b.TinTuyenDung.TieuDe ?? "(Không có tiêu đề)"}' (Tin ID: {b.TinTuyenDungId})"
                                        : $"Về Tin ID: {b.TinTuyenDungId} (Không tìm thấy chi tiết tin)", // Fallback if Include fails or TTD deleted
                       Timestamp = b.NgayBaoCao, // Assume UTC Kind from DB
                       Icon = "fas fa-flag text-danger"
                   });

                // --- Execute Queries Asynchronously and Log Results ---
                _logger.LogDebug("Executing recent activity source queries...");
                List<RecentActivityItem> recentUsers = new List<RecentActivityItem>();
                List<RecentActivityItem> recentPosts = new List<RecentActivityItem>();
                List<RecentActivityItem> recentReports = new List<RecentActivityItem>();
                string? errorSource = null;

                try
                {
                    _logger.LogDebug("Fetching recent users...");
                    recentUsers = await recentUsersQuery.ToListAsync();
                    _logger.LogDebug("Fetched {Count} recent users.", recentUsers.Count);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error fetching recent users for activity feed."); errorSource = "users"; }

                try
                {
                    _logger.LogDebug("Fetching recent job posts...");
                    recentPosts = await recentPostsQuery.ToListAsync();
                    _logger.LogDebug("Fetched {Count} recent job posts.", recentPosts.Count);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error fetching recent job posts for activity feed."); errorSource = "posts"; }

                try
                {
                    _logger.LogDebug("Fetching recent reports...");
                    recentReports = await recentReportsQuery.ToListAsync();
                    _logger.LogDebug("Fetched {Count} recent pending reports.", recentReports.Count);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error fetching recent reports for activity feed."); errorSource = "reports"; }


                // Check if any source failed or all returned empty
                if (errorSource != null)
                {
                   _logger.LogWarning("API: Recent activity feed may be incomplete due to error fetching from '{Source}'.", errorSource);
                   // Optionally return a partial result or an error depending on requirements
                   // For now, we continue and combine whatever was fetched.
                }
                 if (recentUsers.Count == 0 && recentPosts.Count == 0 && recentReports.Count == 0 && errorSource == null)
                {
                    _logger.LogWarning("ADMIN DASHBOARD: Recent activity queries returned no results. Check database data and enum filters (LoaiTk, TrangThai, TrangThaiXuLy) and date ranges.");
                    // Return empty list - this is not necessarily an error, could be no recent activity.
                    return Ok(new List<object>());
                }


                // --- Combine, Sort, and Limit Results ---
                 _logger.LogDebug("Combining {uCount} users, {pCount} posts, {rCount} reports. Sorting by timestamp (desc) and taking top {Count} activities...",
                     recentUsers.Count, recentPosts.Count, recentReports.Count, count);
                var allActivities = recentUsers
                                     .Concat(recentPosts)
                                     .Concat(recentReports)
                                     .OrderByDescending(a => a.Timestamp)
                                     .Take(count)
                                     .ToList();
                 _logger.LogDebug("Final combined recent activities count: {Count}", allActivities.Count);

                // --- Format for JSON Response ---
                _logger.LogDebug("Formatting {Count} activity items for JSON response...", allActivities.Count);
                var formattedActivities = allActivities.Select(a => new
                {
                    a.Type,
                    a.Description,
                    RelativeTime = GetRelativeTime(a.Timestamp, now), // Calculate relative time
                    a.Icon,
                    Timestamp = a.Timestamp.ToString("o") // ISO 8601 format (UTC)
                }).ToList();

                 _logger.LogInformation("API: Recent activity data prepared successfully ({Count} items).", formattedActivities.Count);
                return Ok(formattedActivities);
            }
            catch (Exception ex)
            {
                // Catch any unexpected errors during combination/formatting
                _logger.LogError(ex, "API: *** CRITICAL ERROR *** processing recent activity data after fetching sources.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi xử lý hoạt động gần đây." });
            }
        }


        // --- API Endpoint: Job Distribution by Industry Chart ---
        // (Relies on TinTuyenDung_NganhNghes being populated and TrangThai enum comparison)
        [HttpGet("jobs-by-industry")]
        public async Task<IActionResult> GetJobsByIndustry(int topN = 5)
        {
             if (topN <= 0) topN = 5;
             if (topN > 20) topN = 20;
             _logger.LogInformation("API: Fetching job distribution by top {TopN} industries.", topN);
             // --- DIAGNOSTIC CHECK ---
             // If this chart is empty, check:
             // 1. Do TinTuyenDung records exist with TrangThai = 'daduyet' (exact string)?
             // 2. Do corresponding records exist in TinTuyenDung_NganhNghes linking these TTDs to NganhNghe records?
             // 3. Do the linked NganhNghe records have a non-null 'Ten' value?
             // -------------------------
            try
            {
                var approvedState = TrangThaiTinTuyenDung.daduyet;
                 _logger.LogDebug("Querying job counts per industry for approved postings (State: '{ApprovedState}'). Including NganhNghe and TinTuyenDung...", approvedState.ToString());

                // Ensure necessary Includes and Where conditions
                var industryCounts = await _context.TinTuyenDung_NganhNghes
                    .Include(tnn => tnn.NganhNghe) // Need NganhNghe.Ten
                    .Include(tnn => tnn.TinTuyenDung) // Need TinTuyenDung.TrangThai
                    .Where(tnn => tnn.TinTuyenDung != null && tnn.TinTuyenDung.TrangThai == approvedState && // Filter by status
                                  tnn.NganhNghe != null && tnn.NganhNghe.Ten != null) // Ensure NganhNghe and its Name exist
                    .GroupBy(tnn => tnn.NganhNghe!.Ten) // Group by industry name (non-null checked by Where)
                    .Select(g => new {
                        IndustryName = g.Key, // Key is NganhNghe.Ten
                        JobCount = g.Count()  // Count TinTuyenDung_NganhNghe records per group
                    })
                    .OrderByDescending(g => g.JobCount)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} industries with approved job counts.", industryCounts.Count);

                if (!industryCounts.Any())
                {
                    _logger.LogWarning("API: No approved job postings found with associated industries. Returning empty chart data for jobs-by-industry. Check TinTuyenDung.TrangThai = '{ApprovedState}' AND TinTuyenDung_NganhNghes data.", approvedState.ToString());
                    // Return structure expected by Chart.js even when empty
                    return Ok(new { labels = new List<string>(), datasets = new [] { new { data = new List<int>(), backgroundColor = new List<string>() } } });
                }

                // Process top N and "Other"
                var topIndustries = industryCounts.Take(topN).ToList();
                int otherCount = industryCounts.Skip(topN).Sum(i => i.JobCount);
                int otherIndustryCount = Math.Max(0, industryCounts.Count - topN); // How many industries are grouped into "Other"

                var chartLabels = topIndustries.Select(i => i.IndustryName).ToList();
                var chartDataPoints = topIndustries.Select(i => i.JobCount).ToList();

                if (otherCount > 0 && otherIndustryCount > 0)
                {
                    // Add "Other" category if there are remaining industries and jobs
                    chartLabels.Add($"Ngành khác ({otherIndustryCount})");
                    chartDataPoints.Add(otherCount);
                    _logger.LogDebug("Added 'Ngành khác' category with count {OtherCount} representing {OtherIndustryCount} industries.", otherCount, otherIndustryCount);
                }
                 else if (otherCount > 0) {
                     // Should not happen if otherIndustryCount is 0, but as a fallback
                     chartLabels.Add("Ngành khác");
                     chartDataPoints.Add(otherCount);
                     _logger.LogDebug("Added 'Ngành khác' category with count {OtherCount} (unusual case).", otherCount);
                 }

                var backgroundColors = GetChartColors(chartLabels.Count); // Use helper for colors

                var doughnutChartData = new {
                    labels = chartLabels,
                    datasets = new[] {
                        new {
                            label = "Số lượng bài đăng", // Tooltip label
                            data = chartDataPoints,
                            backgroundColor = backgroundColors,
                            hoverOffset = 4,
                            borderColor = "#ffffff" // White border between segments
                        }
                    }
                };
                 _logger.LogInformation("API: Job distribution by industry data prepared successfully ({LabelCount} categories).", chartLabels.Count);
                return Ok(doughnutChartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting job distribution by industry.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy dữ liệu phân bổ việc làm." });
            }
        }

        // --- REPLACEMENT SECTION: Job Posting Status Distribution ---
        // Provides data for a chart showing job posting counts by status.
        // Route: /admin/dashboard/job-status-distribution
        [HttpGet("job-status-distribution")]
        public async Task<IActionResult> GetJobPostingStatusDistribution()
        {
            _logger.LogInformation("API: Fetching job posting status distribution.");
            // --- DIAGNOSTIC CHECK ---
            // If this chart is empty or shows only one status, verify the TrangThai column
            // in the TinTuyenDung table contains various expected string values
            // (e.g., 'choduyet', 'daduyet', 'hethan', 'bituchoi', 'taman', 'datuyen', 'daxoa').
            // Check the exact strings and casing.
            // -------------------------
            try
            {
                 _logger.LogDebug("Querying job counts grouped by status (TrangThai)...");

                 // Group by the TrangThai enum directly. EF Core handles the conversion based on DbContext config.
                 var statusCounts = await _context.TinTuyenDungs
                    // Add a basic filter if needed, e.g., exclude 'daxoa' if desired:
                    // .Where(t => t.TrangThai != TrangThaiTinTuyenDung.daxoa)
                    .GroupBy(t => t.TrangThai) // Group by the enum property
                    .Select(g => new {
                        Status = g.Key,      // The enum value itself (e.g., TrangThaiTinTuyenDung.daduyet)
                        Count = g.Count()    // Count of postings in this status
                    })
                    .OrderBy(x => x.Status) // Optional: Order consistently (by enum value)
                    .ToListAsync();

                 _logger.LogInformation("Found {Count} distinct job posting statuses with counts.", statusCounts.Count);

                if (!statusCounts.Any())
                {
                    _logger.LogWarning("API: No job postings found in any status. Returning empty chart data for job-status-distribution. Check TinTuyenDung table content.");
                    return Ok(new { labels = new List<string>(), datasets = new [] { new { data = new List<int>(), backgroundColor = new List<string>() } } });
                }

                // Prepare data for Chart.js
                var chartLabels = new List<string>();
                var chartDataPoints = new List<int>();

                foreach (var item in statusCounts)
                {
                    // Get the display name using the extension method if available, otherwise use enum name
                    string? displayName = item.Status.GetDisplayName() ?? item.Status.ToString();
                    chartLabels.Add(displayName);
                    chartDataPoints.Add(item.Count);
                     _logger.LogTrace("Status: {StatusEnum} ('{DisplayName}'), Count: {Count}", item.Status, displayName, item.Count);
                }

                var backgroundColors = GetChartColors(chartLabels.Count); // Use helper

                // Structure data for Chart.js doughnut chart
                var doughnutChartData = new
                {
                    labels = chartLabels,
                    datasets = new[] {
                        new {
                            label = "Số lượng", // Tooltip label for the dataset
                            data = chartDataPoints,
                            backgroundColor = backgroundColors,
                            hoverOffset = 4,
                            borderColor = "#ffffff" // White border between segments
                        }
                    }
                };

                #if DEBUG
                try {
                     // Log smaller datasets directly
                    if (chartLabels.Count < 20)
                        _logger.LogDebug("Job Status Distribution Data (JSON): {ChartDataJson}", JsonSerializer.Serialize(doughnutChartData));
                    else
                        _logger.LogDebug("Job Status Distribution Data prepared ({LabelCount} labels).", chartLabels.Count);
                 } catch (Exception jsonEx) { _logger.LogWarning(jsonEx, "Failed to serialize job status distribution data for log."); }
                #endif

                _logger.LogInformation("API: Job posting status distribution data prepared successfully ({LabelCount} statuses).", chartLabels.Count);
                return Ok(doughnutChartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API: Error getting job posting status distribution.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy dữ liệu trạng thái tin đăng." });
            }
        }


        // --- REMOVED: Monthly Goals Progress ---
        // [HttpGet("monthly-goals")] public async Task<ActionResult<IEnumerable<MonthlyGoalProgressViewModel>>> GetMonthlyGoalsProgress() { ... }


        // --- Helper Methods ---

        // Helper to get display text for Recent Activity Job Posting Type
        private string GetJobPostingActivityType(TrangThaiTinTuyenDung status)
        {
            // Use DisplayName extension for consistency if attributes are defined on the enum
            return status.GetDisplayName() ?? status switch { // Fallback to switch if DisplayName not found
                TrangThaiTinTuyenDung.choduyet => "Bài đăng chờ duyệt",
                TrangThaiTinTuyenDung.daduyet => "Bài đăng đã duyệt",
                TrangThaiTinTuyenDung.bituchoi => "Bài đăng bị từ chối",
                TrangThaiTinTuyenDung.hethan => "Bài đăng hết hạn",
                TrangThaiTinTuyenDung.taman => "Bài đăng tạm ẩn",
                TrangThaiTinTuyenDung.datuyen => "Bài đăng đã tuyển",
                TrangThaiTinTuyenDung.daxoa => "Bài đăng đã xóa",
                _ => $"Cập nhật bài đăng ({status})" // Fallback with enum name
            };
        }

        // Helper to get icon class for Recent Activity Job Posting Type
        private string GetJobPostingActivityIcon(TrangThaiTinTuyenDung status)
        {
             // Using Font Awesome 5 classes as examples
             return status switch {
                 TrangThaiTinTuyenDung.choduyet => "fas fa-file-alt text-warning", // Pending (Orange/Yellow)
                 TrangThaiTinTuyenDung.daduyet => "fas fa-check-circle text-success", // Approved (Green)
                 TrangThaiTinTuyenDung.bituchoi => "fas fa-times-circle text-danger", // Rejected (Red)
                 TrangThaiTinTuyenDung.hethan => "fas fa-calendar-times text-secondary", // Expired (Gray)
                 TrangThaiTinTuyenDung.taman => "fas fa-eye-slash text-muted", // Hidden (Muted Gray)
                 TrangThaiTinTuyenDung.datuyen => "fas fa-user-check text-info", // Filled (Blue/Teal)
                 TrangThaiTinTuyenDung.daxoa => "fas fa-trash-alt text-dark", // Deleted (Dark Gray/Black)
                 _ => "fas fa-file-signature text-primary" // Default/Update (Blue)
             };
        }

        // Helper to generate a list of distinct colors for charts
        private List<string> GetChartColors(int count)
        {
            // Predefined list of visually distinct colors (adjust as needed)
            var baseColors = new List<string> {
                 "rgba(78, 115, 223, 0.8)",   // Primary Blue
                 "rgba(28, 200, 138, 0.8)",   // Success Green
                 "rgba(54, 185, 204, 0.8)",   // Info Teal
                 "rgba(246, 194, 62, 0.8)",   // Warning Yellow
                 "rgba(231, 74, 59, 0.8)",    // Danger Red
                 "rgba(133, 135, 150, 0.8)",  // Secondary Gray
                 "rgba(110, 99, 198, 0.8)",   // Purple
                 "rgba(253, 126, 20, 0.8)",   // Orange
                 "rgba(220, 53, 69, 0.8)",    // Darker Red (like Bootstrap danger)
                 "rgba(25, 135, 84, 0.8)",    // Darker Green (like Bootstrap success)
                 "rgba(13, 110, 253, 0.8)",   // Brighter Blue (Bootstrap primary)
                 "rgba(255, 193, 7, 0.8)",    // Amber Yellow (Bootstrap warning)
            };
            // Repeat colors if more are needed than defined in the base list
            var colors = new List<string>();
            if (count <= 0) return colors; // Handle edge case
            for(int i=0; i<count; i++) {
                colors.Add(baseColors[i % baseColors.Count]);
            }
            return colors;
        }


        // Helper Method: Calculate Relative Time String (Vietnamese)
        private string GetRelativeTime(DateTime dateTime, DateTime now)
        {
            // Ensure both DateTimes have the same Kind, preferably Utc for calculations
            var dtUtc = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            var nowUtc = DateTime.SpecifyKind(now, DateTimeKind.Utc);

            var timeSpan = nowUtc - dtUtc;

            // Handle future dates or clock skew gracefully
            if (timeSpan.TotalSeconds < 0)
            {
                 // If difference is small, treat as 'just now', otherwise show timestamp
                 return timeSpan.TotalSeconds > -2 ? "vừa xong" : dtUtc.ToLocalTime().ToString("g");
            }

            if (timeSpan.TotalSeconds < 2) return "vừa xong";
            if (timeSpan.TotalSeconds < 60) return $"{(int)timeSpan.TotalSeconds} giây trước";
            if (timeSpan.TotalMinutes < 2) return "1 phút trước";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 2) return "1 giờ trước";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} giờ trước";

            // Check for yesterday based on Date part
            if (nowUtc.Date.AddDays(-1) == dtUtc.Date) return "hôm qua";

            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} ngày trước";
            if (timeSpan.TotalDays < 14) return "1 tuần trước";
            if (timeSpan.TotalDays < 31) return $"{(int)Math.Floor(timeSpan.TotalDays / 7)} tuần trước"; // Use Floor

            // Approximate months/years
            int months = (nowUtc.Year - dtUtc.Year) * 12 + nowUtc.Month - dtUtc.Month;
            // Adjust if the day hasn't been reached yet in the current month
            if (nowUtc.Day < dtUtc.Day) {
                months--;
            }

            if (months <= 0) {
                // If less than a month, handled by weeks/days above.
                // This can happen if the date is e.g., 30 days ago but spans across month boundary.
                // Re-calculate using weeks for clarity if months is 0 or negative.
                 int weeks = (int)Math.Floor(timeSpan.TotalDays / 7);
                 if (weeks >= 4) return $"4 tuần trước"; // Cap at 4 weeks before showing date
                 else if (weeks > 1) return $"{weeks} tuần trước";
                 else if (weeks == 1) return "1 tuần trước";
                 else return $"{(int)timeSpan.TotalDays} ngày trước"; // Fallback if less than a week
            }
            if (months == 1) return "1 tháng trước";
            if (months < 12) return $"{months} tháng trước";

            int years = (int)Math.Floor((double)months / 12);
            if (years == 1) return "1 năm trước";
            if (years > 1) return $"{years} năm trước";

            // Fallback: Show date if calculation somehow fails or is very old/odd
             _logger.LogWarning("Could not determine relative time for timestamp {TimestampUTC:o} relative to {NowUTC:o}. Falling back to absolute date.", dtUtc, nowUtc);
            return dtUtc.ToLocalTime().ToString("d MMM yyyy");
        }


        // Helper Record for Recent Activity (Internal to this controller)
        private record RecentActivityItem
        {
            public string Type { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public DateTime Timestamp { get; init; } // Store as DateTime (assume UTC from DB)
            public string Icon { get; init; } = string.Empty;
        }
    }

    // --- Enum DisplayName Extension Method ---
    // (Keep this helper class as it's used by the Job Status Distribution)
    public static class EnumExtensions
    {
        public static string? GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null) return null;
            try
            {
                string enumString = enumValue.ToString();
                var memberInfo = enumValue.GetType().GetMember(enumString).FirstOrDefault();

                if (memberInfo != null)
                {
                    // Prioritize DisplayAttribute
                    var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>(false);
                    if (displayAttribute != null)
                    {
                        // GetName() can handle resource-based localization if set up
                        string? name = displayAttribute.GetName();
                        if (!string.IsNullOrEmpty(name)) return name;

                        // Fallback to the Name property if GetName() is null/empty
                        if (!string.IsNullOrEmpty(displayAttribute.Name)) return displayAttribute.Name;
                    }
                }
                // Fallback to the enum member's name if no attribute or name found
                return enumString;
            }
            catch (Exception ex)
            {
                 // Using Console.WriteLine as a basic logger in a static context.
                 // Replace with a proper static logging setup if available.
                 // Consider injecting ILogger if used frequently or in non-static contexts.
                 Console.WriteLine($"ERROR [EnumExtensions.GetDisplayName]: Failed to get display name for {enumValue.GetType().Name}.{enumValue}: {ex.Message}");
                 return enumValue.ToString(); // Fallback to enum name on error
            }
        }
    }
}