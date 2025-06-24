using LearningManagementSystem.Models;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace LearningManagementSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationController(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return Unauthorized("Bạn cần đăng nhập để xem thông báo.");
            }

            var notifications = await _notificationRepository.GetByUserNameAsync(userName);
            return View(notifications.OrderByDescending(n => n.CreatedDate));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Thông báo không hợp lệ.";
                return RedirectToAction("Index");
            }

            var notification = await _notificationRepository.GetByIdAsync(id);
            if (notification == null)
            {
                TempData["Error"] = "Không tìm thấy thông báo.";
                return RedirectToAction("Index");
            }

            var userName = User.Identity.Name;
            if (notification.UserName != userName)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa thông báo này.";
                return RedirectToAction("Index");
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateAsync(notification);
            TempData["Success"] = "Đã đánh dấu thông báo là đã đọc.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
                return Unauthorized();

            var notifications = await _notificationRepository.GetByUserNameAsync(userName);
            var unread = notifications.Where(n => !n.IsRead).ToList();
            foreach (var n in unread)
                n.IsRead = true;
            if (unread.Any())
                await _notificationRepository.UpdateRangeAsync(unread);
            return RedirectToAction("Index");
        }

        // Phương thức để lấy thông báo cho dropdown (dùng trong _Layout.cshtml)
        [NonAction]
        public async Task PrepareNotificationData()
        {
            var userName = User.Identity.Name;
            if (!string.IsNullOrEmpty(userName))
            {
                var notifications = await _notificationRepository.GetByUserNameAsync(userName);
                ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
                ViewBag.Notifications = notifications.OrderByDescending(n => n.CreatedDate).Take(3).ToList();
            }
            else
            {
                ViewBag.UnreadCount = 0;
                ViewBag.Notifications = new List<Notification>();
            }
        }

        #region Quản lý Thông báo
        [HttpGet]
        public async Task<IActionResult> SendNotification(int page = 1)
        {
            // Lấy danh sách user không phải Admin
            var userRepo = HttpContext.RequestServices.GetService(typeof(LearningManagementSystem.Repositories.IUserRepository)) as LearningManagementSystem.Repositories.IUserRepository;
            var users = userRepo.GetAll().Where(u => u.Role != null && u.Role.RoleName != "Admin").ToList();
            ViewBag.UserNames = users.Select(u => u.UserName).ToList();

            // Lấy thông báo cho user hiện tại (dành cho layout hoặc header)
            if (User.Identity.IsAuthenticated)
            {
                var userName = User.Identity.Name;
                var notifications = await _notificationRepository.GetByUserNameAsync(userName);
                ViewBag.UnreadCount = notifications.Count(n => !n.IsRead);
                ViewBag.Notifications = notifications.OrderByDescending(n => n.CreatedDate).Take(3).ToList();
            }
            else
            {
                ViewBag.UnreadCount = 0;
                ViewBag.Notifications = new List<Notification>();
            }

            // Lấy tất cả thông báo đã gửi để hiển thị trong bảng (phân trang)
            var allNotifications = await _notificationRepository.GetAllNotificationsAsync();
            int pageSize = 10;
            int totalNotifications = allNotifications.Count;
            int totalPages = (int)Math.Ceiling((double)totalNotifications / pageSize);
            var pagedNotifications = allNotifications.OrderByDescending(n => n.CreatedDate)
                                                     .Skip((page - 1) * pageSize)
                                                     .Take(pageSize)
                                                     .ToList();
            ViewBag.AllNotifications = pagedNotifications;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Admin/Notification/SendNotification.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(string title, string content, string userName, bool sendToAll)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
            {
                TempData["Error"] = "Tiêu đề và nội dung không được để trống.";
                return RedirectToAction("SendNotification");
            }

            try
            {
                if (sendToAll)
                {
                    // Lấy tất cả user không phải Admin
                    var userRepo = HttpContext.RequestServices.GetService(typeof(LearningManagementSystem.Repositories.IUserRepository)) as LearningManagementSystem.Repositories.IUserRepository;
                    var users = userRepo.GetAll().Where(u => u.Role != null && u.Role.RoleName != "Admin").ToList();
                    var notifications = users.Select(user => new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserName = user.UserName,
                        Title = title,
                        Content = content,
                        CreatedDate = DateTime.Now,
                        IsRead = false
                    }).ToList();

                    await _notificationRepository.AddRangeAsync(notifications);
                    TempData["Success"] = "Thông báo đã được gửi đến tất cả người dùng (trừ Admin)!";
                }
                else
                {
                    if (string.IsNullOrEmpty(userName))
                    {
                        TempData["Error"] = "Vui lòng chọn người dùng để gửi thông báo.";
                        return RedirectToAction("SendNotification");
                    }
                    // Kiểm tra user có phải Admin không
                    var userRepo = HttpContext.RequestServices.GetService(typeof(LearningManagementSystem.Repositories.IUserRepository)) as LearningManagementSystem.Repositories.IUserRepository;
                    var user = userRepo.GetByUserName(userName);
                    if (user != null && user.Role != null && user.Role.RoleName == "Admin")
                    {
                        TempData["Error"] = "Không thể gửi thông báo cho tài khoản Admin.";
                        return RedirectToAction("SendNotification");
                    }

                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserName = userName,
                        Title = title,
                        Content = content,
                        CreatedDate = DateTime.Now,
                        IsRead = false
                    };

                    await _notificationRepository.AddAsync(notification);
                    TempData["Success"] = $"Thông báo đã được gửi đến {userName}!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi gửi thông báo: " + ex.Message;
            }

            return RedirectToAction("SendNotification");
        }
        #endregion
    }
}