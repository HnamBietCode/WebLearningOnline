using LearningManagementSystem.Models;

namespace LearningManagementSystem.Repositories
{
    public interface IEnrollmentRepository
    {
        IQueryable<Enrollment> GetAll();
        Enrollment GetById(string id);
        Enrollment GetEnrollment(string userName, string courseId);
        IQueryable<Enrollment> GetEnrollmentsByUser(string userName);
        void Add(Enrollment enrollment);
        void Update(Enrollment enrollment);
        void Delete(string id);
        void Save();

        bool Enroll(string userName, string courseId);
        bool Unenroll(string userName, string courseId);
        List<Course> GetEnrolledCourses(string userName);
        bool IsEnrolled(string userName, string courseId);
    }
}
