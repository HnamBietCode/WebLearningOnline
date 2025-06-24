using LearningManagementSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LearningManagementSystem.Repositories
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task AddRangeAsync(IEnumerable<Notification> notifications);
        Task<List<Notification>> GetByUserNameAsync(string userName);

        Task<List<Notification>> GetAllNotificationsAsync();
        Task<Notification> GetByIdAsync(string notificationId);
        Task UpdateAsync(Notification notification);
        Task<List<string>> GetAllUserNamesAsync();
        Task UpdateRangeAsync(IEnumerable<Notification> notifications);
    }
}