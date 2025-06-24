using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class CourseRepository : ICourseRepository
    {
        private readonly LMSContext _context;
        private readonly ILogger<CourseRepository> _logger;

        public CourseRepository(LMSContext context, ILogger<CourseRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IQueryable<Course> GetAll()
        {
            _logger.LogInformation("Fetching all courses without lessons.");
            return _context.Courses.AsQueryable();
        }

        public IQueryable<Course> GetAllWithLessons()
        {
            _logger.LogInformation("Fetching all courses with lessons.");
            return _context.Courses
                .Include(c => c.Lessons)
                .AsQueryable();
        }

        public Course GetById(string id)
        {
            _logger.LogInformation($"Fetching course with ID: {id}");
            var course = _context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefault(c => c.CourseId == id);

            if (course == null)
            {
                _logger.LogWarning($"Course with ID: {id} not found.");
            }
            return course;
        }
        public Course GetByIdUser(string id)
        {
            _logger.LogInformation($"Fetching course with ID: {id}");
            var course = _context.Courses
                .Include(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.User)
                        .ThenInclude(u => u.Role)
                .FirstOrDefault(c => c.CourseId == id);

            if (course == null)
            {
                _logger.LogWarning($"Course with ID: {id} not found.");
            }
            else
            {
                // Logging để kiểm tra dữ liệu CourseInstructors
                if (course.CourseInstructors != null && course.CourseInstructors.Any())
                {
                    _logger.LogInformation($"Course {id} has {course.CourseInstructors.Count} instructors.");
                    foreach (var ci in course.CourseInstructors)
                    {
                        _logger.LogInformation($"Instructor for course {id}: {ci.UserName} - {ci.User?.FullName}");
                    }
                }
                else
                {
                    _logger.LogInformation($"No instructors assigned to course {id}.");
                }
            }

            return course;
        }

        public void Add(Course course)
        {
            _logger.LogInformation($"Adding course: {course.CourseName}, CourseId: {course.CourseId}");
            _context.Courses.Add(course);
        }

        public void Update(Course updatedCourse)
        {
            _logger.LogInformation($"Updating course: {updatedCourse.CourseName}, CourseId: {updatedCourse.CourseId}");

            // Lấy entity từ database và đảm bảo nó được theo dõi
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == updatedCourse.CourseId);
            if (course == null)
            {
                _logger.LogWarning($"Course with CourseId: {updatedCourse.CourseId} not found.");
                throw new Exception("Course not found.");
            }

            // Cập nhật các thuộc tính
            _context.Entry(course).CurrentValues.SetValues(updatedCourse);

            // Lưu thay đổi
            try
            {
                _context.SaveChanges();
                _logger.LogInformation($"Course {course.CourseId} updated successfully in database.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving changes for course: {course.CourseId}");
                throw; // Ném lại ngoại lệ để action có thể xử lý
            }
        }

        public void Delete(string id)
        {
            _logger.LogInformation($"Deleting course with ID: {id}");

            // Lấy course cần xóa
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == id);
            if (course == null)
            {
                _logger.LogWarning($"Course with ID: {id} not found for deletion.");
                return;
            }

            try
            {
                // 1. Xóa các OrderDetails trực tiếp liên quan đến Course
                var orderDetails = _context.OrderDetails.Where(od => od.CourseId == id).ToList();
                if (orderDetails.Any())
                {
                    _logger.LogInformation($"Removing {orderDetails.Count} order details for course {id}");
                    _context.OrderDetails.RemoveRange(orderDetails);
                    _context.SaveChanges();
                }

                // 2. Xóa các AssignmentSubmissions
                var lessons = _context.Lessons.Where(l => l.CourseId == id).ToList();
                foreach (var lesson in lessons)
                {
                    var assignments = _context.Assignments.Where(a => a.LessonId == lesson.LessonId).ToList();
                    foreach (var assignment in assignments)
                    {
                        var questions = _context.AssignmentQuestions.Where(q => q.AssignmentId == assignment.AssignmentId).ToList();
                        foreach (var question in questions)
                        {
                            var submissions = _context.AssignmentSubmissions.Where(s => s.QuestionId == question.QuestionId).ToList();
                            if (submissions.Any())
                            {
                                _logger.LogInformation($"Removing {submissions.Count} submissions for question {question.QuestionId}");
                                _context.AssignmentSubmissions.RemoveRange(submissions);
                                _context.SaveChanges();
                            }
                        }
                    }
                }

                // 3. Xóa các AssignmentQuestionOptions
                foreach (var lesson in lessons)
                {
                    var assignments = _context.Assignments.Where(a => a.LessonId == lesson.LessonId).ToList();
                    foreach (var assignment in assignments)
                    {
                        var questions = _context.AssignmentQuestions.Where(q => q.AssignmentId == assignment.AssignmentId).ToList();
                        foreach (var question in questions)
                        {
                            var options = _context.AssignmentQuestionOptions.Where(o => o.QuestionId == question.QuestionId).ToList();
                            if (options.Any())
                            {
                                _logger.LogInformation($"Removing {options.Count} options for question {question.QuestionId}");
                                _context.AssignmentQuestionOptions.RemoveRange(options);
                                _context.SaveChanges();
                            }
                        }
                    }
                }

                // 4. Xóa các AssignmentQuestions
                foreach (var lesson in lessons)
                {
                    var assignments = _context.Assignments.Where(a => a.LessonId == lesson.LessonId).ToList();
                    foreach (var assignment in assignments)
                    {
                        var questions = _context.AssignmentQuestions.Where(q => q.AssignmentId == assignment.AssignmentId).ToList();
                        if (questions.Any())
                        {
                            _logger.LogInformation($"Removing {questions.Count} questions for assignment {assignment.AssignmentId}");
                            _context.AssignmentQuestions.RemoveRange(questions);
                            _context.SaveChanges();
                        }
                    }
                }

                // 5. Xóa các Assignments
                foreach (var lesson in lessons)
                {
                    var assignments = _context.Assignments.Where(a => a.LessonId == lesson.LessonId).ToList();
                    if (assignments.Any())
                    {
                        _logger.LogInformation($"Removing {assignments.Count} assignments for lesson {lesson.LessonId}");
                        _context.Assignments.RemoveRange(assignments);
                        _context.SaveChanges();
                    }
                }

                // 6. Xóa các Lessons
                if (lessons.Any())
                {
                    _logger.LogInformation($"Removing {lessons.Count} lessons for course {id}");
                    _context.Lessons.RemoveRange(lessons);
                    _context.SaveChanges();
                }

                // 7. Xóa các CourseInstructors
                var courseInstructors = _context.CourseInstructors.Where(ci => ci.CourseId == id).ToList();
                if (courseInstructors.Any())
                {
                    _logger.LogInformation($"Removing {courseInstructors.Count} course instructors for course {id}");
                    _context.CourseInstructors.RemoveRange(courseInstructors);
                    _context.SaveChanges();
                }

                // 8. Xóa các Comments
                var comments = _context.Comments.Where(c => c.CourseId == id).ToList();
                if (comments.Any())
                {
                    _logger.LogInformation($"Removing {comments.Count} comments for course {id}");
                    _context.Comments.RemoveRange(comments);
                    _context.SaveChanges();
                }

                // 9. Xóa các Enrollments
                var enrollments = _context.Enrollments.Where(e => e.CourseId == id).ToList();
                if (enrollments.Any())
                {
                    _logger.LogInformation($"Removing {enrollments.Count} enrollments for course {id}");
                    _context.Enrollments.RemoveRange(enrollments);
                    _context.SaveChanges();
                }

                // 10. Xóa các Payments và OrderDetails liên quan
                var payments = _context.Payments.Where(p => p.CourseId == id).ToList();
                var paymentIds = payments.Select(p => p.PaymentId).ToList();
                if (paymentIds.Any())
                {
                    var relatedOrderDetails = _context.OrderDetails.Where(od => paymentIds.Contains(od.PaymentId)).ToList();
                    if (relatedOrderDetails.Any())
                    {
                        _logger.LogInformation($"Removing {relatedOrderDetails.Count} related order details for course {id}");
                        _context.OrderDetails.RemoveRange(relatedOrderDetails);
                        _context.SaveChanges();
                    }

                    _logger.LogInformation($"Removing {payments.Count} payments for course {id}");
                    _context.Payments.RemoveRange(payments);
                    _context.SaveChanges();
                }

                // 11. Xóa các CartItems
                var cartItems = _context.CartItems.Where(ci => ci.CourseId == id).ToList();
                if (cartItems.Any())
                {
                    _logger.LogInformation($"Removing {cartItems.Count} cart items for course {id}");
                    _context.CartItems.RemoveRange(cartItems);
                    _context.SaveChanges();
                }

                // 12. Cuối cùng, xóa Course
                _logger.LogInformation($"Removing course {id}");
                _context.Courses.Remove(course);
                _context.SaveChanges();

                _logger.LogInformation($"Course with ID: {id} and all related data deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting course with ID: {id}. Details: {ex.Message}");
                throw;
            }
        }

        public void Save()
        {
            _logger.LogInformation("Saving changes to database.");
            _context.SaveChanges();
        }

        public async Task<Course> GetCourseByIdAsync(string courseId)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseId == courseId);
        }
    }
}