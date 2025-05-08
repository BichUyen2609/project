using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HeThongTimViec.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Thêm using này
using Microsoft.Extensions.Logging; // Thêm để ghi log
using Microsoft.AspNetCore.Authorization; // Thêm nếu muốn bảo vệ API

namespace HeThongTimViec.Controllers
{
    [Route("api/[controller]/[action]")] // Route chuẩn cho API
    [ApiController]
    // [Authorize] // Bỏ comment dòng này nếu muốn API này chỉ người đăng nhập mới gọi được
    public class DiaChiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiaChiController> _logger;

        public DiaChiController(ApplicationDbContext context, ILogger<DiaChiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/DiaChi/GetQuanHuyenByThanhPho?thanhPhoId=123
        [HttpGet]
        public async Task<IActionResult> GetQuanHuyenByThanhPho([FromQuery] int? thanhPhoId)
        {
            if (!thanhPhoId.HasValue || thanhPhoId.Value <= 0)
            {
                _logger.LogInformation("API GetQuanHuyenByThanhPho được gọi với thanhPhoId không hợp lệ hoặc null.");
                return Ok(new List<object>()); // Trả về list rỗng chuẩn JSON
            }

            _logger.LogDebug("API GetQuanHuyenByThanhPho được gọi cho ThanhPhoId: {ThanhPhoId}", thanhPhoId.Value);
            try
            {
                 var quanHuyens = await _context.QuanHuyens
                    .AsNoTracking()
                    .Where(qh => qh.ThanhPhoId == thanhPhoId.Value)
                    .OrderBy(qh => qh.Ten)
                    .Select(qh => new { id = qh.Id, ten = qh.Ten })
                    .ToListAsync();

                 _logger.LogDebug("Tìm thấy {Count} Quận/Huyện cho ThanhPhoId: {ThanhPhoId}", quanHuyens.Count, thanhPhoId.Value);
                 return Ok(quanHuyens);
            }
            catch(Exception ex)
            {
                 _logger.LogError(ex, "Lỗi khi truy vấn Quận/Huyện cho ThanhPhoId: {ThanhPhoId}", thanhPhoId.Value);
                 return StatusCode(500, "Lỗi máy chủ nội bộ khi lấy dữ liệu quận/huyện."); // Trả về lỗi 500 nếu có lỗi DB
            }
        }

        // GET: api/DiaChi/GetAllThanhPho
        [HttpGet]
        public async Task<IActionResult> GetAllThanhPho()
        {
             _logger.LogDebug("API GetAllThanhPho được gọi.");
             try
             {
                  var thanhPhos = await _context.ThanhPhos
                     .AsNoTracking()
                     .OrderBy(tp => tp.Ten)
                     .Select(tp => new { id = tp.Id, ten = tp.Ten })
                     .ToListAsync();
                  _logger.LogDebug("Tìm thấy {Count} Thành phố.", thanhPhos.Count);
                 return Ok(thanhPhos);
             }
             catch(Exception ex)
             {
                  _logger.LogError(ex, "Lỗi khi truy vấn danh sách Thành phố.");
                  return StatusCode(500, "Lỗi máy chủ nội bộ khi lấy dữ liệu thành phố.");
             }
        }
    }
}