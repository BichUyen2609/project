using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using trungtamdaotao.Data;
using trungtamdaotao.Models;

namespace trungtamdaotao.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Courses
        public async Task<IActionResult> Index()
        {
            // All users can view the list of courses
            var courses = await _context.Courses
                .Where(c => c.StartDate > DateTime.Now)
                .ToListAsync();
                
            return View(courses);
        }

        // GET: Courses/Details/5
        public async Task<IActionResult> Details(int? id)
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

            // Get current registration count
            ViewBag.CurrentRegistrations = await _context.CourseRegistrations
                .CountAsync(cr => cr.CourseId == id && !cr.IsCanceled);
                
            // Check if current user is already registered
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                ViewBag.IsRegistered = await _context.CourseRegistrations
                    .AnyAsync(cr => cr.CourseId == id && cr.UserId == userId && !cr.IsCanceled);
            }

            return View(course);
        }

        // POST: Courses/Register/5
        [Authorize] // Only authenticated users can register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Check if course start date is in the future
            if (course.StartDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể đăng ký khóa học đã khai giảng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _userManager.GetUserId(User);
            
            // Check if user is already registered
            var existingRegistration = await _context.CourseRegistrations
                .FirstOrDefaultAsync(cr => cr.CourseId == id && cr.UserId == userId && !cr.IsCanceled);
                
            if (existingRegistration != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký khóa học này rồi.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check if course has reached max capacity
            var currentRegistrations = await _context.CourseRegistrations
                .CountAsync(cr => cr.CourseId == id && !cr.IsCanceled);
                
            if (currentRegistrations >= course.MaxStudents)
            {
                TempData["ErrorMessage"] = "Khóa học đã đạt số lượng học viên tối đa.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Create new registration
            var registration = new CourseRegistration
            {
                CourseId = id,
                UserId = userId,
                RegistrationDate = DateTime.Now,
                IsCanceled = false
            };

            _context.Add(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký khóa học thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Courses/Cancel/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            // Check if course start date is in the future
            if (course.StartDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể hủy đăng ký khóa học đã khai giảng.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = _userManager.GetUserId(User);
            
            // Find the registration
            var registration = await _context.CourseRegistrations
                .FirstOrDefaultAsync(cr => cr.CourseId == id && cr.UserId == userId && !cr.IsCanceled);
                
            if (registration == null)
            {
                TempData["ErrorMessage"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Cancel the registration
            registration.IsCanceled = true;
            registration.CancelDate = DateTime.Now;
            
            _context.Update(registration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hủy đăng ký khóa học thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}