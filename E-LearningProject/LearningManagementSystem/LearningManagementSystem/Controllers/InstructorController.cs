using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using LearningManagementSystem.ViewModels;


[Authorize(Roles = "Instructor")]
public class InstructorController : Controller
{
    private readonly LMSContext _context;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IAssignmentQuestionRepository _questionRepository;
    private readonly IAssignmentQuestionOptionRepository _optionRepository;
    private readonly IAssignmentSubmissionRepository _submissionRepository;
    private readonly ILessonRepository _lessonRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger _logger;

    public InstructorController(
        LMSContext context,
        IAssignmentRepository assignmentRepository,
        IAssignmentQuestionRepository questionRepository,
        ILessonRepository lessonRepository,
        IAssignmentQuestionOptionRepository optionRepository,
        IAssignmentSubmissionRepository submissionRepository,
        ICourseRepository courseRepository,
         IUserRepository userRepository,
    ILogger<InstructorController> logger)
    {
        _context = context;
        _assignmentRepository = assignmentRepository;
        _questionRepository = questionRepository;
        _optionRepository = optionRepository;
        _lessonRepository = lessonRepository;
        _submissionRepository = submissionRepository;
        _userRepository = userRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    #region Dashboard
    // GET: Instructor/Dashboard
    public IActionResult Dashboard()
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Login", "Account");
        }

        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("UserName is null or empty after authentication.");
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var courses = _courseRepository.GetAllWithLessons()
                .Where(c => c.CourseInstructors.Any(ci => ci.UserName == userName))
                .ToList();
            var totalCourses = courses.Count;
            var totalLessons = courses.Sum(c => c.Lessons.Count);
            var totalAssignments = courses.SelectMany(c => c.Lessons)
                .SelectMany(l => _assignmentRepository.GetByLessonId(l.LessonId))
                .Count();
            var pendingSubmissions = _submissionRepository.GetByAssignmentId("")
                .Count(s => s.Score == null && s.Assignment.Lesson.Course.CourseInstructors.Any(ci => ci.UserName == userName));

            ViewBag.Notifications = _context.Notifications
                .Where(n => n.UserName == userName)
                .OrderByDescending(n => n.CreatedDate)
                .Take(5)
                .ToList();
            ViewBag.UnreadCount = _context.Notifications
                .Count(n => n.UserName == userName && !n.IsRead);

            var model = new InstructorDashboardViewModel
            {
                TotalCourses = totalCourses,
                PendingSubmissions = pendingSubmissions,
                TotalLessons = totalLessons,
                TotalAssignments = totalAssignments
            };

            return View("~/Views/Instructor/Dashboard.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user: {UserName}", userName);
            TempData["Error"] = "Có lỗi xảy ra khi tải trang Dashboard.";
            return RedirectToAction("Courses");
        }
    }
    #endregion

    public async Task<IActionResult> ViewProgressByUser()
    {
        ViewData["Title"] = "Quản lý tiến trình học - E-Learning System (Instructor)";
        var instructorUserName = User.Identity.Name;
        var courses = await _context.Courses
            .Include(c => c.CourseInstructors)
                .ThenInclude(ci => ci.User)
                .ThenInclude(u => u.Role)
            .Where(c => c.CourseInstructors.Any(ci => ci.User.UserName == instructorUserName && ci.User.Role.RoleName == "Instructor"))
            .ToListAsync();

        if (courses == null || !courses.Any())
        {
            ViewBag.NoCourse = true;
            return View("~/Views/Instructor/Progress/ViewProgressByUser.cshtml", new List<ProgressViewModel>());
        }
        ViewBag.NoCourse = false;

        var courseIds = courses.Select(c => c.CourseId).ToList();
        var progressData = await _context.Progresses
            .Include(p => p.User)
            .Include(p => p.Lesson)
                .ThenInclude(l => l.Course)
            .Where(p => courseIds.Contains(p.Lesson.CourseId))
            .GroupBy(p => new { p.User.UserName, p.Lesson.CourseId })
            .Select(g => new ProgressViewModel
            {
                UserName = g.Key.UserName,
                FullName = g.First().User.FullName,
                CourseId = g.Key.CourseId,
                CourseName = g.First().Lesson.Course.CourseName,
                TotalLessons = _context.Lessons.Count(l => l.CourseId == g.Key.CourseId),
                CompletedLessons = g.Count(p => p.CompletionStatus),
                CompletionDate = g.Max(p => p.CompletionDate)
            })
            .ToListAsync();

        return View("~/Views/Instructor/Progress/ViewProgressByUser.cshtml", progressData);
    }

    #region Quản lý điểm số

    [Authorize]
    public async Task<IActionResult> Grade(int page = 1)
    {
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("UserName is null or empty.");
            TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var assignments = await _context.Assignments
                .Include(a => a.Lesson)
                .Include(a => a.Course)
                .ThenInclude(c => c.CourseInstructors)
                .Where(a => a.Course.CourseInstructors.Any(ci => ci.UserName == userName) && a.AssignmentType == "Exercise")
                .ToListAsync();

            int pageSize = 10;
            int totalAssignments = assignments.Count();
            int totalPages = (int)Math.Ceiling((double)totalAssignments / pageSize);
            var pagedAssignments = assignments.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            _logger.LogInformation("Loaded {AssignmentCount} assignments for grading by user {UserName}.", assignments.Count, userName);
            return View("~/Views/Instructor/Grade/Grade.cshtml", pagedAssignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for grading by user {UserName}.", userName);
            TempData["Error"] = "Có lỗi xảy ra khi tải danh sách bài tập.";
            return RedirectToAction("ManageCourses");
        }
    }

    [Authorize]
    public async Task<IActionResult> GradeExams(int page = 1)
    {
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("UserName is null or empty.");
            TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var assignments = await _context.Assignments
                .Include(a => a.Course)
                .ThenInclude(c => c.CourseInstructors)
                .Where(a => a.Course.CourseInstructors.Any(ci => ci.UserName == userName) && a.AssignmentType == "Test")
                .ToListAsync();

            int pageSize = 10;
            int totalAssignments = assignments.Count();
            int totalPages = (int)Math.Ceiling((double)totalAssignments / pageSize);
            var pagedAssignments = assignments.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            _logger.LogInformation("Loaded {AssignmentCount} exams for grading by user {UserName}.", assignments.Count, userName);
            return View("~/Views/Instructor/Grade/GradeExams.cshtml", pagedAssignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading exams for grading by user {UserName}.", userName);
            TempData["Error"] = "Có lỗi xảy ra khi tải danh sách bài kiểm tra.";
            return RedirectToAction("ManageCourses");
        }
    }

    [Authorize]
    public IActionResult SelectGradeType()
    {
        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("UserName is null or empty.");
            TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("User {UserName} accessed SelectGradeType.", userName);
        return View("~/Views/Instructor/Grade/SelectGradeType.cshtml");
    }

    [HttpPost]
    public IActionResult SelectGradeType(string gradeType)
    {
        if (string.IsNullOrEmpty(gradeType))
        {
            TempData["Error"] = "Vui lòng chọn loại chấm điểm.";
            return RedirectToAction("SelectGradeType");
        }

        if (gradeType == "assignment")
        {
            return RedirectToAction("Grade");
        }
        else if (gradeType == "exam")
        {
            return RedirectToAction("GradeExams");
        }

        TempData["Error"] = "Loại chấm điểm không hợp lệ.";
        return RedirectToAction("SelectGradeType");
    }


    [Authorize]
    public async Task<IActionResult> GradeAssignment(string submissionId, string assignmentId = null)
    {
        _logger.LogInformation("GradeAssignment called with submissionId: {SubmissionId}, assignmentId: {AssignmentId}",
            submissionId ?? "null", assignmentId ?? "null");

        // Check if submissionId is null or empty
        if (string.IsNullOrEmpty(submissionId))
        {
            _logger.LogWarning("submissionId is null or empty in GradeAssignment.");
            TempData["Error"] = "Submission ID không hợp lệ.";

            // Nếu có assignmentId, redirect về ListStudentSubmissions với assignmentId
            if (!string.IsNullOrEmpty(assignmentId))
            {
                return RedirectToAction("ListStudentSubmissions", new { assignmentId = assignmentId });
            }
            return RedirectToAction("GradeExams");
        }

        try
        {
            // Log the submissionId being queried
            _logger.LogInformation("Querying for submission with SubmissionId: {SubmissionId}", submissionId);

            // Fetch the submission from the database
            var submission = await _context.AssignmentSubmissions
                .Include(s => s.User)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

            if (submission == null)
            {
                _logger.LogWarning("Submission with ID {SubmissionId} not found in database.", submissionId);
                TempData["Error"] = "Không tìm thấy bài nộp với ID này.";

                // Nếu có assignmentId, redirect về ListStudentSubmissions với assignmentId
                if (!string.IsNullOrEmpty(assignmentId))
                {
                    return RedirectToAction("ListStudentSubmissions", new { assignmentId = assignmentId });
                }
                return RedirectToAction("GradeExams");
            }

            // Verify user permissions
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName is null or empty in GradeAssignment.");
                TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
                return RedirectToAction("Login", "Account");
            }

            // Prepare the model for the view
            var model = submission.Assignment;
            model.Submissions = new List<AssignmentSubmission> { submission };

            // Truyền assignmentId vào ViewBag để sử dụng khi redirect
            ViewBag.AssignmentId = assignmentId ?? submission.AssignmentId;
            ViewBag.AssignmentType = model.AssignmentType;

            _logger.LogInformation("Successfully loaded submission {SubmissionId} for grading.", submissionId);
            return View("~/Views/Instructor/Grade/GradeAssignment.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GradeAssignment for submissionId: {SubmissionId}", submissionId);
            TempData["Error"] = "Có lỗi xảy ra khi tải chi tiết bài nộp.";

            // Nếu có assignmentId, redirect về ListStudentSubmissions với assignmentId
            if (!string.IsNullOrEmpty(assignmentId))
            {
                return RedirectToAction("ListStudentSubmissions", new { assignmentId = assignmentId });
            }
            return RedirectToAction("GradeExams");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitGradeAndFeedback(string submissionId, string score, string feedback, string assignmentId = null)
    {
        double parsedScore = 0;
        if (!string.IsNullOrWhiteSpace(score))
        {
            // Thử parse với dấu chấm
            if (!double.TryParse(score.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsedScore))
            {
                // Nếu vẫn không được, thử lại với culture hiện tại
                double.TryParse(score, out parsedScore);
            }
        }

        var submission = await _context.AssignmentSubmissions
            .Include(s => s.Question)
            .Include(s => s.Assignment) // Include Assignment để lấy AssignmentId
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

        if (submission == null)
        {
            TempData["Error"] = "Không tìm thấy bài nộp.";
            if (!string.IsNullOrEmpty(assignmentId))
            {
                return RedirectToAction("ListStudentSubmissions", new { assignmentId = assignmentId });
            }
            return RedirectToAction("GradeExams");
        }

        if (parsedScore > submission.Question.MaxScore)
        {
            TempData["Error"] = $"Điểm không được vượt quá {submission.Question.MaxScore}.";
            var redirectAssignmentId = assignmentId ?? submission.AssignmentId;
            return RedirectToAction("GradeAssignment", new { submissionId = submission.SubmissionId, assignmentId = redirectAssignmentId });
        }

        submission.Score = parsedScore;
        submission.Feedback = feedback;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Điểm và nhận xét đã được lưu thành công.";

        var finalAssignmentId = assignmentId ?? submission.AssignmentId;
        return RedirectToAction("ListStudentSubmissions", new { assignmentId = finalAssignmentId });
    }

    // Phương thức GetStudentSubmissions cần được cải thiện để xử lý dữ liệu trả về đúng format
    [HttpGet]
    public async Task<IActionResult> GetStudentSubmissions(string assignmentId, string courseId = null, string status = null, int page = 1)
    {
        _logger.LogInformation("GetStudentSubmissions called with assignmentId: {AssignmentId}, courseId: {CourseId}, status: {Status}, page: {Page}",
            assignmentId, courseId, status, page);

        try
        {
            if (string.IsNullOrEmpty(assignmentId))
            {
                return Json(new { success = false, message = "AssignmentId is required." });
            }

            const int pageSize = 10;
            var query = _context.AssignmentSubmissions
                .Include(s => s.User)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .Where(s => s.AssignmentId == assignmentId && s.SubmittedDate != null);

            // Apply additional filters if provided
            if (!string.IsNullOrEmpty(courseId))
            {
                query = query.Where(s => s.Assignment.CourseId == courseId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "graded":
                        query = query.Where(s => s.Score.HasValue);
                        break;
                    case "not-graded":
                    case "submitted":
                        query = query.Where(s => !s.Score.HasValue);
                        break;
                }
            }

            var totalItems = await query.CountAsync();
            var submissions = await query
                .OrderByDescending(s => s.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var start = totalItems > 0 ? (page - 1) * pageSize + 1 : 0;
            var end = Math.Min(start + pageSize - 1, totalItems);

            // Convert to the format expected by frontend
            var studentSubmissions = submissions.Select(s => new
            {
                submissionId = s.SubmissionId,
                fullName = s.User?.FullName ?? "Không xác định",
                studentId = s.User?.UserName ?? "N/A",
                assignmentTitle = s.Assignment?.Title ?? "Không xác định",
                courseName = s.Assignment?.Course?.CourseName ?? "Không xác định",
                submittedAt = s.SubmittedDate,
                status = s.Score.HasValue ? "graded" : "submitted",
                score = s.Score
            }).ToList();

            var response = new
            {
                success = true,
                data = studentSubmissions,
                pagination = new
                {
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    start = start,
                    end = end,
                    total = totalItems
                },
                stats = new
                {
                    total = totalItems
                }
            };

            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetStudentSubmissions");
            return Json(new { success = false, message = "Đã có lỗi xảy ra khi tải dữ liệu: " + ex.Message });
        }
    }

    [Authorize]
    public async Task<IActionResult> ListStudentSubmissions(string assignmentId, int page = 1)
    {
        _logger.LogInformation("ListStudentSubmissions called with assignmentId: {AssignmentId}, page: {Page}", assignmentId, page);

        if (string.IsNullOrEmpty(assignmentId))
        {
            _logger.LogWarning("AssignmentId is null or empty.");
            TempData["Error"] = "ID bài kiểm tra không hợp lệ.";
            return RedirectToAction("GradeExams");
        }

        var userName = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("UserName is null or empty.");
            TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
            return RedirectToAction("Login", "Account");
        }

        try
        {
            const int pageSize = 10;

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                    .ThenInclude(c => c.CourseInstructors)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
            {
                _logger.LogWarning("Assignment with ID {AssignmentId} not found.", assignmentId);
                TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                return RedirectToAction("GradeExams");
            }

            if (!assignment.Course?.CourseInstructors?.Any(ci => ci.UserName == userName) ?? true)
            {
                _logger.LogWarning("User {UserName} has no permission for assignment {AssignmentId}.", userName, assignmentId);
                TempData["Error"] = "Bạn không có quyền truy cập vào bài kiểm tra này.";
                return RedirectToAction("GradeExams");
            }

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.User)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Course)
                .Where(s => s.AssignmentId == assignmentId && s.SubmittedDate != null)
                .ToListAsync();

            var studentSubmissions = submissions.Select(s => new StudentSubmissionInfo
            {
                SubmissionId = s.SubmissionId,
                FullName = s.User?.FullName ?? "Không xác định",
                StudentId = s.User?.UserName ?? "N/A",
                AssignmentTitle = s.Assignment?.Title ?? "Không xác định",
                CourseName = s.Assignment?.Course?.CourseName ?? "Không xác định",
                SubmittedAt = s.SubmittedDate,
                Status = s.Score.HasValue ? "graded" : "submitted",
                Score = s.Score
            }).ToList();

            var totalItems = studentSubmissions.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var start = (page - 1) * pageSize + 1;
            var end = Math.Min(start + pageSize - 1, totalItems);

            var pagedSubmissions = studentSubmissions
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new StudentSubmissionListViewModel
            {
                Submissions = pagedSubmissions,
                CourseOptions = await _context.Courses
                    .Where(c => c.CourseInstructors.Any(ci => ci.UserName == userName))
                    .Select(c => new CourseOption
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName
                    })
                    .OrderBy(c => c.CourseName)
                    .ToListAsync(),
                Pagination = new PaginationInfo
                {
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalItems = totalItems,
                    Start = start,
                    End = end
                },
                Stats = new SubmissionStats
                {
                    Total = totalItems
                }
            };

            _logger.LogInformation("Loaded {StudentCount} students for assignment {AssignmentId}.", studentSubmissions.Count, assignmentId);
            ViewBag.AssignmentId = assignmentId;
            ViewBag.AssignmentType = assignment.AssignmentType;
            return View("~/Views/Instructor/Grade/ListStudentSubmissions.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading students for assignment {AssignmentId}.", assignmentId);
            TempData["Error"] = "Có lỗi xảy ra khi tải danh sách sinh viên.";
            return RedirectToAction("GradeExams");
        }
    }
    #endregion


}