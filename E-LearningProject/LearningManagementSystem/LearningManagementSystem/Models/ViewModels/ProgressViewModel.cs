namespace LearningManagementSystem.Models.ViewModels
{
    public class ProgressViewModel
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public int TotalLessons { get; set; }
        public int CompletedLessons { get; set; }
        public DateTime? CompletionDate { get; set; }
    }
}