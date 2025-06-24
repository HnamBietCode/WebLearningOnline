using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace LearningManagementSystem.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ICourseRepository _courseRepository;
        private readonly ICommentRepository _commentRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly LMSContext _context;
        private readonly INotificationRepository _notificationRepository;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentQuestionRepository _questionRepository;
        private readonly IAssignmentQuestionOptionRepository _optionRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ICourseRepository courseRepository,
            ICommentRepository commentRepository,
            IEnrollmentRepository enrollmentRepository,
            IUserRepository userRepository,
            ILessonRepository lessonRepository,
            LMSContext context,
            INotificationRepository notificationRepository,
            IAssignmentRepository assignmentRepository,
            IAssignmentQuestionRepository questionRepository,
            IAssignmentQuestionOptionRepository optionRepository,
            IPasswordHasher<User> passwordHasher,
            ILogger<AdminController> logger)
        {
            _courseRepository = courseRepository;
            _commentRepository = commentRepository;
            _enrollmentRepository = enrollmentRepository;
            _userRepository = userRepository;
            _assignmentRepository = assignmentRepository;
            _notificationRepository = notificationRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _lessonRepository = lessonRepository;
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        #region Dashboard

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Lấy năm hiện tại
                int currentYear = DateTime.Now.Year;

                // Tính tổng số tiền theo tháng
                var monthlyPayments = await _context.Payments
                    .Where(p => p.PaymentStatus == "Completed" && p.PaymentDate.Year == currentYear)
                    .GroupBy(p => p.PaymentDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        Total = g.Sum(p => p.Amount)
                    })
                    .ToDictionaryAsync(
                        x => $"Tháng {x.Month}",
                        x => x.Total
                    );

                // Tạo view model
                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = _userRepository.GetAll().Count(),
                    TotalCourses = _courseRepository.GetAll().Count(),
                    TotalComments = _commentRepository.GetAll().Count(),
                    TotalEnrollments = _enrollmentRepository.GetAll().Count(),
                    TotalPayments = await _context.Payments
                        .Where(p => p.PaymentStatus == "Completed")
                        .SumAsync(p => p.Amount),
                    MonthlyPayments = monthlyPayments
                };

                // Khởi tạo dữ liệu cho tất cả các tháng (1-12) nếu không có giao dịch
                for (int i = 1; i <= 12; i++)
                {
                    string monthKey = $"Tháng {i}";
                    if (!viewModel.MonthlyPayments.ContainsKey(monthKey))
                    {
                        viewModel.MonthlyPayments[monthKey] = 0;
                    }
                }

                // Sắp xếp theo thứ tự tháng
                viewModel.MonthlyPayments = viewModel.MonthlyPayments
                    .OrderBy(x => int.Parse(x.Key.Split(' ')[1]))
                    .ToDictionary(x => x.Key, x => x.Value);

                return View("~/Views/Admin/Dashboard.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Admin Dashboard.");
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang Dashboard. Vui lòng thử lại.";
                return RedirectToAction("ManageCourses");
            }
        }

        #endregion

        #region Quản lý bình luận (Comment Management)

        public IActionResult ManageComments(int page = 1)
        {
            _logger.LogInformation("ManageComments called.");
            int pageSize = 10;
            var allComments = _commentRepository.GetAll().OrderByDescending(c => c.CreatedDate).ToList();
            int totalComments = allComments.Count();
            int totalPages = (int)Math.Ceiling((double)totalComments / pageSize);
            var pagedComments = allComments.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View("~/Views/Admin/Comment/ManageComments.cshtml", pagedComments);
        }

        // POST: Admin/DeleteComment/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(string id)
        {
            _logger.LogInformation($"DeleteComment called with CommentId: {id}");

            var comment = _commentRepository.GetById(id);
            if (comment == null)
            {
                _logger.LogWarning($"Comment with CommentId: {id} not found.");
                TempData["Error"] = "Bình luận không tồn tại.";
                return RedirectToAction("ManageComments");
            }

            try
            {
                _commentRepository.Delete(id);
                _commentRepository.Save();
                TempData["Success"] = "Xóa bình luận thành công.";
                _logger.LogInformation($"Comment {id} deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting comment: {id}");
                TempData["Error"] = "Đã xảy ra lỗi khi xóa bình luận. Vui lòng thử lại.";
            }

            return RedirectToAction("ManageComments");
        }

        #endregion

        #region Quản lý người dùng (User Management)

        // GET: Admin/ManageUsers
        public IActionResult ManageUsers(int page = 1)
        {
            int pageSize = 10;
            var allUsers = _userRepository.GetAll().OrderBy(u => u.UserName).ToList();
            int totalUsers = allUsers.Count();
            int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            var pagedUsers = allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            return View("~/Views/Admin/User/ManageUsers.cshtml", pagedUsers);
        }

        // GET: Admin/CreateUser
        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            try
            {
                ViewBag.Roles = _context.Roles.ToList();
                return View("~/Views/Admin/User/CreateUser.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles in CreateUser.");
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách vai trò. Vui lòng thử lại.";
                return RedirectToAction("ManageUsers");
            }
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User model, string password, IFormFile AvatarFile)
        {
            // Gán giá trị tạm thời cho Password để vượt qua kiểm tra ModelState
            model.Password = password ?? string.Empty;

            // Tắt validation cho các trường không cần kiểm tra
            ModelState.Remove("AvatarFile"); // Vì đây là IFormFile, không cần validate qua ModelState

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra xem UserName đã tồn tại chưa
                    var existingUser = _userRepository.GetByUserName(model.UserName);
                    if (existingUser != null)
                    {
                        ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                        ViewBag.Roles = _context.Roles.ToList();
                        return View("~/Views/Admin/User/CreateUser.cshtml", model);
                    }

                    // Kiểm tra email đã tồn tại chưa
                    var existingEmail = _userRepository.GetByEmail(model.Email);
                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        ViewBag.Roles = _context.Roles.ToList();
                        return View("~/Views/Admin/User/CreateUser.cshtml", model);
                    }

                    // Kiểm tra mật khẩu
                    if (string.IsNullOrEmpty(password))
                    {
                        ModelState.AddModelError("password", "Mật khẩu không được để trống.");
                        ViewBag.Roles = _context.Roles.ToList();
                        return View("~/Views/Admin/User/CreateUser.cshtml", model);
                    }

                    // Kiểm tra RoleId có hợp lệ không
                    var role = _context.Roles.FirstOrDefault(r => r.RoleId == model.RoleId);
                    if (role == null)
                    {
                        ModelState.AddModelError("RoleId", "Vai trò không hợp lệ.");
                        ViewBag.Roles = _context.Roles.ToList();
                        return View("~/Views/Admin/User/CreateUser.cshtml", model);
                    }

                    // Xử lý upload avatar
                    if (AvatarFile != null && AvatarFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars", fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await AvatarFile.CopyToAsync(stream);
                        }
                        model.Avatar = "/avatars/" + fileName;
                    }
                    else
                    {
                        // Nếu không có avatar được tải lên, gán avatar mặc định
                        model.Avatar = "/images/defaultAvatar.png";
                    }

                    // Băm mật khẩu
                    model.HashPassword(_passwordHasher, password);

                    // Thêm người dùng
                    _userRepository.Add(model);
                    _userRepository.Save();

                    TempData["Success"] = "Thêm người dùng thành công.";
                    return RedirectToAction("ManageUsers");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating user: {model.UserName}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi thêm người dùng. Vui lòng thử lại.");
                }
            }
            else
            {
                // Ghi log các lỗi trong ModelState
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState is invalid in CreateUser. Errors: {0}", string.Join(", ", errors));
            }

            ViewBag.Roles = _context.Roles.ToList();
            return View("~/Views/Admin/User/CreateUser.cshtml", model);
        }

        // GET: Admin/EditUser/{userName}
        // GET: Admin/EditUser/{userName}
        public IActionResult EditUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("EditUser called with null or empty userName.");
                return NotFound();
            }

            try
            {
                var user = _userRepository.GetByUserName(userName);
                if (user == null)
                {
                    _logger.LogWarning($"User with UserName: {userName} not found.");
                    return NotFound();
                }
                ViewBag.Roles = _context.Roles.ToList();
                return View("~/Views/Admin/User/EditUser.cshtml", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user: {userName}");
                TempData["Error"] = "Đã xảy ra lỗi khi lấy thông tin người dùng. Vui lòng thử lại.";
                return RedirectToAction("ManageUsers");
            }
        }

        // POST: Admin/EditUser/{userName}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string userName, User model, string password, IFormFile AvatarFile)
        {
            if (string.IsNullOrEmpty(userName) || userName != model.UserName)
            {
                _logger.LogWarning($"UserName mismatch: {userName} != {model.UserName}");
                return NotFound();
            }

            try
            {
                var user = _userRepository.GetByUserName(userName);
                if (user == null)
                {
                    _logger.LogWarning($"User with UserName: {userName} not found.");
                    return NotFound();
                }

                // Cập nhật các thuộc tính từ model
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.RoleId = model.RoleId;
                user.Bio = model.Bio;

                // Kiểm tra email trùng (nếu email thay đổi)
                if (user.Email != model.Email)
                {
                    var existingEmail = _userRepository.GetByEmail(model.Email);
                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        ViewBag.Roles = _context.Roles.ToList();
                        return View("~/Views/Admin/User/EditUser.cshtml", user);
                    }
                }

                // Xử lý upload avatar
                if (AvatarFile != null && AvatarFile.Length > 0)
                {
                    // Xóa avatar cũ nếu có
                    if (!string.IsNullOrEmpty(user.Avatar))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Lưu avatar mới
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AvatarFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await AvatarFile.CopyToAsync(stream);
                    }
                    user.Avatar = "/avatars/" + fileName;
                }

                // Nếu có mật khẩu mới, băm và cập nhật
                if (!string.IsNullOrEmpty(password))
                {
                    user.HashPassword(_passwordHasher, password);
                }

                // Xóa ModelState cũ và tái xác thực
                ModelState.Clear();
                TryValidateModel(user);

                if (ModelState.IsValid)
                {
                    try
                    {
                        // Kiểm tra RoleId có hợp lệ không
                        var role = _context.Roles.FirstOrDefault(r => r.RoleId == model.RoleId);
                        if (role == null)
                        {
                            _logger.LogWarning($"Invalid RoleId: {model.RoleId}");
                            TempData["Error"] = "Vai trò không hợp lệ.";
                            ViewBag.Roles = _context.Roles.ToList();
                            return View("~/Views/Admin/User/EditUser.cshtml", user);
                        }

                        _userRepository.Update(user);
                        _userRepository.Save();
                        TempData["Success"] = "Chỉnh sửa người dùng thành công.";
                        _logger.LogInformation($"User {user.UserName} updated successfully.");
                        return RedirectToAction("ManageUsers");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error updating user: {userName}");
                        TempData["Error"] = "Đã xảy ra lỗi khi chỉnh sửa người dùng: " + ex.Message;
                    }
                }
                else
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("ModelState is invalid in EditUser. Errors: {0}", string.Join(", ", errors));
                    TempData["Error"] = "Vui lòng kiểm tra lại thông tin: " + string.Join(", ", errors);
                }

                ViewBag.Roles = _context.Roles.ToList();
                return View("~/Views/Admin/User/EditUser.cshtml", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user for edit: {userName}");
                TempData["Error"] = "Đã xảy ra lỗi khi lấy thông tin người dùng: " + ex.Message;
                return RedirectToAction("ManageUsers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("DeleteUser called with null or empty userName.");
                TempData["Error"] = "Tên đăng nhập không hợp lệ.";
                return RedirectToAction("ManageUsers");
            }

            try
            {
                var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
                if (string.Equals(userName, currentUserName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"User {userName} attempted to delete themselves.");
                    TempData["Error"] = "Bạn không thể xóa chính tài khoản của mình.";
                    return RedirectToAction("ManageUsers");
                }

                // Lấy thông tin người dùng để kiểm tra AvatarUrl
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName.Trim().ToLower() == userName.Trim().ToLower());
                if (user == null)
                {
                    _logger.LogWarning($"User {userName} not found.");
                    TempData["Error"] = $"Người dùng {userName} không tồn tại.";
                    return RedirectToAction("ManageUsers");
                }

                // Xóa ảnh đại diện (nếu có và không phải ảnh mặc định)
                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar != "/avatars/default-avatar.jpg")
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars", user.Avatar.TrimStart('/').Replace("avatars/", ""));
                    _logger.LogInformation($"Attempting to delete avatar at path: {avatarPath}");

                    if (System.IO.File.Exists(avatarPath))
                    {
                        System.IO.File.Delete(avatarPath);
                        _logger.LogInformation($"Avatar deleted successfully: {avatarPath}");
                    }
                    else
                    {
                        _logger.LogWarning($"Avatar file not found: {avatarPath}");
                    }
                }
                else if (user.Avatar == "/avatars/default-avatar.jpg")
                {
                    _logger.LogInformation($"Skipping deletion of default avatar: {user.Avatar}");
                }

                // Gọi phương thức Delete trong repository
                await _userRepository.Delete(userName);

                _logger.LogInformation($"User {userName} deleted successfully.");
                TempData["Success"] = $"Xóa người dùng {userName} thành công.";
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {userName}. Inner exception: {ex.InnerException?.Message}");
                var errorMessage = ex.Message.Contains("ràng buộc khóa ngoại") || ex.InnerException?.Message.Contains("REFERENCE constraint") == true
                    ? "Không thể xóa người dùng do còn dữ liệu liên quan trong hệ thống."
                    : $"Đã xảy ra lỗi khi xóa người dùng: {ex.Message}";
                TempData["Error"] = errorMessage;
            }

            return RedirectToAction("ManageUsers");
        }

        #endregion

    }
}