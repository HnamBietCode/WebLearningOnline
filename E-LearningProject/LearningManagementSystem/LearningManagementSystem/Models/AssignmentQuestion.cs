using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class AssignmentQuestion 
    {
        [Key]
        [StringLength(50)]
        public string QuestionId { get; set; }

        [Required]
        [StringLength(50)]
        public string AssignmentId { get; set; }

        [Required]
        [StringLength(1000)]
        public string QuestionText { get; set; }

        [Required]
        [StringLength(50)]
        public string QuestionType { get; set; } // MultipleChoice/Essay

        [Required]
        public int OrderNumber { get; set; }

        [Required]
        public double? MaxScore { get; set; } = 1.0;
        public  Assignment Assignment { get; set; }

        public List<AssignmentQuestionOption>? Options { get; set; }
        public List<AssignmentSubmission>? Submissions { get; set; } 
    }
}
