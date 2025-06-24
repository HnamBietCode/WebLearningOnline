using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net.Http.Json;

namespace LearningManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly LMSContext _context;
        private readonly ICourseRepository _courseRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(
            LMSContext context,
            ICourseRepository courseRepository,
            IEnrollmentRepository enrollmentRepository,
            INotificationRepository notificationRepository,
            ILogger<HomeController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _courseRepository = courseRepository;
            _enrollmentRepository = enrollmentRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy thông tin người dùng (nếu đã đăng nhập)
                var userName = User.Identity.IsAuthenticated ? User.FindFirst(ClaimTypes.Name)?.Value : null;
                _logger.LogInformation($"Current userName: {userName}");

                User user = null;
                if (!string.IsNullOrEmpty(userName))
                {
                    user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.UserName == userName);
                    if (user != null && user.Role == null)
                    {
                        _logger.LogWarning($"User {userName} has an invalid RoleId: {user.RoleId}");
                    }
                }

                // Lấy danh sách khóa học với Comments để tính trung bình đánh giá
                var courses = await _context.Courses
                    .Include(c => c.Lessons)
                    .Include(c => c.Assignments)
                        .ThenInclude(a => a.Questions)
                            .ThenInclude(q => q.Options)
                    .Include(c => c.Comments)
                    .ToListAsync();

                var enrollments = userName != null
                    ? await _context.Enrollments
                        .Where(e => e.UserName == userName)
                        .ToListAsync()
                    : new List<Enrollment>();

                // Lấy danh sách CourseId trong giỏ hàng của user hiện tại từ Cart
                List<string> cartCourseIds = new List<string>();
                if (!string.IsNullOrEmpty(userName))
                {
                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.UserName == userName && c.Status == "Active");

                    if (cart != null)
                    {
                        cartCourseIds = cart.CartItems
                            .Select(ci => ci.CourseId)
                            .ToList();
                    }
                }

                var viewModel = new HomeViewModel
                {
                    User = user, // Đảm bảo User chứa Avatar nếu đã có trong database
                    Courses = courses.Select(c => new CourseListViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        Title = c.CourseName,
                        Description = c.Description,
                        CreatedDate = c.CreatedDate,
                        ImageUrl = c.ImageUrl,
                        Price = c.Price,
                        IsEnrolled = enrollments.Any(e => e.CourseId == c.CourseId),
                        Lessons = c.Lessons,
                        Assignments = c.Assignments,
                        AverageRating = c.Comments.Any()
                            ? c.Comments.Average(com => com.Rating ?? 0)
                            : (double?)null
                    }).ToList(),
                    Enrollments = enrollments,
                    CartCourseIds = cartCourseIds
                };

                // Lấy thông báo cho user hiện tại
                if (User.Identity.IsAuthenticated && !string.IsNullOrEmpty(userName))
                {
                    try
                    {
                        var notifications = await _notificationRepository.GetByUserNameAsync(userName);
                        ViewBag.UnreadCount = notifications?.Count(n => !n.IsRead) ?? 0;
                        ViewBag.Notifications = notifications?.OrderByDescending(n => n.CreatedDate).Take(3).ToList() ?? new List<Notification>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching notifications for user {userName}.");
                        ViewBag.UnreadCount = 0;
                        ViewBag.Notifications = new List<Notification>();
                    }
                }
                else
                {
                    ViewBag.UnreadCount = 0;
                    ViewBag.Notifications = new List<Notification>();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Home page.");
                return View("Error");
            }
        }
    }
}