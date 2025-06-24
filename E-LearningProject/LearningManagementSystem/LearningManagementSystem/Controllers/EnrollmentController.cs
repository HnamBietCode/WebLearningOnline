using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace LearningManagementSystem.Controllers
{
    public class EnrollmentController : Controller
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly LMSContext _context;
        private readonly ILogger<EnrollmentController> _logger;

        public EnrollmentController(
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            INotificationRepository notificationRepository,
            LMSContext context,
            ILogger<EnrollmentController> logger)
        {
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _notificationRepository = notificationRepository;
            _context = context;
            _logger = logger;
        }

        // POST: Enrollment/Enroll
        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> Enroll(string courseId)
        {
            _logger.LogInformation($"Enroll called with CourseId: {courseId}");

            try
            {
                if (string.IsNullOrWhiteSpace(courseId))
                {
                    _logger.LogWarning("Invalid CourseId.");
                    TempData["Error"] = "ID khóa học không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                var course = _courseRepository.GetById(courseId);
                if (course == null)
                {
                    _logger.LogWarning($"Course with CourseId: {courseId} not found.");
                    TempData["Error"] = "Khóa học không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                var userName = User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    _logger.LogError("UserName could not be determined. User might not be authenticated.");
                    TempData["Error"] = "Không thể xác định người dùng. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                // Lấy thông tin người dùng từ cơ sở dữ liệu
                var user = _context.Users.FirstOrDefault(u => u.UserName == userName);
                if (user == null)
                {
                    _logger.LogWarning($"User {userName} not found.");
                    TempData["Error"] = "Người dùng không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                // Đăng ký khóa học
                var success = _enrollmentRepository.Enroll(userName, courseId);
                if (success)
                {
                    // Tạo thông báo
                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserName = userName,
                        Title = "Đăng ký khóa học thành công",
                        Content = $"Bạn đã đăng ký khóa học '{course.CourseName}' thành công!",
                        CreatedDate = DateTime.Now,
                        IsRead = false
                    };
                    await _notificationRepository.AddAsync(notification);

                    // Lưu tất cả thay đổi (đăng ký và thông báo)
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error saving changes for User {userName} after enrolling in CourseId: {courseId}. InnerException: {ex.InnerException?.Message}");
                        TempData["Error"] = "Đăng ký thành công nhưng lỗi khi lưu thông tin. Vui lòng kiểm tra lại.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Cập nhật ViewBag để hiển thị thông báo trong dropdown chuông
                    try
                    {
                        var notifications = await _notificationRepository.GetByUserNameAsync(userName);
                        ViewBag.UnreadCount = notifications?.Count(n => !n.IsRead) ?? 0;
                        ViewBag.Notifications = notifications?.OrderByDescending(n => n.CreatedDate).Take(3).ToList() ?? new List<Notification>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching notifications for User {userName}.");
                        ViewBag.UnreadCount = 0;
                        ViewBag.Notifications = new List<Notification>();
                    }

                    _logger.LogInformation($"User {userName} successfully enrolled in CourseId: {courseId}.");
                    TempData["Success"] = $"Đăng ký khóa học '{course.CourseName}' thành công!{(course.Price > 0 ? "" : " Khóa học này miễn phí.")}";
                }
                else
                {
                    _logger.LogWarning($"User {userName} has already enrolled in CourseId: {courseId} or enrollment failed.");
                    TempData["Error"] = "Bạn đã đăng ký khóa học này rồi hoặc đăng ký thất bại.";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while enrolling: {ex.Message}, InnerException: {ex.InnerException?.Message}");
                TempData["Error"] = $"Đã có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Enrollment/Unenroll
        [HttpPost]
        public async Task<IActionResult> Unenroll(string courseId)
        {
            _logger.LogInformation($"Unenroll called with CourseId: {courseId}");

            try
            {
                if (string.IsNullOrWhiteSpace(courseId))
                {
                    _logger.LogWarning("Invalid CourseId.");
                    TempData["Error"] = "ID khóa học không hợp lệ.";
                    return RedirectToAction("Index", "Home");
                }

                var course = _courseRepository.GetById(courseId);
                if (course == null)
                {
                    _logger.LogWarning($"Course with CourseId: {courseId} not found.");
                    TempData["Error"] = "Khóa học không tồn tại.";
                    return RedirectToAction("Index", "Home");
                }

                var userName = User.Identity.Name;
                if (string.IsNullOrWhiteSpace(userName))
                {
                    _logger.LogError("UserName could not be determined. User might not be authenticated.");
                    TempData["Error"] = "Không thể xác định người dùng. Vui lòng đăng nhập lại.";
                    return RedirectToAction("Login", "Account");
                }

                // Hủy đăng ký khóa học
                var success = _enrollmentRepository.Unenroll(userName, courseId);
                if (success)
                {
                    // Tạo thông báo hủy đăng ký
                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid().ToString(),
                        UserName = userName,
                        Title = "Hủy đăng ký khóa học",
                        Content = $"Bạn đã hủy đăng ký khóa học '{course.CourseName}'.",
                        CreatedDate = DateTime.Now,
                        IsRead = false
                    };
                    await _notificationRepository.AddAsync(notification);

                    // Lưu thay đổi (hủy đăng ký và thông báo)
                    await _context.SaveChangesAsync();

                    // Cập nhật ViewBag để hiển thị thông báo trong dropdown chuông
                    try
                    {
                        var notifications = await _notificationRepository.GetByUserNameAsync(userName);
                        ViewBag.UnreadCount = notifications?.Count(n => !n.IsRead) ?? 0;
                        ViewBag.Notifications = notifications?.OrderByDescending(n => n.CreatedDate).Take(3).ToList() ?? new List<Notification>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error fetching notifications for User {userName}.");
                        ViewBag.UnreadCount = 0;
                        ViewBag.Notifications = new List<Notification>();
                    }

                    _logger.LogInformation($"User {userName} successfully unenrolled from CourseId: {courseId}.");
                    TempData["Success"] = "Hủy đăng ký khóa học thành công!";
                }
                else
                {
                    _logger.LogWarning($"User {userName} has not enrolled in CourseId: {courseId} or unenrollment failed.");
                    TempData["Error"] = "Bạn chưa đăng ký khóa học này hoặc hủy đăng ký thất bại.";
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while unenrolling: {ex.Message}, InnerException: {ex.InnerException?.Message}");
                TempData["Error"] = $"Đã có lỗi xảy ra: {ex.Message}";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Enrollment/ManageEnrollments
        public async Task<IActionResult> ManageEnrollments(int page = 1)
        {
            _logger.LogInformation($"User Authenticated: {User.Identity.IsAuthenticated}");
            _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            _logger.LogInformation("ManageEnrollments GET called.");

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!roles.Any(r => r == "Admin" || r == "Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            IEnumerable<Enrollment> enrollments;
            if (User.IsInRole("Instructor"))
            {
                var instructorUserName = User.Identity.Name;
                if (string.IsNullOrEmpty(instructorUserName))
                {
                    TempData["Error"] = "Không thể xác định danh tính giảng viên.";
                    return RedirectToAction("Index", "Home");
                }

                var courses = await _context.Courses
                    .Include(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                        .ThenInclude(u => u.Role)
                    .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor"))
                    .ToListAsync();

                if (!courses.Any())
                {
                    TempData["Error"] = "Bạn chưa được phân công khóa học nào.";
                    return RedirectToAction("Index", "Instructor");
                }

                var courseIds = courses.Select(c => c.CourseId).ToList();
                enrollments = await _context.Enrollments
                    .Include(e => e.User)
                    .Include(e => e.Course)
                    .Where(e => courseIds.Contains(e.CourseId))
                    .ToListAsync();
            }
            else
            {
                enrollments = _enrollmentRepository.GetAll().ToList();
            }

            // Add pagination
            int pageSize = 10;
            int totalEnrollments = enrollments.Count();
            int totalPages = (int)Math.Ceiling((double)totalEnrollments / pageSize);
            var pagedEnrollments = enrollments.OrderByDescending(e => e.EnrollmentDate)
                                            .Skip((page - 1) * pageSize)
                                            .Take(pageSize)
                                            .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Enrollment/ManageEnrollments.cshtml", pagedEnrollments);
        }

        // GET: Enrollment/CreateEnrollment
        public async Task<IActionResult> CreateEnrollment()
        {
            _logger.LogInformation($"User Authenticated: {User.Identity.IsAuthenticated}");
            _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            _logger.LogInformation("CreateEnrollment GET called.");

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!roles.Any(r => r == "Admin" || r == "Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var instructorUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(instructorUserName))
            {
                TempData["Error"] = "Không thể xác định danh tính giảng viên.";
                return RedirectToAction("Index", "Home");
            }

            var courses = await _context.Courses
                .Include(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.User)
                    .ThenInclude(u => u.Role)
                .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") || User.IsInRole("Admin"))
                .ToListAsync();

            if (!courses.Any())
            {
                TempData["Error"] = "Bạn chưa được phân công khóa học nào.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            var students = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName == "Student")
                .ToListAsync();

            if (!students.Any())
            {
                TempData["Error"] = "Không có học viên nào để ghi danh.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            ViewBag.Courses = new SelectList(courses, "CourseId", "CourseName");
            ViewBag.Users = new SelectList(students, "UserName", "FullName");
            return View("~/Views/Enrollment/CreateEnrollment.cshtml", new Enrollment());
        }

        // POST: Enrollment/CreateEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEnrollment(Enrollment enrollment)
        {
            _logger.LogInformation("CreateEnrollment POST called.");

            // Bỏ qua validation cho EnrollmentId vì nó được tạo trong controller
            ModelState.Remove("EnrollmentId");

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!roles.Any(r => r == "Admin" || r == "Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var instructorUserName = User.Identity?.Name;
            if (string.IsNullOrEmpty(instructorUserName))
            {
                TempData["Error"] = "Không thể xác định danh tính giảng viên.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra khóa học có thuộc giảng viên hoặc Admin không
                    var course = await _context.Courses
                        .Include(c => c.CourseInstructors)
                            .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role)
                        .FirstOrDefaultAsync(c => c.CourseId == enrollment.CourseId &&
                                                (c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") || User.IsInRole("Admin")));

                    if (course == null)
                    {
                        TempData["Error"] = "Khóa học không hợp lệ hoặc không thuộc quyền quản lý của bạn.";
                        await PopulateDropdowns(instructorUserName);
                        return View("~/Views/Enrollment/CreateEnrollment.cshtml", enrollment);
                    }

                    // Kiểm tra học viên có tồn tại và là Student không
                    var userExists = await _context.Users
                        .Include(u => u.Role)
                        .AnyAsync(u => u.UserName == enrollment.UserName && u.Role != null && u.Role.RoleName == "Student");

                    if (!userExists)
                    {
                        TempData["Error"] = "Học viên không tồn tại hoặc không phải là học viên.";
                        await PopulateDropdowns(instructorUserName);
                        return View("~/Views/Enrollment/CreateEnrollment.cshtml", enrollment);
                    }

                    // Kiểm tra xem học viên đã ghi danh khóa học này chưa
                    var existingEnrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserName == enrollment.UserName && e.CourseId == enrollment.CourseId);

                    if (existingEnrollment != null)
                    {
                        TempData["Error"] = "Học viên đã được ghi danh vào khóa học này.";
                        await PopulateDropdowns(instructorUserName);
                        return View("~/Views/Enrollment/CreateEnrollment.cshtml", enrollment);
                    }

                    // Gán giá trị và lưu
                    enrollment.EnrollmentId = Guid.NewGuid().ToString();
                    enrollment.EnrollmentDate = DateTime.Now;
                    _context.Enrollments.Add(enrollment);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm ghi danh thành công!";
                    return RedirectToAction(nameof(ManageEnrollments));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating enrollment.");
                    TempData["Error"] = $"Lỗi khi thêm ghi danh: {ex.Message}";
                }
            }

            await PopulateDropdowns(instructorUserName);
            return View("~/Views/Enrollment/CreateEnrollment.cshtml", enrollment);
        }

        // GET: Enrollment/EditEnrollment/{id}
        public async Task<IActionResult> EditEnrollment(string id)
        {
            _logger.LogInformation($"User Authenticated: {User.Identity.IsAuthenticated}");
            _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            _logger.LogInformation($"EditEnrollment GET called with id: {id}");

            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "ID ghi danh không hợp lệ.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            var instructorUserName = User.Identity.Name;
            if (string.IsNullOrEmpty(instructorUserName))
            {
                TempData["Error"] = "Không thể xác định danh tính giảng viên.";
                return RedirectToAction("Index", "Home");
            }

            var enrollment = await _context.Enrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .Include(e => e.Course.CourseInstructors)
                    .ThenInclude(ci => ci.User)
                    .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null)
            {
                TempData["Error"] = "Ghi danh không tồn tại.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            var courseInstructors = enrollment.Course.CourseInstructors?.ToList() ?? new List<CourseInstructor>();
            if (!courseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") && !User.IsInRole("Admin"))
            {
                var authorizedCourses = await _context.Courses
                    .Include(c => c.CourseInstructors)
                    .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor"))
                    .Select(c => c.CourseId)
                    .ToListAsync();

                if (!authorizedCourses.Any())
                {
                    TempData["Error"] = $"Bạn không có quyền chỉnh sửa ghi danh này. Username: {instructorUserName}, CourseId: {enrollment.CourseId}";
                    return RedirectToAction(nameof(ManageEnrollments));
                }
            }

            // Lấy danh sách tất cả khóa học mà người dùng hiện tại có quyền quản lý
            var courses = await _context.Courses
                .Include(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.User)
                    .ThenInclude(u => u.Role)
                .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") || User.IsInRole("Admin"))
                .ToListAsync();

            if (!courses.Any())
            {
                TempData["Error"] = "Bạn chưa được phân công khóa học nào.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            // Lấy danh sách CourseId mà người dùng hiện tại đã đăng ký, ngoại trừ bản ghi hiện tại
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.UserName == enrollment.UserName && e.EnrollmentId != enrollment.EnrollmentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            // Lọc các khóa học khả dụng: chỉ hiển thị khóa học chưa đăng ký hoặc khóa học hiện tại
            var availableCourses = courses
                .Where(c => !enrolledCourseIds.Contains(c.CourseId) || c.CourseId == enrollment.CourseId)
                .ToList();

            if (!availableCourses.Any())
            {
                ViewBag.Courses = new SelectList(new[] { enrollment.Course }, "CourseId", "CourseName", enrollment.CourseId);
                ViewBag.NoAvailableCourse = true;
            }
            else
            {
                ViewBag.Courses = new SelectList(availableCourses, "CourseId", "CourseName", enrollment.CourseId);
                ViewBag.NoAvailableCourse = false;
            }

            ViewBag.IsDisabled = true; // Luôn đặt IsDisabled là true vì không cho phép thay đổi UserName
            return View("~/Views/Enrollment/EditEnrollment.cshtml", enrollment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEnrollment(string id, Enrollment enrollment)
        {
            _logger.LogInformation("EditEnrollment POST called.");

            if (string.IsNullOrEmpty(id) || id != enrollment.EnrollmentId)
            {
                TempData["Error"] = "ID ghi danh không hợp lệ hoặc không khớp.";
                return RedirectToAction(nameof(ManageEnrollments));
            }

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!roles.Any(r => r == "Admin" || r == "Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            var instructorUserName = User.Identity.Name;
            if (string.IsNullOrEmpty(instructorUserName))
            {
                TempData["Error"] = "Không thể xác định danh tính giảng viên.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                var existingEnrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Course.CourseInstructors)
                    .FirstOrDefaultAsync(e => e.EnrollmentId == id);

                if (existingEnrollment == null)
                {
                    TempData["Error"] = "Ghi danh không tồn tại.";
                    return RedirectToAction(nameof(ManageEnrollments));
                }

                var courseInstructors = existingEnrollment.Course.CourseInstructors?.ToList() ?? new List<CourseInstructor>();
                if (!courseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") && !User.IsInRole("Admin"))
                {
                    var authorizedCourses = await _context.Courses
                        .Include(c => c.CourseInstructors)
                        .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor"))
                        .Select(c => c.CourseId)
                        .ToListAsync();

                    if (!authorizedCourses.Any())
                    {
                        TempData["Error"] = $"Bạn không có quyền chỉnh sửa ghi danh này. Username: {instructorUserName}, CourseId: {existingEnrollment.CourseId}";
                        await PopulateDropdowns(instructorUserName, existingEnrollment.CourseId);
                        return View("~/Views/Enrollment/EditEnrollment.cshtml", existingEnrollment);
                    }
                }

                var newCourse = await _context.Courses
                    .Include(c => c.CourseInstructors)
                    .FirstOrDefaultAsync(c => c.CourseId == enrollment.CourseId && (c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") || User.IsInRole("Admin")));

                if (newCourse == null)
                {
                    ModelState.AddModelError("CourseId", "Khóa học mới không hợp lệ hoặc không thuộc quyền quản lý của bạn.");
                    await PopulateDropdowns(instructorUserName, enrollment.CourseId);
                    return View("~/Views/Enrollment/EditEnrollment.cshtml", enrollment);
                }

                try
                {
                    existingEnrollment.CourseId = enrollment.CourseId;
                    _context.Update(existingEnrollment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Ghi danh được cập nhật thành công!";
                    return RedirectToAction(nameof(ManageEnrollments));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating enrollment.");
                    ModelState.AddModelError("", $"Lỗi khi cập nhật ghi danh: {ex.InnerException?.Message ?? ex.Message}");
                    await PopulateDropdowns(instructorUserName, enrollment.CourseId);
                    return View("~/Views/Enrollment/EditEnrollment.cshtml", enrollment);
                }
            }

            // Xử lý khi ModelState không hợp lệ
            await PopulateDropdowns(instructorUserName, string.IsNullOrEmpty(enrollment?.CourseId) ? null : enrollment.CourseId);
            return View("~/Views/Enrollment/EditEnrollment.cshtml", enrollment);
        }

        // POST: Enrollment/DeleteEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEnrollment(string id)
        {
            _logger.LogInformation("DeleteEnrollment POST called.");

            var roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            if (!roles.Any(r => r == "Admin" || r == "Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            try
            {
                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .Include(e => e.Course.CourseInstructors)
                    .FirstOrDefaultAsync(e => e.EnrollmentId == id);

                if (enrollment == null)
                {
                    TempData["Error"] = "Không tìm thấy bản ghi danh để xóa.";
                    return RedirectToAction(nameof(ManageEnrollments));
                }

                var instructorUserName = User.Identity.Name;
                var courseInstructors = enrollment.Course.CourseInstructors?.ToList() ?? new List<CourseInstructor>();
                if (!courseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") && !User.IsInRole("Admin"))
                {
                    var authorizedCourses = await _context.Courses
                        .Include(c => c.CourseInstructors)
                        .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor"))
                        .Select(c => c.CourseId)
                        .ToListAsync();

                    if (!authorizedCourses.Any())
                    {
                        TempData["Error"] = $"Bạn không có quyền xóa ghi danh này. Username: {instructorUserName}, CourseId: {enrollment.CourseId}";
                        return RedirectToAction(nameof(ManageEnrollments));
                    }
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa ghi danh thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting enrollment.");
                TempData["Error"] = $"Lỗi khi xóa ghi danh: {ex.Message}";
            }
            return RedirectToAction(nameof(ManageEnrollments));
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(string instructorUserName, string selectedCourseId = null)
        {
            // Lấy danh sách khóa học mà người dùng hiện tại có quyền quản lý
            var courses = await _context.Courses
                .Include(c => c.CourseInstructors)
                    .ThenInclude(ci => ci.User)
                    .ThenInclude(u => u.Role)
                .Where(c => c.CourseInstructors.Any(ci => ci.User != null && ci.User.UserName == instructorUserName && ci.User.Role != null && ci.User.Role.RoleName == "Instructor") || User.IsInRole("Admin"))
                .ToListAsync();

            // Lấy danh sách người dùng có vai trò "Student" và chưa đăng ký bất kỳ khóa học nào
            var students = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName == "Student" &&
                           !_context.Enrollments.Any(e => e.UserName == u.UserName)) // Loại bỏ người dùng đã đăng ký
                .ToListAsync(); // Đảm bảo không null

            ViewBag.Courses = new SelectList(courses, "CourseId", "CourseName", selectedCourseId);
            ViewBag.Users = new SelectList(students, "UserName", "FullName");
        }
    }
}