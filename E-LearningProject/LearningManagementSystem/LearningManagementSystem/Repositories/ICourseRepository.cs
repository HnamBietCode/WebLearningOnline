using LearningManagementSystem.Models;

namespace LearningManagementSystem.Repositories
{
    public interface ICourseRepository
    {
        IQueryable<Course> GetAll();
        IQueryable<Course> GetAllWithLessons(); 
        Course GetById(string id);
        Course GetByIdUser(string id);
        void Add(Course course);
        void Update(Course course);
        void Delete(string id);
        void Save();

        Task<Course> GetCourseByIdAsync(string courseId);
    }
}
