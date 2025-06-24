    using System.ComponentModel.DataAnnotations;
    using Microsoft.AspNetCore.Identity;
    using System.Collections.Generic;

    namespace LearningManagementSystem.Models
    {
        public class User
        {
            [Key]
            [Required]
            [StringLength(50)]
            public string UserName { get; set; }

            [Required]
            [StringLength(256)]
            private string _password; // Lưu mật khẩu đã băm

            public string Password
            {
                get => _password;
                set => _password = value;
            }

            [StringLength(100)]
            public string FullName { get; set; }

            [Required]
            [StringLength(100)]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(50)]
            public string RoleId { get; set; }

            [StringLength(200)]
            public string? Avatar { get; set; }

            [StringLength(1000)]
            public string? Bio { get; set; }

            // Navigation properties
            public Role? Role { get; set; }
            public List<Comment>? Comments { get; set; }
            public List<Enrollment>? Enrollments { get; set; }
            public List<Progress>? Progresses { get; set; }
            public List<Payment>? Payments { get; set; }
            public List<Notification>? Notifications { get; set; }
            public List<CourseInstructor>? CourseInstructors { get; set; }
            public List<AssignmentSubmission>? AssignmentSubmissions { get; set; }

            public List<Cart>? Carts { get; set; }



            // Phương thức để băm mật khẩu
            public void HashPassword(IPasswordHasher<User> passwordHasher, string plainPassword)
            {
                _password = passwordHasher.HashPassword(this, plainPassword);
            }

            // Phương thức để xác minh mật khẩu
            public bool VerifyPassword(IPasswordHasher<User> passwordHasher, string plainPassword)
            {
                var result = passwordHasher.VerifyHashedPassword(this, _password, plainPassword);
                return result == PasswordVerificationResult.Success;
            }
        }
    }