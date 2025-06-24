using System.Collections.Generic;

namespace LearningManagementSystem.Models.ViewModels
{
    public class HomeViewModel
    {
        public User User { get; set; }
        public List<CourseListViewModel> Courses { get; set; }
        public string SearchQuery { get; set; }

        public List<Enrollment> Enrollments { get; set; }

        public List<AssignmentSubmission> Submissions { get; set; }

        public List<ChatMessage> Messages { get; set; }

        // Thêm thuộc tính này để truyền danh sách CourseId đã có trong giỏ hàng
        public List<string> CartCourseIds { get; set; }
    }
}
