using HeThongTimViec.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace HeThongTimViec.ViewModels.JobPosting
{
    public class LichLamViecViewModel
    {
        public int? Id { get; set; } // Dùng để xác định khi edit/delete

        [Required(ErrorMessage = "Vui lòng chọn ngày trong tuần.")]
        public NgayTrongTuan NgayTrongTuan { get; set; }

        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = @"{0:hh\:mm}")]
        public TimeSpan? GioBatDau { get; set; }

        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = @"{0:hh\:mm}")]
        public TimeSpan? GioKetThuc { get; set; }

        public BuoiLamViec? BuoiLamViec { get; set; }

        // Dùng để đánh dấu dòng lịch cần xóa khi submit form Edit
        // Không cần hiển thị trên UI, nhưng cần có trong model binding
        public bool MarkedForDeletion { get; set; } = false;
    }
}