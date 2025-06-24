using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningManagementSystem.Models
{
    public class OrderDetail
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string OrderDetailId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentId { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        // Navigation properties
        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}