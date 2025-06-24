using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Progress
    {
        [Key]
        [Required, StringLength(50)]
        public string ProgressId { get; set; }

        [Required, StringLength(50)]
        public string UserName { get; set; }

        [Required, StringLength(50)]
        public string LessonId { get; set; }

        public bool CompletionStatus { get; set; }

        public DateTime? CompletionDate { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Lesson Lesson { get; set; }
    }
}