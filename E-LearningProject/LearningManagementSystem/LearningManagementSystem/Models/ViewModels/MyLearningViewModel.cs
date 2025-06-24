using System;

namespace LearningManagementSystem.Models.ViewModels
{
    public class MyLearningViewModel
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public int Progress { get; set; }
        public DateTime LastAccessed { get; set; }
    }
} 