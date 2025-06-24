using System.ComponentModel.DataAnnotations;

namespace LearningManagementSystem.Models
{
    public class Role
    {
        [Key]
        [Required, StringLength(50)]
        public string RoleId { get; set; }

        [Required, StringLength(50)]
        public string RoleName { get; set; }

        public List<User> Users { get; set; }
    }
}
