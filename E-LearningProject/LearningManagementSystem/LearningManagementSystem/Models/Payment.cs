using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Payment
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string PaymentId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }    

        [StringLength(50)]
        public string? CourseId { get; set; } 

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [Required]
        [StringLength(50)]
        public string TransactionType { get; set; } 

        // Navigation properties
        public User? User { get; set; }
        public Course? Course { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }
    }
}