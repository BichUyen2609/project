using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Thêm using cho ILogger nếu bạn muốn log chuyên nghiệp hơn
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using trungtamdaotao.Data;
using trungtamdaotao.Models;
using trungtamdaotao.Models.AdminViewModels; // Thêm namespace cho ViewModels nếu cần
using trungtamdaotao.ViewModels; // Thêm namespace cho ViewModels nếu cần

namespace trungtamdaotao.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        // Tùy chọn: Thêm logger để ghi log lỗi chi tiết hơn
        // private readonly ILogger<AdminController> _logger;

        // public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) // Thêm UserManager và RoleManager
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            // _logger = logger; // Uncomment nếu dùng ILogger
        }
            // _logger = logger; // Uncomment nếu dùng ILogger

        // Trang tổng quan cho admin với tổng quan thống kê
        public async Task<IActionResult> Dashboard()
        {
            // Lấy thống kê học viên
            var courseStats = _context.GetCourseStudentStats();

            // Lấy thống kê doanh thu
            var revenueStats = _context.GetCourseRevenue();

            // Lấy doanh thu hàng tháng cho năm hiện tại
            var monthlyRevenue = _context.GetMonthlyRevenue(DateTime.Now.Year);

            // Chuẩn bị view model với khởi tạo an toàn
            var viewModel = new AdminDashboardViewModel
            {
                CourseStats = courseStats?.ToList() ?? new List<CourseStudentStats>(),
                RevenueStats = revenueStats?.ToList() ?? new List<CourseRevenue>(),
                MonthlyRevenue = monthlyRevenue?.ToList() ?? new List<MonthlyRevenue>()
            };

            return View(viewModel);
        }

        #region Quản lý khóa học

        // GET: Admin/Courses
        public async Task<IActionResult> Courses(int page = 1, int pageSize = 10)
        {
            var totalItems = await _context.Courses.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            var courses = await _context.Courses
                .OrderBy(c => c.CourseName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(courses);
        }

        // GET: Admin/CourseDetails/5
public async Task<IActionResult> CourseDetails(int id)
{
    if (id <= 0)
    {
        return NotFound();
    }

    // 1. Vẫn phải Include để lấy dữ liệu gốc từ DB
    var course = await _context.Courses
                       .Include(c => c.CourseRegistrations)
                           .ThenInclude(cr => cr.User) // Cần User để lấy FullName, Email...
                       .FirstOrDefaultAsync(c => c.CourseId == id);

    if (course == null)
    {
        return NotFound();
    }

    // 2. Tạo và đổ dữ liệu vào ViewModel
    var viewModel = new CourseDetailsViewModel
    {
        CourseId = course.CourseId,
        CourseCode = course.CourseCode,
        CourseName = course.CourseName,
        Instructor = course.Instructor,
        StartDate = course.StartDate,
        TuitionFee = course.TuitionFee,
        MaxStudents = course.MaxStudents,
        CreatedAt = course.CreatedAt,
        RegisteredStudentCount = course.CourseRegistrations?.Count ?? 0 // <-- Tính toán số lượng ở đây
    };

    // 3. Chuyển đổi danh sách đăng ký sang danh sách thông tin đơn giản
    if (course.CourseRegistrations != null)
    {
        foreach (var registration in course.CourseRegistrations)
        {
            viewModel.RegisteredStudents.Add(new StudentRegistrationInfo
            {
                FullName = registration.User?.FullName ?? "N/A", // Xử lý nếu User null
                Email = registration.User?.Email ?? "N/A",
                PhoneNumber = registration.User?.PhoneNumber ?? "N/A",
                RegistrationDate = registration.RegistrationDate,
                // Ví dụ trạng thái - bạn có thể lấy từ registration.IsConfirmed nếu có
                Status = "Đã đăng ký"
            });
        }
    }

    // 4. Trả về View với ViewModel đã được chuẩn bị
    return View(viewModel);
}

        // GET: Admin/CreateCourse
        public IActionResult CreateCourse()
        {
            // Khởi tạo ngày bắt đầu mặc định hợp lệ hơn
            var defaultStartDate = DateTime.Today.AddDays(7);
             // Đảm bảo không rơi vào cuối tuần nếu cần (ví dụ)
            if (defaultStartDate.DayOfWeek == DayOfWeek.Saturday) defaultStartDate = defaultStartDate.AddDays(2);
            else if (defaultStartDate.DayOfWeek == DayOfWeek.Sunday) defaultStartDate = defaultStartDate.AddDays(1);

            return View(new Course { StartDate = defaultStartDate });
        }

        // POST: Admin/CreateCourse
        // POST: Admin/CreateCourse
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateCourse(Course course)
{
    Console.WriteLine("--- Bắt đầu CreateCourse POST ---");
    Console.WriteLine($"Dữ liệu nhận được: CourseCode={course.CourseCode}, Name={course.CourseName}, StartDate={course.StartDate}, Fee={course.TuitionFee}, MaxStudents={course.MaxStudents}");

    // --- Phần kiểm tra logic nghiệp vụ giữ nguyên ---
    if (course.TuitionFee < 0)
    {
        Console.WriteLine("Validation Error: Học phí âm.");
        ModelState.AddModelError("TuitionFee", "Học phí không thể âm.");
    }
    if (course.MaxStudents <= 0)
    {
        Console.WriteLine("Validation Error: Số lượng học viên không hợp lệ.");
        ModelState.AddModelError("MaxStudents", "Số lượng học viên tối đa phải lớn hơn 0.");
    }
    // So sánh chỉ phần ngày, bỏ qua giờ phút giây
    if (course.StartDate.Date < DateTime.Today)
    {
        Console.WriteLine("Validation Error: Ngày bắt đầu trong quá khứ.");
        ModelState.AddModelError("StartDate", "Ngày bắt đầu không thể là quá khứ.");
    }
    // --- Hết phần kiểm tra logic nghiệp vụ ---

    // Kiểm tra ModelState tổng thể (bao gồm cả Data Annotations từ Model)
    if (!ModelState.IsValid)
    {
        Console.WriteLine("ModelState không hợp lệ. Các lỗi:");
        foreach (var modelStateKey in ViewData.ModelState.Keys)
        {
            var value = ViewData.ModelState[modelStateKey];
            foreach (var error in value.Errors)
            {
                Console.WriteLine($"- Key: {modelStateKey}, Lỗi: {error.ErrorMessage}");
            }
        }
        Console.WriteLine("-> Trả về View với lỗi ModelState.");
        return View(course); // Trả về View để hiển thị lỗi validation
    }

    // Nếu ModelState hợp lệ, tiếp tục xử lý
    Console.WriteLine("ModelState hợp lệ. Tiến hành lưu vào DB...");
    try // <-- Bắt đầu khối try-catch
    {
        course.CreatedAt = DateTime.Now;
        _context.Add(course);
        Console.WriteLine("Đã gọi _context.Add(course). Chuẩn bị gọi SaveChangesAsync...");

        // Thực hiện lưu vào cơ sở dữ liệu
        await _context.SaveChangesAsync(); // <-- Lệnh lưu vào DB

        Console.WriteLine("-> SaveChangesAsync thành công!");
        TempData["SuccessMessage"] = "Đã tạo khóa học thành công!";
        Console.WriteLine("-> Chuyển hướng đến trang Courses.");
        return RedirectToAction(nameof(Courses));
    }
    catch (DbUpdateException dbEx) // <-- Bắt lỗi cụ thể liên quan đến DB
    {
        Console.WriteLine($"!!! Lỗi DbUpdateException khi SaveChangesAsync: {dbEx.Message}");
        // Lấy lỗi gốc từ DB nếu có (rất quan trọng!)
        Console.WriteLine($"!!! Inner Exception: {dbEx.InnerException?.Message}");
        // Log chi tiết hơn nếu cần (ghi toàn bộ exception)
        // Console.WriteLine($"!!! Full Exception Details: {dbEx.ToString()}");

        // Thêm lỗi vào ModelState để hiển thị cho người dùng
        ModelState.AddModelError("", "Không thể lưu khóa học. Có lỗi xảy ra với cơ sở dữ liệu. Vui lòng kiểm tra Console Output hoặc liên hệ quản trị viên.");
        Console.WriteLine("-> Trả về View với lỗi DbUpdateException.");
        return View(course); // Trả về View để hiển thị lỗi
    }
    catch (Exception ex) // <-- Bắt các lỗi không mong muốn khác
    {
        Console.WriteLine($"!!! Lỗi không mong muốn Exception: {ex.Message}");
        // Log chi tiết hơn nếu cần
        // Console.WriteLine($"!!! Full Exception Details: {ex.ToString()}");

        ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn trong quá trình tạo khóa học.");
        Console.WriteLine("-> Trả về View với lỗi Exception chung.");
        return View(course); // Trả về View để hiển thị lỗi
    }
}

        // GET: Admin/EditCourse/5
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Admin/EditCourse/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, Course course)
        {
            if (id != course.CourseId)
            {
                return NotFound();
            }

            // Kiểm tra logic nghiệp vụ
            if (course.TuitionFee < 0)
            {
                ModelState.AddModelError("TuitionFee", "Học phí không thể âm.");
            }
            if (course.MaxStudents <= 0)
            {
                ModelState.AddModelError("MaxStudents", "Số lượng học viên tối đa phải lớn hơn 0.");
            }

            // Chỉ kiểm tra ngày bắt đầu nếu khóa học chưa bắt đầu
            var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id);
            if (existingCourse == null) // Thêm kiểm tra null cho existingCourse
            {
                 return NotFound(); // Không tìm thấy khóa học gốc
            }

            // So sánh ngày, bỏ qua giờ phút giây
            if (existingCourse.StartDate.Date > DateTime.Today && course.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Không thể đặt Ngày bắt đầu trong quá khứ cho khóa học chưa diễn ra.");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Giữ nguyên ngày tạo
                    course.CreatedAt = existingCourse.CreatedAt;

                    _context.Update(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật khóa học thành công!";
                    return RedirectToAction(nameof(Courses)); // Đảm bảo redirect đúng sau khi sửa thành công
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await CourseExists(course.CourseId))
                    {
                        return NotFound();
                    }
                    else
                    {
                         // Thông báo lỗi cụ thể hơn về xung đột dữ liệu
                        ModelState.AddModelError("", "Dữ liệu khóa học này đã được thay đổi bởi người khác kể từ khi bạn mở trang. Vui lòng tải lại trang và thử chỉnh sửa lại.");
                        // Không nên return View(course) ở đây ngay lập tức
                        // Cần tải lại dữ liệu mới nhất để hiển thị cho người dùng
                        var currentCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id);
                         if (currentCourse != null) {
                             // Có thể gán lại giá trị hiện tại vào model để người dùng xem
                             // Hoặc chỉ hiển thị thông báo lỗi và yêu cầu tải lại
                             // Ví dụ gán lại:
                             // ModelState.SetModelValue("Property", new ValueProviderResult(currentCourse.Property.ToString(), CultureInfo.InvariantCulture));
                             // Cách đơn giản nhất là chỉ báo lỗi và yêu cầu tải lại.
                         }
                         return View(course); // Trả về view với dữ liệu cũ và thông báo lỗi
                    }
                }
                 catch (DbUpdateException dbEx) // Thêm bắt lỗi DB khi Update
                {
                    // _logger.LogError(dbEx, $"Lỗi khi cập nhật khóa học ID {id}.");
                    Console.WriteLine($"ERROR updating course {id}: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    ModelState.AddModelError("", "Không thể cập nhật khóa học. Có lỗi xảy ra với cơ sở dữ liệu.");
                }
                catch (Exception ex) // Thêm bắt lỗi chung khi Update
                {
                    // _logger.LogError(ex, $"Lỗi không mong muốn khi cập nhật khóa học ID {id}.");
                    Console.WriteLine($"ERROR updating course {id}: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn trong quá trình cập nhật.");
                }
            }
            // Nếu ModelState không hợp lệ hoặc có lỗi xảy ra, trả về view Edit
             return View(course);
        }

        // GET: Admin/DeleteCourse/5
        public async Task<IActionResult> DeleteCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);
            if (course == null)
            {
                return NotFound();
            }

            // Kiểm tra có đăng ký không trước khi hiển thị trang xác nhận xóa
            bool hasRegistrations = await _context.CourseRegistrations.AnyAsync(cr => cr.CourseId == id && !cr.IsCanceled);
            if (hasRegistrations)
            {
                // Đặt thông báo vào ViewBag hoặc TempData để hiển thị trên View Delete
                ViewBag.ErrorMessage = "Không thể xóa khóa học này vì đã có học viên đăng ký.";
            }


            return View("DeleteCourse", course);
        }

        // POST: Admin/DeleteCourse/5
[HttpPost, ActionName("DeleteCourse")] // Giữ nguyên ActionName này
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteCourseConfirmed(int id) // Tên phương thức không ảnh hưởng routing do có ActionName
{
    var course = await _context.Courses
        .Include(c => c.CourseRegistrations)
        .FirstOrDefaultAsync(c => c.CourseId == id);

    if (course == null)
    {
        // Có thể trả về TempData và Redirect nếu muốn thông báo ở trang danh sách
        TempData["ErrorMessage"] = "Không tìm thấy khóa học để xóa.";
        return RedirectToAction(nameof(Courses));
        // Hoặc return NotFound(); // Trả về trang lỗi 404
    }

    // Kiểm tra lại lần nữa ngay trước khi xóa
    bool hasActiveRegistrations = course.CourseRegistrations.Any(cr => !cr.IsCanceled);
    if (hasActiveRegistrations)
    {
        ModelState.AddModelError("", "Không thể xóa khóa học vì vẫn còn học viên đăng ký hợp lệ.");
        ViewBag.ErrorMessage = "Không thể xóa khóa học vì vẫn còn học viên đăng ký hợp lệ.";
        // Trả về View "DeleteCourse.cshtml" với model và lỗi
        return View("DeleteCourse", course);
    }

    try
    {
        var canceledRegistrations = course.CourseRegistrations.Where(cr => cr.IsCanceled).ToList();
        if (canceledRegistrations.Any())
        {
            _context.CourseRegistrations.RemoveRange(canceledRegistrations);
            // Cân nhắc SaveChanges riêng ở đây nếu cần thiết do ràng buộc FK
            // await _context.SaveChangesAsync();
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(); // Lưu thay đổi sau khi xóa Course (và registrations nếu có)

        TempData["SuccessMessage"] = "Đã xóa khóa học thành công!";
        return RedirectToAction(nameof(Courses)); // Chuyển hướng về danh sách
    }
    catch (DbUpdateException dbEx)
    {
        Console.WriteLine($"ERROR deleting course {id}: {dbEx.InnerException?.Message ?? dbEx.Message}");
        ModelState.AddModelError("", "Không thể xóa khóa học. Đã xảy ra lỗi cơ sở dữ liệu. Chi tiết: " + (dbEx.InnerException?.Message ?? dbEx.Message));
        ViewBag.ErrorMessage = "Không thể xóa khóa học. Có lỗi cơ sở dữ liệu xảy ra.";
        // Trả về View "DeleteCourse.cshtml" với model và lỗi
        return View("DeleteCourse", course);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR deleting course {id}: {ex.Message}");
        ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn trong quá trình xóa.");
        ViewBag.ErrorMessage = "Đã xảy ra lỗi không mong muốn.";
         // Trả về View "DeleteCourse.cshtml" với model và lỗi
        return View("DeleteCourse", course);
    }
}


        #endregion

        #region Báo cáo doanh thu

        // GET: Admin/RevenueByMonth
        public IActionResult RevenueByMonth()
        {
            var currentYear = DateTime.Now.Year;
            // Cần đảm bảo phương thức GetMonthlyRevenue tồn tại trong DbContext và hoạt động đúng
            var monthlyRevenue = _context.GetMonthlyRevenue(currentYear);
            ViewBag.Year = currentYear;
            return View(monthlyRevenue ?? new List<MonthlyRevenue>()); // Trả về list rỗng nếu null
        }

        // GET: Admin/RevenueByDateRange
        public IActionResult RevenueByDateRange()
        {
            // Thiết lập khoảng thời gian mặc định là tháng hiện tại
            ViewBag.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            ViewBag.EndDate = DateTime.Today; // Nên dùng DateTime.Today để không bao gồm giờ hiện tại
            return View();
        }

        // POST: Admin/RevenueByDateRange
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RevenueByDateRange(DateTime startDate, DateTime endDate)
        {
            // Nên chuẩn hóa endDate để bao gồm cả ngày cuối cùng
             endDate = endDate.Date.AddDays(1).AddTicks(-1); // Kết thúc vào 23:59:59.999... của ngày chọn

            if (startDate.Date > endDate.Date) // So sánh chỉ phần ngày
            {
                ModelState.AddModelError("", "Ngày bắt đầu không được sau ngày kết thúc.");
                 // Trả về view nhập liệu với lỗi
                ViewBag.StartDate = startDate.Date; // Giữ lại giá trị người dùng nhập
                ViewBag.EndDate = endDate.Date.AddDays(-1).AddTicks(1); // Khôi phục giá trị gốc trước khi chuẩn hóa
                return View();
            }

             // Kiểm tra khoảng thời gian hợp lý hơn
            if (startDate < DateTime.Now.AddYears(-20) || endDate > DateTime.Now.AddYears(5))
            {
                ModelState.AddModelError("", "Khoảng ngày chọn không hợp lệ.");
                ViewBag.StartDate = startDate.Date;
                ViewBag.EndDate = endDate.Date.AddDays(-1).AddTicks(1);
                return View();
            }


            // Đảm bảo GetRevenueByDateRange tồn tại và hoạt động đúng
            var revenueData = _context.GetRevenueByDateRange(startDate, endDate);
            ViewBag.StartDate = startDate.Date; // Chỉ hiển thị ngày
            ViewBag.EndDate = endDate.Date;   // Chỉ hiển thị ngày
            return View("RevenueByDateRangeResults", revenueData ?? new List<MonthlyRevenue>()); // Trả về list rỗng nếu null
        }

        // GET: Admin/RevenueByCourse
        public IActionResult RevenueByCourse()
        {
             // Đảm bảo GetCourseRevenue tồn tại và hoạt động đúng
            var courseRevenue = _context.GetCourseRevenue();
            return View(courseRevenue ?? new List<CourseRevenue>()); // Trả về list rỗng nếu null
        }

        // Xuất doanh thu theo khoảng thời gian ra CSV
        public IActionResult ExportRevenueByDateRange(DateTime startDate, DateTime endDate)
        {
            // Chuẩn hóa endDate tương tự như trong action POST
             endDate = endDate.Date.AddDays(1).AddTicks(-1);

            if (startDate.Date > endDate.Date)
            {
                return BadRequest("Ngày bắt đầu không được sau ngày kết thúc");
            }

            var revenueData = _context.GetRevenueByDateRange(startDate, endDate)?.ToList(); // Thêm kiểm tra null

            if (revenueData == null)
            {
                // Xử lý trường hợp không có dữ liệu hoặc lỗi từ DB
                 return Content("Không có dữ liệu doanh thu cho khoảng thời gian này hoặc có lỗi xảy ra.");
            }


            var sb = new StringBuilder();
            // Thêm UTF8 BOM để Excel mở file CSV tiếng Việt đúng
             sb.Append('\uFEFF');
            sb.AppendLine("Năm,Tháng,Doanh Thu");

            foreach (var item in revenueData)
            {
                 // Định dạng số tiền có thể cần chuẩn hóa (ví dụ: dùng dấu chấm thay vì phẩy)
                sb.AppendLine($"{item.Year},{item.Month},{item.TotalRevenue}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
        }

        #endregion

           #region Quản lý học viên (Đã sửa để dùng UserManager)

        // GET: Admin/Students
        public async Task<IActionResult> Students(int page = 1, int pageSize = 10, string searchTerm = "")
        {
            // Sử dụng _userManager.Users thay vì _context.Users
            IQueryable<ApplicationUser> query = _userManager.Users;

            // Apply search if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim().ToLower(); // Chuẩn hóa để tìm kiếm không phân biệt hoa thường
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchTerm)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)) || // Phone number không cần ToLower
                     (u.UserName != null && u.UserName.ToLower().Contains(searchTerm))
                );
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure page is within valid range
            page = Math.Max(1, page);
            page = Math.Min(page, totalPages > 0 ? totalPages : 1);

            var students = await query
                .OrderBy(u => u.FullName) // Sắp xếp theo FullName
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SearchTerm = searchTerm;

            return View(students);
        }

        // GET: Admin/StudentDetails/id
        public async Task<IActionResult> StudentDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("ID học viên không được trống.");
            }

            // Dùng UserManager để tìm user
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                 TempData["ErrorMessage"] = "Không tìm thấy học viên.";
                return RedirectToAction(nameof(Students)); // Hoặc NotFound
            }

            // Lấy các khóa học đã đăng ký (vẫn cần _context ở đây)
            var registrations = await _context.CourseRegistrations
                                        .Include(cr => cr.Course)
                                        .Where(cr => cr.UserId == id && !cr.IsCanceled)
                                        .OrderBy(cr => cr.Course.CourseName)
                                        .ToListAsync();
            ViewBag.Registrations = registrations;

            // Lấy vai trò của người dùng
            ViewBag.Roles = await _userManager.GetRolesAsync(user);


            return View(user); // Truyền ApplicationUser
        }

        // GET: Admin/CreateStudent
        public IActionResult CreateStudent()
        {
            // Sử dụng CreateStudentViewModel có mật khẩu
            return View(new CreateStudentViewModel
            {
                BirthDate = DateTime.Today.AddYears(-20) // Default reasonable age
            });
        }

        // POST: Admin/CreateStudent
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateStudent(CreateStudentViewModel model) // Sử dụng ViewModel không cần UserName nếu đã xóa
{
    // Sửa Log: Không ghi model.UserName nữa vì không có input
    Console.WriteLine("--- Bắt đầu CreateStudent POST ---");
    Console.WriteLine($"Dữ liệu nhận được: Email={model.Email}, FullName={model.FullName}"); // Bỏ UserName khỏi log này

    // Validation tùy chỉnh (ví dụ)
    if (model.BirthDate.HasValue && model.BirthDate.Value > DateTime.Today.AddYears(-10))
    {
        ModelState.AddModelError("BirthDate", "Học viên phải đủ 10 tuổi trở lên.");
    }

    // Thêm kiểm tra null hoặc empty cho Email và Password trước khi dùng
    if (string.IsNullOrEmpty(model.Email))
    {
        ModelState.AddModelError("Email", "Email là bắt buộc.");
    }
     if (string.IsNullOrEmpty(model.Password))
    {
        // Mặc dù [Required] trên ViewModel đã xử lý, kiểm tra lại không thừa
        ModelState.AddModelError("Password", "Mật khẩu là bắt buộc.");
    }


    if (ModelState.IsValid)
    {
        // Tạo đối tượng User, đặt UserName = Email
        var user = new ApplicationUser
        {
            // *** Đặt UserName bằng Email từ Model ***
            UserName = model.Email,
            Email = model.Email,
            // *** ------------------------------ ***
            FullName = model.FullName,
            BirthDate = model.BirthDate,
            PhoneNumber = model.PhoneNumber, // Lấy từ ViewModel
            EmailConfirmed = true,           // Admin tạo nên xác thực luôn
            CreatedAt = DateTime.Now
        };

        // *** SỬ DỤNG UserManager ĐỂ TẠO USER VÀ MẬT KHẨU ***
        var result = await _userManager.CreateAsync(user, model.Password); // Dùng user và model.Password

        if (result.Succeeded)
        {
            Console.WriteLine($"UserManager.CreateAsync thành công cho user: {user.UserName}"); // Log UserName đã gán

            // Gán vai trò "Student"
            string defaultRole = "Student";
            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                Console.WriteLine($"Đã tạo vai trò: {defaultRole}");
            }
            var roleResult = await _userManager.AddToRoleAsync(user, defaultRole);

            if (roleResult.Succeeded)
            {
                Console.WriteLine($"Đã gán vai trò {defaultRole} cho user {user.UserName}");
                TempData["SuccessMessage"] = "Đã tạo học viên thành công!";
                return RedirectToAction(nameof(Students));
            }
            else
            {
                Console.WriteLine($"Lỗi khi gán vai trò {defaultRole} cho user {user.UserName}");
                await _userManager.DeleteAsync(user); // Xóa user nếu không gán được vai trò
                ModelState.AddModelError("", $"Không thể gán vai trò '{defaultRole}'. Tài khoản chưa được tạo.");
                AddErrors(roleResult);
            }
        }
        else // Lỗi khi _userManager.CreateAsync
        {
             // Sửa Log: Log lỗi với UserName đã được gán (chính là Email)
            Console.WriteLine($"Lỗi khi UserManager.CreateAsync cho user (UserName={user.UserName}):");
            AddErrors(result);
        }
    }
    else // ModelState không hợp lệ
    {
        Console.WriteLine("ModelState không hợp lệ khi tạo Student:");
         foreach (var modelStateKey in ViewData.ModelState.Keys)
        {
            var value = ViewData.ModelState[modelStateKey];
            foreach (var error in value.Errors)
            {
                Console.WriteLine($"- Key: {modelStateKey}, Lỗi: {error.ErrorMessage}");
            }
        }
    }

    // Nếu có lỗi, trả về View với model hiện tại
    Console.WriteLine("-> Trả về View CreateStudent với lỗi.");
    return View(model);
}


        // GET: Admin/EditStudent/id
        public async Task<IActionResult> EditStudent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("ID học viên không được trống.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy học viên.");
            }

            var viewModel = new EditStudentViewModel
            {
                Id = user.Id,
                UserName = user.UserName, // Thường không cho sửa Username
                Email = user.Email,
                FullName = user.FullName,
                BirthDate = user.BirthDate,
                PhoneNumber = user.PhoneNumber // Thêm PhoneNumber
            };

            return View(viewModel);
        }

        // POST: Admin/EditStudent/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(string id, EditStudentViewModel model)
        {
            Console.WriteLine($"--- Bắt đầu EditStudent POST cho ID: {id} ---");
            Console.WriteLine($"Dữ liệu nhận được: Email={model.Email}, FullName={model.FullName}, Phone={model.PhoneNumber}");

            if (id != model.Id)
            {
                Console.WriteLine("Lỗi: ID không khớp.");
                return NotFound("ID không khớp.");
            }

            // Validation tùy chỉnh
            if (model.BirthDate.HasValue && model.BirthDate.Value > DateTime.Today.AddYears(-10))
            {
                ModelState.AddModelError("BirthDate", "Học viên phải đủ 10 tuổi trở lên.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState không hợp lệ khi sửa Student.");
                // In lỗi ModelState nếu cần
                Console.WriteLine("-> Trả về View EditStudent với lỗi ModelState.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                Console.WriteLine($"Lỗi: Không tìm thấy user với ID: {id} để cập nhật.");
                TempData["ErrorMessage"] = "Không tìm thấy học viên.";
                return RedirectToAction(nameof(Students));
            }

            // Kiểm tra và cập nhật Email (và Username nếu cần)
            var oldEmail = user.Email;
            if (!string.Equals(oldEmail, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Email thay đổi: {oldEmail} -> {model.Email}. Kiểm tra trùng lặp...");
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != user.Id)
                {
                    Console.WriteLine($"Lỗi: Email '{model.Email}' đã tồn tại cho user khác ({emailExists.UserName}).");
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi học viên khác.");
                    Console.WriteLine("-> Trả về View EditStudent với lỗi Email trùng lặp.");
                    return View(model);
                }
                user.Email = model.Email;
                // Quyết định xem có cập nhật UserName không
                // user.UserName = model.Email; // Nếu UserName luôn giống Email
                Console.WriteLine($"Đã cập nhật Email (và có thể cả UserName).");
            }
            else
            {
                Console.WriteLine("Email không thay đổi.");
            }

            // Cập nhật các thuộc tính khác
            user.FullName = model.FullName;
            user.BirthDate = model.BirthDate;
            user.PhoneNumber = model.PhoneNumber;

            Console.WriteLine("Chuẩn bị gọi UserManager.UpdateAsync...");
            // *** SỬ DỤNG UserManager ĐỂ CẬP NHẬT ***
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                Console.WriteLine("-> UserManager.UpdateAsync thành công!");
                TempData["SuccessMessage"] = "Đã cập nhật thông tin học viên thành công!";
                Console.WriteLine("-> Chuyển hướng đến trang Students.");
                return RedirectToAction(nameof(Students));
            }
            else // Lỗi khi UpdateAsync
            {
                Console.WriteLine("Lỗi khi UserManager.UpdateAsync:");
                AddErrors(result); // Thêm lỗi vào ModelState và Console
                Console.WriteLine("-> Trả về View EditStudent với lỗi UpdateAsync.");
                return View(model); // Trả về View để hiển thị lỗi
            }
        }


        // GET: Admin/DeleteStudent/id
        public async Task<IActionResult> DeleteStudent(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("ID học viên không được trống.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Không tìm thấy học viên.");
            }

            // Kiểm tra xem user có đăng ký khóa học nào không (vẫn dùng _context)
            bool hasRegistrations = await _context.CourseRegistrations
                                        .AnyAsync(cr => cr.UserId == id && !cr.IsCanceled);
            if (hasRegistrations)
            {
                 ViewBag.ErrorMessage = "Không thể xóa học viên này vì họ đã đăng ký khóa học. Vui lòng hủy các đăng ký trước.";
            }

            return View(user); // Truyền ApplicationUser vào View
        }

        // POST: Admin/DeleteStudent/id
        [HttpPost, ActionName("DeleteStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                 TempData["ErrorMessage"] = "ID học viên không hợp lệ.";
                 return RedirectToAction(nameof(Students));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                 TempData["ErrorMessage"] = "Không tìm thấy học viên để xóa.";
                 return RedirectToAction(nameof(Students));
            }

            // Kiểm tra lại có đăng ký khóa học không (dùng _context)
             bool hasRegistrations = await _context.CourseRegistrations
                                        .AnyAsync(cr => cr.UserId == id && !cr.IsCanceled);
            if (hasRegistrations)
            {
                 ModelState.AddModelError("", "Không thể xóa học viên này vì họ đã đăng ký khóa học.");
                 ViewBag.ErrorMessage = "Không thể xóa học viên này vì họ đã đăng ký khóa học.";
                 // Trả về view xác nhận xóa với lỗi
                 return View("DeleteStudent", user);
            }

            // An toàn hơn: Xóa các đăng ký liên quan (nếu có) TRƯỚC khi xóa user
            // để tránh lỗi khóa ngoại nếu không có cascade delete
            try
            {
                var userRegistrations = await _context.CourseRegistrations
                                            .Where(cr => cr.UserId == id)
                                            .ToListAsync();
                if(userRegistrations.Any())
                {
                     _context.CourseRegistrations.RemoveRange(userRegistrations);
                     await _context.SaveChangesAsync(); // Lưu việc xóa đăng ký trước
                     Console.WriteLine($"Đã xóa {userRegistrations.Count} đăng ký liên quan của user {id}.");
                }
            }
            catch (Exception exReg)
            {
                 Console.WriteLine($"Lỗi khi xóa đăng ký liên quan của user {id}: {exReg.Message}");
                 ModelState.AddModelError("", "Lỗi khi xóa dữ liệu đăng ký liên quan.");
                 ViewBag.ErrorMessage = "Lỗi khi xóa dữ liệu đăng ký liên quan.";
                 return View("DeleteStudent", user);
            }


            // Thực hiện xóa user bằng UserManager
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                Console.WriteLine($"Đã xóa thành công user: {user.UserName}");
                TempData["SuccessMessage"] = "Đã xóa học viên thành công!";
                return RedirectToAction(nameof(Students));
            }
            else
            {
                // Lỗi khi xóa user
                Console.WriteLine($"Lỗi khi xóa user: {user.UserName}");
                AddErrors(result);
                 ViewBag.ErrorMessage = "Đã xảy ra lỗi khi xóa học viên.";
                 // Trả về view xác nhận xóa với lỗi
                 return View("DeleteStudent", user);
            }
        }

        // GET: Admin/ExportStudents
        public async Task<IActionResult> ExportStudents() // Nên là async nếu dùng UserManager
        {
            try
            {
                // Lấy danh sách bằng UserManager
                var students = await _userManager.Users.OrderBy(u=>u.FullName).ToListAsync();

                var sb = new StringBuilder();
                // Thêm UTF8 BOM để Excel mở file CSV tiếng Việt đúng
                sb.Append('\uFEFF');
                sb.AppendLine("UserID,Họ Tên,Username,Email,Số Điện Thoại,Ngày Sinh,Ngày Tạo"); // Thêm UserID

                foreach (var student in students)
                {
                    // Xử lý giá trị null và định dạng
                    var fullName = student.FullName ?? "";
                    var email = student.Email ?? "";
                    var phone = student.PhoneNumber ?? "";
                    var birthDate = student.BirthDate?.ToString("dd/MM/yyyy") ?? ""; // Dùng ?. và ??
                    var createdAt = student.CreatedAt.ToString("dd/MM/yyyy HH:mm");

                    // Đảm bảo các trường chứa dấu phẩy được bao trong dấu nháy kép
                     sb.AppendLine($"\"{student.Id}\",\"{fullName.Replace("\"", "\"\"")}\",\"{student.UserName}\",\"{email}\",\"{phone}\",\"{birthDate}\",\"{createdAt}\"");
                }

                return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"DanhSachHocVien_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR exporting students: {ex.Message}");
                TempData["ErrorMessage"] = "Không thể xuất danh sách học viên. Có lỗi xảy ra.";
                return RedirectToAction(nameof(Students));
            }
        }

         // Hàm trợ giúp để thêm lỗi từ IdentityResult vào ModelState
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                 Console.WriteLine($"- Identity Error: {error.Code} - {error.Description}"); // Log lỗi Identity
            }
        }

        #endregion // Kết thúc Quản lý Học viên


        #region Quản lý đăng ký học viên

        // GET: Admin/StudentRegistrations
        public async Task<IActionResult> StudentRegistrations(int page = 1, int pageSize = 20)
        {
            var query = _context.CourseRegistrations
                .Include(cr => cr.Course)
                .Include(cr => cr.User)
                .OrderByDescending(cr => cr.RegistrationDate);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo page không nhỏ hơn 1 và không lớn hơn totalPages
            page = Math.Max(1, page);
            page = Math.Min(page, totalPages > 0 ? totalPages : 1);


            var registrations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(registrations);
        }

        // POST: Admin/CancelRegistration/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            // Nên lấy cả thông tin Course để kiểm tra thêm nếu cần (ví dụ: khóa học đã bắt đầu chưa)
            var registration = await _context.CourseRegistrations
                                        .Include(r => r.Course)
                                        .FirstOrDefaultAsync(r => r.RegistrationId == id);

            if (registration == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đăng ký.";
                return RedirectToAction(nameof(StudentRegistrations));
            }

             if (registration.IsCanceled) {
                 TempData["WarningMessage"] = "Đăng ký này đã được hủy trước đó.";
                 return RedirectToAction(nameof(StudentRegistrations));
             }

             // Optional: Thêm logic nghiệp vụ, ví dụ không cho hủy nếu khóa học đã kết thúc
             // if (registration.Course.EndDate < DateTime.Now) {
             //    TempData["ErrorMessage"] = "Không thể hủy đăng ký cho khóa học đã kết thúc.";
             //    return RedirectToAction(nameof(StudentRegistrations));
             // }

            registration.IsCanceled = true;
            registration.CancelDate = DateTime.Now;
            // Cân nhắc: Có cần cập nhật lại số lượng học viên hiện tại của khóa học?
            // var course = await _context.Courses.FindAsync(registration.CourseId);
            // if (course != null) { course.CurrentStudents--; } // Cần thêm trường CurrentStudents vào model Course

            try
            {
                 await _context.SaveChangesAsync();
                 TempData["SuccessMessage"] = "Đã hủy đăng ký khóa học thành công!";
            }
            catch (DbUpdateException dbEx)
            {
                 // _logger.LogError(dbEx, $"Lỗi khi hủy đăng ký ID {id}.");
                Console.WriteLine($"ERROR canceling registration {id}: {dbEx.InnerException?.Message ?? dbEx.Message}");
                TempData["ErrorMessage"] = "Không thể hủy đăng ký. Có lỗi xảy ra với cơ sở dữ liệu.";
            }
             catch (Exception ex)
            {
                 // _logger.LogError(ex, $"Lỗi không mong muốn khi hủy đăng ký ID {id}.");
                Console.WriteLine($"ERROR canceling registration {id}: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi không mong muốn.";
            }


            return RedirectToAction(nameof(StudentRegistrations));
        }

        #endregion

        #region Phương thức hỗ trợ

        private async Task<bool> CourseExists(int id)
        {
            return await _context.Courses.AnyAsync(e => e.CourseId == id);
        }

        #endregion
    }

    // View model cho bảng điều khiển admin
    public class AdminDashboardViewModel
    {
        // Khởi tạo trực tiếp trong ViewModel để tránh null
        public List<CourseStudentStats> CourseStats { get; set; } = new List<CourseStudentStats>();
        public List<CourseRevenue> RevenueStats { get; set; } = new List<CourseRevenue>();
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new List<MonthlyRevenue>();
    }

    
}