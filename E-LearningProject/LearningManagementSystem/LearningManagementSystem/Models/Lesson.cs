using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LearningManagementSystem.Models
{
    public class Lesson
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string LessonId { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; }

        [Required]
        [StringLength(100)]
        public string LessonTitle { get; set; }

        [StringLength(4000)]
        public string? Content { get; set; }

        [StringLength(200)]
        public string? VideoUrl { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        // Navigation properties
        public Course? Course { get; set; }
        public List<Progress>? Progresses { get; set; }
        public List<Assignment>? Assignments { get; set; }

    }
}