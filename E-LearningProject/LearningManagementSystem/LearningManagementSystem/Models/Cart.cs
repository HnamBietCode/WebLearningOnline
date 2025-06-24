using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace LearningManagementSystem.Models
{
    public class Cart
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string CartId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public DateTime LastModifiedDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } 

        // Navigation properties
        public User User { get; set; }
        public List<CartItem> CartItems { get; set; }
    }
} 