using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Models.ViewModels
{
    public class TakeTestViewModel
    {
        public string CourseId { get; set; }
        public string AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public int DurationInMinutes { get; set; } // Thời gian làm bài (phút)
        public List<AssignmentQuestion> Questions { get; set; }
        public Dictionary<string, string> SelectedAnswers { get; set; } // Lưu câu trả lời đã chọn
    }
}