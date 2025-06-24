using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using LearningManagementSystem.Repositories;
using System.Security.Claims;

namespace LearningManagementSystem.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly LMSContext _context;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentQuestionRepository _questionRepository;
        private readonly IAssignmentQuestionOptionRepository _optionRepository;
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(LMSContext context, IAssignmentRepository assignmentRepository,
             IAssignmentQuestionRepository questionRepository, IAssignmentQuestionOptionRepository optionRepository, ILogger<AssignmentController> logger)
        {
            _context = context;
            _assignmentRepository = assignmentRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _logger = logger;
        }

        private async Task<string> GetCourseIdFromLesson(string lessonId)
        {
            var lesson = await _context.Lessons
                .Where(l => l.LessonId == lessonId)
                .FirstOrDefaultAsync();
            return lesson?.CourseId;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitAssignment(string lessonId, Dictionary<string, string> answers)
        {
            _logger.LogInformation($"SubmitAssignment called. LessonId: {lessonId}, Answers Count: {answers?.Count ?? 0}");

            // Kiểm tra lessonId
            if (string.IsNullOrEmpty(lessonId))
            {
                _logger.LogWarning("LessonId is null or empty.");
                TempData["Error"] = "ID bài học không hợp lệ.";
                return RedirectToAction("CourseDetails", "Course", new { id = lessonId });
            }

            // Kiểm tra answers
            if (answers == null || !answers.Any())
            {
                _logger.LogWarning("Answers dictionary is null or empty.");
                TempData["Error"] = "Không có câu trả lời nào được gửi. Vui lòng chọn ít nhất một đáp án.";
                var courseIdFromLesson = await GetCourseIdFromLesson(lessonId);
                return RedirectToAction("CourseDetails", "Course", new { id = courseIdFromLesson ?? lessonId });
            }

            // Lấy thông tin người dùng
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName could not be determined from claims.");
                TempData["Error"] = "Bạn cần đăng nhập để nộp bài.";
                var courseIdFromLesson = await GetCourseIdFromLesson(lessonId);
                return RedirectToAction("CourseDetails", "Course", new { id = courseIdFromLesson ?? lessonId });
            }

            // Lấy thông tin bài học và các bài tập liên quan
            var lesson = await _context.Lessons
                .Include(l => l.Assignments)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .Include(l => l.Assignments)
                    .ThenInclude(a => a.Submissions)
                .Include(l => l.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with ID {lessonId} not found.");
                TempData["Error"] = "Không tìm thấy bài học.";
                return RedirectToAction("CourseDetails", "Course", new { id = lessonId });
            }

            var courseId = lesson.CourseId;
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId not found for the lesson.");
                TempData["Error"] = "Không tìm thấy khóa học liên quan đến bài học này.";
                return RedirectToAction("CourseDetails", "Course", new { id = lessonId });
            }

            // Lấy danh sách câu hỏi từ các bài tập
            var questions = lesson.Assignments
                .Where(a => a.AssignmentType.Equals("Exercise", StringComparison.OrdinalIgnoreCase))
                .SelectMany(a => a.Questions)
                .ToList();

            if (!questions.Any())
            {
                _logger.LogWarning($"No questions found for lesson {lessonId}.");
                TempData["Error"] = "Bài học này không có câu hỏi nào để nộp.";
                return RedirectToAction("CourseDetails", "Course", new { id = courseId });
            }

            int submissionCount = 0;

            foreach (var question in questions)
            {
                if (!answers.ContainsKey(question.QuestionId))
                {
                    _logger.LogInformation($"Question {question.QuestionId} not found in answers. Skipping.");
                    continue;
                }

                var submittedAnswer = answers[question.QuestionId];
                if (string.IsNullOrWhiteSpace(submittedAnswer))
                {
                    _logger.LogInformation($"Answer for Question {question.QuestionId} is empty. Skipping.");
                    continue;
                }

                var assignment = lesson.Assignments.FirstOrDefault(a => a.Questions.Any(q => q.QuestionId == question.QuestionId));
                if (assignment == null)
                {
                    _logger.LogWarning($"Assignment not found for Question {question.QuestionId}.");
                    continue;
                }

                // Kiểm tra xem người dùng đã nộp bài cho câu hỏi này chưa
                var existingSubmission = await _context.AssignmentSubmissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignment.AssignmentId && s.QuestionId == question.QuestionId && s.UserName == userName);

                bool? isCorrect = null;
                string selectedOptionText = null;
                string selectedOptionLabel = null;
                double? score = null;

                if (question.QuestionType == "MultipleChoice")
                {
                    var selectedOption = question.Options?.FirstOrDefault(o => o.OptionId == submittedAnswer);
                    if (selectedOption != null)
                    {
                        isCorrect = selectedOption.IsCorrect;
                        selectedOptionLabel = selectedOption.OptionLabel;
                        selectedOptionText = null;
                        // Tự động chấm điểm: Nếu đúng thì được MaxScore, sai thì 0
                        double? maxScore = question.MaxScore ?? 0; // Gán mặc định là 0 nếu MaxScore là null
                        score = isCorrect == true ? maxScore : 0;
                        _logger.LogInformation($"MultipleChoice Question {question.QuestionId}, SelectedOption: {selectedOption.OptionId}, IsCorrect: {isCorrect}, Label: {selectedOptionLabel}, Score: {score}, MaxScore: {maxScore}");
                    }
                    else
                    {
                        _logger.LogWarning($"Option {submittedAnswer} not found for Question {question.QuestionId}.");
                        continue;
                    }
                }
                else if (question.QuestionType == "Essay")
                {
                    isCorrect = null; // Tự luận không chấm tự động
                    selectedOptionText = submittedAnswer;
                    selectedOptionLabel = null;
                    score = null; // Đợi giáo viên chấm điểm
                    _logger.LogInformation($"Essay Question {question.QuestionId}, Answer: {submittedAnswer}");
                }
                else
                {
                    _logger.LogWarning($"Unsupported question type {question.QuestionType} for Question {question.QuestionId}.");
                    continue;
                }

                if (existingSubmission == null)
                {
                    // Tạo mới bản ghi nộp bài
                    var submission = new AssignmentSubmission
                    {
                        SubmissionId = Guid.NewGuid().ToString(),
                        AssignmentId = assignment.AssignmentId,
                        QuestionId = question.QuestionId,
                        UserName = userName,
                        IsCorrect = isCorrect,
                        SubmittedDate = DateTime.Now,
                        SelectedOptionText = selectedOptionText,
                        SelectedOptionLabel = selectedOptionLabel,
                        Score = score,
                        Feedback = null
                    };
                    _context.AssignmentSubmissions.Add(submission);
                    submissionCount++;
                    _logger.LogInformation($"Added new submission for Question {question.QuestionId}, Score: {score}");
                }
                else
                {
                    // Cập nhật bản ghi nộp bài hiện có
                    existingSubmission.IsCorrect = isCorrect;
                    existingSubmission.SubmittedDate = DateTime.Now;
                    existingSubmission.SelectedOptionText = selectedOptionText;
                    existingSubmission.SelectedOptionLabel = selectedOptionLabel;
                    existingSubmission.Score = score;
                    existingSubmission.Feedback = null;
                    _logger.LogInformation($"Updated existing submission for Question {question.QuestionId}, Score: {score}");
                }
            }

            if (submissionCount == 0 && answers.Any(a => !string.IsNullOrWhiteSpace(a.Value)))
            {
                _logger.LogWarning("No valid submissions were processed.");
                TempData["Error"] = "Không có câu trả lời hợp lệ nào được xử lý. Vui lòng kiểm tra lại các đáp án đã chọn.";
                return RedirectToAction("CourseDetails", "Course", new { id = courseId });
            }

            try
            {
                _logger.LogInformation($"Saving changes. Number of submissions to save: {submissionCount}");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved submissions.");
                TempData["Success"] = "Nộp bài tập thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving submissions.");
                TempData["Error"] = $"Có lỗi xảy ra khi nộp bài: {ex.Message}";
            }

            return RedirectToAction("Details", "Course", new { id = courseId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AutoSubmitTest(string assignmentId, Dictionary<string, string> SelectedAnswers)
        {
            _logger.LogInformation($"AutoSubmitTest called. AssignmentId: {assignmentId}, SelectedAnswers Count: {SelectedAnswers?.Count ?? 0}");

            if (string.IsNullOrEmpty(assignmentId))
            {
                _logger.LogWarning("AssignmentId is null or empty.");
                TempData["Error"] = "ID bài kiểm tra không hợp lệ.";
                return RedirectToAction("AllTests", "Course");
            }

            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName could not be determined from claims.");
                TempData["Error"] = "Bạn cần đăng nhập để nộp bài.";
                var courseIdFromAssignment = await GetCourseIdFromAssignment(assignmentId);
                return RedirectToAction("AllTests", "Course", new { id = courseIdFromAssignment ?? assignmentId });
            }

            var assignment = await _context.Assignments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
            {
                _logger.LogWarning($"Assignment with ID {assignmentId} not found.");
                TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                return RedirectToAction("AllTests", "Course");
            }

            var courseId = assignment.CourseId;
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId not found for the assignment.");
                TempData["Error"] = "Không tìm thấy khóa học liên quan đến bài kiểm tra này.";
                return RedirectToAction("AllTests", "Course", new { id = assignmentId });
            }

            var questions = assignment.Questions.ToList();
            if (!questions.Any())
            {
                _logger.LogWarning($"No questions found for assignment {assignmentId}.");
                TempData["Error"] = "Bài kiểm tra này không có câu hỏi nào để nộp.";
                return RedirectToAction("AllTests", "Course", new { id = courseId });
            }

            int submissionCount = 0;

            foreach (var question in questions)
            {
                var submittedAnswer = SelectedAnswers?.ContainsKey(question.QuestionId) == true ? SelectedAnswers[question.QuestionId] : null;

                bool? isCorrect = null;
                string selectedOptionText = null;
                string selectedOptionLabel = null;
                double? score = null;
                string submissionContent = submittedAnswer;

                if (question.QuestionType == "MultipleChoice")
                {
                    if (!string.IsNullOrWhiteSpace(submittedAnswer))
                    {
                        var selectedOption = question.Options?.FirstOrDefault(o => o.OptionId == submittedAnswer);
                        if (selectedOption != null)
                        {
                            isCorrect = selectedOption.IsCorrect;
                            selectedOptionLabel = selectedOption.OptionLabel;
                            submissionContent = selectedOption.OptionId;
                            // Chuyển đổi MaxScore từ double? sang double bằng cách làm tròn
                            double maxScore = question.MaxScore.HasValue ? Math.Round(question.MaxScore.Value, 1) : 0;
                            score = isCorrect == true ? maxScore : 0;
                            _logger.LogInformation($"MultipleChoice Question {question.QuestionId}, SelectedOption: {selectedOption.OptionId}, IsCorrect: {isCorrect}, MaxScore: {maxScore}, Score: {score}");
                        }
                        else
                        {
                            _logger.LogWarning($"Option {submittedAnswer} not found for Question {question.QuestionId}. Setting as null.");
                            submissionContent = null;
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"No answer provided for MultipleChoice Question {question.QuestionId}. Score set to 0.");
                        score = 0;
                    }
                }
                else if (question.QuestionType == "Essay")
                {
                    selectedOptionText = submittedAnswer;
                    submissionContent = submittedAnswer;
                    _logger.LogInformation($"Essay Question {question.QuestionId}, Answer: {submittedAnswer ?? "null"}");
                }
                else
                {
                    _logger.LogWarning($"Unsupported question type {question.QuestionType} for Question {question.QuestionId}.");
                    continue;
                }

                var existingSubmission = await _context.AssignmentSubmissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignment.AssignmentId && s.QuestionId == question.QuestionId && s.UserName == userName);

                if (existingSubmission == null)
                {
                    var submission = new AssignmentSubmission
                    {
                        SubmissionId = Guid.NewGuid().ToString(),
                        AssignmentId = assignment.AssignmentId,
                        QuestionId = question.QuestionId,
                        UserName = userName,
                        IsCorrect = isCorrect,
                        SubmittedDate = DateTime.Now,
                        SelectedOptionText = selectedOptionText,
                        SelectedOptionLabel = selectedOptionLabel,
                        Score = score,
                        Feedback = null
                    };
                    _context.AssignmentSubmissions.Add(submission);
                    submissionCount++;
                    _logger.LogInformation($"Added new submission for Question {question.QuestionId} with SubmissionContent: {submissionContent ?? "null"}, Score: {score}");
                }
                else
                {
                    existingSubmission.IsCorrect = isCorrect;
                    existingSubmission.SubmittedDate = DateTime.Now;
                    existingSubmission.SelectedOptionText = selectedOptionText;
                    existingSubmission.SelectedOptionLabel = selectedOptionLabel;
                    existingSubmission.Score = score;
                    _logger.LogInformation($"Updated existing submission for Question {question.QuestionId} with SubmissionContent: {submissionContent ?? "null"}, Score: {score}");
                }
            }

            try
            {
                _logger.LogInformation($"Saving changes. Number of submissions to save: {submissionCount}");
                int changes = await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully saved {changes} changes to the database.");
                TempData["Success"] = "Bài kiểm tra đã được tự động lưu khi hết thời gian.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving submissions.");
                TempData["Error"] = $"Có lỗi xảy ra khi lưu bài: {ex.Message}";
                return RedirectToAction("AllTests", "Course", new { id = courseId });
            }

            return RedirectToAction("AllTests", "Course", new { id = courseId });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitTest(string assignmentId, Dictionary<string, string> SelectedAnswers)
        {
            _logger.LogInformation($"SubmitTest called. AssignmentId: {assignmentId}, SelectedAnswers Count: {SelectedAnswers?.Count ?? 0}");

            if (string.IsNullOrEmpty(assignmentId))
            {
                _logger.LogWarning("AssignmentId is null or empty.");
                TempData["Error"] = "ID bài kiểm tra không hợp lệ.";
                return RedirectToAction("AllTests", "Course");
            }

            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName could not be determined from claims.");
                TempData["Error"] = "Bạn cần đăng nhập để nộp bài.";
                var courseIdFromAssignment = await GetCourseIdFromAssignment(assignmentId);
                return RedirectToAction("AllTests", "Course", new { id = courseIdFromAssignment ?? assignmentId });
            }

            var assignment = await _context.Assignments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

            if (assignment == null)
            {
                _logger.LogWarning($"Assignment with ID {assignmentId} not found.");
                TempData["Error"] = "Không tìm thấy bài kiểm tra.";
                return RedirectToAction("AllTests", "Course");
            }

            var courseId = assignment.CourseId;
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId not found for the assignment.");
                TempData["Error"] = "Không tìm thấy khóa học liên quan đến bài kiểm tra này.";
                return RedirectToAction("AllTests", "Course", new { id = assignmentId });
            }

            var questions = assignment.Questions.ToList();
            if (!questions.Any())
            {
                _logger.LogWarning($"No questions found for assignment {assignmentId}.");
                TempData["Error"] = "Bài kiểm tra này không có câu hỏi nào để nộp.";
                return RedirectToAction("AllTests", "Course", new { id = courseId });
            }

            int submissionCount = 0;

            foreach (var question in questions)
            {
                var submittedAnswer = SelectedAnswers?.ContainsKey(question.QuestionId) == true ? SelectedAnswers[question.QuestionId] : null;

                bool? isCorrect = null;
                string selectedOptionText = null;
                string selectedOptionLabel = null;
                double? score = null;
                string submissionContent = submittedAnswer;

                if (question.QuestionType == "MultipleChoice")
                {
                    if (!string.IsNullOrWhiteSpace(submittedAnswer))
                    {
                        var selectedOption = question.Options?.FirstOrDefault(o => o.OptionId == submittedAnswer);
                        if (selectedOption != null)
                        {
                            isCorrect = selectedOption.IsCorrect;
                            selectedOptionLabel = selectedOption.OptionLabel;
                            submissionContent = selectedOption.OptionId;
                            // Chuyển đổi MaxScore từ double? sang double bằng cách làm tròn
                            double maxScore = question.MaxScore.HasValue ? Math.Round(question.MaxScore.Value, 1) : 0;
                            score = isCorrect == true ? maxScore : 0;
                            _logger.LogInformation($"MultipleChoice Question {question.QuestionId}, SelectedOption: {selectedOption.OptionId}, IsCorrect: {isCorrect}, MaxScore: {maxScore}, Score: {score}");
                        }
                        else
                        {
                            _logger.LogWarning($"Option {submittedAnswer} not found for Question {question.QuestionId}. Setting as null.");
                            submissionContent = null;
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"No answer provided for MultipleChoice Question {question.QuestionId}. Score set to 0.");
                        score = 0;
                    }
                }
                else if (question.QuestionType == "Essay")
                {
                    isCorrect = null;
                    selectedOptionText = submittedAnswer;
                    selectedOptionLabel = null;
                    submissionContent = submittedAnswer;
                    _logger.LogInformation($"Essay Question {question.QuestionId}, Answer: {submittedAnswer ?? "null"}");
                }
                else
                {
                    _logger.LogWarning($"Unsupported question type {question.QuestionType} for Question {question.QuestionId}.");
                    continue;
                }

                var existingSubmission = await _context.AssignmentSubmissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == assignment.AssignmentId && s.QuestionId == question.QuestionId && s.UserName == userName);

                if (existingSubmission == null)
                {
                    var submission = new AssignmentSubmission
                    {
                        SubmissionId = Guid.NewGuid().ToString(),
                        AssignmentId = assignment.AssignmentId,
                        QuestionId = question.QuestionId,
                        UserName = userName,
                        IsCorrect = isCorrect,
                        SubmittedDate = DateTime.Now,
                        SelectedOptionText = selectedOptionText,
                        SelectedOptionLabel = selectedOptionLabel,
                        Score = score,
                        Feedback = null
                    };
                    _context.AssignmentSubmissions.Add(submission);
                    submissionCount++;
                    _logger.LogInformation($"Added new submission for Question {question.QuestionId} with SubmissionContent: {submissionContent ?? "null"}, Score: {score}");
                }
                else
                {
                    existingSubmission.IsCorrect = isCorrect;
                    existingSubmission.SubmittedDate = DateTime.Now;
                    existingSubmission.SelectedOptionText = selectedOptionText;
                    existingSubmission.SelectedOptionLabel = selectedOptionLabel;
                    existingSubmission.Score = score;
                    _logger.LogInformation($"Updated existing submission for Question {question.QuestionId} with SubmissionContent: {submissionContent ?? "null"}, Score: {score}");
                }
            }

            try
            {
                _logger.LogInformation($"Saving changes. Number of submissions to save: {submissionCount}");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully saved submissions.");
                TempData["Success"] = "Nộp bài kiểm tra thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving submissions.");
                TempData["Error"] = $"Có lỗi xảy ra khi nộp bài: {ex.Message}";
            }

            return RedirectToAction("AllTests", "Course", new { id = courseId });
        }

        private async Task<string> GetCourseIdFromAssignment(string assignmentId)
        {
            var assignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
            return assignment?.CourseId;
        }

        // GET: Assignment/ManageAssignments
        public async Task<IActionResult> ManageAssignments(string lessonId, int page = 1)
        {
            if (string.IsNullOrEmpty(lessonId))
            {
                _logger.LogWarning("lessonId is null or empty.");
                TempData["Error"] = "ID bài học không hợp lệ.";
                return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName is null or empty.");
                TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
                return RedirectToAction("Login", "Account");
            }

            var lesson = await _context.Lessons
                .Include(l => l.Assignments)
                    .ThenInclude(a => a.Submissions)
                .Include(l => l.Course)
                    .ThenInclude(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);

            if (lesson == null)
            {
                _logger.LogWarning("Lesson with ID {LessonId} not found.", lessonId);
                TempData["Error"] = $"Không tìm thấy bài học với ID {lessonId}.";
                return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
            }

            if (!User.IsInRole("Admin") && (!lesson.Course?.CourseInstructors?.Any(ci => ci.User?.UserName == userName) ?? true))
            {
                _logger.LogWarning("User {UserName} has no permission for lesson {LessonId}.", userName, lessonId);
                TempData["Error"] = "Bạn không có quyền truy cập vào bài học này.";
                return RedirectToAction("ManageCourses", "Course");
            }

            // Phân trang assignments
            int pageSize = 10;
            var allAssignments = lesson.Assignments?.OrderByDescending(a => a.CreatedDate).ToList() ?? new List<Assignment>();
            int totalAssignments = allAssignments.Count();
            int totalPages = (int)Math.Ceiling((double)totalAssignments / pageSize);
            var pagedAssignments = allAssignments.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            lesson.Assignments = pagedAssignments;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            _logger.LogInformation("Successfully loaded lesson {LessonId} with {AssignmentCount} assignments.", lessonId, lesson.Assignments?.Count ?? 0);
            return View("~/Views/Assignment/ManageAssignments.cshtml", lesson);
        }

        // GET: Assignment/CreateAssignment
        public IActionResult CreateAssignment(string lessonId)
        {
            if (string.IsNullOrEmpty(lessonId))
            {
                _logger.LogWarning("LessonId is null or empty when accessing CreateAssignment.");
                return NotFound();
            }

            var lesson = _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefault(l => l.LessonId == lessonId);

            if (lesson == null)
            {
                _logger.LogWarning("Lesson not found for LessonId: {LessonId}", lessonId);
                return NotFound();
            }

            if (string.IsNullOrEmpty(lesson.CourseId))
            {
                _logger.LogWarning("CourseId is null for LessonId: {LessonId}", lessonId);
                return NotFound();
            }

            var assignment = new Assignment
            {
                LessonId = lessonId,
                CourseId = lesson.CourseId,
                CreatedDate = DateTime.Now,
                AssignmentType = "Exercise"
            };

            _logger.LogInformation("Assigned CourseId: {CourseId}, LessonId: {LessonId}", assignment.CourseId, assignment.LessonId);
            return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
        }

        // POST: Assignment/CreateAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAssignment(Assignment assignment)
        {
            ModelState.Remove("AssignmentId");
            ModelState.Remove("Course");
            ModelState.Remove("Lesson");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("AssignmentType");

            if (assignment.Questions != null)
            {
                for (int i = 0; i < assignment.Questions.Count; i++)
                {
                    ModelState.Remove($"Questions[{i}].QuestionId");
                    ModelState.Remove($"Questions[{i}].AssignmentId");
                    ModelState.Remove($"Questions[{i}].Assignment");

                    var question = assignment.Questions[i];
                    if (question != null)
                    {
                        if (question.QuestionType == "Essay")
                        {
                            if (question.Options != null)
                            {
                                for (int j = 0; j < question.Options.Count; j++)
                                {
                                    ModelState.Remove($"Questions[{i}].Options[{j}].OptionId");
                                    ModelState.Remove($"Questions[{i}].Options[{j}].QuestionId");
                                    ModelState.Remove($"Questions[{i}].Options[{j}].Question");
                                    ModelState.Remove($"Questions[{i}].Options[{j}].OptionText");
                                    ModelState.Remove($"Questions[{i}].Options[{j}].IsCorrect");
                                }
                            }
                        }
                        else if (question.QuestionType == "MultipleChoice" && question.Options != null)
                        {
                            for (int j = 0; j < question.Options.Count; j++)
                            {
                                ModelState.Remove($"Questions[{i}].Options[{j}].OptionId");
                                ModelState.Remove($"Questions[{i}].Options[{j}].QuestionId");
                                ModelState.Remove($"Questions[{i}].Options[{j}].Question");
                            }
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}", string.Join(", ", errors));
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập vào: " + string.Join(", ", errors);
                return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
            }

            try
            {
                assignment.AssignmentId = Guid.NewGuid().ToString();
                assignment.CreatedDate = DateTime.Now;
                assignment.AssignmentType = "Exercise";

                if (assignment.Questions == null || !assignment.Questions.Any())
                {
                    TempData["Error"] = "Phải có ít nhất một câu hỏi.";
                    return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                }

                var validQuestions = new List<AssignmentQuestion>();
                int questionIndex = 0;
                foreach (var question in assignment.Questions)
                {
                    if (question == null || string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        questionIndex++;
                        continue;
                    }

                    question.QuestionId = Guid.NewGuid().ToString();
                    question.OrderNumber = questionIndex + 1;

                    if (question.QuestionType == "MultipleChoice")
                    {
                        if (question.Options == null || question.Options.Count < 2)
                        {
                            TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án.";
                            return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                        }

                        var validOptions = question.Options
                            .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                            .ToList();

                        if (validOptions.Count < 2)
                        {
                            TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án hợp lệ.";
                            return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                        }

                        string formKey = $"Questions[{questionIndex}].CorrectOptionIndex";
                        if (!Request.Form.ContainsKey(formKey) || string.IsNullOrEmpty(Request.Form[formKey]))
                        {
                            TempData["Error"] = $"Vui lòng chọn đáp án đúng cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                            return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                        }

                        if (!int.TryParse(Request.Form[formKey], out int correctOptionIndex) || correctOptionIndex < 0 || correctOptionIndex >= validOptions.Count)
                        {
                            TempData["Error"] = $"Đáp án đúng không hợp lệ cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                            return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                        }

                        for (int i = 0; i < validOptions.Count; i++)
                        {
                            var option = validOptions[i];
                            option.OptionId = Guid.NewGuid().ToString();
                            option.QuestionId = question.QuestionId;
                            option.IsCorrect = (i == correctOptionIndex);
                        }
                        question.Options = validOptions;
                    }
                    else if (question.QuestionType == "Essay")
                    {
                        question.Options = null;
                    }
                    else
                    {
                        TempData["Error"] = $"Loại câu hỏi không hợp lệ cho câu hỏi {questionIndex + 1}.";
                        return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                    }

                    validQuestions.Add(question);
                    questionIndex++;
                }

                if (!validQuestions.Any())
                {
                    TempData["Error"] = "Phải có ít nhất một câu hỏi hợp lệ.";
                    return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                }

                int totalMaxScore = validQuestions.Sum(q => (int)(q.MaxScore ?? 0));
                if (totalMaxScore != 10)
                {
                    TempData["Error"] = "Tổng điểm tối đa của tất cả câu hỏi phải bằng 10.";
                    return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
                }

                assignment.Questions = null;
                _assignmentRepository.Add(assignment);
                _assignmentRepository.Save();

                foreach (var question in validQuestions)
                {
                    question.AssignmentId = assignment.AssignmentId;
                    _questionRepository.Add(question);
                    if (question.Options != null)
                    {
                        foreach (var option in question.Options)
                        {
                            _optionRepository.Add(option);
                        }
                    }
                }

                _questionRepository.Save();
                _optionRepository.Save();

                TempData["Success"] = "Tạo bài tập thành công!";
                return RedirectToAction("ManageAssignments", new { lessonId = assignment.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assignment: {Message}", ex.Message);
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                return View("~/Views/Assignment/CreateAssignment.cshtml", assignment);
            }
        }

        // GET: Assignment/EditAssignment
        public IActionResult EditAssignment(string assignmentId)
        {
            if (string.IsNullOrEmpty(assignmentId))
            {
                return NotFound();
            }

            var assignment = _assignmentRepository.GetById(assignmentId);
            if (assignment == null)
            {
                return NotFound();
            }

            assignment.AssignmentType = "Exercise";
            assignment.Questions = assignment.Questions?.OrderBy(q => q.OrderNumber).ToList();

            if (assignment.Questions != null)
            {
                foreach (var question in assignment.Questions)
                {
                    if (question.QuestionType == "MultipleChoice" && question.QuestionId != null)
                    {
                        question.Options = _optionRepository.GetByQuestionId(question.QuestionId)
                            .OrderBy(o => o.OptionLabel)
                            .ToList();
                    }
                }
            }

            return View("~/Views/Assignment/EditAssignment.cshtml", assignment);
        }

        // POST: Assignment/EditAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAssignment(string assignmentId, Assignment updatedAssignment)
        {
            if (assignmentId != updatedAssignment.AssignmentId)
            {
                return NotFound();
            }

            ModelState.Remove("Course");
            ModelState.Remove("Lesson");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("AssignmentType");

            if (updatedAssignment.Questions != null)
            {
                for (int i = 0; i < updatedAssignment.Questions.Count; i++)
                {
                    if (updatedAssignment.Questions[i] == null || string.IsNullOrWhiteSpace(updatedAssignment.Questions[i].QuestionText))
                    {
                        updatedAssignment.Questions[i] = null;
                        continue;
                    }

                    ModelState.Remove($"Questions[{i}].AssignmentId");
                    ModelState.Remove($"Questions[{i}].Assignment");
                    ModelState.Remove($"Questions[{i}].QuestionId");

                    if (updatedAssignment.Questions[i]?.Options != null)
                    {
                        for (int j = 0; j < updatedAssignment.Questions[i].Options.Count; j++)
                        {
                            if (updatedAssignment.Questions[i].Options[j] == null ||
                                string.IsNullOrWhiteSpace(updatedAssignment.Questions[i].Options[j].OptionText))
                            {
                                updatedAssignment.Questions[i].Options[j] = null;
                                continue;
                            }

                            ModelState.Remove($"Questions[{i}].Options[{j}].QuestionId");
                            ModelState.Remove($"Questions[{i}].Options[{j}].Question");
                            ModelState.Remove($"Questions[{i}].Options[{j}].OptionId");
                        }

                        if (updatedAssignment.Questions[i].Options != null)
                        {
                            updatedAssignment.Questions[i].Options = updatedAssignment.Questions[i].Options
                                .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                                .ToList();
                        }
                    }
                }

                updatedAssignment.Questions = updatedAssignment.Questions
                    .Where(q => q != null && !string.IsNullOrWhiteSpace(q.QuestionText))
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
                TempData["Title"] = updatedAssignment.Title;
                TempData["Description"] = updatedAssignment.Description;
                TempData["DurationMinutes"] = updatedAssignment.DurationMinutes?.ToString();
                return View("~/Views/Assignment/EditAssignment.cshtml", updatedAssignment);
            }

            try
            {
                var existingAssignment = _assignmentRepository.GetById(assignmentId);
                if (existingAssignment == null)
                {
                    return NotFound();
                }

                existingAssignment.Title = updatedAssignment.Title;
                existingAssignment.Description = updatedAssignment.Description;
                existingAssignment.AssignmentType = "Exercise";
                existingAssignment.DurationMinutes = updatedAssignment.DurationMinutes;

                var existingQuestions = _questionRepository.GetByAssignmentId(assignmentId).ToList();
                var existingQuestionIds = existingQuestions.Select(q => q.QuestionId).ToList();

                if (!updatedAssignment.Questions.Any(q => !string.IsNullOrEmpty(q?.QuestionText)))
                {
                    TempData["Error"] = "Phải có ít nhất một câu hỏi hợp lệ.";
                    return View("~/Views/Assignment/EditAssignment.cshtml", updatedAssignment);
                }

                var validQuestions = new List<AssignmentQuestion>();
                for (int questionIndex = 0; questionIndex < updatedAssignment.Questions.Count; questionIndex++)
                {
                    var question = updatedAssignment.Questions[questionIndex];
                    if (question == null || string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(question.QuestionId))
                    {
                        question.QuestionId = Guid.NewGuid().ToString();
                    }
                    question.AssignmentId = assignmentId;
                    question.OrderNumber = questionIndex + 1;

                    var existingQuestion = existingQuestions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
                    if (existingQuestion != null && existingQuestion.QuestionType != question.QuestionType)
                    {
                        if (existingQuestion.QuestionType == "MultipleChoice" && question.QuestionType == "Essay")
                        {
                            var options = _optionRepository.GetByQuestionId(question.QuestionId).ToList();
                            foreach (var option in options)
                            {
                                _optionRepository.Delete(option.OptionId);
                            }
                            question.Options = null;
                        }
                    }

                    if (question.QuestionType == "MultipleChoice")
                    {
                        ProcessMultipleChoiceQuestion(question, questionIndex, updatedAssignment);
                        validQuestions.Add(question);

                        if (!existingQuestionIds.Contains(question.QuestionId))
                        {
                            _questionRepository.Add(question);
                        }
                        else if (existingQuestion != null)
                        {
                            existingQuestion.QuestionText = question.QuestionText;
                            existingQuestion.MaxScore = question.MaxScore;
                            existingQuestion.OrderNumber = question.OrderNumber;
                            existingQuestion.QuestionType = question.QuestionType;
                            _questionRepository.Update(existingQuestion);
                        }
                    }
                    else if (question.QuestionType == "Essay")
                    {
                        question.Options = null;
                        validQuestions.Add(question);

                        if (!existingQuestionIds.Contains(question.QuestionId))
                        {
                            _questionRepository.Add(question);
                        }
                        else if (existingQuestion != null)
                        {
                            existingQuestion.QuestionText = question.QuestionText;
                            existingQuestion.MaxScore = question.MaxScore;
                            existingQuestion.OrderNumber = question.OrderNumber;
                            existingQuestion.QuestionType = question.QuestionType;
                            existingQuestion.Options = null;
                            _questionRepository.Update(existingQuestion);
                        }
                    }
                    else
                    {
                        TempData["Error"] = $"Loại câu hỏi không hợp lệ cho câu hỏi {questionIndex + 1}.";
                        return View("~/Views/Assignment/EditAssignment.cshtml", updatedAssignment);
                    }
                }

                var updatedQuestionIds = validQuestions.Select(q => q.QuestionId).ToList();
                var questionsToDelete = existingQuestions.Where(q => !updatedQuestionIds.Contains(q.QuestionId)).ToList();
                foreach (var question in questionsToDelete)
                {
                    var options = _optionRepository.GetByQuestionId(question.QuestionId).ToList();
                    foreach (var option in options)
                    {
                        _optionRepository.Delete(option.OptionId);
                    }
                    _questionRepository.Delete(question.QuestionId);
                }

                int totalMaxScore = validQuestions.Sum(q => (int)(q.MaxScore ?? 0));
                if (totalMaxScore != 10)
                {
                    TempData["Error"] = "Tổng điểm tối đa của tất cả câu hỏi phải bằng 10.";
                    return View("~/Views/Assignment/EditAssignment.cshtml", updatedAssignment);
                }

                _assignmentRepository.Update(existingAssignment);
                _assignmentRepository.Save();
                _questionRepository.Save();
                _optionRepository.Save();

                TempData["Success"] = "Cập nhật bài tập thành công!";
                return RedirectToAction("ManageAssignments", new { lessonId = existingAssignment.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing assignment: {Message}", ex.Message);
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                TempData["Title"] = updatedAssignment.Title;
                TempData["Description"] = updatedAssignment.Description;
                TempData["DurationMinutes"] = updatedAssignment.DurationMinutes?.ToString();
                return View("~/Views/Assignment/EditAssignment.cshtml", updatedAssignment);
            }
        }

        // POST: Assignment/DeleteAssignment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssignment(string assignmentId)
        {
            if (string.IsNullOrEmpty(assignmentId))
            {
                _logger.LogWarning("AssignmentId is null or empty.");
                TempData["Error"] = "ID bài tập hoặc bài kiểm tra không hợp lệ.";
                return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
            }

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("UserName is null or empty.");
                TempData["Error"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
                return RedirectToAction("Login", "Account");
            }

            Assignment assignment = null;
            try
            {
                assignment = await _context.Assignments
                    .Include(a => a.Course)
                        .ThenInclude(c => c.CourseInstructors)
                    .Include(a => a.Lesson)
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .Include(a => a.Submissions)
                    .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment with ID {AssignmentId} not found.", assignmentId);
                    TempData["Error"] = "Không tìm thấy bài tập hoặc bài kiểm tra.";
                    return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
                }

                if (!User.IsInRole("Admin") && (!assignment.Course?.CourseInstructors?.Any(ci => ci.UserName == userName) ?? true))
                {
                    _logger.LogWarning("User {UserName} has no permission to delete assignment {AssignmentId}.", userName, assignmentId);
                    TempData["Error"] = "Bạn không có quyền xóa bài tập hoặc bài kiểm tra này.";
                    return RedirectToAction("ManageCourses", "Course");
                }

                if (assignment.Submissions != null && assignment.Submissions.Any())
                {
                    _context.AssignmentSubmissions.RemoveRange(assignment.Submissions);
                    _logger.LogInformation("Removed {SubmissionCount} submissions for AssignmentId: {AssignmentId}", assignment.Submissions.Count, assignmentId);
                }

                if (assignment.Questions != null && assignment.Questions.Any())
                {
                    foreach (var question in assignment.Questions)
                    {
                        if (question.Options != null && question.Options.Any())
                        {
                            _optionRepository.DeleteRange(question.Options.Select(o => o.OptionId));
                            _logger.LogInformation("Removed {OptionCount} options for QuestionId: {QuestionId}", question.Options.Count, question.QuestionId);
                        }
                        _questionRepository.Delete(question.QuestionId);
                        _logger.LogInformation("Removed question with QuestionId: {QuestionId}", question.QuestionId);
                    }
                }

                _assignmentRepository.Delete(assignmentId);
                _assignmentRepository.Save();
                _questionRepository.Save();
                _optionRepository.Save();
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Xóa {(assignment.AssignmentType == "Test" ? "bài kiểm tra" : "bài tập")} thành công!";
                _logger.LogInformation("Assignment {AssignmentId} ({AssignmentType}) deleted successfully by {UserName}.", assignmentId, assignment.AssignmentType, userName);

                if (assignment.AssignmentType == "Test")
                {
                    return RedirectToAction("ManageTests", new { courseId = assignment.CourseId });
                }
                return RedirectToAction("ManageAssignments", new { lessonId = assignment.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment {AssignmentId}.", assignmentId);
                TempData["Error"] = $"Có lỗi xảy ra khi xóa {(assignment?.AssignmentType == "Test" ? "bài kiểm tra" : "bài tập")}: {ex.Message}";
                return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
            }
        }

        private void ProcessMultipleChoiceQuestion(AssignmentQuestion question, int questionIndex, Assignment updatedAssignment)
        {
            if (question.Options == null || !question.Options.Any())
            {
                TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án.";
                throw new InvalidOperationException($"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án.");
            }

            var validOptions = question.Options
                .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                .ToList();

            if (validOptions.Count < 2)
            {
                TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án hợp lệ.";
                throw new InvalidOperationException($"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án hợp lệ.");
            }

            string formKey = $"Questions[{questionIndex}].CorrectOptionIndex";
            if (!Request.Form.ContainsKey(formKey) || string.IsNullOrEmpty(Request.Form[formKey]))
            {
                TempData["Error"] = $"Vui lòng chọn đáp án đúng cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                throw new InvalidOperationException($"Vui lòng chọn đáp án đúng cho câu hỏi trắc nghiệm {questionIndex + 1}.");
            }

            if (!int.TryParse(Request.Form[formKey], out int correctOptionIndex) || correctOptionIndex < 0 || correctOptionIndex >= validOptions.Count)
            {
                TempData["Error"] = $"Đáp án đúng không hợp lệ cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                throw new InvalidOperationException($"Đáp án đúng không hợp lệ cho câu hỏi trắc nghiệm {questionIndex + 1}.");
            }

            var existingOptions = _optionRepository.GetByQuestionId(question.QuestionId).ToList();
            var existingOptionIds = existingOptions.Select(o => o.OptionId).ToList();

            var updatedOptionIds = validOptions.Where(o => !string.IsNullOrEmpty(o.OptionId)).Select(o => o.OptionId).ToList();
            var optionsToDelete = existingOptions.Where(o => !updatedOptionIds.Contains(o.OptionId)).ToList();
            foreach (var option in optionsToDelete)
            {
                _optionRepository.Delete(option.OptionId);
            }

            for (int i = 0; i < validOptions.Count; i++)
            {
                var option = validOptions[i];
                option.OptionLabel = ((char)(65 + i)).ToString();
                option.IsCorrect = (i == correctOptionIndex);

                if (string.IsNullOrEmpty(option.OptionId))
                {
                    option.OptionId = Guid.NewGuid().ToString();
                    option.QuestionId = question.QuestionId;
                    _optionRepository.Add(option);
                }
                else
                {
                    var existingOption = existingOptions.FirstOrDefault(o => o.OptionId == option.OptionId);
                    if (existingOption != null)
                    {
                        existingOption.OptionText = option.OptionText;
                        existingOption.OptionLabel = option.OptionLabel;
                        existingOption.IsCorrect = option.IsCorrect;
                        _optionRepository.Update(existingOption);
                    }
                }
            }
            question.Options = validOptions;
        }
    }
}