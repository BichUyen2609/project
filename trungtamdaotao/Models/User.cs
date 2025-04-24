using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace trungtamdaotao.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<CourseRegistration> CourseRegistrations { get; set; }
    }
}
