using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace trungtamdaotao.Models
{
    public class CourseRegistration
    {
        [Key]
        public int RegistrationId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string UserId { get; set; } // Vì Identity dùng string cho khóa chính

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public bool IsCanceled { get; set; } = false;

        public DateTime? CancelDate { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } // Sửa thành ApplicationUser
    }
}
