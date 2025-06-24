using LearningManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LearningManagementSystem.Repositories;
using LearningManagementSystem.Data;
using System.Security.Claims;

namespace LearningManagementSystem.Controllers
{
    public class TestController : Controller
    {
        private readonly LMSContext _context;
        private readonly IAssignmentRepository _assignmentRepository;
        private readonly IAssignmentQuestionRepository _questionRepository;
        private readonly IAssignmentQuestionOptionRepository _optionRepository;
        private readonly ILogger<TestController> _logger;

        public TestController(
            LMSContext context,
            IAssignmentRepository assignmentRepository,
            IAssignmentQuestionRepository questionRepository,
            IAssignmentQuestionOptionRepository optionRepository,
            ILogger<TestController> logger)
        {
            _context = context;
            _assignmentRepository = assignmentRepository;
            _questionRepository = questionRepository;
            _optionRepository = optionRepository;
            _logger = logger;
        }

        // GET: Instructor/ManageTests
        public IActionResult ManageTests(string courseId, int page = 1)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId is null or empty when accessing ManageTests.");
                return NotFound();
            }

            var course = _context.Courses
                .Include(c => c.Assignments.Where(a => a.AssignmentType == "Test"))
                .ThenInclude(a => a.Questions)
                .ThenInclude(q => q.Options)
                .Include(c => c.Assignments.Where(a => a.AssignmentType == "Test")).ThenInclude(a => a.Submissions)
                .FirstOrDefault(c => c.CourseId == courseId);

            if (course == null)
            {
                _logger.LogWarning("Course not found for CourseId: {CourseId}", courseId);
                return NotFound();
            }

            // Phân trang bài kiểm tra
            int pageSize = 10;
            var allTests = course.Assignments?.Where(a => a.AssignmentType == "Test").OrderByDescending(a => a.CreatedDate).ToList() ?? new List<Assignment>();
            int totalTests = allTests.Count();
            int totalPages = (int)Math.Ceiling((double)totalTests / pageSize);
            var pagedTests = allTests.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            course.Assignments = pagedTests;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View("~/Views/Test/ManageTests.cshtml", course); // Cập nhật đường dẫn
        }

        // GET: Instructor/CreateTest
        public IActionResult CreateTest(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                _logger.LogWarning("CourseId is null or empty when accessing CreateTest.");
                return NotFound();
            }

            var course = _context.Courses
                .FirstOrDefault(c => c.CourseId == courseId);

            if (course == null)
            {
                _logger.LogWarning("Course not found for CourseId: {CourseId}", courseId);
                return NotFound();
            }

            var test = new Assignment
            {
                CourseId = courseId,
                CreatedDate = DateTime.Now,
                AssignmentType = "Test"
            };

            _logger.LogInformation("Assigned CourseId: {CourseId}", test.CourseId);
            return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
        }

        // POST: Instructor/CreateTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTest(Assignment test)
        {
            // Tắt validation cho các trường sẽ được gán trong code
            ModelState.Remove("AssignmentId");
            ModelState.Remove("Course");
            ModelState.Remove("Lesson");
            ModelState.Remove("LessonId");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("AssignmentType");

            // Kiểm tra CourseId trước khi tiếp tục
            if (string.IsNullOrEmpty(test.CourseId) || !_context.Courses.Any(c => c.CourseId == test.CourseId))
            {
                _logger.LogWarning("Invalid CourseId: {CourseId}", test.CourseId);
                TempData["Error"] = "Khóa học không hợp lệ.";
                return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
            }

            // Xử lý validation cho Questions và MaxScore
            if (test.Questions == null || !test.Questions.Any())
            {
                _logger.LogWarning("No questions provided for test creation.");
                TempData["Error"] = "Phải có ít nhất một câu hỏi.";
                return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
            }

            var validQuestions = new List<AssignmentQuestion>();
            int questionIndex = 0;
            double totalMaxScore = 0;

            foreach (var question in test.Questions)
            {
                if (question == null || string.IsNullOrWhiteSpace(question.QuestionText))
                {
                    _logger.LogWarning("Skipping invalid question at index {Index}: QuestionText is null or empty", questionIndex);
                    continue;
                }

                ModelState.Remove($"Questions[{questionIndex}].QuestionId");
                ModelState.Remove($"Questions[{questionIndex}].AssignmentId");
                ModelState.Remove($"Questions[{questionIndex}].Assignment");

                if (!question.MaxScore.HasValue || question.MaxScore <= 0)
                {
                    _logger.LogWarning("Invalid MaxScore for question at index {Index}: {MaxScore}", questionIndex, question.MaxScore);
                    TempData["Error"] = $"Điểm tối đa cho câu hỏi {questionIndex + 1} phải lớn hơn 0.";
                    return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
                }

                totalMaxScore += question.MaxScore.Value;
                question.QuestionId = Guid.NewGuid().ToString();
                question.OrderNumber = questionIndex + 1;

                if (question.QuestionType == "MultipleChoice")
                {
                    if (question.Options == null || question.Options.Count < 2)
                    {
                        _logger.LogWarning("Invalid options for MultipleChoice question at index {Index}: Less than 2 options", questionIndex);
                        TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án.";
                        return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
                    }

                    var validOptions = question.Options
                        .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                        .ToList();

                    if (validOptions.Count < 2)
                    {
                        _logger.LogWarning("Less than 2 valid options for MultipleChoice question at index {Index}", questionIndex);
                        TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án hợp lệ.";
                        return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
                    }

                    string formKey = $"Questions[{questionIndex}].CorrectOptionIndex";
                    if (!Request.Form.ContainsKey(formKey) || string.IsNullOrEmpty(Request.Form[formKey]))
                    {
                        _logger.LogWarning("Correct option not selected for MultipleChoice question at index {Index}", questionIndex);
                        TempData["Error"] = $"Vui lòng chọn đáp án đúng cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                        return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
                    }

                    if (!int.TryParse(Request.Form[formKey], out int correctOptionIndex) || correctOptionIndex < 0 || correctOptionIndex >= validOptions.Count)
                    {
                        _logger.LogWarning("Invalid correct option index for question at index {Index}", questionIndex);
                        TempData["Error"] = $"Đáp án đúng không hợp lệ cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                        return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
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
                    _logger.LogWarning("Invalid question type for question at index {Index}: {QuestionType}", questionIndex, question.QuestionType);
                    TempData["Error"] = $"Loại câu hỏi không hợp lệ cho câu hỏi {questionIndex + 1}.";
                    return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
                }

                validQuestions.Add(question);
                questionIndex++;
            }

            if (!validQuestions.Any())
            {
                _logger.LogWarning("No valid questions after validation.");
                TempData["Error"] = "Phải có ít nhất một câu hỏi hợp lệ.";
                return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
            }

            if (totalMaxScore != 10)
            {
                _logger.LogWarning("Total MaxScore {TotalMaxScore} does not equal 10 for AssignmentId: {AssignmentId}", totalMaxScore, test.AssignmentId);
                TempData["Error"] = $"Tổng điểm tối đa phải chính xác bằng 10 (hiện tại là {totalMaxScore}).";
                return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
            }

            try
            {
                test.AssignmentId = Guid.NewGuid().ToString();
                test.CreatedDate = DateTime.Now;
                test.AssignmentType = "Test";
                test.LessonId = null;

                _logger.LogInformation("Attempting to save test with AssignmentId: {AssignmentId}", test.AssignmentId);
                test.Questions = null;
                _assignmentRepository.Add(test);
                _assignmentRepository.Save();
                _logger.LogInformation("Successfully saved Assignment with AssignmentId: {AssignmentId}", test.AssignmentId);

                foreach (var question in validQuestions)
                {
                    question.AssignmentId = test.AssignmentId;
                    _logger.LogInformation("Adding question with QuestionId: {QuestionId} to AssignmentId: {AssignmentId}", question.QuestionId, test.AssignmentId);
                    _questionRepository.Add(question);
                    if (question.Options != null)
                    {
                        foreach (var option in question.Options)
                        {
                            _logger.LogInformation("Adding option with OptionId: {OptionId} to QuestionId: {QuestionId}", option.OptionId, question.QuestionId);
                            _optionRepository.Add(option);
                        }
                    }
                }

                _logger.LogInformation("Saving questions for AssignmentId: {AssignmentId}", test.AssignmentId);
                _questionRepository.Save();
                _logger.LogInformation("Successfully saved questions for AssignmentId: {AssignmentId}", test.AssignmentId);

                _logger.LogInformation("Saving options for AssignmentId: {AssignmentId}", test.AssignmentId);
                _optionRepository.Save();
                _logger.LogInformation("Successfully saved options for AssignmentId: {AssignmentId}", test.AssignmentId);

                TempData["Success"] = "Tạo bài kiểm tra thành công!";
                return RedirectToAction("ManageTests", "Test", new { courseId = test.CourseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test: {Message}. Inner Exception: {InnerException}", ex.Message, ex.InnerException?.Message);
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}. Inner Exception: {ex.InnerException?.Message}";
                return View("~/Views/Test/CreateTest.cshtml", test); // Cập nhật đường dẫn
            }
        }

        // GET: Instructor/EditTest?assignmentId={assignmentId}
        public IActionResult EditTest(string assignmentId)
        {
            if (string.IsNullOrEmpty(assignmentId))
            {
                _logger.LogWarning("AssignmentId is null or empty when accessing EditTest.");
                return NotFound();
            }

            var test = _assignmentRepository.GetById(assignmentId);
            if (test == null)
            {
                _logger.LogWarning("Assignment not found for AssignmentId: {AssignmentId}", assignmentId);
                return NotFound();
            }

            // Đặt AssignmentType là "Test" để đảm bảo giá trị
            test.AssignmentType = "Test";

            // Sắp xếp câu hỏi theo OrderNumber
            test.Questions = test.Questions?.OrderBy(q => q.OrderNumber).ToList();

            // Đảm bảo các thông tin đáp án được load đầy đủ
            if (test.Questions != null)
            {
                foreach (var question in test.Questions)
                {
                    if (question.QuestionType == "MultipleChoice" && question.QuestionId != null)
                    {
                        question.Options = _optionRepository.GetByQuestionId(question.QuestionId)
                            .OrderBy(o => o.OptionLabel)
                            .ToList();
                    }
                }
            }

            return View("~/Views/Test/EditTest.cshtml", test); // Cập nhật đường dẫn
        }

        // POST: Instructor/EditTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditTest(string assignmentId, Assignment updatedTest)
        {
            if (assignmentId != updatedTest.AssignmentId)
            {
                _logger.LogWarning("AssignmentId mismatch: {AssignmentId} does not match {UpdatedAssignmentId}", assignmentId, updatedTest.AssignmentId);
                return NotFound();
            }

            // Tắt validation cho các trường không cần thiết
            ModelState.Remove("Course");
            ModelState.Remove("Lesson");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("AssignmentType");

            // Xác thực Questions và Options
            if (updatedTest.Questions != null)
            {
                for (int i = 0; i < updatedTest.Questions.Count; i++)
                {
                    // Bỏ qua các câu hỏi trống
                    if (updatedTest.Questions[i] == null || string.IsNullOrWhiteSpace(updatedTest.Questions[i].QuestionText))
                    {
                        updatedTest.Questions[i] = null;
                        continue;
                    }

                    ModelState.Remove($"Questions[{i}].AssignmentId");
                    ModelState.Remove($"Questions[{i}].Assignment");
                    ModelState.Remove($"Questions[{i}].QuestionId");

                    if (updatedTest.Questions[i]?.Options != null)
                    {
                        for (int j = 0; j < updatedTest.Questions[i].Options.Count; j++)
                        {
                            if (updatedTest.Questions[i].Options[j] == null || string.IsNullOrWhiteSpace(updatedTest.Questions[i].Options[j].OptionText))
                            {
                                updatedTest.Questions[i].Options[j] = null;
                                continue;
                            }
                            ModelState.Remove($"Questions[{i}].Options[{j}].QuestionId");
                            ModelState.Remove($"Questions[{i}].Options[{j}].Question");
                            ModelState.Remove($"Questions[{i}].Options[{j}].OptionId");
                        }
                        updatedTest.Questions[i].Options = updatedTest.Questions[i].Options
                            .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                            .ToList();
                    }
                }
                updatedTest.Questions = updatedTest.Questions
                    .Where(q => q != null && !string.IsNullOrWhiteSpace(q.QuestionText))
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Vui lòng kiểm tra lại thông tin nhập vào.";
                TempData["Title"] = updatedTest.Title;
                TempData["Description"] = updatedTest.Description;
                TempData["DurationMinutes"] = updatedTest.DurationMinutes?.ToString();
                return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
            }

            try
            {
                var existingTest = _assignmentRepository.GetById(assignmentId);
                if (existingTest == null)
                {
                    _logger.LogWarning("Assignment not found for AssignmentId: {AssignmentId}", assignmentId);
                    return NotFound();
                }

                existingTest.Title = updatedTest.Title;
                existingTest.Description = updatedTest.Description;
                existingTest.AssignmentType = "Test";
                existingTest.DurationMinutes = updatedTest.DurationMinutes;

                var existingQuestions = _questionRepository.GetByAssignmentId(assignmentId).ToList();
                var existingQuestionIds = existingQuestions.Select(q => q.QuestionId).ToList();

                if (!updatedTest.Questions.Any(q => !string.IsNullOrEmpty(q?.QuestionText)))
                {
                    TempData["Error"] = "Phải có ít nhất một câu hỏi hợp lệ.";
                    return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
                }

                var validQuestions = new List<AssignmentQuestion>();
                for (int questionIndex = 0; questionIndex < updatedTest.Questions.Count; questionIndex++)
                {
                    var question = updatedTest.Questions[questionIndex];
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
                        if (question.Options == null || question.Options.Count < 2)
                        {
                            TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án.";
                            return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
                        }
                        var validOptions = question.Options
                            .Where(o => o != null && !string.IsNullOrWhiteSpace(o.OptionText))
                            .ToList();
                        if (validOptions.Count < 2)
                        {
                            TempData["Error"] = $"Câu hỏi trắc nghiệm {questionIndex + 1} phải có ít nhất 2 đáp án hợp lệ.";
                            return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
                        }
                        string formKey = $"Questions[{questionIndex}].CorrectOptionIndex";
                        if (!Request.Form.ContainsKey(formKey) || string.IsNullOrEmpty(Request.Form[formKey]))
                        {
                            TempData["Error"] = $"Vui lòng chọn đáp án đúng cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                            return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
                        }
                        if (!int.TryParse(Request.Form[formKey], out int correctOptionIndex) || correctOptionIndex < 0 || correctOptionIndex >= validOptions.Count)
                        {
                            TempData["Error"] = $"Đáp án đúng không hợp lệ cho câu hỏi trắc nghiệm {questionIndex + 1}.";
                            return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
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
                        validQuestions.Add(question);
                        if (!existingQuestionIds.Contains(question.QuestionId))
                        {
                            _questionRepository.Add(question);
                        }
                        else
                        {
                            if (existingQuestion != null)
                            {
                                existingQuestion.QuestionText = question.QuestionText;
                                existingQuestion.MaxScore = question.MaxScore;
                                existingQuestion.OrderNumber = question.OrderNumber;
                                existingQuestion.QuestionType = question.QuestionType;
                                _questionRepository.Update(existingQuestion);
                            }
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
                        else
                        {
                            if (existingQuestion != null)
                            {
                                existingQuestion.QuestionText = question.QuestionText;
                                existingQuestion.MaxScore = question.MaxScore;
                                existingQuestion.OrderNumber = question.OrderNumber;
                                existingQuestion.QuestionType = question.QuestionType;
                                existingQuestion.Options = null;
                                _questionRepository.Update(existingQuestion);
                            }
                        }
                    }
                    else
                    {
                        TempData["Error"] = $"Loại câu hỏi không hợp lệ cho câu hỏi {questionIndex + 1}.";
                        return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
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
                    return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
                }
                _assignmentRepository.Update(existingTest);
                _assignmentRepository.Save();
                _questionRepository.Save();
                _optionRepository.Save();
                TempData["Success"] = "Cập nhật bài kiểm tra thành công!";
                return RedirectToAction("ManageTests", new { courseId = existingTest.CourseId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
                TempData["Title"] = updatedTest.Title;
                TempData["Description"] = updatedTest.Description;
                TempData["DurationMinutes"] = updatedTest.DurationMinutes?.ToString();
                return View("~/Views/Test/EditTest.cshtml", updatedTest); // Cập nhật đường dẫn
            }
        }

        // POST: Instructor/DeleteTest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(string id)
        {
            if (string.IsNullOrEmpty(id))
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
                    .FirstOrDefaultAsync(a => a.AssignmentId == id);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment with ID {AssignmentId} not found.", id);
                    TempData["Error"] = "Không tìm thấy bài tập hoặc bài kiểm tra.";
                    return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
                }

                if (!User.IsInRole("Admin") && (!assignment.Course?.CourseInstructors?.Any(ci => ci.UserName == userName) ?? true))
                {
                    _logger.LogWarning("User {UserName} has no permission to delete assignment {AssignmentId}.", userName, id);
                    TempData["Error"] = "Bạn không có quyền xóa bài tập hoặc bài kiểm tra này.";
                    return RedirectToAction("ManageCourses", "Course");
                }

                if (assignment.Submissions != null && assignment.Submissions.Any())
                {
                    _context.AssignmentSubmissions.RemoveRange(assignment.Submissions);
                    _logger.LogInformation("Removed {SubmissionCount} submissions for AssignmentId: {AssignmentId}", assignment.Submissions.Count, id);
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

                _assignmentRepository.Delete(id);
                _assignmentRepository.Save();
                _questionRepository.Save();
                _optionRepository.Save();
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Xóa {(assignment.AssignmentType == "Test" ? "bài kiểm tra" : "bài tập")} thành công!";
                _logger.LogInformation("Assignment {AssignmentId} ({AssignmentType}) deleted successfully by {UserName}.", id, assignment.AssignmentType, userName);

                if (assignment.AssignmentType == "Test")
                {
                    return RedirectToAction("ManageTests", new { courseId = assignment.CourseId });
                }
                return RedirectToAction("ManageAssignments", new { lessonId = assignment.LessonId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting assignment {AssignmentId}.", id);
                TempData["Error"] = $"Có lỗi xảy ra khi xóa {(assignment?.AssignmentType == "Test" ? "bài kiểm tra" : "bài tập")}: {ex.Message}";
                return RedirectToAction(User.IsInRole("Admin") ? "ManageCourses" : "ManageCourses", "Course");
            }
        }
    }
}