namespace HeThongTimViec.Views.ViewModels.Admin.JobPosts
{
    public class AdminJobPostStatisticsViewModel
    {
        public int TotalJobs { get; set; }
        public string TotalJobsChangeText { get; set; } = ""; // e.g. "15% so với tháng trước"

        public int ActiveJobs { get; set; }
        public string ActiveJobsChangeText { get; set; } = "";

        public int PendingJobs { get; set; }
        public string PendingJobsChangeText { get; set; } = "";

        public int ExpiredOrRejectedJobs { get; set; } // Combined for "Đã hết hạn / Bị từ chối"
        public string ExpiredOrRejectedJobsChangeText { get; set; } = "";
    }
}