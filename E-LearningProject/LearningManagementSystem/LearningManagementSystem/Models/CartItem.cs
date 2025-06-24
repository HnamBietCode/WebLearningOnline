using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class CartItem
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string CartItemId { get; set; }

        [Required]
        [StringLength(50)]
        public string CartId { get; set; }

        [Required]
        [StringLength(50)]
        public string CourseId { get; set; }

        [Required]
        public DateTime AddedDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        // Navigation properties
        public Cart Cart { get; set; }
        public Course Course { get; set; }
    }
} 