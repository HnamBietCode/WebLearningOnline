using LearningManagementSystem.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningManagementSystem.Models.ViewModels
{
    public class UserProfileEditViewModel
    {
        public string UserName { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }

        public string Bio { get; set; } 

        public List<Course>? EnrolledCourses { get; set; }
        public List<UserCommentViewModel>? Comments { get; set; }
    }

    public class UserCommentViewModel
    {
        public string CourseTitle { get; set; }
        public string Content { get; set; }
        public DateTime CommentDate { get; set; }
    }
}