using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace trungtamdaotao.Models
{
    // Course class represents a course in the training center.
    // It contains properties for course ID, code, name, instructor, start date, tuition fee, maximum students, and creation date.
    // It also has a collection of course registrations associated with the course.
public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required, StringLength(20)]
    public string CourseCode { get; set; }

    [Required, StringLength(100)]
    public string CourseName { get; set; }

    [StringLength(100)]
    public string Instructor { get; set; }

    [Required][DataType(DataType.Date)] 
    public DateTime StartDate { get; set; }

    public decimal TuitionFee { get; set; }

    public int MaxStudents { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

       public virtual ICollection<CourseRegistration> CourseRegistrations { get; set; } = new List<CourseRegistration>();
}
}
