// File: Controllers/TaiKhoanController.cs
// Version: Hoàn thiện - Đầy đủ mã nguồn - Đã sửa logic chuyển hướng

using HeThongTimViec.Data;
using HeThongTimViec.Models;
using HeThongTimViec.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System; // Thêm để dùng DateTime, Guid, Exception
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using HETHONGTIMVIEC.Controllers;
// using System.Text.Encodings.Web; // Chỉ cần nếu dùng HtmlEncoder trong gửi email

namespace HeThongTimViec.Controllers
{
    /// <summary>
    /// Controller quản lý các hoạt động liên quan đến tài khoản người dùng:
    /// Đăng ký, Đăng nhập, Đăng xuất, Quên mật khẩu, Cài đặt tài khoản, Chuyển đổi vai trò xem.
    /// Đã cập nhật để chuyển hướng đúng đến DashboardController.Index sau đăng nhập và chuyển đổi vai trò.
    /// </summary>
    public class TaiKhoanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaiKhoanController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        // private readonly IEmailSender _emailSender; // Dịch vụ gửi email (nếu có)

        public TaiKhoanController(
            ApplicationDbContext context,
            ILogger<TaiKhoanController> logger,
            IHttpContextAccessor httpContextAccessor
            /*, IEmailSender emailSender */ )
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            // _emailSender = emailSender;
        }

        // --- Các hàm Helper ---

        // Tạo SelectList loại tài khoản (bỏ Admin)
        private SelectList GetLoaiTaiKhoanList()
        {
            return new SelectList(Enum.GetValues(typeof(LoaiTaiKhoan))
                                    .Cast<LoaiTaiKhoan>()
                                    .Where(e => e != LoaiTaiKhoan.quantrivien)
                                    .Select(e => new SelectListItem
                                    {
                                        Value = e.ToString(),
                                        Text = e == LoaiTaiKhoan.canhan ? "Cá nhân / Nhà tuyển dụng cá nhân" : "Nhà tuyển dụng Doanh nghiệp"
                                    }), "Value", "Text");
        }

        // Lấy danh sách Thành Phố cho Dropdown
        private async Task PopulateThanhPhoDropdownAsync(object? selectedThanhPho = null)
        {
             ViewBag.ThanhPhoList = new SelectList(await _context.ThanhPhos.OrderBy(tp => tp.Ten).ToListAsync(),
                                                 "Id", "Ten", selectedThanhPho);
        }

        // Lấy danh sách Quận Huyện theo Thành phố cho Dropdown
        private async Task PopulateQuanHuyenDropdownAsync(int? thanhPhoId, object? selectedQuanHuyen = null)
        {
             if (thanhPhoId.HasValue && thanhPhoId > 0)
             {
                 ViewBag.QuanHuyenList = new SelectList(await _context.QuanHuyens
                                                                   .Where(qh => qh.ThanhPhoId == thanhPhoId)
                                                                   .OrderBy(qh => qh.Ten).ToListAsync(),
                                                        "Id", "Ten", selectedQuanHuyen);
             } else {
                  ViewBag.QuanHuyenList = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Ten"); // Rỗng nếu không có TP
             }
        }

        // Tạo SelectList Giới Tính
        private SelectList GetGioiTinhList() {
            return new SelectList(Enum.GetValues(typeof(GioiTinhNguoiDung))
                                   .Cast<GioiTinhNguoiDung>()
                                   .Select(e => new SelectListItem {
                                       Value = e.ToString(),
                                       Text = e == GioiTinhNguoiDung.nam ? "Nam" : (e == GioiTinhNguoiDung.nu ? "Nữ" : "Khác")
                                   }),
                                  "Value", "Text");
        }

        // Làm mới cookie đăng nhập sau khi thông tin người dùng thay đổi (vd: đổi tên)
        private async Task RefreshSignIn(int userId)
        {
            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null)
            {
                 _logger.LogWarning("Không tìm thấy user {UserId} để làm mới cookie.", userId);
                 return;
            }

            // Lấy principal hiện tại từ context
            var principalResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (principalResult?.Principal == null || !principalResult.Succeeded) {
                 _logger.LogWarning("Không thể lấy principal hiện tại để làm mới claims cho user {UserId}", userId);
                 return; // Không thể làm mới nếu không lấy được principal cũ
             }

             // Tạo lại danh sách claims với thông tin mới nhất từ user
             var newClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen), // Cập nhật Họ Tên
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.LoaiTk.ToString())
                 // Thêm các claim khác nếu bạn có lưu trong cookie
            };
            var newIdentity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var newPrincipal = new ClaimsPrincipal(newIdentity);

            // Đăng nhập lại với principal mới, giữ nguyên properties cũ (như RememberMe)
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal, principalResult.Properties);
             _logger.LogDebug("Đã làm mới cookie đăng nhập cho user {UserId} sau khi cập nhật hồ sơ.", userId);
         }


        // --- Actions ---

        // GET: /TaiKhoan/DangKy
        [AllowAnonymous]
        public async Task<IActionResult> DangKy(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.LoaiTkList = GetLoaiTaiKhoanList();
            await PopulateThanhPhoDropdownAsync();
            await PopulateQuanHuyenDropdownAsync(null); // Khởi tạo rỗng
            ViewBag.GioiTinhList = GetGioiTinhList();
            return View(new RegisterViewModel());
        }

        // POST: /TaiKhoan/DangKy
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangKy(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            // Luôn load lại dropdown phòng trường hợp validation fail và trả về View
            ViewBag.LoaiTkList = GetLoaiTaiKhoanList();
            await PopulateThanhPhoDropdownAsync(model.ThanhPhoId);
            await PopulateQuanHuyenDropdownAsync(model.ThanhPhoId, model.QuanHuyenId);
            ViewBag.GioiTinhList = GetGioiTinhList();

            // --- Custom Validation ---
            if (model.LoaiTkDangKy == LoaiTaiKhoan.doanhnghiep)
            {
                 if (string.IsNullOrWhiteSpace(model.TenCongTy)) { ModelState.AddModelError(nameof(model.TenCongTy), "Vui lòng nhập tên công ty."); }
                 if (string.IsNullOrWhiteSpace(model.MaSoThue)) { ModelState.AddModelError(nameof(model.MaSoThue), "Vui lòng nhập mã số thuế."); }
                 if (string.IsNullOrWhiteSpace(model.DiaChiDangKyKinhDoanh)) { ModelState.AddModelError(nameof(model.DiaChiDangKyKinhDoanh), "Vui lòng nhập địa chỉ đăng ký kinh doanh."); }
                 // Kiểm tra MST duy nhất
                 if (!string.IsNullOrWhiteSpace(model.MaSoThue) && await _context.HoSoDoanhNghieps.AnyAsync(h => h.MaSoThue == model.MaSoThue)) {
                     ModelState.AddModelError(nameof(model.MaSoThue), "Mã số thuế này đã được đăng ký.");
                 }
            }
            // Kiểm tra Email duy nhất
            if (await _context.NguoiDungs.AnyAsync(u => u.Email == model.Email)) {
                ModelState.AddModelError(nameof(model.Email), "Địa chỉ email này đã được sử dụng.");
            }
            // Kiểm tra SĐT duy nhất (nếu có nhập)
            if (!string.IsNullOrEmpty(model.Sdt) && await _context.NguoiDungs.AnyAsync(u => u.Sdt == model.Sdt)) {
                 ModelState.AddModelError(nameof(model.Sdt), "Số điện thoại này đã được sử dụng.");
            }
            // --- Hết Custom Validation ---

            if (ModelState.IsValid)
            {
                // Hash mật khẩu
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                // Xác định trạng thái ban đầu
                TrangThaiTaiKhoan initialStatus = (model.LoaiTkDangKy == LoaiTaiKhoan.doanhnghiep)
                                                ? TrangThaiTaiKhoan.choxacminh // DN cần chờ duyệt
                                                : TrangThaiTaiKhoan.kichhoat; // Cá nhân kích hoạt ngay

                // Tạo đối tượng NguoiDung
                var user = new NguoiDung
                {
                    Email = model.Email,
                    MatKhauHash = passwordHash,
                    HoTen = model.HoTen,
                    Sdt = model.Sdt,
                    LoaiTk = model.LoaiTkDangKy,
                    GioiTinh = model.GioiTinh,
                    NgaySinh = model.NgaySinh,
                    DiaChiChiTiet = model.DiaChiChiTiet,
                    QuanHuyenId = model.QuanHuyenId,
                    ThanhPhoId = model.ThanhPhoId,
                    NgayTao = DateTime.UtcNow,
                    NgayCapNhat = DateTime.UtcNow,
                    TrangThaiTk = initialStatus,
                    LanDangNhapCuoi = null,
                    UrlAvatar = null // Mặc định avatar null
                };

                // Sử dụng Transaction để đảm bảo tính toàn vẹn (hoặc tạo user hoặc không tạo gì cả)
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Lưu NguoiDung vào DB để lấy Id
                    _context.NguoiDungs.Add(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Tạo người dùng mới ID {UserId}: {Email}", user.Id, user.Email);

                    // 2. Tạo hồ sơ tương ứng (HoSoDoanhNghiep hoặc HoSoUngVien)
                    if (user.LoaiTk == LoaiTaiKhoan.doanhnghiep)
                    {
                        var hoSoDN = new HoSoDoanhNghiep
                        {
                            NguoiDungId = user.Id, // Liên kết với NguoiDung vừa tạo
                            TenCongTy = model.TenCongTy!, // Dấu ! để báo compiler rằng ta đã check null ở validation
                            MaSoThue = model.MaSoThue!,
                            UrlWebsite = model.UrlWebsite,
                            DiaChiDangKy = model.DiaChiDangKyKinhDoanh,
                            DaXacMinh = false, // Mặc định là chưa xác minh
                            AdminXacMinhId = null,
                            NgayXacMinh = null
                        };
                        _context.HoSoDoanhNghieps.Add(hoSoDN);
                        _logger.LogInformation("Tạo hồ sơ Doanh nghiệp chờ duyệt cho User ID {UserId}", user.Id);
                    }
                    else // LoaiTk = canhan
                    {
                        var hoSoUV = new HoSoUngVien
                        {
                            NguoiDungId = user.Id, // Liên kết với NguoiDung vừa tạo
                            TrangThaiTimViec = TrangThaiTimViec.dangtimtichcuc // Trạng thái mặc định
                            // Có thể thêm các giá trị mặc định khác nếu cần
                        };
                        _context.HoSoUngViens.Add(hoSoUV);
                        _logger.LogInformation("Tạo hồ sơ Ứng viên cho User ID {UserId}", user.Id);
                    }
                    // 3. Lưu hồ sơ vào DB
                    await _context.SaveChangesAsync();

                    // 4. Nếu tất cả thành công, commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Transaction đăng ký cho User ID {UserId} thành công.", user.Id);

                    // 5. Chuyển hướng sau khi đăng ký thành công
                    if (user.LoaiTk == LoaiTaiKhoan.doanhnghiep)
                    {
                         // DN -> Trang thông báo chờ duyệt
                        return RedirectToAction(nameof(DangKyThanhCongChoDuyet));
                    }
                    else // canhan
                    {
                         // Cá nhân -> Trang đăng nhập với thông báo thành công
                         TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                         return RedirectToAction(nameof(DangNhap));
                    }
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi ở bất kỳ bước nào, rollback transaction
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi nghiêm trọng khi đăng ký và tạo hồ sơ cho {Email}.", model.Email);
                    // Thêm lỗi chung vào ModelState để hiển thị cho người dùng
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi không mong muốn trong quá trình đăng ký, vui lòng thử lại sau.");
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi transaction, hiển thị lại form đăng ký với lỗi
            _logger.LogWarning("Đăng ký không thành công cho {Email}. ModelState Invalid: {IsInvalid}", model.Email, !ModelState.IsValid);
            return View(model);
        }

        // GET: /TaiKhoan/DangKyThanhCongChoDuyet
        [AllowAnonymous]
        public IActionResult DangKyThanhCongChoDuyet()
        {
            // View này chỉ hiển thị thông báo cho NTD Doanh nghiệp
            return View();
        }

        // GET: /TaiKhoan/DangNhap
        [AllowAnonymous]
        public IActionResult DangNhap(string? returnUrl = null)
        {
            // Xóa session cũ (nếu có) để đảm bảo trạng thái sạch khi vào trang đăng nhập
             _httpContextAccessor.HttpContext?.Session.Clear();
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /TaiKhoan/DangNhap
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangNhap(LoginViewModel model, string? returnUrl = null)
        {
            string appRoot = Url.Content("~/") ?? "/"; // Lấy URL gốc của ứng dụng
            returnUrl ??= appRoot; // Gán mặc định nếu returnUrl là null
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Lấy user từ DB theo Email, kèm theo HoSoDoanhNghiep nếu có
                var user = await _context.NguoiDungs
                                    .Include(u => u.HoSoDoanhNghiep)
                                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null) // Kiểm tra user tồn tại
                {
                    // 1. Kiểm tra trạng thái tài khoản trước khi check mật khẩu
                     if (user.TrangThaiTk == TrangThaiTaiKhoan.choxacminh)
                     {
                         _logger.LogWarning("Login attempt for unverified account: {Email}", model.Email);
                         ModelState.AddModelError(string.Empty, "Tài khoản của bạn đang chờ phê duyệt.");
                         return View(model); // Trả về View với lỗi
                     }
                     if (user.TrangThaiTk == TrangThaiTaiKhoan.tamdung || user.TrangThaiTk == TrangThaiTaiKhoan.bidinhchi)
                     {
                          _logger.LogWarning("Login attempt for suspended/banned account: {Email}", model.Email);
                         ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị tạm dừng hoặc đình chỉ.");
                         return View(model); // Trả về View với lỗi
                     }

                    // 2. Chỉ kiểm tra mật khẩu nếu tài khoản hợp lệ
                    if (BCrypt.Net.BCrypt.Verify(model.Password, user.MatKhauHash))
                    {
                        _logger.LogInformation("User {Email} đăng nhập thành công.", user.Email);

                        // 3. Tạo Claims cho người dùng
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID người dùng
                            new Claim(ClaimTypes.Name, user.HoTen),                   // Tên người dùng
                            new Claim(ClaimTypes.Email, user.Email),                  // Email
                            new Claim(ClaimTypes.Role, user.LoaiTk.ToString())        // Vai trò (quan trọng)
                            // Thêm các claim khác nếu cần
                        };
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe, // Lưu cookie lâu dài?
                            IssuedUtc = DateTimeOffset.UtcNow // Thời gian phát hành
                            // Có thể đặt ExpiresUtc nếu muốn thời gian cố định thay vì session
                        };
                        if(model.RememberMe) {
                            authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14); // Ví dụ: cookie tồn tại 14 ngày
                        }

                        // 4. Thực hiện đăng nhập (tạo cookie)
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // 5. Cập nhật lần đăng nhập cuối (không quá quan trọng, bỏ qua lỗi nếu có)
                        try
                        {
                             user.LanDangNhapCuoi = DateTime.UtcNow;
                             await _context.SaveChangesAsync();
                        }
                        catch (Exception ex) {
                             _logger.LogWarning(ex, "Không thể cập nhật LastLoginTime cho user {UserId}", user.Id);
                        }

                        // 6. Xử lý Session (đặt trạng thái xem mặc định)
                        var session = _httpContextAccessor.HttpContext?.Session;
                        if (session != null)
                        {
                             session.Clear(); // Luôn xóa session cũ
                             if (user.LoaiTk == LoaiTaiKhoan.canhan)
                             {
                                 session.SetInt32("DangLaNTD", 0); // Cá nhân mặc định là Ứng viên
                                 _logger.LogDebug("Session 'DangLaNTD' được đặt là 0 cho user {UserId}", user.Id);
                             }
                             else if (user.LoaiTk == LoaiTaiKhoan.doanhnghiep && user.HoSoDoanhNghiep != null)
                             {
                                  // Lưu tên công ty vào session để hiển thị (ví dụ)
                                  session.SetString("TenCongTy", user.HoSoDoanhNghiep.TenCongTy);
                                  _logger.LogDebug("Session 'TenCongTy' được đặt cho user {UserId}", user.Id);
                             }
                             // Không cần đặt gì cho Admin
                        } else {
                            _logger.LogWarning("Session không khả dụng khi đăng nhập cho user {UserId}", user.Id);
                        }

                        // --- ĐÃ SỬA: Logic chuyển hướng ---
                        // Xác định URL mặc định là Dashboard/Index cho TẤT CẢ các vai trò
                        string defaultRedirectTarget = Url.Action("Index", "Dashboard")!;
                        if (string.IsNullOrEmpty(defaultRedirectTarget))
                        {
                            _logger.LogError("Không thể tạo URL cho Dashboard/Index. Kiểm tra cấu hình routing.");
                            defaultRedirectTarget = appRoot; // Fallback về trang chủ nếu lỗi
                        }

                        string redirectTarget;
                        // Ưu tiên returnUrl nếu nó hợp lệ, cục bộ và không phải trang chủ mặc định
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != appRoot)
                        {
                             _logger.LogInformation("Đang chuyển hướng người dùng {UserId} (Vai trò: {UserRole}) đến returnUrl được cung cấp: {ReturnUrl}", user.Id, user.LoaiTk, returnUrl);
                            redirectTarget = returnUrl;
                        }
                        else
                        {
                            // Nếu không có returnUrl hợp lệ, dùng trang Dashboard/Index làm mặc định
                             _logger.LogInformation("Đang chuyển hướng người dùng {UserId} (Vai trò: {UserRole}) đến trang dashboard mặc định ({DefaultTarget}). returnUrl ban đầu: '{OriginalReturnUrl}'",
                                user.Id, user.LoaiTk, defaultRedirectTarget, ViewData["ReturnUrl"]);
                            redirectTarget = defaultRedirectTarget;
                        }
                        // --- KẾT THÚC SỬA ĐỔI ---

                        _logger.LogInformation("Final redirect target for user {UserId}: {RedirectTarget}", user.Id, redirectTarget);
                        // Sử dụng LocalRedirect để tăng bảo mật
                        return LocalRedirect(redirectTarget);
                    }
                    else
                    {
                        // Sai mật khẩu
                         _logger.LogWarning("Sai mật khẩu cho {Email}", model.Email);
                    }
                }
                else
                {
                    // Email không tồn tại
                    _logger.LogWarning("Email đăng nhập không tồn tại: {Email}", model.Email);
                }

                // Nếu đến đây là do sai mật khẩu hoặc email không tồn tại
                ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không hợp lệ.");
            }
            else // ModelState không hợp lệ ngay từ đầu
            {
                 _logger.LogWarning("Đăng nhập thất bại do ModelState không hợp lệ.");
            }

            // Nếu có lỗi, hiển thị lại form đăng nhập với thông báo lỗi
            return View(model);
        }

        // POST: /TaiKhoan/DangXuat
        [HttpPost]
        [Authorize] // Chỉ người đã đăng nhập mới đăng xuất được
        [ValidateAntiForgeryToken] // Chống tấn công CSRF
        public async Task<IActionResult> DangXuat()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Xóa cookie xác thực của người dùng
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa toàn bộ dữ liệu trong Session của người dùng
            _httpContextAccessor.HttpContext?.Session.Clear();

            _logger.LogInformation("User {Email} (ID: {UserId}) đã đăng xuất.", userEmail ?? "[không có email]", userId ?? "[không có ID]");

            // Chuyển hướng về trang chủ sau khi đăng xuất
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        // GET: /TaiKhoan/QuenMatKhau
        [AllowAnonymous]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        // POST: /TaiKhoan/QuenMatKhau
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuenMatKhau(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Chỉ xử lý cho email tồn tại và tài khoản đang kích hoạt
                var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == model.Email && u.TrangThaiTk == TrangThaiTaiKhoan.kichhoat);
                if (user != null)
                {
                    // === PHẦN QUAN TRỌNG: CẦN TRIỂN KHAI BẢO MẬT HƠN ===
                    // Tạo token reset tạm thời (ví dụ: Guid) - KHÔNG ĐỦ AN TOÀN cho production
                    var resetToken = Guid.NewGuid().ToString("N").Substring(0, 20);
                    // TODO: Thay thế bằng cơ chế token an toàn:
                    // 1. Dùng Identity UserManager để tạo token (nếu dùng ASP.NET Core Identity).
                    // 2. Tự tạo token (JWT, hoặc random string), hash nó, lưu hash + UserID + ExpiresAt vào DB.
                    _logger.LogWarning("Đang tạo token reset KHÔNG BẢO MẬT {Token} cho user {Email}", resetToken, user.Email);

                    // Tạo link callback chứa email và token
                    var callbackUrl = Url.Action(
                        action: nameof(DatLaiMatKhau),
                        controller: "TaiKhoan",
                        values: new { email = user.Email, code = resetToken },
                        protocol: Request.Scheme); // Lấy scheme http/https tự động

                    _logger.LogInformation("Link đặt lại MK (DEBUG) cho {Email}: {Url}", model.Email, callbackUrl);

                    // --- TODO: GỬI EMAIL CHỨA callbackUrl CHO NGƯỜỜI DÙNG ---
                    // Cần inject và sử dụng một dịch vụ gửi email (IEmailSender)
                    // string emailSubject = "Yêu cầu đặt lại mật khẩu";
                    // string emailBody = $"Chào {user.HoTen},\n\nVui lòng nhấn vào link sau để đặt lại mật khẩu của bạn:\n{callbackUrl}\n\nNếu bạn không yêu cầu điều này, vui lòng bỏ qua email.";
                    // await _emailSender.SendEmailAsync(model.Email, emailSubject, emailBody);
                    // --- KẾT THÚC GỬI EMAIL ---

                    // Lưu ý: Để tăng tốc phát triển, tạm thời chỉ ghi link ra Debug Output
                    System.Diagnostics.Debug.WriteLine($"DEBUG ONLY - Link đặt lại MK cho {model.Email}: {callbackUrl}");

                }
                else
                {
                     // Email không tồn tại hoặc tài khoản không kích hoạt
                     _logger.LogWarning("Yêu cầu đặt lại MK cho email không tồn tại hoặc chưa kích hoạt: {Email}", model.Email);
                     // Không thông báo lỗi cụ thể để tránh dò quét email
                }

                // Luôn chuyển hướng đến trang xác nhận chung chung để bảo mật thông tin email nào tồn tại/không tồn tại
                 TempData["InfoMessage"] = "Nếu email của bạn tồn tại trong hệ thống và đã được kích hoạt, một hướng dẫn đặt lại mật khẩu sẽ được gửi đến bạn.";
                 return RedirectToAction(nameof(QuenMatKhauXacNhan));
            }

            // Nếu ModelState không hợp lệ
            return View(model);
        }

        // GET: /TaiKhoan/QuenMatKhauXacNhan
        [AllowAnonymous]
        public IActionResult QuenMatKhauXacNhan()
        {
            // View này chỉ hiển thị thông báo chung chung đã đặt trong TempData
            return View();
        }

        // GET: /TaiKhoan/DatLaiMatKhau
        [AllowAnonymous]
        public IActionResult DatLaiMatKhau(string? code = null, string? email = null)
        {
            // Kiểm tra đầu vào cơ bản
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Truy cập link đặt lại MK không hợp lệ (thiếu code hoặc email).");
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(DangNhap)); // Chuyển về đăng nhập
            }

            // === PHẦN QUAN TRỌNG: CẦN TRIỂN KHAI BẢO MẬT HƠN ===
            // TODO: Xác thực token `code` dựa trên `email`.
            // - Tìm trong DB xem có bản ghi token nào ứng với hash của `code`, `email` và chưa hết hạn?
            // - Nếu không hợp lệ -> Redirect về DangNhap với TempData báo lỗi.
            _logger.LogWarning("Đang xử lý yêu cầu đặt lại MK với cơ chế xác thực token KHÔNG BẢO MẬT. Email: {Email}, Token: {Code}", email, code);
            // Nếu token hợp lệ (theo logic TODO ở trên) thì mới hiển thị View

            var model = new ResetPasswordViewModel { Token = code, Email = email };
            return View(model);
        }

        // POST: /TaiKhoan/DatLaiMatKhau
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatLaiMatKhau(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                 _logger.LogWarning("POST Đặt lại MK thất bại do ModelState không hợp lệ cho Email: {Email}", model.Email);
                return View(model); // Hiển thị lại form với lỗi validation
            }

            // === PHẦN QUAN TRỌNG: CẦN TRIỂN KHAI BẢO MẬT HƠN ===
             // TODO: Xác thực lại token `model.Token` cho `model.Email` một lần nữa trước khi đổi mật khẩu.
             // - Đảm bảo token vẫn hợp lệ và chưa hết hạn/chưa bị dùng.
             // - Nếu không hợp lệ:
             //   ModelState.AddModelError(string.Empty, "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
             //   return View(model);
             _logger.LogWarning("Đang tiến hành đặt lại MK với cơ chế xác thực token KHÔNG BẢO MẬT. Email: {Email}, Token: {Token}", model.Email, model.Token);

            // Tìm user hợp lệ (đang kích hoạt)
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Email == model.Email && u.TrangThaiTk == TrangThaiTaiKhoan.kichhoat);
            if (user != null)
            {
                // Hash mật khẩu mới và cập nhật thông tin user
                user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                user.NgayCapNhat = DateTime.UtcNow;
                // TODO: Sau khi đổi MK thành công, cần vô hiệu hóa token đã sử dụng trong CSDL.
                // Ví dụ: Đánh dấu token là đã dùng hoặc xóa bản ghi token.

                try
                {
                    // Lưu thay đổi vào DB
                    // _context.Update(user); // Không cần thiết nếu user được tracked
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đặt lại mật khẩu thành công cho {Email}", model.Email);
                    // TODO: Gửi email thông báo đổi MK thành công (tùy chọn)

                    TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới.";
                    return RedirectToAction(nameof(DangNhap)); // Chuyển về trang đăng nhập
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi lưu mật khẩu mới cho {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi lưu mật khẩu mới. Vui lòng thử lại.");
                    // Không return Redirect ở đây, để hiển thị lỗi trên form
                }
            }
            else
            {
                 // User không tồn tại hoặc không kích hoạt tại thời điểm POST (có thể link đã cũ)
                 _logger.LogWarning("User không tìm thấy hoặc không hoạt động khi POST đặt lại MK cho {Email}", model.Email);
                 // Không báo lỗi cụ thể, chỉ chuyển hướng về đăng nhập
                 TempData["ErrorMessage"] = "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                 return RedirectToAction(nameof(DangNhap));
            }

            // Nếu có lỗi ModelState hoặc lỗi DB, hiển thị lại form
            return View(model);
        }

        // GET: /TaiKhoan/Lockout (Dùng khi triển khai cơ chế khóa tài khoản tạm thời)
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        // GET: /TaiKhoan/AccessDenied (Trang thông báo không có quyền)
        [AllowAnonymous] // Hoặc [Authorize] nếu chỉ người đăng nhập mới thấy
        public IActionResult AccessDenied(string? returnUrl = null)
        {
             _logger.LogWarning("Truy cập bị từ chối cho user {UserName} đến {ReturnUrl}", User.Identity?.Name ?? "Anonymous", returnUrl);
             ViewData["ReturnUrl"] = returnUrl; // Có thể dùng để hiển thị link quay lại
             return View();
        }

        // POST: /TaiKhoan/ChuyenDoiVaiTro (Đổi chế độ xem giữa Ứng viên và NTD Cá nhân)
        [HttpPost]
        [Authorize(Roles = nameof(LoaiTaiKhoan.canhan))] // Chỉ user 'canhan' mới dùng được
        [ValidateAntiForgeryToken]
        public IActionResult ChuyenDoiVaiTro(string? returnUrl = null)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null) // Kiểm tra session có tồn tại không
            {
                _logger.LogError("Session không khả dụng khi đổi vai trò cho user {UserId}.", User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData["ErrorMessage"] = "Phiên làm việc không hợp lệ, vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(DangNhap)); // Chuyển về đăng nhập nếu mất session
            }

            // Lấy trạng thái hiện tại và tính trạng thái mới
            bool dangLaNTD = session.GetInt32("DangLaNTD") == 1;
            bool seLaNTD = !dangLaNTD; // Đảo ngược trạng thái
            session.SetInt32("DangLaNTD", seLaNTD ? 1 : 0); // Lưu trạng thái mới vào session

            _logger.LogInformation("User {UserId} đổi chế độ xem. DangLaNTD bây giờ là: {IsEmployer}", User.FindFirstValue(ClaimTypes.NameIdentifier), seLaNTD);

            // --- ĐÃ SỬA: Logic chuyển hướng ---
            // Đích đến mặc định LUÔN LÀ Dashboard/Index sau khi chuyển đổi
            string dashboardUrl = Url.Action("Index", "Dashboard")!;
            if (string.IsNullOrEmpty(dashboardUrl)) {
                 _logger.LogError("Không thể tạo URL cho Dashboard/Index khi đổi vai trò.");
                 dashboardUrl = Url.Content("~/") ?? "/"; // Fallback nếu có lỗi routing
             }

            string targetUrlToRedirect = dashboardUrl; // Mặc định là dashboard

            // Kiểm tra returnUrl: Nếu hợp lệ, cục bộ và *không phải* là chính trang dashboard thì dùng nó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Equals(dashboardUrl, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Đang chuyển hướng sau khi đổi vai trò đến returnUrl được cung cấp: {ReturnUrl}", returnUrl);
                targetUrlToRedirect = returnUrl; // Ưu tiên returnUrl nếu hợp lệ
            } else {
                // Ghi log nếu returnUrl bị bỏ qua
                if(!string.IsNullOrEmpty(returnUrl) && returnUrl.Equals(dashboardUrl, StringComparison.OrdinalIgnoreCase)) {
                     _logger.LogDebug("Bỏ qua returnUrl ({ReturnUrl}) vì nó là trang dashboard, chuyển về dashboard mặc định.", returnUrl);
                } else {
                     _logger.LogDebug("Đang chuyển hướng sau khi đổi vai trò đến dashboard mặc định ({DashboardUrl}). returnUrl ban đầu: '{OriginalReturnUrl}'", dashboardUrl, returnUrl);
                }
            }
            // --- KẾT THÚC SỬA ĐỔI ---

            // Chuyển hướng đến URL đích đã xác định
            return LocalRedirect(targetUrlToRedirect);
        }

        // GET: /TaiKhoan/CaiDat (Hiển thị form cài đặt thông tin chung)
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> CaiDat()
        {
             var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
             if (!int.TryParse(userIdString, out var userId)) {
                 _logger.LogWarning("Không thể parse User ID từ claims trong GET CaiDat.");
                 return Unauthorized();
             }

             // Lấy thông tin user hiện tại từ DB để điền vào form
             var user = await _context.NguoiDungs
                                    .Include(u => u.ThanhPho) // Include để lấy tên TP
                                    .Include(u => u.QuanHuyen) // Include để lấy tên QH
                                    .FirstOrDefaultAsync(u => u.Id == userId);

             if (user == null) {
                  _logger.LogError("User ID {UserId} không tìm thấy trong DB khi GET CaiDat.", userId);
                 return NotFound("Không tìm thấy tài khoản của bạn.");
             }

             // Chuẩn bị dữ liệu cho các dropdown list
             await PopulateThanhPhoDropdownAsync(user.ThanhPhoId);
             await PopulateQuanHuyenDropdownAsync(user.ThanhPhoId, user.QuanHuyenId);
             ViewBag.GioiTinhList = GetGioiTinhList();

             // Truyền model user vào View
             return View(user);
        }

        // POST: /TaiKhoan/CaiDat (Xử lý cập nhật thông tin chung)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        // Bind chỉ các thuộc tính cho phép sửa từ form để tránh overposting
        public async Task<IActionResult> CaiDat([Bind("Id,HoTen,Sdt,GioiTinh,NgaySinh,DiaChiChiTiet,QuanHuyenId,ThanhPhoId")] NguoiDung updatedUserFormData)
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); // Đã Authorize nên chắc chắn có ID

            // Ngăn người dùng sửa thông tin của người khác bằng cách gửi ID giả mạo
            if (updatedUserFormData.Id != currentUserId)
            {
                _logger.LogWarning("User {ActualUserId} cố gắng POST cài đặt cho ID khác {TargetUserId}", currentUserId, updatedUserFormData.Id);
                return Forbid("Bạn không có quyền thay đổi thông tin của tài khoản này.");
            }

            // Lấy đối tượng User gốc từ DB để cập nhật, không tạo mới
            var userInDb = await _context.NguoiDungs.FindAsync(currentUserId);
            if (userInDb == null)
            {
                _logger.LogError("User ID {UserId} không tìm thấy trong DB khi POST CaiDat.", currentUserId);
                return NotFound("Tài khoản không tồn tại.");
            }

             // Kiểm tra SĐT duy nhất nếu SĐT được thay đổi và không rỗng
             if (!string.IsNullOrEmpty(updatedUserFormData.Sdt) && updatedUserFormData.Sdt != userInDb.Sdt) {
                 if (await _context.NguoiDungs.AnyAsync(u => u.Sdt == updatedUserFormData.Sdt && u.Id != currentUserId)) {
                     ModelState.AddModelError(nameof(NguoiDung.Sdt), "Số điện thoại này đã được sử dụng bởi tài khoản khác.");
                 }
             }

             // Kiểm tra ModelState chỉ cho các thuộc tính đã Bind
            if (ModelState.IsValid)
            {
                 // Cập nhật các thuộc tính của đối tượng trong DB từ dữ liệu form hợp lệ
                 userInDb.HoTen = updatedUserFormData.HoTen;
                 userInDb.Sdt = string.IsNullOrWhiteSpace(updatedUserFormData.Sdt) ? null : updatedUserFormData.Sdt; // Cho phép rỗng/null
                 userInDb.GioiTinh = updatedUserFormData.GioiTinh;
                 userInDb.NgaySinh = updatedUserFormData.NgaySinh;
                 userInDb.DiaChiChiTiet = updatedUserFormData.DiaChiChiTiet;
                 userInDb.QuanHuyenId = updatedUserFormData.QuanHuyenId;
                 userInDb.ThanhPhoId = updatedUserFormData.ThanhPhoId;
                 userInDb.NgayCapNhat = DateTime.UtcNow; // Luôn cập nhật ngày giờ

                 try
                 {
                     // Lưu thay đổi vào DB (không cần gọi _context.Update nếu đối tượng được tracked)
                     await _context.SaveChangesAsync();
                     TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                     _logger.LogInformation("User {UserId} cập nhật cài đặt thành công.", currentUserId);

                     // Quan trọng: Nếu Họ Tên thay đổi, cần làm mới cookie để Layout hiển thị tên mới ngay lập tức
                     if (User.FindFirstValue(ClaimTypes.Name) != userInDb.HoTen)
                     {
                        await RefreshSignIn(currentUserId); // Gọi hàm làm mới cookie
                     }

                     // Quay lại trang cài đặt với thông báo thành công
                     return RedirectToAction(nameof(CaiDat));
                 }
                 catch (DbUpdateConcurrencyException ex) {
                      // Xử lý lỗi nếu có người khác sửa cùng lúc (hiếm gặp)
                      _logger.LogWarning(ex, "Lỗi concurrency khi cập nhật cài đặt user {UserId}", currentUserId);
                      ModelState.AddModelError("", "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng tải lại trang và thử lại.");
                      // Load lại giá trị mới nhất từ DB để hiển thị trên form lỗi
                      await ex.Entries.Single().ReloadAsync();
                       userInDb = (NguoiDung)ex.Entries.Single().Entity; // Gán lại userInDb với giá trị mới nhất từ DB
                 }
                 catch (Exception ex) // Bắt các lỗi DB khác
                 {
                     _logger.LogError(ex, "Lỗi khi cập nhật cài đặt user {UserId}", currentUserId);
                     ModelState.AddModelError("", "Không thể lưu thay đổi. Đã có lỗi xảy ra, vui lòng thử lại.");
                 }
            } else {
                 // Nếu ModelState không hợp lệ ngay từ đầu
                 _logger.LogWarning("Cập nhật cài đặt thất bại cho user {UserId} do ModelState không hợp lệ.", currentUserId);
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi DB, hiển thị lại form với lỗi
            // Cần load lại Dropdowns và truyền lại model userInDb (có thể đã được cập nhật từ DB nếu lỗi concurrency)
            await PopulateThanhPhoDropdownAsync(userInDb.ThanhPhoId);
            await PopulateQuanHuyenDropdownAsync(userInDb.ThanhPhoId, userInDb.QuanHuyenId);
            ViewBag.GioiTinhList = GetGioiTinhList();
            // Truyền userInDb vào View để hiển thị đúng dữ liệu hiện tại (có thể là giá trị cũ hoặc mới nhất từ DB) và các lỗi
            return View(userInDb);
        }

        // API Endpoint để lấy Quận/Huyện theo Thành phố (dùng cho JavaScript AJAX)
        [AllowAnonymous] // Cho phép truy cập không cần đăng nhập
        [HttpGet("api/diachi/quanhuyen/{thanhPhoId}")] // Định tuyến rõ ràng cho API
        public async Task<IActionResult> GetQuanHuyenByThanhPho(int thanhPhoId)
        {
             if (thanhPhoId <= 0) {
                 _logger.LogInformation("API GetQuanHuyenByThanhPho được gọi với thanhPhoId không hợp lệ: {ThanhPhoId}", thanhPhoId);
                 return BadRequest("ID Thành phố không hợp lệ."); // Trả về lỗi 400
             }

            _logger.LogDebug("API GetQuanHuyenByThanhPho được gọi cho ThanhPhoId: {ThanhPhoId}", thanhPhoId);
             // Lấy danh sách QH không cần tracking, chỉ lấy Id và Ten
             var quanHuyens = await _context.QuanHuyens
                                        .AsNoTracking()
                                        .Where(qh => qh.ThanhPhoId == thanhPhoId)
                                        .OrderBy(qh => qh.Ten)
                                        .Select(qh => new { qh.Id, qh.Ten })
                                        .ToListAsync();

            _logger.LogDebug("Tìm thấy {Count} Quận/Huyện cho ThanhPhoId: {ThanhPhoId}", quanHuyens.Count, thanhPhoId);
             return Ok(quanHuyens); // Trả về JSON danh sách Quận Huyện
        }
    }
}