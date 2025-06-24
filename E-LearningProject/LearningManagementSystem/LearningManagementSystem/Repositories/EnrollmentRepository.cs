using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly LMSContext _context;

        public EnrollmentRepository(LMSContext context)
        {
            _context = context;
        }

        public IQueryable<Enrollment> GetAll()
        {
            return _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();
        }

        public Enrollment GetById(string id)
        {
            return _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .FirstOrDefault(e => e.EnrollmentId == id);
        }

        public Enrollment GetEnrollment(string userName, string courseId)
        {
            return _context.Enrollments
                .FirstOrDefault(e => e.UserName == userName && e.CourseId == courseId);
        }

        public IQueryable<Enrollment> GetEnrollmentsByUser(string userName)
        {
            return _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Where(e => e.UserName == userName)
                .AsQueryable();
        }

        public void Add(Enrollment enrollment)
        {
            _context.Enrollments.Add(enrollment);
        }

        public void Update(Enrollment updatedEnrollment)
        {
          
            var enrollment = _context.Enrollments.FirstOrDefault(e => e.EnrollmentId == updatedEnrollment.EnrollmentId);
            if (enrollment == null)
            {
                throw new Exception("Enrollment not found.");
            }
            _context.Entry(enrollment).CurrentValues.SetValues(updatedEnrollment);
        }
        public void Delete(string enrollmentId)
        {
            var enrollment = GetById(enrollmentId);
            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public bool Enroll(string userName, string courseId)
        {
            // Kiểm tra xem người dùng và khóa học có tồn tại không
            var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
            var course = _context.Courses.FirstOrDefault(c => c.CourseId == courseId);
            if (user == null || course == null)
            {
                return false;
            }

            // Kiểm tra xem người dùng đã đăng ký khóa học này chưa
            var existingEnrollment = GetEnrollment(userName, courseId);
            if (existingEnrollment != null)
            {
                return false; // Đã đăng ký rồi
            }

            // Thêm bản ghi đăng ký
            var enrollment = new Enrollment
            {
                EnrollmentId = Guid.NewGuid().ToString(), // Tạo ID ngẫu nhiên
                UserName = userName,
                CourseId = courseId,
                EnrollmentDate = DateTime.Now
            };
            Add(enrollment);
            Save();
            return true;
        }

        public bool Unenroll(string userName, string courseId)
        {
            var enrollment = GetEnrollment(userName, courseId);
            if (enrollment == null)
            {
                return false;
            }

      
            Delete(enrollment.EnrollmentId);
            Save();
            return true;
        }

        public List<Course> GetEnrolledCourses(string userName)
        {
            return _context.Enrollments
                .Where(e => e.UserName == userName)
                .Include(e => e.Course) // Bao gồm Course để lấy thông tin khóa học
                .Select(e => e.Course)
                .ToList();
        }

        public bool IsEnrolled(string userName, string courseId)
        {
            return _context.Enrollments
                .Any(e => e.UserName == userName && e.CourseId == courseId);
        }
    }
}