// File: Controllers/CaNhanController.cs

using HeThongTimViec.Data;
using HeThongTimViec.Models;
using HeThongTimViec.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http; // Cần cho StatusCodes
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic; // Cần cho List
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HeThongTimViec.Extensions;     // Cần cho .ToSelectList()
using Microsoft.Extensions.Logging; // Cần cho ILogger

namespace HeThongTimViec.Controllers
{
    [Authorize] // Toàn bộ controller yêu cầu đăng nhập
    public class CaNhanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CaNhanController> _logger; // Khai báo logger

        // Constants for file handling
        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedAvatarExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedCvExtensions = { ".pdf", ".doc", ".docx" };
        private const string AvatarFolderPath = "file/img/Avatar";
        private const string CvFolderPath = "file/CV";

        // Constructor đã sửa để inject và gán logger
        public CaNhanController(ApplicationDbContext context,
                                IWebHostEnvironment webHostEnvironment,
                                ILogger<CaNhanController> logger) // Inject ILogger
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger; // Gán logger
        }

        // --- TryGetUserId ---
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out userId))
            {
                _logger.LogWarning("Không thể parse User ID từ ClaimTypes.NameIdentifier: '{UserIdStr}'", userIdStr);
                return false;
            }
             _logger.LogDebug("Lấy được User ID: {UserId}", userId);
            return true;
        }

        // --- Index Action ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized("Không thể xác định người dùng.");

            _logger.LogInformation("Đang lấy thông tin trang Index cho User ID: {UserId}", userId);
            var nguoiDung = await _context.NguoiDungs
                .Include(nd => nd.ThanhPho)
                .Include(nd => nd.QuanHuyen)
                .Include(nd => nd.HoSoUngVien)
                .Include(nd => nd.LichRanhUngViens)
                .Include(nd => nd.DiaDiemMongMuons)
                    .ThenInclude(dd => dd.QuanHuyen)
                        .ThenInclude(qh => qh.ThanhPho)
                .FirstOrDefaultAsync(nd => nd.Id == userId && nd.LoaiTk == LoaiTaiKhoan.canhan);

            if (nguoiDung == null)
            {
                 _logger.LogWarning("Không tìm thấy NguoiDung loại 'canhan' với ID: {UserId} hoặc người dùng không tồn tại.", userId);
                 var userExists = await _context.NguoiDungs.AnyAsync(nd => nd.Id == userId);
                 if(userExists)
                 {
                     TempData["ErrorMessage"] = "Bạn đang ở vai trò Nhà tuyển dụng. Chuyển sang vai trò Ứng viên để xem trang này.";
                     return RedirectToAction("Index", "Home"); // Hoặc RedirectToAction("Index", "Dashboard");
                 }
                 return NotFound("Không tìm thấy hồ sơ ứng viên cho tài khoản của bạn.");
            }

            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];

            ViewData["Title"] = "Trang Cá Nhân";
            return View(nguoiDung);
        }

        #region Profile Edit (CV Upload)

        // --- EditProfile GET ---
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
             _logger.LogInformation("Đang lấy form EditProfile cho User ID: {UserId}", userId);
            var hoSo = await _context.HoSoUngViens.FirstOrDefaultAsync(h => h.NguoiDungId == userId);

            if (hoSo == null)
            {
                 _logger.LogInformation("Chưa có HoSoUngVien cho User ID: {UserId}. Đang tạo mới.", userId);
                hoSo = new HoSoUngVien { NguoiDungId = userId, TrangThaiTimViec = TrangThaiTimViec.dangtimtichcuc }; // Gán default
                _context.HoSoUngViens.Add(hoSo);
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Đã tạo HoSoUngVien mới cho User ID: {UserId}", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi khởi tạo HoSoUngVien cho User ID: {UserId}", userId);
                    TempData["ErrorMessage"] = "Lỗi khi khởi tạo hồ sơ.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Chuẩn bị ViewBag cho dropdown Enum
            ViewBag.LoaiLuongList = hoSo.LoaiLuongMongMuon.ToSelectList(true, "-- Chọn loại lương --");
            ViewBag.TrangThaiList = hoSo.TrangThaiTimViec.ToSelectList(false);

            ViewData["Title"] = "Chỉnh sửa Hồ sơ Ứng viên";
            return View(hoSo);
        }

        // --- EditProfile POST ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            int NguoiDungId,
            [Bind("NguoiDungId,TieuDeHoSo,GioiThieuBanThan,ViTriMongMuon,LoaiLuongMongMuon,MucLuongMongMuon,TrangThaiTimViec,ChoPhepTimKiem")] HoSoUngVien hoSoUngVien,
            IFormFile? cvFile)
        {
            if (!TryGetUserId(out var currentUserId) || NguoiDungId != currentUserId) { _logger.LogWarning("Forbid: User ID {CurrentUserId} cố gắng sửa hồ sơ của User ID {NguoiDungId}", currentUserId, NguoiDungId); return Forbid(); }
            if (NguoiDungId != hoSoUngVien.NguoiDungId) { _logger.LogWarning("BadRequest: NguoiDungId không khớp trong EditProfile POST."); return BadRequest("Dữ liệu không hợp lệ."); }

             _logger.LogInformation("Đang xử lý EditProfile POST cho User ID: {UserId}", NguoiDungId);
            var existingHoSo = await _context.HoSoUngViens.FindAsync(NguoiDungId);
            if (existingHoSo == null) { _logger.LogWarning("NotFound: Không tìm thấy HoSoUngVien ID: {HoSoId} để cập nhật.", NguoiDungId); return NotFound("Không tìm thấy hồ sơ."); }

            if (hoSoUngVien.LoaiLuongMongMuon == LoaiLuong.thoathuan || !hoSoUngVien.LoaiLuongMongMuon.HasValue)
            {
                hoSoUngVien.MucLuongMongMuon = null;
                ModelState.Remove("MucLuongMongMuon");
                 _logger.LogDebug("Reset MucLuongMongMuon về null cho User ID: {UserId}", NguoiDungId);
            }

            string? newCvUrl = null;
            bool cvUploadError = false;
            if (cvFile != null && cvFile.Length > 0)
            {
                 _logger.LogInformation("Đang xử lý tải lên CV cho User ID: {UserId}, Tên file: {FileName}, Kích thước: {FileSize}", NguoiDungId, cvFile.FileName, cvFile.Length);
                var validationResult = ValidateFile(cvFile, AllowedCvExtensions);
                if (validationResult == null)
                {
                    try
                    {
                         _logger.LogDebug("Đang xóa CV cũ (nếu có): {OldCvUrl}", existingHoSo.UrlCvMacDinh);
                        DeleteFile(existingHoSo.UrlCvMacDinh);
                        newCvUrl = await SaveFileAsync(cvFile, CvFolderPath, NguoiDungId);
                        _logger.LogInformation("Đã lưu CV mới thành công cho User ID: {UserId} tại đường dẫn: {NewCvUrl}", NguoiDungId, newCvUrl);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "Lỗi khi lưu file CV mới cho User ID: {UserId}", NguoiDungId);
                         ModelState.AddModelError("cvFile", "Lỗi khi tải lên CV. Vui lòng thử lại.");
                         cvUploadError = true;
                    }
                }
                else
                {
                     _logger.LogWarning("File CV không hợp lệ cho User ID: {UserId}. Lỗi: {ValidationError}", NguoiDungId, validationResult);
                     ModelState.AddModelError("cvFile", validationResult);
                     cvUploadError = true;
                }
            }

            _context.Entry(existingHoSo).CurrentValues.SetValues(hoSoUngVien);
            if (newCvUrl != null)
            {
                existingHoSo.UrlCvMacDinh = newCvUrl;
            }

            if (ModelState.IsValid && !cvUploadError)
            {
                try
                {
                    await _context.SaveChangesAsync();
                     _logger.LogInformation("Cập nhật HoSoUngVien ID: {HoSoId} thành công.", NguoiDungId);
                    TempData["SuccessMessage"] = "Cập nhật hồ sơ ứng viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                     _logger.LogWarning(ex, "Lỗi DbUpdateConcurrencyException khi cập nhật HoSoUngVien ID: {HoSoId}", NguoiDungId);
                     ModelState.AddModelError("", "Hồ sơ này đã được cập nhật bởi một người khác. Vui lòng tải lại trang và thử lại.");
                }
                catch (Exception ex)
                {
                     _logger.LogError(ex, "Lỗi không xác định khi cập nhật HoSoUngVien ID: {HoSoId}", NguoiDungId);
                     ModelState.AddModelError("", "Đã xảy ra lỗi hệ thống khi lưu hồ sơ.");
                }
            }

             _logger.LogWarning("ModelState không hợp lệ hoặc có lỗi tải CV khi EditProfile POST cho User ID: {UserId}. Quay lại View.", NguoiDungId);
            ViewBag.LoaiLuongList = existingHoSo.LoaiLuongMongMuon.ToSelectList(true, "-- Chọn loại lương --");
            ViewBag.TrangThaiList = existingHoSo.TrangThaiTimViec.ToSelectList(false);
            ViewData["Title"] = "Chỉnh sửa Hồ sơ Ứng viên";
            return View(existingHoSo);
        }

        #endregion

        #region Account Edit (Avatar Upload)

        // --- EditAccount GET ---
        [HttpGet]
        public async Task<IActionResult> EditAccount()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            _logger.LogInformation("Đang lấy form EditAccount cho User ID: {UserId}", userId);
            var nguoiDung = await _context.NguoiDungs.FindAsync(userId);
            if (nguoiDung == null || nguoiDung.LoaiTk != LoaiTaiKhoan.canhan) { _logger.LogWarning("NotFound: Không tìm thấy NguoiDung ID: {UserId} hoặc không phải 'canhan'.", userId); return NotFound("Tài khoản không tồn tại hoặc không hợp lệ."); }

            await PopulateLocationDropdowns(nguoiDung.ThanhPhoId, nguoiDung.QuanHuyenId);
            ViewBag.GioiTinhList = nguoiDung.GioiTinh.ToSelectList(true, "-- Chọn giới tính --");

            ViewData["Title"] = "Chỉnh sửa Thông tin Tài khoản";
            return View(nguoiDung);
        }

        // --- EditAccount POST ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(
            int Id,
            [Bind("Id,HoTen,Sdt,GioiTinh,NgaySinh,DiaChiChiTiet,QuanHuyenId,ThanhPhoId")] NguoiDung nguoiDungForm,
            IFormFile? avatarFile)
        {
             if (!TryGetUserId(out var currentUserId) || Id != currentUserId) { _logger.LogWarning("Forbid: User ID {CurrentUserId} cố gắng sửa tài khoản User ID {Id}", currentUserId, Id); return Forbid(); }
             if (Id != nguoiDungForm.Id) { _logger.LogWarning("BadRequest: ID không khớp trong EditAccount POST."); return BadRequest("ID không khớp."); }

            _logger.LogInformation("Đang xử lý EditAccount POST cho User ID: {UserId}", Id);
            var userToUpdate = await _context.NguoiDungs.FindAsync(Id);
            if (userToUpdate == null || userToUpdate.LoaiTk != LoaiTaiKhoan.canhan) { _logger.LogWarning("NotFound: Không tìm thấy NguoiDung ID: {UserId} để cập nhật.", Id); return NotFound("Tài khoản không tồn tại."); }

            string? newAvatarUrl = null;
            bool avatarUploadError = false;
            if (avatarFile != null && avatarFile.Length > 0)
            {
                _logger.LogInformation("Đang xử lý tải lên Avatar cho User ID: {UserId}, Tên file: {FileName}, Kích thước: {FileSize}", Id, avatarFile.FileName, avatarFile.Length);
                var validationResult = ValidateFile(avatarFile, AllowedAvatarExtensions);
                if (validationResult == null)
                {
                    try
                    {
                         _logger.LogDebug("Đang xóa Avatar cũ (nếu có): {OldAvatarUrl}", userToUpdate.UrlAvatar);
                        DeleteFile(userToUpdate.UrlAvatar);
                        newAvatarUrl = await SaveFileAsync(avatarFile, AvatarFolderPath, Id);
                         _logger.LogInformation("Đã lưu Avatar mới thành công cho User ID: {UserId} tại đường dẫn: {NewAvatarUrl}", Id, newAvatarUrl);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogError(ex, "Lỗi khi lưu file Avatar mới cho User ID: {UserId}", Id);
                         ModelState.AddModelError("avatarFile", "Lỗi khi tải lên ảnh đại diện.");
                         avatarUploadError = true;
                    }
                }
                else
                {
                     _logger.LogWarning("File Avatar không hợp lệ cho User ID: {UserId}. Lỗi: {ValidationError}", Id, validationResult);
                     ModelState.AddModelError("avatarFile", validationResult);
                     avatarUploadError = true;
                }
            }

            userToUpdate.HoTen = nguoiDungForm.HoTen;
            userToUpdate.Sdt = nguoiDungForm.Sdt;
            userToUpdate.GioiTinh = nguoiDungForm.GioiTinh;
            userToUpdate.NgaySinh = nguoiDungForm.NgaySinh;
            userToUpdate.DiaChiChiTiet = nguoiDungForm.DiaChiChiTiet;
            userToUpdate.QuanHuyenId = nguoiDungForm.QuanHuyenId;
            userToUpdate.ThanhPhoId = nguoiDungForm.ThanhPhoId;
            userToUpdate.NgayCapNhat = DateTime.UtcNow;
            if (newAvatarUrl != null) { userToUpdate.UrlAvatar = newAvatarUrl; }

            ModelState.Clear();
            TryValidateModel(userToUpdate);

            if (ModelState.IsValid && !avatarUploadError)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cập nhật NguoiDung ID: {UserId} thành công.",Id);
                    TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                 catch (DbUpdateException ex)
                 {
                    _logger.LogWarning(ex, "Lỗi DbUpdateException khi cập nhật NguoiDung ID: {UserId}", Id);
                     if (ex.InnerException?.Message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) == true && (ex.InnerException.Message.Contains("Sdt", StringComparison.OrdinalIgnoreCase) || ex.InnerException.Message.Contains("'UQ_NguoiDung_Sdt'")))
                     { ModelState.AddModelError("Sdt", "Số điện thoại này đã được đăng ký."); }
                     else { ModelState.AddModelError("", "Lỗi cơ sở dữ liệu khi lưu tài khoản."); }
                 }
                 catch (Exception ex)
                 {
                      _logger.LogError(ex, "Lỗi không xác định khi cập nhật NguoiDung ID: {UserId}", Id);
                      ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.");
                 }
            }

             _logger.LogWarning("ModelState không hợp lệ hoặc có lỗi tải Avatar khi EditAccount POST cho User ID: {UserId}. Quay lại View.", Id);
            await PopulateLocationDropdowns(userToUpdate.ThanhPhoId, userToUpdate.QuanHuyenId);
            ViewBag.GioiTinhList = userToUpdate.GioiTinh.ToSelectList(true, "-- Chọn giới tính --");

            ViewData["Title"] = "Chỉnh sửa Thông tin Tài khoản";
            return View(userToUpdate);
        }

        #endregion

        #region Schedule Edit

        // --- EditSchedule GET ---
        [HttpGet]
        public async Task<IActionResult> EditSchedule()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
             _logger.LogInformation("Đang lấy form EditSchedule cho User ID: {UserId}", userId);
            var currentSchedule = await _context.LichRanhUngViens
                                         .AsNoTracking()
                                         .Where(l => l.NguoiDungId == userId)
                                         .ToListAsync();
            ViewData["Title"] = "Cập nhật Lịch làm việc mong muốn";
            return View(currentSchedule ?? new List<LichRanhUngVien>());
        }

        // --- EditSchedule POST ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSchedule(List<ScheduleEditItem> lichRanhItems)
        {
             if (!TryGetUserId(out var userId)) return Unauthorized();
              _logger.LogInformation("Đang xử lý EditSchedule POST cho User ID: {UserId}", userId);

            var selectedSlots = lichRanhItems?
                .Where(item => item != null && item.IsSelected && item.NguoiDungId == userId)
                .Select(item => new { item.NgayTrongTuan, item.BuoiLamViec })
                .Distinct()
                .Select(item => new LichRanhUngVien { NguoiDungId = userId, NgayTrongTuan = item.NgayTrongTuan, BuoiLamViec = item.BuoiLamViec })
                .ToList() ?? new List<LichRanhUngVien>();

             _logger.LogDebug("Số lượng slot hợp lệ được chọn từ form: {Count}", selectedSlots.Count);

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var currentDbSchedule = await _context.LichRanhUngViens.Where(l => l.NguoiDungId == userId).ToListAsync();
                    if(currentDbSchedule.Any())
                    {
                         _logger.LogInformation("Đang xóa {Count} lịch rảnh cũ của User ID: {UserId}", currentDbSchedule.Count, userId);
                        _context.LichRanhUngViens.RemoveRange(currentDbSchedule);
                        await _context.SaveChangesAsync();
                    } else {
                         _logger.LogInformation("Không có lịch rảnh cũ nào để xóa cho User ID: {UserId}", userId);
                    }

                    if (selectedSlots.Any())
                    {
                         _logger.LogInformation("Đang thêm {Count} lịch rảnh mới cho User ID: {UserId}", selectedSlots.Count, userId);
                        await _context.LichRanhUngViens.AddRangeAsync(selectedSlots);
                        await _context.SaveChangesAsync();
                    } else {
                         _logger.LogInformation("Không có lịch rảnh mới nào được chọn để thêm cho User ID: {UserId}", userId);
                    }

                    await transaction.CommitAsync();
                     _logger.LogInformation("Cập nhật LichRanhUngVien thành công cho User ID: {UserId}. Transaction committed.", userId);
                    TempData["SuccessMessage"] = "Cập nhật lịch làm việc thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi cập nhật LichRanhUngVien cho User ID: {UserId}. Transaction rolled back.", userId);
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật lịch làm việc.";
                    var currentScheduleForView = await _context.LichRanhUngViens.AsNoTracking().Where(l => l.NguoiDungId == userId).ToListAsync();
                    ModelState.AddModelError("", "Lỗi lưu dữ liệu. Vui lòng thử lại.");
                    ViewData["Title"] = "Cập nhật Lịch làm việc mong muốn";
                    return View(currentScheduleForView ?? new List<LichRanhUngVien>());
                }
            }
        }
        #endregion

        #region Locations Edit

         // --- Action GET EditLocations ---
         [HttpGet]
        public async Task<IActionResult> EditLocations()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
             _logger.LogInformation("Đang lấy form EditLocations cho User ID: {UserId}", userId);
            var currentLocationsIds = await _context.DiaDiemMongMuons
                                            .AsNoTracking()
                                            .Where(d => d.NguoiDungId == userId)
                                            .Select(d => d.QuanHuyenId)
                                            .ToListAsync();
            await PopulateLocationDropdowns(null, null);
            ViewData["Title"] = "Cập nhật Khu vực làm việc mong muốn";
            ViewBag.SelectedQuanHuyenIds = currentLocationsIds ?? new List<int>();
            return View();
        }

        // --- Action POST EditLocations ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocations(int[] selectedQuanHuyenIds)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized(new { message = "Người dùng không xác định."});
             _logger.LogInformation("Bắt đầu xử lý EditLocations POST cho User ID: {UserId}", userId);

            var submittedIds = selectedQuanHuyenIds?.Distinct().ToList() ?? new List<int>();
             _logger.LogDebug("Các ID Quận/Huyện được gửi lên: [{SubmittedIds}]", string.Join(", ", submittedIds));

            var validSubmittedDetails = await _context.QuanHuyens
                                                .AsNoTracking()
                                                .Where(qh => submittedIds.Contains(qh.Id))
                                                .Select(qh => new { QuanHuyenId = qh.Id, ThanhPhoId = qh.ThanhPhoId })
                                                .ToListAsync();
            var validSubmittedIds = validSubmittedDetails.Select(d => d.QuanHuyenId).ToList();
             _logger.LogDebug("Các ID Quận/Huyện hợp lệ: [{ValidSubmittedIds}]", string.Join(", ", validSubmittedIds));

              using (var transaction = await _context.Database.BeginTransactionAsync())
              {
                 try
                 {
                     var currentDbLocations = await _context.DiaDiemMongMuons
                                                      .Where(d => d.NguoiDungId == userId)
                                                      .ToListAsync();
                      _logger.LogDebug("Số lượng địa điểm mong muốn hiện tại: {Count}", currentDbLocations.Count);

                     var itemsToRemove = currentDbLocations
                         .Where(dbItem => !validSubmittedIds.Contains(dbItem.QuanHuyenId))
                         .ToList();
                       _logger.LogDebug("Số lượng địa điểm cần xóa: {Count}", itemsToRemove.Count);

                     var detailsToAdd = validSubmittedDetails
                         .Where(detail => !currentDbLocations.Any(db => db.QuanHuyenId == detail.QuanHuyenId))
                         .ToList();
                     _logger.LogDebug("Số lượng địa điểm cần thêm: {Count}", detailsToAdd.Count);
                     var itemsToAdd = detailsToAdd.Select(detail => new DiaDiemMongMuon
                         { NguoiDungId = userId, QuanHuyenId = detail.QuanHuyenId, ThanhPhoId = detail.ThanhPhoId }).ToList();

                     if (itemsToRemove.Any())
                     { _logger.LogInformation("Đang xóa {Count} địa điểm.", itemsToRemove.Count); _context.DiaDiemMongMuons.RemoveRange(itemsToRemove); }
                     if (itemsToAdd.Any())
                     { _logger.LogInformation("Đang thêm {Count} địa điểm.", itemsToAdd.Count); await _context.DiaDiemMongMuons.AddRangeAsync(itemsToAdd); }

                     if (itemsToRemove.Any() || itemsToAdd.Any())
                     { _logger.LogInformation("Đang lưu thay đổi DiaDiemMongMuon..."); await _context.SaveChangesAsync(); _logger.LogInformation("Lưu thành công."); }
                     else { _logger.LogInformation("Không có thay đổi DiaDiemMongMuon."); }

                     await transaction.CommitAsync();
                      _logger.LogInformation("Transaction cập nhật DiaDiemMongMuon đã commit thành công cho User ID: {UserId}.", userId);
                     TempData["SuccessMessage"] = "Cập nhật khu vực mong muốn thành công!";
                     return RedirectToAction(nameof(Index));
                 }
                 catch (Exception ex)
                 {
                      await transaction.RollbackAsync();
                      _logger.LogError(ex, "Lỗi khi cập nhật DiaDiemMongMuon cho User ID: {UserId}. Transaction rolled back.", userId);
                      TempData["ErrorMessage"] = "Đã xảy ra lỗi khi cập nhật khu vực mong muốn.";
                      await PopulateLocationDropdowns(null, null);
                      ViewBag.SelectedQuanHuyenIds = validSubmittedIds;
                      ModelState.AddModelError("", "Lỗi lưu dữ liệu. Vui lòng thử lại.");
                      ViewData["Title"] = "Cập nhật Khu vực làm việc mong muốn";
                      return View();
                 }
              }
        }

        #endregion

        #region Helper Methods

        // --- PopulateLocationDropdowns ---
        private async Task PopulateLocationDropdowns(int? selectedThanhPhoId, int? selectedQuanHuyenId)
        {
            _logger.LogDebug("Bắt đầu PopulateLocationDropdowns với selectedThanhPhoId: {ThanhPhoId}, selectedQuanHuyenId: {QuanHuyenId}", selectedThanhPhoId, selectedQuanHuyenId);
            var thanhPhos = await _context.ThanhPhos.AsNoTracking().OrderBy(tp => tp.Ten).ToListAsync();
            ViewBag.ThanhPhoId = new SelectList(thanhPhos, "Id", "Ten", selectedThanhPhoId);

            IEnumerable<QuanHuyen> quanHuyens = new List<QuanHuyen>();
            if (selectedThanhPhoId.HasValue && selectedThanhPhoId.Value > 0)
            {
                quanHuyens = await _context.QuanHuyens
                                         .AsNoTracking()
                                         .Where(qh => qh.ThanhPhoId == selectedThanhPhoId.Value)
                                         .OrderBy(qh => qh.Ten)
                                         .ToListAsync();
                 _logger.LogDebug("Đã lấy {Count} Quận/Huyện cho ThanhPhoId: {ThanhPhoId}", quanHuyens.Count(), selectedThanhPhoId.Value);
            } else {
                 _logger.LogDebug("Không có ThanhPhoId hợp lệ để lấy Quận/Huyện ban đầu.");
            }
            ViewBag.QuanHuyenId = new SelectList(quanHuyens, "Id", "Ten", selectedQuanHuyenId);
             _logger.LogDebug("Kết thúc PopulateLocationDropdowns. ViewBag.ThanhPhoId có {TPCount} items. ViewBag.QuanHuyenId có {QHCount} items.", thanhPhos.Count, quanHuyens.Count());
        }


        // --- GetQuanHuyenByThanhPho (Action cho AJAX - ĐÃ KHÔI PHỤC) ---
        [HttpGet] // Đảm bảo có attribute này
        public async Task<IActionResult> GetQuanHuyenByThanhPho(int? thanhPhoId)
        {
            // Kiểm tra đăng nhập cơ bản (dù controller đã có Authorize)
            if (User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Unauthorized access attempt to GetQuanHuyenByThanhPho.");
                return Unauthorized(new { message = "Yêu cầu xác thực." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("CaNhanController.GetQuanHuyenByThanhPho called with thanhPhoId: {ThanhPhoId} by User: {UserId}", thanhPhoId ?? 0, currentUserId);

            // Trả về danh sách rỗng nếu không có ID hoặc ID không hợp lệ
            if (!thanhPhoId.HasValue || thanhPhoId.Value <= 0)
            {
                _logger.LogDebug("ThanhPhoId không hợp lệ hoặc null, trả về danh sách rỗng.");
                return Ok(new List<object>()); // Trả về list rỗng
            }

            // Truy vấn Database
            try
            {
                var quanHuyens = await _context.QuanHuyens
                    .AsNoTracking()
                    .Where(qh => qh.ThanhPhoId == thanhPhoId.Value)
                    .OrderBy(qh => qh.Ten)
                    // Chỉ chọn id và ten như JavaScript đang mong đợi
                    .Select(qh => new { id = qh.Id, ten = qh.Ten })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} QuanHuyen for thanhPhoId: {ThanhPhoId}", quanHuyens.Count, thanhPhoId.Value);
                return Ok(quanHuyens); // Trả về JSON thành công
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while getting QuanHuyen for thanhPhoId: {ThanhPhoId} in CaNhanController", thanhPhoId.Value);
                // Trả về lỗi 500 Internal Server Error kèm thông báo
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi máy chủ khi lấy dữ liệu Quận/Huyện." });
            }
        }


        // --- File Handling Helpers ---
        private string? ValidateFile(IFormFile file, string[] allowedExtensions)
        {
             _logger.LogDebug("Validating file: {FileName}, Size: {FileSize}", file?.FileName, file?.Length);
            if (file == null || file.Length == 0) { _logger.LogWarning("Validation failed: File is null or empty."); return "Không có tệp nào được chọn."; }
            if (file.Length > MaxFileSize) { _logger.LogWarning("Validation failed: File size {FileSize} exceeds limit {MaxFileSize}.", file.Length, MaxFileSize); return $"Kích thước tệp không được vượt quá {MaxFileSize / 1024 / 1024} MB."; }
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            { _logger.LogWarning("Validation failed: Invalid extension '{FileExtension}'. Allowed: [{AllowedExtensions}]", fileExtension, string.Join(", ", allowedExtensions)); return $"Loại tệp không hợp lệ. Chỉ chấp nhận: {string.Join(", ", allowedExtensions)}"; }
             _logger.LogDebug("File validation successful for: {FileName}", file.FileName);
            return null; // Hợp lệ
        }

        private async Task<string> SaveFileAsync(IFormFile file, string targetFolderRelativePath, int userId)
        {
             if (file == null || file.Length == 0) throw new ArgumentException("File cannot be null or empty.", nameof(file));
              _logger.LogInformation("Saving file {FileName} for User ID: {UserId} to folder: {TargetFolder}", file.FileName, userId, targetFolderRelativePath);
            string webRootPath = _webHostEnvironment.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath)) { throw new InvalidOperationException("WebRootPath is not configured."); }

            string targetFolderPath = Path.Combine(webRootPath, targetFolderRelativePath.Replace('/', Path.DirectorySeparatorChar));
            _logger.LogDebug("Target folder path: {FolderPath}", targetFolderPath);
            Directory.CreateDirectory(targetFolderPath);

            string originalFileName = Path.GetFileName(file.FileName);
            string fileExtension = Path.GetExtension(originalFileName);
            string uniqueFileName = $"{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{fileExtension}";
            string absoluteFilePath = Path.Combine(targetFolderPath, uniqueFileName);
            _logger.LogDebug("Absolute file path to save: {FilePath}", absoluteFilePath);

            try
            {
                 using (var fileStream = new FileStream(absoluteFilePath, FileMode.Create))
                 {
                     await file.CopyToAsync(fileStream);
                 }
                  _logger.LogInformation("File saved successfully: {FilePath}", absoluteFilePath);
                 string relativeUrl = $"/{targetFolderRelativePath.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
                 return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName} to {FilePath}", originalFileName, absoluteFilePath);
                throw;
            }
        }

        private void DeleteFile(string? relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl)) { _logger.LogDebug("DeleteFile called with null or empty URL. No action taken."); return; }
             _logger.LogInformation("Attempting to delete file with relative URL: {RelativeUrl}", relativeUrl);
            try
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath)) { _logger.LogWarning("WebRootPath is not configured. Cannot delete file."); return; }

                string pathToDelete = relativeUrl.TrimStart('/');
                pathToDelete = pathToDelete.Replace('/', Path.DirectorySeparatorChar);
                string absolutePath = Path.Combine(webRootPath, pathToDelete);

                _logger.LogDebug("Absolute path to delete: {AbsolutePath}", absolutePath);
                if (System.IO.File.Exists(absolutePath))
                {
                    System.IO.File.Delete(absolutePath);
                     _logger.LogInformation("File deleted successfully: {AbsolutePath}", absolutePath);
                } else {
                     _logger.LogWarning("File not found for deletion: {AbsolutePath}", absolutePath);
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting file: {RelativeUrl}", relativeUrl);
            }
        }

        #endregion

    } // End Class CaNhanController

    // --- Helper class đặt BÊN NGOÀI CaNhanController ---
    public static class CaNhanControllerHelper
    {
        public static string GetNgayTrongTuanDisplay(NgayTrongTuan ngay) => ngay switch
        {
            NgayTrongTuan.thu2 => "Thứ Hai", NgayTrongTuan.thu3 => "Thứ Ba", NgayTrongTuan.thu4 => "Thứ Tư",
            NgayTrongTuan.thu5 => "Thứ Năm", NgayTrongTuan.thu6 => "Thứ Sáu", NgayTrongTuan.thu7 => "Thứ Bảy",
            NgayTrongTuan.chunhat => "Chủ Nhật", NgayTrongTuan.ngaylinhhoat => "Linh Hoạt", _ => ngay.ToString()
        };
        public static string GetBuoiLamViecDisplay(BuoiLamViec buoi) => buoi switch
        {
            BuoiLamViec.sang => "Sáng", BuoiLamViec.chieu => "Chiều", BuoiLamViec.toi => "Tối",
            BuoiLamViec.cangay => "Cả Ngày", BuoiLamViec.linhhoat => "Linh Hoạt", _ => buoi.ToString()
        };
    }

} // End Namespace