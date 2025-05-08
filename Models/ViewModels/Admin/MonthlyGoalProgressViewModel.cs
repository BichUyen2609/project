// File: ViewModels/Admin/MonthlyGoalProgressViewModel.cs
namespace HeThongTimViec.ViewModels.Admin // <--- THAY ĐỔI Ở ĐÂY
{
    public class MonthlyGoalProgressViewModel
    {
        /// <summary>
        /// Nhãn mô tả mục tiêu (VD: "Người dùng mới")
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Giá trị mục tiêu cần đạt
        /// </summary>
        public int GoalValue { get; set; }

        /// <summary>
        /// Giá trị thực tế đã đạt được
        /// </summary>
        public int ActualValue { get; set; }

        /// <summary>
        /// Phần trăm hoàn thành mục tiêu (0-100)
        /// </summary>
        public int Percentage { get; set; }
    }
}