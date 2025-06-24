using LearningManagementSystem.Models;
using System.Collections.Generic;

namespace LearningManagementSystem.Models.ViewModels
{
    public class ViewTestResultViewModel
    {
        public string CourseId { get; set; }
        public string AssignmentId { get; set; }
        public string AssignmentTitle { get; set; }
        public List<AssignmentQuestion> Questions { get; set; }
        public List<AssignmentSubmission> Submissions { get; set; }
    }
}