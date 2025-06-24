using System.Collections.Generic;

namespace LearningManagementSystem.Models.ViewModels
{
    public class CourseListViewModel
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ImageUrl { get; set; }
        public bool IsEnrolled { get; set; }
        public decimal? Price { get; set; }

        public string Title { get; set; }
        public List<Lesson> Lessons { get; set; }

        public List<Assignment> Assignments { get; set; }

        public double? AverageRating { get; set; }

        public string InstructorName { get; set; }
    }
}
