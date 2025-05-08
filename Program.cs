// File: Program.cs

// --- USING STATEMENTS ---
using Microsoft.AspNetCore.DataProtection; // Cần cho Data Protection
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using HeThongTimViec.Data;                 // Namespace chứa DbContext
using HeThongTimViec.Models;                // Namespace chứa Models (nếu cần)
using System.IO;                             // Cần cho Path, DirectoryInfo
using System;                                // Cần cho Environment

// --- KHỞI TẠO WEB APPLICATION BUILDER ---
var builder = WebApplication.CreateBuilder(args);

// --- CẤU HÌNH DỊCH VỤ (SERVICES) ---

// 1. Lấy connection string từ cấu hình (appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Dừng ứng dụng nếu không có connection string, vì không thể kết nối DB
    throw new InvalidOperationException("Lỗi cấu hình: Không tìm thấy chuỗi kết nối 'DefaultConnection'.");
}

// 2. Đăng ký DbContext với MySQL Provider
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// 3. CẤU HÌNH DATA PROTECTION (QUAN TRỌNG ĐỂ SỬA LỖI CryptographicException)
// ---------------------------------------------------------------------------
// Phần này chỉ định nơi lưu trữ ổn định cho các khóa mã hóa
// được dùng để bảo vệ cookie Session và Authentication.

// Xác định thư mục lưu khóa trong thư mục home của người dùng trên macOS/Linux
string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
// Đường dẫn đầy đủ: /Users/your_username/.aspnet/DataProtection-Keys
string keysFolder = Path.Combine(userProfileFolder, ".aspnet", "DataProtection-Keys");

try
{
    // Đảm bảo thư mục lưu khóa tồn tại.
    // User chạy 'dotnet run' cần quyền ghi vào thư mục cha (~/.aspnet) để tạo thư mục con.
    Console.WriteLine($"[DataProtection] Đảm bảo thư mục lưu khóa tồn tại tại: {keysFolder}");
    Directory.CreateDirectory(keysFolder);
    Console.WriteLine("[DataProtection] Thư mục lưu khóa đã sẵn sàng.");
}
catch (Exception ex)
{
    // Log lỗi chi tiết nếu không thể tạo/truy cập thư mục khóa.
    Console.WriteLine($"!!! LỖI NGHIÊM TRỌNG !!! Không thể tạo hoặc truy cập thư mục lưu khóa Data Protection: {keysFolder}.");
    Console.WriteLine($"Lỗi chi tiết: {ex.Message}");
    Console.WriteLine("Kiểm tra quyền ghi của user vào thư mục cha hoặc đường dẫn có hợp lệ không.");
    // Dừng ứng dụng vì không thể hoạt động đúng nếu không lưu được khóa.
    throw new InvalidOperationException($"Không thể tạo/truy cập thư mục Data Protection keys tại {keysFolder}. Lỗi này sẽ ngăn Session và Authentication hoạt động đúng.", ex);
}

// Đăng ký và cấu hình dịch vụ Data Protection
builder.Services.AddDataProtection()
    // Chỉ định lưu khóa vào hệ thống tệp tại thư mục đã xác định.
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    // Đặt tên định danh duy nhất cho bộ khóa của ứng dụng này.
    // Quan trọng để cách ly khóa và cho phép chia sẻ giữa các instance (nếu có).
    // *** HÃY THAY BẰNG TÊN RIÊNG CỦA ỨNG DỤNG BẠN ***
    .SetApplicationName("HeThongTimViecCuaBan_v1");

// ---------------------------------------------------------------------------
// KẾT THÚC CẤU HÌNH DATA PROTECTION
// ---------------------------------------------------------------------------

// 4. Cấu hình Cookie Authentication (Xác thực bằng Cookie)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".AspNetCore.Cookies.HeThongTimViec"; // Đặt tên cookie cụ thể (tùy chọn)
        options.Cookie.HttpOnly = true; // Chống XSS: JavaScript không đọc được cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS nếu request là HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; // Giảm thiểu CSRF
        options.ExpireTimeSpan = TimeSpan.FromDays(14); // Thời gian hiệu lực cookie (cho Remember Me)
        options.LoginPath = "/TaiKhoan/DangNhap";     // Trang chuyển đến nếu chưa đăng nhập
        options.AccessDeniedPath = "/TaiKhoan/AccessDenied"; // Trang chuyển đến nếu không có quyền
        options.SlidingExpiration = true; // Gia hạn cookie nếu người dùng còn hoạt động
    });

// 5. Đăng ký các dịch vụ cần thiết cho MVC (Controllers, Views,...)
builder.Services.AddControllersWithViews();

// 6. Đăng ký IHttpContextAccessor (Cho phép truy cập HttpContext từ các dịch vụ khác)
builder.Services.AddHttpContextAccessor();

// 7. Đăng ký dịch vụ Session (Lưu dữ liệu tạm thời phía server)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian session hết hạn nếu không hoạt động
    options.Cookie.Name = ".AspNetCore.Session.HeThongTimViec"; // Đặt tên cookie session (tùy chọn)
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Đảm bảo session hoạt động (quan trọng cho GDPR và chức năng cốt lõi)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// --- BUILD ỨNG DỤNG ---
// Tạo ianstance WebApplication từ các dịch vụ đã cấu hình
var app = builder.Build();

// --- CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE) ---
// Thứ tự của các middleware trong pipeline là RẤT QUAN TRỌNG!

// Cấu hình cho môi trường Production (không phải Development)
if (!app.Environment.IsDevelopment())
{
    // Sử dụng trang xử lý lỗi thân thiện hơn cho người dùng cuối
    app.UseExceptionHandler("/Home/Error");
    // Bật HSTS (HTTP Strict Transport Security) nếu bạn luôn dùng HTTPS
    // Yêu cầu client luôn kết nối bằng HTTPS trong tương lai.
    // app.UseHsts();
}

// Tự động chuyển hướng các yêu cầu HTTP sang HTTPS
app.UseHttpsRedirection();

// Cho phép phục vụ các file tĩnh (như CSS, JavaScript, hình ảnh) từ thư mục wwwroot
app.UseStaticFiles();

// Kích hoạt hệ thống định tuyến (routing) của ASP.NET Core
app.UseRouting();

// *** Kích hoạt các middleware Session, Authentication, Authorization THEO ĐÚNG THỨ TỰ ***
app.UseSession();       // Khôi phục và lưu trữ dữ liệu session từ cookie
app.UseAuthentication(); // Xác định danh tính người dùng từ cookie authentication
app.UseAuthorization(); // Kiểm tra quyền truy cập của người dùng đã xác thực

// Thiết lập các điểm cuối (endpoints) cho routing, ánh xạ URL tới các Controller Action
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Route mặc định

// --- CHẠY ỨNG DỤNG ---
Console.WriteLine("Khởi động ứng dụng web...");
app.Run(); // Bắt đầu lắng nghe các yêu cầu HTTP