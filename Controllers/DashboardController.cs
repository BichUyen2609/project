using HeThongTimViec.Data;
using HeThongTimViec.Models; // Ensure this includes LoaiTaiKhoan, HoSoUngVien, HoSoDoanhNghiep
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;    // Required for IHttpContextAccessor and Session
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HeThongTimViec.Controllers
{
    // Controller này là điểm vào DUY NHẤT cho tất cả các loại dashboard sau khi đăng nhập.
    // Nó sẽ xác định vai trò người dùng và trạng thái session để hiển thị đúng view.
    [Authorize] // Yêu cầu người dùng phải đăng nhập mới truy cập được bất kỳ action nào trong controller này.
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)] // Ngăn trình duyệt cache trang dashboard, đảm bảo luôn lấy dữ liệu mới nhất.
    public class DashboardController : Controller
    {
        // Dependencies được inject qua constructor
        private readonly ILogger<DashboardController> _logger; // Ghi log hoạt động, lỗi
        private readonly ApplicationDbContext _context; // Tương tác với cơ sở dữ liệu
        private readonly IHttpContextAccessor _httpContextAccessor; // Truy cập HttpContext (bao gồm Session)

        public DashboardController(
            ILogger<DashboardController> logger,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // --- Hàm helper kiểm tra Session xem người dùng 'canhan' có đang ở chế độ xem NTD không ---
        private bool IsNtdCaNhanViewActive()
        {
            // Truy cập Session một cách an toàn qua IHttpContextAccessor
            // Trả về true nếu Session "DangLaNTD" có giá trị là 1, ngược lại là false.
            return _httpContextAccessor.HttpContext?.Session?.GetInt32("DangLaNTD") == 1;
        }

        // --- Action chính xử lý tất cả các loại Dashboard ---
        // GET: /Dashboard/Index hoặc /Dashboard
        // Đây là action DUY NHẤT mà người dùng được điều hướng đến sau khi đăng nhập thành công.
        public async Task<IActionResult> Index()
        {
            // --- Lấy thông tin định danh người dùng từ Claims (được tạo khi đăng nhập) ---
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier); // Lấy User ID (dưới dạng string)
            var roleName = User.FindFirstValue(ClaimTypes.Role); // Lấy tên Role (ví dụ: "canhan", "doanhnghiep")
            var hoTenFromClaims = User.FindFirstValue(ClaimTypes.Name); // Lấy Họ tên (dùng cho chào mừng, đặc biệt NTD CN)

            // --- Kiểm tra cơ bản ---
            // 1. Kiểm tra User ID có hợp lệ không
            if (!int.TryParse(userIdString, out var userId))
            {
                _logger.LogWarning("Không thể phân tích User ID ('{UserIdString}') từ claims khi truy cập Dashboard.", userIdString ?? "NULL");
                // Trả về lỗi Unauthorized vì không xác thực được ID người dùng
                return Unauthorized("Không thể xác thực người dùng (ID không hợp lệ).");
            }

            // 2. Kiểm tra Role có tồn tại không
            if (string.IsNullOrEmpty(roleName))
            {
                _logger.LogWarning("User ID {UserId} không có role claim. Không thể xác định loại dashboard.", userId);
                // Trả về lỗi Forbid vì không có quyền (không xác định được quyền)
                return Forbid("Không thể xác định vai trò người dùng để hiển thị bảng điều khiển.");
            }

            _logger.LogInformation("Dashboard access attempt by User ID: {UserId} with Role: {Role}", userId, roleName);

            // --- Logic phân loại dựa trên Role ---
            // Luồng xử lý chính: Dựa vào roleName để quyết định fetch dữ liệu gì và trả về View nào.

            // 1. Xử lý vai trò Ứng viên (canhan)
            // Bao gồm cả logic của UngVienController và TuyenDungCaNhanController cũ
            if (roleName == nameof(LoaiTaiKhoan.canhan))
            {
                // *Kiểm tra phụ*: Người dùng 'canhan' này có đang bật chế độ xem "Nhà tuyển dụng Cá nhân" không?
                if (IsNtdCaNhanViewActive())
                {
                    // Nếu có (Session "DangLaNTD" == 1) -> Hiển thị Dashboard NTD Cá nhân
                    _logger.LogInformation("Hiển thị Dashboard Nhà tuyển dụng Cá nhân (NTD CN) cho User ID: {UserId}", userId);

                    // Gửi Họ tên qua ViewBag để chào mừng trên view NTD CN
                    ViewBag.HoTenNguoiDung = hoTenFromClaims;

                    // Trả về view *cụ thể* cho NTD Cá nhân.
                    // URL trên trình duyệt vẫn là /Dashboard, nhưng nội dung là của EmployerForCandidateDashboard.cshtml
                    // Đảm bảo file Views/Dashboard/EmployerForCandidateDashboard.cshtml tồn tại.
                    return View("EmployerForCandidateDashboard");
                }
                else
                {
                    // Nếu không -> Hiển thị Dashboard Ứng viên thông thường
                    _logger.LogInformation("Hiển thị Dashboard Ứng viên (chuẩn) cho User ID: {UserId}", userId);

                    // Lấy hồ sơ ứng viên từ DB
                    var candidateProfile = await _context.HoSoUngViens
                                                  .Include(h => h.NguoiDung) // Lấy kèm thông tin NguoiDung nếu cần hiển thị trong view
                                                  .FirstOrDefaultAsync(h => h.NguoiDungId == userId);

                    // Kiểm tra nếu chưa có hồ sơ
                    if (candidateProfile == null)
                    {
                        _logger.LogWarning("Không tìm thấy Hồ sơ Ứng viên (chuẩn) cho User ID: {UserId}.", userId);
                        // Xử lý phù hợp: thông báo lỗi, chuyển hướng đến trang tạo hồ sơ...
                        // Ở đây trả về NotFound giống controller gốc.
                        return NotFound("Không tìm thấy hồ sơ ứng viên. Vui lòng cập nhật hồ sơ của bạn.");
                    }

                    // Trả về view *cụ thể* cho Ứng viên kèm theo model hồ sơ.
                    // URL trên trình duyệt vẫn là /Dashboard, nhưng nội dung là của CandidateDashboard.cshtml
                    // Đảm bảo file Views/Dashboard/CandidateDashboard.cshtml tồn tại.
                    return View("CandidateDashboard", candidateProfile);
                }
            }

            // 2. Xử lý vai trò Nhà tuyển dụng (doanhnghiep)
            // Logic của NhaTuyenDungController cũ
            else if (roleName == nameof(LoaiTaiKhoan.doanhnghiep))
            {
                _logger.LogInformation("Hiển thị Dashboard Nhà tuyển dụng (Doanh nghiệp) cho User ID: {UserId}", userId);

                // Lấy hồ sơ doanh nghiệp từ DB
                var employerProfile = await _context.HoSoDoanhNghieps
                                              .Include(h => h.NguoiDung) // Lấy kèm thông tin NguoiDung nếu cần
                                              .FirstOrDefaultAsync(h => h.NguoiDungId == userId);

                // Kiểm tra nếu chưa có hồ sơ (trường hợp lỗi hoặc DN chưa được duyệt/tạo hồ sơ)
                if (employerProfile == null)
                {
                    _logger.LogWarning("Không tìm thấy Hồ sơ Doanh nghiệp cho User ID: {UserId}.", userId);
                     // Xử lý phù hợp: thông báo lỗi, liên hệ admin...
                     return NotFound("Không tìm thấy hồ sơ nhà tuyển dụng. Vui lòng liên hệ quản trị viên nếu bạn tin rằng đây là lỗi.");
                }

                // Trả về view *cụ thể* cho Nhà tuyển dụng kèm theo model hồ sơ.
                // URL trên trình duyệt vẫn là /Dashboard, nhưng nội dung là của EmployerDashboard.cshtml
                // Đảm bảo file Views/Dashboard/EmployerDashboard.cshtml tồn tại.
                return View("EmployerDashboard", employerProfile);
            }

            // 3. Xử lý vai trò Quản trị viên (quantrivien)
            // Logic của AdminController cũ
            else if (roleName == nameof(LoaiTaiKhoan.quantrivien))
            {
                _logger.LogInformation("Hiển thị Dashboard Quản trị viên cho User ID: {UserId}", userId);

                // Có thể fetch dữ liệu tổng hợp cho admin ở đây nếu cần (ví dụ: số tài khoản chờ duyệt)
                // var pendingApprovalCount = await _context.NguoiDungs...
                // ViewBag.PendingApprovals = pendingApprovalCount;

                // Trả về view *cụ thể* cho Quản trị viên.
                // URL trên trình duyệt vẫn là /Dashboard, nhưng nội dung là của AdminDashboard.cshtml
                // Đảm bảo file Views/Dashboard/AdminDashboard.cshtml tồn tại.
                return View("AdminDashboard");
            }

            // 4. Xử lý trường hợp Role không mong muốn (an toàn dự phòng)
            else
            {
                _logger.LogError("User ID {UserId} có vai trò không nhận dạng được hoặc không hỗ trợ: {Role}. Truy cập bị từ chối.", userId, roleName);
                // Trả về lỗi Forbid vì vai trò này không được phép vào dashboard.
                return Forbid($"Vai trò '{roleName}' không được phép truy cập dashboard này.");
            }
        }

        // --- Các Actions khác ---
        // KHÔNG NÊN đặt các action xử lý nghiệp vụ cụ thể của từng vai trò (như Đăng tin, Sửa hồ sơ, Phê duyệt...) vào đây.
        // Nên giữ chúng trong các Controller riêng biệt (vd: JobPostingController, ProfileController, AdminManagementController)
        // để mã nguồn rõ ràng, dễ quản lý và bảo trì hơn.
        // DashboardController chỉ nên làm nhiệm vụ điều hướng ban đầu đến đúng giao diện tổng quan.
    }
}