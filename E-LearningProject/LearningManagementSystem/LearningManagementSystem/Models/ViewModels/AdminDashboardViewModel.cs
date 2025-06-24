namespace LearningManagementSystem.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalLessons { get; set; }
        public int TotalComments { get; set; }
        public int TotalEnrollments { get; set; }
        public int TotalProgresses { get; set; }

        public decimal TotalPayments { get; set; }

        public Dictionary<string, decimal> MonthlyPayments { get; set; }
    }
}
