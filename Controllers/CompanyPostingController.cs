using HeThongTimViec.Data;
using HeThongTimViec.Models;
using HeThongTimViec.ViewModels.JobPosting; // Đảm bảo có using ViewModel nếu cần
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HeThongTimViec.Controllers
{
    /// <summary>
    /// Controller quản lý CRUD tin tuyển dụng cho Nhà tuyển dụng Doanh nghiệp.
    /// Yêu cầu quyền 'doanhnghiep'.
    /// Tin đăng bởi NTD Doanh nghiệp sẽ ở trạng thái 'choduyet' ban đầu. Cho phép ẩn/hiện tin đã duyệt.
    /// </summary>
    [Authorize(Roles = nameof(LoaiTaiKhoan.doanhnghiep))] // Chỉ role 'doanhnghiep'
    public class CompanyPostingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CompanyPostingController> _logger;

        public CompanyPostingController(ApplicationDbContext context, ILogger<CompanyPostingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- Hàm Helper ---

        // Lấy UserId từ Claims
        private bool TryGetUserId(out int userId)
        {
             userId = 0;
             var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
             if (int.TryParse(userIdString, out userId)) { return true; }
             _logger.LogError("Không thể parse UserId từ claims cho NTD Doanh nghiệp.");
             // Cân nhắc trả về lỗi hoặc redirect ở đây nếu không lấy được UserId
             // Ví dụ: throw new InvalidOperationException("Không thể xác thực người dùng.");
             return false;
        }

        // Chuẩn bị ViewBag cho các Dropdown List
        private async Task PrepareDropdownsAsync(int? selectedThanhPhoId = null, int? selectedQuanHuyenId = null) {
             ViewBag.NganhNgheList = await _context.NganhNghes.AsNoTracking().OrderBy(n => n.Ten)
                .Select(n => new SelectListItem { Value = n.Id.ToString(), Text = n.Ten }).ToListAsync();
             ViewBag.ThanhPhoList = new SelectList(await _context.ThanhPhos.AsNoTracking().OrderBy(tp => tp.Ten).ToListAsync(),
                                                   "Id", "Ten", selectedThanhPhoId);
             if (selectedThanhPhoId.HasValue && selectedThanhPhoId > 0) {
                 ViewBag.QuanHuyenList = new SelectList(await _context.QuanHuyens.AsNoTracking()
                    .Where(qh => qh.ThanhPhoId == selectedThanhPhoId).OrderBy(qh => qh.Ten).ToListAsync(),
                                                         "Id", "Ten", selectedQuanHuyenId);
             } else { ViewBag.QuanHuyenList = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Ten"); }
             ViewBag.LoaiHinhCongViecList = new SelectList(Enum.GetValues(typeof(LoaiHinhCongViec)).Cast<LoaiHinhCongViec>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text");
             ViewBag.LoaiLuongList = new SelectList(Enum.GetValues(typeof(LoaiLuong)).Cast<LoaiLuong>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text");
             ViewBag.NgayTrongTuanList = new SelectList(Enum.GetValues(typeof(NgayTrongTuan)).Cast<NgayTrongTuan>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text");
             ViewBag.BuoiLamViecList = new SelectList(Enum.GetValues(typeof(BuoiLamViec)).Cast<BuoiLamViec>().Select(e => new SelectListItem { Value = e.ToString(), Text = GetEnumDisplayName(e) }), "Value", "Text");
        }

        // Hàm helper lấy tên hiển thị cho Enum
        private string GetEnumDisplayName(Enum enumValue) {
            // Có thể thêm logic phức tạp hơn để lấy Display Name từ Attribute
            // Ví dụ tạm thời:
             switch (enumValue) {
                 case LoaiHinhCongViec.banthoigian: return "Bán thời gian"; case LoaiHinhCongViec.thoivu: return "Thời vụ"; case LoaiHinhCongViec.linhhoatkhac: return "Linh hoạt khác";
                 case LoaiLuong.theogio: return "Theo giờ"; case LoaiLuong.theongay: return "Theo ngày"; case LoaiLuong.theoca: return "Theo ca"; case LoaiLuong.theothang: return "Theo tháng"; case LoaiLuong.thoathuan: return "Thỏa thuận"; case LoaiLuong.theoduan: return "Theo dự án";
                 case NgayTrongTuan.thu2: return "Thứ 2"; case NgayTrongTuan.thu3: return "Thứ 3"; case NgayTrongTuan.thu4: return "Thứ 4"; case NgayTrongTuan.thu5: return "Thứ 5"; case NgayTrongTuan.thu6: return "Thứ 6"; case NgayTrongTuan.thu7: return "Thứ 7"; case NgayTrongTuan.chunhat: return "Chủ Nhật"; case NgayTrongTuan.ngaylinhhoat: return "Ngày linh hoạt";
                 case BuoiLamViec.sang: return "Buổi Sáng"; case BuoiLamViec.chieu: return "Buổi Chiều"; case BuoiLamViec.toi: return "Buổi Tối"; case BuoiLamViec.cangay: return "Cả ngày"; case BuoiLamViec.linhhoat: return "Buổi linh hoạt";
                 default: return enumValue.ToString();
             }
        }

        // --- CRUD Actions ---

        // GET: /CompanyPosting
        public async Task<IActionResult> Index()
        {
            if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");

            var postings = await _context.TinTuyenDungs
                                     .Where(t => t.NguoiDangId == userId && t.TrangThai != TrangThaiTinTuyenDung.daxoa)
                                     .OrderByDescending(t => t.NgayCapNhat)
                                     .Select(t => new JobPostingListViewModel { // Sử dụng ViewModel chung nếu phù hợp
                                         Id = t.Id, TieuDe = t.TieuDe, TrangThai = t.TrangThai, NgayDang = t.NgayDang,
                                         NgayHetHan = t.NgayHetHan, SoUngVien = t.UngTuyens.Count })
                                     .AsNoTracking()
                                     .ToListAsync();
            return View(postings); // Views/CompanyPosting/Index.cshtml
        }

        // GET: /CompanyPosting/Create
        public async Task<IActionResult> Create()
        {
            if (!TryGetUserId(out _)) return RedirectToAction("AccessDenied", "TaiKhoan");
            await PrepareDropdownsAsync();
            var viewModel = new CompanyPostingViewModel(); // Đảm bảo dùng đúng ViewModel
            viewModel.LichLamViecItems.Add(new LichLamViecViewModel());
            return View(viewModel); // Views/CompanyPosting/Create.cshtml
        }

        // POST: /CompanyPosting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyPostingViewModel viewModel)
        {
            if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");
             viewModel.LichLamViecItems.RemoveAll(l => l.NgayTrongTuan == 0 && l.GioBatDau == null && l.GioKetThuc == null && l.BuoiLamViec == null);
              if (viewModel.ThanhPhoId <= 0) { ModelState.Remove(nameof(viewModel.QuanHuyenId)); }
             if (viewModel.NgayHetHan.HasValue && viewModel.NgayHetHan.Value.Date < DateTime.UtcNow.Date) { ModelState.AddModelError(nameof(viewModel.NgayHetHan), "Ngày hết hạn không được là ngày trong quá khứ."); }

            if (ModelState.IsValid)
            {
                var newPosting = new TinTuyenDung {
                    NguoiDangId = userId, TieuDe = viewModel.TieuDe, MoTa = viewModel.MoTa, YeuCau = viewModel.YeuCau, QuyenLoi = viewModel.QuyenLoi,
                    LoaiHinhCongViec = viewModel.LoaiHinhCongViec, LoaiLuong = viewModel.LoaiLuong, LuongToiThieu = viewModel.LuongToiThieu, LuongToiDa = viewModel.LuongToiDa,
                    DiaChiLamViec = viewModel.DiaChiLamViec, ThanhPhoId = viewModel.ThanhPhoId, QuanHuyenId = viewModel.QuanHuyenId,
                    YeuCauKinhNghiemText = viewModel.YeuCauKinhNghiemText ?? "Không yêu cầu", YeuCauHocVanText = viewModel.YeuCauHocVanText ?? "Không yêu cầu",
                    SoLuongTuyen = viewModel.SoLuongTuyen, TinGap = viewModel.TinGap, NgayHetHan = viewModel.NgayHetHan,
                    TrangThai = TrangThaiTinTuyenDung.choduyet, // NTD Doanh nghiệp -> Chờ duyệt
                    NgayDang = DateTime.UtcNow, NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
                 };

                if (viewModel.SelectedNganhNgheIds?.Any() ?? false) { foreach (var nnId in viewModel.SelectedNganhNgheIds) { if (await _context.NganhNghes.AnyAsync(n => n.Id == nnId)) { newPosting.TinTuyenDungNganhNghes.Add(new TinTuyenDung_NganhNghe { NganhNgheId = nnId }); } } }
                if (viewModel.LichLamViecItems?.Any() ?? false) { foreach (var lichVM in viewModel.LichLamViecItems) { if(lichVM.NgayTrongTuan != 0 || lichVM.BuoiLamViec.HasValue || lichVM.GioBatDau.HasValue || lichVM.GioKetThuc.HasValue) { newPosting.LichLamViecCongViecs.Add(new LichLamViecCongViec { NgayTrongTuan = lichVM.NgayTrongTuan, GioBatDau = lichVM.GioBatDau, GioKetThuc = lichVM.GioKetThuc, BuoiLamViec = lichVM.BuoiLamViec }); } } }

                try {
                    _context.TinTuyenDungs.Add(newPosting); await _context.SaveChangesAsync();
                    _logger.LogInformation("NTD Doanh nghiệp (User ID: {UserId}) đã tạo Tin Tuyển Dụng mới (ID: {PostingId}) - Chờ duyệt.", userId, newPosting.Id);
                    TempData["SuccessMessage"] = "Đăng tin mới thành công! Tin đang chờ duyệt.";
                    return RedirectToAction(nameof(Index));
                } catch (DbUpdateException ex) {
                    _logger.LogError(ex, "Lỗi DB khi NTD Doanh nghiệp (User ID: {UserId}) tạo tin.", userId); ModelState.AddModelError("", "Lỗi cơ sở dữ liệu khi lưu tin.");
                } catch (Exception ex) {
                    _logger.LogError(ex, "Lỗi không xác định khi NTD Doanh nghiệp (User ID: {UserId}) tạo tin.", userId); ModelState.AddModelError("", "Đã có lỗi xảy ra khi lưu tin.");
                }
            } else {
                 _logger.LogWarning("ModelState không hợp lệ khi NTD Doanh nghiệp (User ID: {UserId}) tạo tin. Lỗi: {Errors}", userId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            await PrepareDropdownsAsync(viewModel.ThanhPhoId, viewModel.QuanHuyenId);
            if (!(viewModel.LichLamViecItems?.Any() ?? false)) {
                if (viewModel.LichLamViecItems == null) viewModel.LichLamViecItems = new List<LichLamViecViewModel>();
                viewModel.LichLamViecItems.Add(new LichLamViecViewModel());
            }
            return View(viewModel);
        }

        // GET: /CompanyPosting/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
             if (id == null || id <= 0) return NotFound("ID không hợp lệ.");
             if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");
             var posting = await _context.TinTuyenDungs.Include(t => t.TinTuyenDungNganhNghes).Include(t => t.LichLamViecCongViecs).FirstOrDefaultAsync(t => t.Id == id);
             if (posting == null) return NotFound($"Không tìm thấy tin tuyển dụng với ID {id}.");
             if (posting.NguoiDangId != userId) { _logger.LogWarning("NTD DN {UserId} cố gắng chỉnh sửa tin {PostingId} không thuộc sở hữu.", userId, id); return Forbid("Bạn không có quyền chỉnh sửa tin tuyển dụng này."); }
             if (posting.TrangThai == TrangThaiTinTuyenDung.daxoa || posting.TrangThai == TrangThaiTinTuyenDung.bituchoi) { TempData["ErrorMessage"] = "Không thể chỉnh sửa tin đã bị xóa hoặc bị từ chối."; return RedirectToAction(nameof(Index)); }
              if (posting.TrangThai == TrangThaiTinTuyenDung.datuyen) { TempData["InfoMessage"] = "Tin đã tuyển đủ người, một số thông tin có thể không được chỉnh sửa."; /* Hoặc return RedirectToAction(nameof(Index)) */ }

             var viewModel = new CompanyPostingViewModel { // Dùng ViewModel của Company
                 Id = posting.Id, TieuDe = posting.TieuDe, MoTa = posting.MoTa, YeuCau = posting.YeuCau, QuyenLoi = posting.QuyenLoi,
                 LoaiHinhCongViec = posting.LoaiHinhCongViec, LoaiLuong = posting.LoaiLuong, LuongToiThieu = posting.LuongToiThieu, LuongToiDa = posting.LuongToiDa,
                 DiaChiLamViec = posting.DiaChiLamViec, ThanhPhoId = posting.ThanhPhoId, QuanHuyenId = posting.QuanHuyenId,
                 YeuCauKinhNghiemText = posting.YeuCauKinhNghiemText, YeuCauHocVanText = posting.YeuCauHocVanText, SoLuongTuyen = posting.SoLuongTuyen,
                 TinGap = posting.TinGap, NgayHetHan = posting.NgayHetHan,
                 SelectedNganhNgheIds = posting.TinTuyenDungNganhNghes.Select(tnn => tnn.NganhNgheId).ToList(),
                 LichLamViecItems = posting.LichLamViecCongViecs.Select(l => new LichLamViecViewModel { Id = l.Id, NgayTrongTuan = l.NgayTrongTuan, GioBatDau = l.GioBatDau, GioKetThuc = l.GioKetThuc, BuoiLamViec = l.BuoiLamViec, MarkedForDeletion=false }).ToList()
              };

             await PrepareDropdownsAsync(viewModel.ThanhPhoId, viewModel.QuanHuyenId);
             if (!viewModel.LichLamViecItems.Any()) viewModel.LichLamViecItems.Add(new LichLamViecViewModel());
             return View(viewModel); // Views/CompanyPosting/Edit.cshtml
        }

        // POST: /CompanyPosting/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CompanyPostingViewModel viewModel)
        {
            if (id != viewModel.Id) return BadRequest("ID không khớp.");
            if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");
            viewModel.LichLamViecItems.RemoveAll(l => l.Id == null && l.NgayTrongTuan == 0 && l.GioBatDau == null && l.GioKetThuc == null && l.BuoiLamViec == null);
            if (viewModel.NgayHetHan.HasValue && viewModel.NgayHetHan.Value.Date < DateTime.UtcNow.Date) { ModelState.AddModelError(nameof(viewModel.NgayHetHan), "Ngày hết hạn không được là ngày trong quá khứ."); }


            if (ModelState.IsValid)
            {
                var postingInDb = await _context.TinTuyenDungs.Include(t => t.TinTuyenDungNganhNghes).Include(t => t.LichLamViecCongViecs).FirstOrDefaultAsync(t => t.Id == id);
                if (postingInDb == null) return NotFound($"Không tìm thấy tin tuyển dụng ID {id}.");
                if (postingInDb.NguoiDangId != userId) { _logger.LogWarning("NTD DN {UserId} cố gắng POST chỉnh sửa tin {PostingId} không thuộc sở hữu.", userId, id); return Forbid("Bạn không có quyền chỉnh sửa tin này."); }
                if (postingInDb.TrangThai == TrangThaiTinTuyenDung.daxoa || postingInDb.TrangThai == TrangThaiTinTuyenDung.bituchoi) { TempData["ErrorMessage"] = "Không thể chỉnh sửa tin đã bị xóa hoặc bị từ chối."; return RedirectToAction(nameof(Index)); }
                if (postingInDb.TrangThai == TrangThaiTinTuyenDung.datuyen) { TempData["InfoMessage"] = "Tin đã tuyển đủ người."; /* Logic tùy chọn */ }


                 postingInDb.TieuDe = viewModel.TieuDe; postingInDb.MoTa = viewModel.MoTa; postingInDb.YeuCau = viewModel.YeuCau; postingInDb.QuyenLoi = viewModel.QuyenLoi;
                 postingInDb.LoaiHinhCongViec = viewModel.LoaiHinhCongViec; postingInDb.LoaiLuong = viewModel.LoaiLuong; postingInDb.LuongToiThieu = viewModel.LuongToiThieu; postingInDb.LuongToiDa = viewModel.LuongToiDa;
                 postingInDb.DiaChiLamViec = viewModel.DiaChiLamViec; postingInDb.ThanhPhoId = viewModel.ThanhPhoId; postingInDb.QuanHuyenId = viewModel.QuanHuyenId;
                 postingInDb.YeuCauKinhNghiemText = viewModel.YeuCauKinhNghiemText ?? "Không yêu cầu"; postingInDb.YeuCauHocVanText = viewModel.YeuCauHocVanText ?? "Không yêu cầu";
                 postingInDb.SoLuongTuyen = viewModel.SoLuongTuyen; postingInDb.TinGap = viewModel.TinGap; postingInDb.NgayHetHan = viewModel.NgayHetHan; postingInDb.NgayCapNhat = DateTime.UtcNow;

                 // QUY TRÌNH DUYỆT: NTD DN sửa tin đã duyệt/ẩn -> Chuyển về chờ duyệt
                 if (postingInDb.TrangThai == TrangThaiTinTuyenDung.daduyet || postingInDb.TrangThai == TrangThaiTinTuyenDung.taman)
                 {
                      postingInDb.TrangThai = TrangThaiTinTuyenDung.choduyet; // Yêu cầu duyệt lại
                      postingInDb.AdminDuyetId = null; postingInDb.NgayDuyet = null;
                      _logger.LogInformation("Tin {PostingId} được chuyển về trạng thái chờ duyệt sau khi sửa bởi NTD Doanh nghiệp {UserId}.", id, userId);
                      TempData["InfoMessage"] = "Tin đã được cập nhật và chuyển về trạng thái chờ duyệt."; // Thêm thông báo
                  }

                  var currentSelectedIdsNN = viewModel.SelectedNganhNgheIds ?? new List<int>();
                  var existingEntriesNN = postingInDb.TinTuyenDungNganhNghes.ToList();
                  var entriesToRemoveNN = existingEntriesNN.Where(e => !currentSelectedIdsNN.Contains(e.NganhNgheId)).ToList();
                  _context.TinTuyenDung_NganhNghes.RemoveRange(entriesToRemoveNN);
                  var existingIdsNN = existingEntriesNN.Select(e => e.NganhNgheId);
                  var idsToAddNN = currentSelectedIdsNN.Except(existingIdsNN).ToList();
                  foreach (var nnId in idsToAddNN) { if (await _context.NganhNghes.AnyAsync(n => n.Id == nnId)) { postingInDb.TinTuyenDungNganhNghes.Add(new TinTuyenDung_NganhNghe { NganhNgheId = nnId }); } }


                  var currentVmItemsL = viewModel.LichLamViecItems ?? new List<LichLamViecViewModel>();
                  var existingDbEntriesL = postingInDb.LichLamViecCongViecs.ToList();
                  var vmIdsToKeepL = currentVmItemsL.Where(vm => vm.Id.HasValue && !vm.MarkedForDeletion).Select(vm => vm.Id!.Value).ToList();
                  var dbEntriesToRemoveL = existingDbEntriesL.Where(db => !vmIdsToKeepL.Contains(db.Id)).ToList();
                  _context.LichLamViecCongViecs.RemoveRange(dbEntriesToRemoveL);
                  foreach (var lichVM in currentVmItemsL) {
                       if (!lichVM.MarkedForDeletion) {
                           if (lichVM.Id.HasValue && lichVM.Id > 0) { var lichInDb = existingDbEntriesL.FirstOrDefault(l => l.Id == lichVM.Id.Value); if (lichInDb != null) { lichInDb.NgayTrongTuan = lichVM.NgayTrongTuan; lichInDb.GioBatDau = lichVM.GioBatDau; lichInDb.GioKetThuc = lichVM.GioKetThuc; lichInDb.BuoiLamViec = lichVM.BuoiLamViec; } }
                           else { if(lichVM.NgayTrongTuan != 0 || lichVM.BuoiLamViec.HasValue || lichVM.GioBatDau.HasValue || lichVM.GioKetThuc.HasValue) { postingInDb.LichLamViecCongViecs.Add(new LichLamViecCongViec { NgayTrongTuan = lichVM.NgayTrongTuan, GioBatDau = lichVM.GioBatDau, GioKetThuc = lichVM.GioKetThuc, BuoiLamViec = lichVM.BuoiLamViec }); } }
                       }
                  }

                try {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("NTD Doanh nghiệp (User ID: {UserId}) đã cập nhật Tin Tuyển Dụng (ID: {PostingId}) thành công.", userId, id);
                    // TempData["SuccessMessage"] được set bên trên nếu chuyển về chờ duyệt
                    if (TempData["InfoMessage"] == null) // Chỉ set Success nếu không có Info
                    {
                        TempData["SuccessMessage"] = "Cập nhật tin tuyển dụng thành công!";
                    }
                    return RedirectToAction(nameof(Index));
                } catch (DbUpdateConcurrencyException ex) {
                     _logger.LogWarning(ex, "Lỗi concurrency khi NTD Doanh nghiệp (User ID: {UserId}) cập nhật tin {PostingId}", userId, id); ModelState.AddModelError("", "Dữ liệu đã bị thay đổi bởi người khác. Vui lòng kiểm tra lại.");
                      var entry = ex.Entries.Single(); var databaseValues = await entry.GetDatabaseValuesAsync();
                     if (databaseValues != null) { var dbPosting = (TinTuyenDung)databaseValues.ToObject(); viewModel.ThanhPhoId = dbPosting.ThanhPhoId; viewModel.QuanHuyenId = dbPosting.QuanHuyenId; }
                     else { ModelState.AddModelError("", "Không thể lấy dữ liệu mới nhất từ CSDL."); }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Lỗi khi NTD Doanh nghiệp (User ID: {UserId}) cập nhật tin {PostingId}", userId, id); ModelState.AddModelError("", "Đã có lỗi xảy ra khi lưu thay đổi.");
                }
            } else {
                 _logger.LogWarning("ModelState không hợp lệ khi NTD Doanh nghiệp (User ID: {UserId}) cập nhật tin {PostingId}. Lỗi: {Errors}", userId, id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            await PrepareDropdownsAsync(viewModel.ThanhPhoId, viewModel.QuanHuyenId);
            if (!(viewModel.LichLamViecItems?.Any(l=>!l. MarkedForDeletion) ?? false)) {
                if (viewModel.LichLamViecItems == null) viewModel.LichLamViecItems = new List<LichLamViecViewModel>();
                viewModel.LichLamViecItems.Add(new LichLamViecViewModel());
            }
            return View(viewModel);
        }

        // GET: /CompanyPosting/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0) return NotFound("ID không hợp lệ.");
             if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");
             var postingInfo = await _context.TinTuyenDungs.Where(t => t.Id == id).Select(t => new { t.Id, t.TieuDe, t.NguoiDangId, t.TrangThai }).AsNoTracking().FirstOrDefaultAsync();
             if (postingInfo == null) return NotFound($"Không tìm thấy tin tuyển dụng ID {id}.");
             if (postingInfo.NguoiDangId != userId) { _logger.LogWarning("NTD DN {UserId} cố gắng xem trang xóa tin {PostingId} không thuộc sở hữu.", userId, id); return Forbid("Bạn không có quyền xóa tin này."); }
             if (postingInfo.TrangThai == TrangThaiTinTuyenDung.daxoa) { TempData["InfoMessage"] = "Tin tuyển dụng này đã được xóa trước đó."; return RedirectToAction(nameof(Index)); }
             ViewBag.PostingTitle = postingInfo.TieuDe; ViewBag.PostingId = postingInfo.Id;
             return View(); // Views/CompanyPosting/Delete.cshtml
        }

        // POST: /CompanyPosting/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
             if (!TryGetUserId(out int userId)) return RedirectToAction("AccessDenied", "TaiKhoan");
             var posting = await _context.TinTuyenDungs.FindAsync(id);
             if (posting == null) { TempData["InfoMessage"] = "Tin tuyển dụng không tồn tại hoặc đã bị xóa."; return RedirectToAction(nameof(Index)); }
             if (posting.NguoiDangId != userId) { _logger.LogWarning("NTD DN {UserId} cố gắng xác nhận xóa tin {PostingId} không thuộc sở hữu.", userId, id); return Forbid("Bạn không có quyền xóa tin này."); }
             if (posting.TrangThai == TrangThaiTinTuyenDung.daxoa) { TempData["InfoMessage"] = "Tin tuyển dụng này đã được xóa trước đó."; return RedirectToAction(nameof(Index)); }
             try {
                 posting.TrangThai = TrangThaiTinTuyenDung.daxoa; posting.NgayCapNhat = DateTime.UtcNow;
                 await _context.SaveChangesAsync();
                 _logger.LogInformation("NTD Doanh nghiệp (User ID: {UserId}) đã xóa (logic) Tin Tuyển Dụng (ID: {PostingId})", userId, id);
                 TempData["SuccessMessage"] = $"Đã xóa thành công tin tuyển dụng '{posting.TieuDe}'.";
             } catch (Exception ex) {
                 _logger.LogError(ex, "Lỗi khi NTD Doanh nghiệp (User ID: {UserId}) xóa (logic) tin {PostingId}", userId, id); TempData["ErrorMessage"] = "Đã có lỗi xảy ra khi xóa tin tuyển dụng.";
             }
             return RedirectToAction(nameof(Index));
        }

        // *** ACTION MỚI ĐỂ ẨN/HIỆN TIN (CHO DOANH NGHIỆP) ***
        // POST: /CompanyPosting/ToggleVisibility/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            // 1. Kiểm tra quyền và lấy UserId
            if (!TryGetUserId(out int userId))
            {
                // Lỗi nghiêm trọng, không xác thực được user
                return RedirectToAction("AccessDenied", "TaiKhoan"); // Hoặc trả lỗi 500
            }

            // 2. Tìm tin tuyển dụng
            var posting = await _context.TinTuyenDungs.FindAsync(id);

            // 3. Kiểm tra tồn tại
            if (posting == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin tuyển dụng.";
                return RedirectToAction(nameof(Index));
            }

            // 4. Kiểm tra quyền sở hữu
            if (posting.NguoiDangId != userId)
            {
                _logger.LogWarning("NTD Doanh nghiệp {UserId} cố gắng ẩn/hiện tin {PostingId} không thuộc sở hữu.", userId, id);
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện hành động này với tin tuyển dụng này.";
                return RedirectToAction(nameof(Index));
            }

            // 5. Xác định trạng thái mới và thông báo
            TrangThaiTinTuyenDung newStatus;
            string successMessage;
            string? infoMessage = null; // Dùng để thông báo nếu cần duyệt lại

            if (posting.TrangThai == TrangThaiTinTuyenDung.daduyet)
            {
                // Đang duyệt -> Chuyển sang Tạm ẩn
                newStatus = TrangThaiTinTuyenDung.taman;
                successMessage = $"Đã ẩn thành công tin tuyển dụng '{posting.TieuDe}'.";
                _logger.LogInformation("NTD Doanh nghiệp (User ID: {UserId}) đã ẩn Tin Tuyển Dụng (ID: {PostingId})", userId, id);
            }
            else if (posting.TrangThai == TrangThaiTinTuyenDung.taman)
            {
                // Đang tạm ẩn -> Chuyển sang Chờ duyệt (QUAN TRỌNG: Doanh nghiệp hiện lại phải chờ duyệt)
                newStatus = TrangThaiTinTuyenDung.choduyet;
                successMessage = $"Đã yêu cầu hiển thị lại tin tuyển dụng '{posting.TieuDe}'. Tin đang chờ duyệt.";
                infoMessage = successMessage; // Dùng Info message thay vì Success
                posting.AdminDuyetId = null; // Xóa thông tin duyệt cũ
                posting.NgayDuyet = null;
                _logger.LogInformation("NTD Doanh nghiệp (User ID: {UserId}) đã yêu cầu hiện lại Tin Tuyển Dụng (ID: {PostingId}) - Chuyển sang Chờ duyệt.", userId, id);
            }
            else
            {
                // Trạng thái khác không cho phép
                _logger.LogWarning("NTD Doanh nghiệp {UserId} cố gắng ẩn/hiện tin {PostingId} đang ở trạng thái không hợp lệ ({CurrentStatus}).", userId, id, posting.TrangThai);
                TempData["InfoMessage"] = "Chỉ có thể ẩn tin đang hiển thị hoặc yêu cầu hiện lại tin đang tạm ẩn.";
                return RedirectToAction(nameof(Index));
            }

            // 6. Cập nhật trạng thái và ngày giờ
            posting.TrangThai = newStatus;
            posting.NgayCapNhat = DateTime.UtcNow;

            // 7. Lưu thay đổi
            try
            {
                await _context.SaveChangesAsync();
                 if (!string.IsNullOrEmpty(infoMessage)) {
                     TempData["InfoMessage"] = infoMessage; // Ưu tiên Info Message nếu có
                 } else {
                     TempData["SuccessMessage"] = successMessage;
                 }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi NTD Doanh nghiệp (User ID: {UserId}) ẩn/hiện tin {PostingId}", userId, id);
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra khi cập nhật trạng thái tin tuyển dụng.";
            }

            // 8. Redirect về Index
            return RedirectToAction(nameof(Index));
        }

    }
}