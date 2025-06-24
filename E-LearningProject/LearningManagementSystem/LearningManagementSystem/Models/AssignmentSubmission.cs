using LearningManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

public class AssignmentSubmission
{
    [Key]
    [StringLength(50)]
    public string SubmissionId { get; set; }

    [Required]
    [StringLength(50)]
    public string AssignmentId { get; set; }

    [Required]
    [StringLength(50)]
    public string QuestionId { get; set; }

    [Required]
    [StringLength(50)]
    public string UserName { get; set; }

    public bool? IsCorrect { get; set; }

    public double? Score { get; set; }

    [StringLength(1000)]
    public string? Feedback { get; set; }

    [Required]
    public DateTime SubmittedDate { get; set; }

    // Các trường mới
    [StringLength(1000)]
    public string? SelectedOptionText { get; set; } // Nội dung đáp án đã chọn (ví dụ: "A. Hello")

    [StringLength(10)]
    public string? SelectedOptionLabel { get; set; } // Nhãn đáp án (ví dụ: "A")

    // Navigation properties
    public Assignment? Assignment { get; set; }
    public AssignmentQuestion? Question { get; set; }
    public User? User { get; set; }
}