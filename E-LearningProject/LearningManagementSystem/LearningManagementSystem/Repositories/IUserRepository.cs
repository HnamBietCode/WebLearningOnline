using LearningManagementSystem.Models;

namespace LearningManagementSystem.Repositories
{
    public interface IUserRepository
    {
        IQueryable<User> GetAll();
        User GetByUserName(string userName);

        User GetByEmail(string email);
        void Add(User user);
        void Update(User user);
        Task Delete(string userName);
        void Save();
    }
}
