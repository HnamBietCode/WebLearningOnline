using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class AssignmentQuestionOption
    {
        [Key]
        [StringLength(50)]
        public string OptionId { get; set; }

        [Required]
        [StringLength(50)]
        public string QuestionId { get; set; }

        [Required]
        [StringLength(1000)]
        public string OptionText { get; set; }

        [Required]
        [StringLength(10)]
        public string OptionLabel { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        public AssignmentQuestion? Question { get; set; }
    }
}
