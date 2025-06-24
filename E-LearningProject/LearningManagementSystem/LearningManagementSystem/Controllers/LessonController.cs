using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace LearningManagementSystem.Controllers
{
    public class LessonController : Controller
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly IProgressRepository _progressRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly ICourseRepository _courseRepository;
        private readonly ILogger<LessonController> _logger;

        public LessonController(
            ILessonRepository lessonRepository,
            IProgressRepository progressRepository,
            IEnrollmentRepository enrollmentRepository,
            ICourseRepository courseRepository,
            ILogger<LessonController> logger)
        {
            _lessonRepository = lessonRepository;
            _progressRepository = progressRepository;
            _enrollmentRepository = enrollmentRepository;
            _courseRepository = courseRepository;
            _logger = logger;
        }
        [Authorize(Roles = "Student")] 
        [HttpGet]
        public IActionResult ViewLesson(string lessonId)
        {
            _logger.LogInformation($"ViewLesson called with LessonId: {lessonId}");

            // Kiểm tra lessonId có hợp lệ không
            if (string.IsNullOrEmpty(lessonId))
            {
                _logger.LogWarning("LessonId is empty.");
                return BadRequest("LessonId không được để trống.");
            }

            // Lấy thông tin bài học
            var lesson = _lessonRepository.GetById(lessonId);
            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with LessonId: {lessonId} not found.");
                return NotFound("Không tìm thấy bài học.");
            }

            // Lấy thông tin khóa học
            var course = _courseRepository.GetById(lesson.CourseId);
            if (course == null)
            {
                _logger.LogWarning($"Course with CourseId: {lesson.CourseId} not found.");
                return NotFound("Không tìm thấy khóa học.");
            }

            // Lấy UserName từ thông tin người dùng hiện tại
            var userName = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogError("UserName could not be determined from claims.");
                return Unauthorized("Bạn cần đăng nhập để xem bài học.");
            }

            // Kiểm tra xem người dùng đã đăng ký khóa học chưa
            var enrollment = _enrollmentRepository.GetAll()
                .FirstOrDefault(e => e.UserName == userName && e.CourseId == lesson.CourseId);
            if (enrollment == null)
            {
                _logger.LogWarning($"User {userName} has not enrolled in CourseId: {lesson.CourseId}.");
                TempData["Error"] = "Bạn chưa đăng ký khóa học này.";
                return RedirectToAction("Index", "Home");
            }

            // Lấy tiến độ của người dùng cho bài học này
            var progress = _progressRepository.GetAll()
                .FirstOrDefault(p => p.UserName == userName && p.LessonId == lessonId);

            // Tạo ViewModel để hiển thị thông tin bài học và tiến độ
            var viewModel = new LessonViewModel
            {
                Lesson = lesson,
                Progress = progress
            };

            return View(viewModel);
        }
        [Authorize(Roles = "Admin, Instructor")]
        public IActionResult ManageLessons(string courseId = null, int page = 1)
        {
            _logger.LogInformation($"User Authenticated: {User.Identity.IsAuthenticated}");
            _logger.LogInformation($"User Roles: {string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))}");
            _logger.LogInformation($"ManageLessons GET called with CourseId: {courseId}");

            if (!User.IsInRole("Admin") && !User.IsInRole("Instructor"))
            {
                _logger.LogWarning("User does not have required role (Admin or Instructor).");
                TempData["Error"] = "Bạn không có quyền truy cập vào trang này.";
                return RedirectToAction("AccessDenied", "Account");
            }

            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId is null or empty.");
                TempData["Error"] = "Vui lòng chọn một khóa học.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            var course = _courseRepository.GetById(courseId);
            if (course == null)
            {
                _logger.LogWarning($"Course with CourseId: {courseId} not found.");
                TempData["Error"] = "Khóa học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            try
            {
                var allLessons = _lessonRepository.GetLessonsByCourse(courseId).OrderBy(l => l.OrderNumber).ToList();
                int pageSize = 10;
                int totalLessons = allLessons.Count();
                int totalPages = (int)Math.Ceiling((double)totalLessons / pageSize);
                var pagedLessons = allLessons.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                course.Lessons = pagedLessons;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching lessons for CourseId: {courseId}");
                TempData["Error"] = "Đã xảy ra lỗi khi lấy danh sách bài học.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            _logger.LogInformation("Rendering ManageLessons view for CourseId: {courseId}", courseId);
            return View("~/Views/Lesson/ManageLessons.cshtml", course);
        }

        // GET: Lesson/AddLesson/{courseId}
        [Authorize(Roles = "Admin, Instructor")]
        [HttpGet]
        public IActionResult AddLesson(string courseId)
        {
            _logger.LogInformation($"AddLesson GET called for CourseId: {courseId}");

            if (string.IsNullOrEmpty(courseId) || _courseRepository.GetById(courseId) == null)
            {
                _logger.LogWarning($"Course with CourseId: {courseId} not found.");
                TempData["Error"] = "Khóa học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            var lessons = _lessonRepository.GetLessonsByCourse(courseId).ToList();
            var defaultOrderNumber = lessons.Count + 1;

            ViewData["CourseId"] = courseId;
            ViewBag.DefaultOrderNumber = defaultOrderNumber;

            return View("~/Views/Lesson/CreateLesson.cshtml", new Lesson());
        }

        // POST: Lesson/AddLesson/{courseId}
        [Authorize(Roles = "Admin, Instructor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(500 * 1024 * 1024)]
        public async Task<IActionResult> AddLesson(string courseId, Lesson lesson, IFormFile videoFile)
        {
            _logger.LogInformation($"AddLesson POST called for CourseId: {courseId}");
            _logger.LogInformation($"Received Lesson: Title={lesson.LessonTitle}, Content={lesson.Content}, VideoFile={(videoFile != null ? videoFile.FileName : "null")}");

            var course = _courseRepository.GetById(courseId);
            if (course == null)
            {
                _logger.LogWarning($"Course with CourseId: {courseId} not found.");
                TempData["Error"] = "Khóa học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            lesson.LessonId = Guid.NewGuid().ToString();
            lesson.CourseId = courseId;

            var lessons = _lessonRepository.GetLessonsByCourse(courseId).ToList();
            if (lesson.OrderNumber <= 0)
            {
                lesson.OrderNumber = lessons.Count + 1;
            }

            if (videoFile != null && videoFile.Length > 0)
            {
                if (videoFile.Length > 500 * 1024 * 1024)
                {
                    ModelState.AddModelError("videoFile", "File video không được lớn hơn 500MB.");
                }
                else
                {
                    try
                    {
                        var videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/videos");
                        if (!Directory.Exists(videoDirectory))
                        {
                            Directory.CreateDirectory(videoDirectory);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                        var filePath = Path.Combine(videoDirectory, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await videoFile.CopyToAsync(stream);
                        }
                        lesson.VideoUrl = $"/videos/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading video file.");
                        ModelState.AddModelError("videoFile", "Đã xảy ra lỗi khi tải lên video. Vui lòng thử lại.");
                    }
                }
            }

            lesson.Progresses = lesson.Progresses ?? new List<Progress>();

            ModelState.Clear();
            TryValidateModel(lesson);

            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation($"Adding Lesson: {lesson.LessonId}, Title={lesson.LessonTitle}, CourseId={lesson.CourseId}");
                    _lessonRepository.Add(lesson);
                    _lessonRepository.Save();
                    TempData["Success"] = "Thêm bài học thành công.";
                    _logger.LogInformation($"Lesson {lesson.LessonId} added successfully to Course {courseId}.");
                    return RedirectToAction("ManageLessons", new { courseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error adding lesson to course: {courseId}");
                    TempData["Error"] = $"Đã xảy ra lỗi khi thêm bài học: {ex.Message}";
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState is invalid in AddLesson. Errors: {0}", string.Join(", ", errors));
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.";
            }

            TempData["LessonTitle"] = lesson.LessonTitle;
            TempData["Content"] = lesson.Content;
            TempData["OrderNumber"] = lesson.OrderNumber.ToString();
            ViewData["CourseId"] = courseId;
            ViewBag.DefaultOrderNumber = lessons.Count + 1;

            return View("~/Views/Lesson/CreateLesson.cshtml", lesson);
        }

        // GET: Lesson/EditLesson/{lessonId}
        [Authorize(Roles = "Admin, Instructor")]
        [HttpGet]
        public IActionResult EditLesson(string lessonId)
        {
            _logger.LogInformation($"EditLesson GET called for LessonId: {lessonId}");

            var lesson = _lessonRepository.GetById(lessonId);
            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with LessonId: {lessonId} not found.");
                TempData["Error"] = "Bài học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            _logger.LogInformation($"Lesson {lessonId} loaded successfully for editing.");
            return View("~/Views/Lesson/EditLesson.cshtml", lesson);
        }

        // POST: Lesson/EditLesson/{lessonId}
        [Authorize(Roles = "Admin, Instructor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLesson(string lessonId, Lesson model, IFormFile videoFile)
        {
            _logger.LogInformation($"EditLesson POST called for LessonId: {lessonId}");

            var lesson = _lessonRepository.GetById(lessonId);
            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with LessonId: {lessonId} not found.");
                TempData["Error"] = "Bài học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            _logger.LogInformation($"Lesson before update: LessonId={lesson.LessonId}, Title={lesson.LessonTitle}, Content={lesson.Content}, OrderNumber={lesson.OrderNumber}, VideoUrl={lesson.VideoUrl}");

            lesson.LessonTitle = model.LessonTitle;
            lesson.Content = model.Content;

            if (videoFile != null && videoFile.Length > 0)
            {
                var allowedExtensions = new[] { ".mp4", ".avi", ".mov" };
                var extension = Path.GetExtension(videoFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Định dạng video không được hỗ trợ. Vui lòng sử dụng .mp4, .avi, hoặc .mov.";
                    return View("~/Views/Lesson/EditLesson.cshtml", lesson);
                }

                const long maxSize = 500 * 1024 * 1024;
                if (videoFile.Length > maxSize)
                {
                    TempData["Error"] = "Kích thước video vượt quá 500MB. Vui lòng chọn video nhỏ hơn.";
                    return View("~/Views/Lesson/EditLesson.cshtml", lesson);
                }

                try
                {
                    if (!string.IsNullOrEmpty(lesson.VideoUrl))
                    {
                        var oldVideoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lesson.VideoUrl.TrimStart('/'));
                        _logger.LogInformation($"Attempting to delete old video at path: {oldVideoPath}");

                        if (System.IO.File.Exists(oldVideoPath))
                        {
                            System.IO.File.Delete(oldVideoPath);
                            _logger.LogInformation($"Old video deleted successfully: {oldVideoPath}");
                        }
                        else
                        {
                            _logger.LogWarning($"Old video file not found: {oldVideoPath}");
                        }
                    }

                    var videoDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");
                    if (!Directory.Exists(videoDirectory))
                    {
                        Directory.CreateDirectory(videoDirectory);
                        _logger.LogInformation($"Created video directory: {videoDirectory}");
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(videoFile.FileName);
                    var filePath = Path.Combine(videoDirectory, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(stream);
                    }
                    lesson.VideoUrl = $"/videos/{fileName}";
                    _logger.LogInformation($"New video uploaded: {lesson.VideoUrl}");
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx, $"IO Error while handling video file for LessonId: {lessonId}. Details: {ioEx.Message}");
                    TempData["Error"] = "Đã xảy ra lỗi khi xử lý video: " + ioEx.Message;
                    return View("~/Views/Lesson/EditLesson.cshtml", lesson);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error while handling video file for LessonId: {lessonId}. Details: {ex.Message}");
                    TempData["Error"] = "Đã xảy ra lỗi không xác định khi xử lý video: " + ex.Message;
                    return View("~/Views/Lesson/EditLesson.cshtml", lesson);
                }
            }

            lesson.CourseId = lesson.CourseId;
            lesson.Progresses = lesson.Progresses ?? new List<Progress>();

            _logger.LogInformation($"Lesson after update: LessonId={lesson.LessonId}, Title={lesson.LessonTitle}, Content={lesson.Content}, OrderNumber={lesson.OrderNumber}, VideoUrl={lesson.VideoUrl}");

            ModelState.Clear();
            if (string.IsNullOrWhiteSpace(lesson.LessonTitle))
                ModelState.AddModelError("LessonTitle", "Tiêu đề bài học không được để trống.");
            if (lesson.LessonTitle?.Length > 100)
                ModelState.AddModelError("LessonTitle", "Tiêu đề bài học tối đa 100 ký tự.");
            if (lesson.Content?.Length > 4000)
                ModelState.AddModelError("Content", "Nội dung bài học tối đa 4000 ký tự.");
            if (lesson.VideoUrl?.Length > 200)
                ModelState.AddModelError("VideoUrl", "Đường dẫn video tối đa 200 ký tự.");

            if (ModelState.IsValid)
            {
                try
                {
                    _lessonRepository.Update(lesson);
                    await _lessonRepository.SaveAsync();
                    TempData["Success"] = "Chỉnh sửa bài học thành công.";
                    _logger.LogInformation($"Lesson {lesson.LessonId} updated successfully.");
                    return RedirectToAction("ManageLessons", new { courseId = lesson.CourseId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating lesson: {lessonId}. Details: {ex.Message}");
                    TempData["Error"] = $"Đã xảy ra lỗi khi chỉnh sửa bài học: {ex.Message}";
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("ModelState is invalid in EditLesson. Errors: {0}", string.Join(", ", errors));
                TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại các trường.";
                TempData["LessonTitle"] = lesson.LessonTitle;
                TempData["Content"] = lesson.Content;
                TempData["VideoUrl"] = lesson.VideoUrl;
                TempData["OrderNumber"] = lesson.OrderNumber.ToString();
            }

            return View("~/Views/Lesson/EditLesson.cshtml", lesson);
        }

        // POST: Lesson/DeleteLesson/{lessonId}
        [Authorize(Roles = "Admin, Instructor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteLesson(string lessonId)
        {
            _logger.LogInformation($"DeleteLesson POST called for LessonId: {lessonId}");

            var lesson = _lessonRepository.GetById(lessonId);
            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with LessonId: {lessonId} not found.");
                TempData["Error"] = "Bài học không tồn tại.";
                return RedirectToAction("ManageCourses", User.IsInRole("Admin") ? "Admin" : "Instructor");
            }

            try
            {
                var courseId = lesson.CourseId;

                if (!string.IsNullOrEmpty(lesson.VideoUrl))
                {
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", lesson.VideoUrl.TrimStart('/'));
                    _logger.LogInformation($"Attempting to delete video at path: {videoPath}");

                    if (System.IO.File.Exists(videoPath))
                    {
                        try
                        {
                            System.IO.File.Delete(videoPath);
                            _logger.LogInformation($"Video file deleted successfully: {videoPath}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to delete video file at path: {videoPath}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Video file does not exist at path: {videoPath}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Lesson {lessonId} has no video to delete.");
                }

                _lessonRepository.Delete(lessonId);
                _lessonRepository.Save();

                var remainingLessons = _lessonRepository.GetLessonsByCourse(courseId).ToList();
                for (int i = 0; i < remainingLessons.Count; i++)
                {
                    remainingLessons[i].OrderNumber = i + 1;
                    _lessonRepository.Update(remainingLessons[i]);
                }
                _lessonRepository.Save();

                TempData["Success"] = "Xóa bài học thành công.";
                _logger.LogInformation($"Lesson {lessonId} deleted successfully from Course {courseId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting lesson: {lessonId}");
                TempData["Error"] = "Đã xảy ra lỗi khi xóa bài học. Vui lòng thử lại.";
            }

            return RedirectToAction("ManageLessons", new { courseId = lesson.CourseId });
        }
    }
}