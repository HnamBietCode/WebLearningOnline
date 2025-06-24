    using System.ComponentModel.DataAnnotations;
    using System.Collections.Generic;

    namespace LearningManagementSystem.Models
    {
        public class Course
        {
            [Key]
            [Required]
            [StringLength(50)]
            public string CourseId { get; set; }

            [Required]
            [StringLength(100)]
            public string CourseName { get; set; }

            [Required]
            [StringLength(1000)]
            public string Description { get; set; }

            [Required]  
            public DateTime CreatedDate { get; set; }

            [StringLength(200)]
            public string? ImageUrl { get; set; }

            [Range(0, double.MaxValue)]
            public decimal? Price { get; set; } // Giá tiền, có thể NULL (miễn phí)

            // Navigation properties
            public List<Lesson>? Lessons { get; set; }
            public List<Enrollment>? Enrollments { get; set; }
            public List<Comment>? Comments { get; set; }
            public List<Assignment>? Assignments { get; set; }
            public List<Payment>? Payments { get; set; }
            public List<CourseInstructor>? CourseInstructors { get; set; }
            public List<CartItem>? CartItems { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }

    }
    }