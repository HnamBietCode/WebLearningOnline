using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Enrollment
    {
        [Key]
        [Required, StringLength(50)]
        public string EnrollmentId { get; set; }

        [Required, StringLength(50)]
        public string UserName { get; set; }

        [Required, StringLength(50)]
        public string CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Course? Course { get; set; }
    }
}