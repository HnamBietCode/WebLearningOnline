using LearningManagementSystem.Models;

namespace LearningManagementSystem.Repositories
{
    public interface ICommentRepository
    {
        IQueryable<Comment> GetAll();
        Comment GetById(string id);
        void Add(Comment comment);
        void Update(Comment comment);
        void Delete(string id);
        void Save();
    }
}
