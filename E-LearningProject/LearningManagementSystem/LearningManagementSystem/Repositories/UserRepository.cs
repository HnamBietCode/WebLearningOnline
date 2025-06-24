using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LearningManagementSystem.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LMSContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserRepository(LMSContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IQueryable<User> GetAll()
        {
            return _context.Users
                .Include(u => u.Role) // Sửa từ Roles thành Role
                .Include(u => u.Comments)
                .Include(u => u.Enrollments)
                .Include(u => u.Progresses)
                .AsQueryable();
        }

        public User GetByUserName(string userName)
        {
            return _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserName == userName);
        }
        public User GetByEmail(string email)
        {
            return _context.Users.Include(u => u.Role)
                                 .FirstOrDefault(u => u.Email == email);
        }
        public void Add(User user)
        {
            _context.Users.Add(user);
        }

        public void Update(User updatedUser)
        {

            // Lấy entity từ database và đảm bảo nó được theo dõi
            var user = _context.Users.FirstOrDefault(u => u.UserName == updatedUser.UserName);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            // Cập nhật các thuộc tính
            _context.Entry(user).CurrentValues.SetValues(updatedUser);

            // Không gọi SaveChanges() ở đây vì bạn đã gọi _userRepository.Save() trong action
        }

        public async Task Delete(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                throw new ArgumentException("Tên đăng nhập không được để trống.", nameof(userName));

            string normalizedUserName = userName.Trim().ToLower();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName.Trim().ToLower() == normalizedUserName);
                if (user == null)
                    throw new KeyNotFoundException($"Người dùng {userName} không tồn tại trong hệ thống.");

                // Xóa các AssignmentSubmissions liên quan
                var submissions = await _context.AssignmentSubmissions
                    .Where(s => s.UserName.Trim().ToLower() == normalizedUserName)
                    .ToListAsync();
                if (submissions.Any())
                    _context.AssignmentSubmissions.RemoveRange(submissions);

                // Xóa Cart và CartItems liên quan
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserName.Trim().ToLower() == normalizedUserName);
                if (cart != null)
                {
                    if (cart.CartItems.Any())
                        _context.CartItems.RemoveRange(cart.CartItems);
                    _context.Carts.Remove(cart);
                }

                // Xóa các Enrollments liên quan
                var enrollments = await _context.Enrollments
                    .Where(e => e.UserName.Trim().ToLower() == normalizedUserName)
                    .ToListAsync();
                if (enrollments.Any())
                    _context.Enrollments.RemoveRange(enrollments);

                // Xóa các Comments liên quan
                var comments = await _context.Comments
                    .Where(c => c.UserName.Trim().ToLower() == normalizedUserName)
                    .ToListAsync();
                if (comments.Any())
                    _context.Comments.RemoveRange(comments);

                // Xóa các Notifications liên quan
                var notifications = await _context.Notifications
                    .Where(n => n.UserName.Trim().ToLower() == normalizedUserName)
                    .ToListAsync();
                if (notifications.Any())
                    _context.Notifications.RemoveRange(notifications);

                // Xóa các CourseInstructors liên quan
                var courseInstructors = await _context.CourseInstructors
                    .Where(ci => ci.UserName.Trim().ToLower() == normalizedUserName)
                    .ToListAsync();
                if (courseInstructors.Any())
                    _context.CourseInstructors.RemoveRange(courseInstructors);

                // Xóa User
                _context.Users.Remove(user);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}