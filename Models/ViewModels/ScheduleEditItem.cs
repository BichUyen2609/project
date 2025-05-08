// File: ViewModels/ScheduleEditItem.cs
using HeThongTimViec.Models;

namespace HeThongTimViec.ViewModels
{
    // ViewModel for binding the schedule edit form checkboxes
    public class ScheduleEditItem
    {
        public int NguoiDungId { get; set; }
        public NgayTrongTuan NgayTrongTuan { get; set; }
        public BuoiLamViec BuoiLamViec { get; set; }
        public bool IsSelected { get; set; } // To capture checkbox state
    }
}