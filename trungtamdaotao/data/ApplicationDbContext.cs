using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using trungtamdaotao.Models;

namespace trungtamdaotao.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseRegistration> CourseRegistrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationUser entity
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                    .HasMaxLength(100);
                
                entity.Property(u => u.BirthDate)
                    .IsRequired(false);
                
                entity.Property(u => u.CreatedAt)
                    .IsRequired();
            });

            // Configure Course entity
            modelBuilder.Entity<Course>(entity =>
            {
                entity.ToTable("courses");
                entity.HasKey(c => c.CourseId);
                entity.Property(c => c.CourseId)
                    .ValueGeneratedOnAdd(); // Đảm bảo CourseId tự động tăng
                entity.Property(c => c.CourseCode)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(c => c.CourseName)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(c => c.Instructor)
                    .HasMaxLength(100);
                entity.Property(c => c.TuitionFee)
                    .IsRequired()
                    .HasColumnType("decimal(18, 2)");
                entity.Property(c => c.MaxStudents)
                    .IsRequired();
                entity.Property(c => c.StartDate)
                    .IsRequired();
                entity.Property(c => c.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()"); // Giá trị mặc định là thời gian hiện tại (SQL Server)
            });

            // Configure CourseRegistration entity
            modelBuilder.Entity<CourseRegistration>(entity =>
            {
                entity.HasKey(cr => cr.RegistrationId);
                
                entity.Property(cr => cr.RegistrationDate)
                    .IsRequired();
                
                entity.Property(cr => cr.IsCanceled)
                    .IsRequired();

                entity.HasOne(cr => cr.Course)
                    .WithMany(c => c.CourseRegistrations)
                    .HasForeignKey(cr => cr.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cr => cr.User)
                    .WithMany(u => u.CourseRegistrations)
                    .HasForeignKey(cr => cr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // The following methods are used for statistics and reporting functionality
        public IEnumerable<CourseStudentStats> GetCourseStudentStats()
        {
            return Courses
                .Select(c => new CourseStudentStats
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    TotalStudents = c.CourseRegistrations.Count(cr => !cr.IsCanceled)
                })
                .ToList();
        }

        public IEnumerable<CourseRevenue> GetCourseRevenue()
        {
            return Courses
                .Select(c => new CourseRevenue
                {
                    CourseId = c.CourseId,
                    CourseName = c.CourseName,
                    Enrollments = c.CourseRegistrations.Count(cr => !cr.IsCanceled),
                    TuitionFee = c.TuitionFee,
                    TotalRevenue = c.TuitionFee * c.CourseRegistrations.Count(cr => !cr.IsCanceled)
                })
                .ToList();
        }

        public IEnumerable<MonthlyRevenue> GetMonthlyRevenue(int year)
        {
            return CourseRegistrations
                .Where(cr => !cr.IsCanceled && cr.RegistrationDate.Year == year)
                .GroupBy(cr => new { cr.RegistrationDate.Year, cr.RegistrationDate.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(cr => cr.Course.TuitionFee)
                })
                .OrderBy(mr => mr.Year)
                .ThenBy(mr => mr.Month)
                .ToList();
        }

        public IEnumerable<MonthlyRevenue> GetRevenueByDateRange(DateTime startDate, DateTime endDate)
        {
            return CourseRegistrations
                .Where(cr => !cr.IsCanceled && 
                       cr.RegistrationDate >= startDate && 
                       cr.RegistrationDate <= endDate)
                .GroupBy(cr => new { cr.RegistrationDate.Year, cr.RegistrationDate.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    TotalRevenue = g.Sum(cr => cr.Course.TuitionFee)
                })
                .OrderBy(mr => mr.Year)
                .ThenBy(mr => mr.Month)
                .ToList();
        }

        public bool CanRegisterCourse(int courseId)
        {
            var course = Courses.Find(courseId);
            if (course == null)
                return false;

            var currentRegistrations = CourseRegistrations
                .Count(cr => cr.CourseId == courseId && !cr.IsCanceled);

            return currentRegistrations < course.MaxStudents;
        }
    }
}