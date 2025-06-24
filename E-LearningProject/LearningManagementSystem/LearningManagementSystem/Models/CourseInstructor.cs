    using System.ComponentModel.DataAnnotations;

    namespace LearningManagementSystem.Models
    {
        public class CourseInstructor
        {
            [Required]
            [StringLength(50)]
            public string CourseId { get; set; }

            [Required]
            [StringLength(50)]
            public string UserName { get; set; }

            // Navigation properties
            public Course Course { get; set; }
            public User User { get; set; }
        }
    }