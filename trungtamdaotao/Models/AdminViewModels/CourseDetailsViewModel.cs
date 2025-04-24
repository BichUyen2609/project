using System;
using System.Collections.Generic;
using trungtamdaotao.Models; // Đảm bảo using đúng namespace của Course và User

namespace trungtamdaotao.ViewModels // Hoặc namespace phù hợp
{
    public class CourseDetailsViewModel
    {
        // Thông tin cơ bản của khóa học
        public int CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Instructor { get; set; }
        public DateTime StartDate { get; set; }
        public decimal TuitionFee { get; set; }
        public int MaxStudents { get; set; }
        public DateTime CreatedAt { get; set; }

        // Số lượng học viên đã đăng ký (tính sẵn)
        public int RegisteredStudentCount { get; set; }

        // Danh sách thông tin học viên đã đăng ký (đơn giản hóa)
        public List<StudentRegistrationInfo> RegisteredStudents { get; set; } = new List<StudentRegistrationInfo>();
    }

    // Lớp phụ để chứa thông tin cần thiết của học viên đăng ký
    public class StudentRegistrationInfo
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Status { get; set; } // Ví dụ: "Đã đăng ký", "Đã xác nhận"...
    }
}