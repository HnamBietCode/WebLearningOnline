using LearningManagementSystem.Models;

namespace LearningManagementSystem.Repositories
{
    public interface IProgressRepository
    {
        IQueryable<Progress> GetAll();
        Progress GetById(string id);
        Progress GetByUserAndLesson(string userName, string lessonId);
        void Add(Progress progress);
        void Update(Progress progress);
        void Delete(string id);
        Task SaveAsync();
    }
}
