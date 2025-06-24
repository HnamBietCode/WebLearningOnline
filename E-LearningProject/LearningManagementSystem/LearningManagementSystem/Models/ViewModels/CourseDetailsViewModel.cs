namespace LearningManagementSystem.Models.ViewModels
{
    public class CourseDetailsViewModel
    {
        public CourseListViewModel Course { get; set; }
        public bool IsEnrolled { get; set; }
        public List<Progress> Progresses { get; set; }
        public List<Comment> Comments { get; set; }
        public Dictionary<string, (string SelectedOptionText, string SelectedOptionLabel, bool? IsCorrect, double? Score)> PreviousSubmissions { get; set; }
        public string NewCommentContent { get; set; }
        public int NewCommentRating { get; set; }
    }
}