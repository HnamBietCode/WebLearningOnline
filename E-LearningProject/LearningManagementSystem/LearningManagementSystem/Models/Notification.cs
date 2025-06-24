using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Notification
    {
        [Key]
        [Required]
        [StringLength(50)]
        public string NotificationId { get; set; }

        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        [Required]
        public bool IsRead { get; set; }

        // Navigation property
        public User User { get; set; }
    }
}