namespace LearningManagementSystem.ViewModels
{
    public class StudentSubmissionListViewModel
    {
        public List<StudentSubmissionInfo> Submissions { get; set; }
        public List<CourseOption> CourseOptions { get; set; }
        public PaginationInfo Pagination { get; set; }
        public SubmissionStats Stats { get; set; }
    }

    public class StudentSubmissionInfo
    {
        public string SubmissionId { get; set; }
        public string FullName { get; set; }
        public string StudentId { get; set; }
        public string AssignmentTitle { get; set; }
        public string CourseName { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string Status { get; set; }
        public double? Score { get; set; }
        public string AssignmentType { get; set; }
    }

    public class CourseOption
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }

    public class SubmissionStats
    {
        public int Total { get; set; }
    }
}