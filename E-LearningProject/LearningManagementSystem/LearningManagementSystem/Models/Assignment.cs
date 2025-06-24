using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Assignment
    {
        [Key]
        [StringLength(50)]
        public string AssignmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; }

        [StringLength(50)]
        public string? LessonId { get; set; }

        [Required]
        [StringLength(50)]
        public string AssignmentType { get; set; } // Exercise/Test

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public int? DurationMinutes { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public Course Course { get; set; }

        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; }

        public List<AssignmentQuestion>? Questions { get; set; } 
        public List<AssignmentSubmission>? Submissions { get; set; } 
    }
}
