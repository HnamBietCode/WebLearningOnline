using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LearningManagementSystem.Repositories
{
    public class LessonRepository : ILessonRepository
    {
        private readonly LMSContext _context;
        private readonly ILogger<LessonRepository> _logger; // Thêm logger

        public LessonRepository(LMSContext context, ILogger<LessonRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<Lesson> GetAll()
        {
            _logger.LogInformation("Fetching all lessons.");
            var lessons = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Progresses)
                .ToList();
            _logger.LogInformation($"Found {lessons.Count} lessons.");
            return lessons;
        }

        public Lesson GetById(string lessonId)
        {
            _logger.LogInformation($"Fetching lesson with ID: {lessonId}");
            var lesson = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Progresses)
                .FirstOrDefault(l => l.LessonId == lessonId);

            if (lesson == null)
            {
                _logger.LogWarning($"Lesson with ID: {lessonId} not found.");
            }
            else
            {
                _logger.LogInformation($"Lesson {lessonId} fetched successfully.");
            }

            return lesson;
        }

        public IEnumerable<Lesson> GetLessonsByCourse(string courseId)
        {
            _logger.LogInformation($"Fetching lessons for Course ID: {courseId}");
            var lessons = _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Progresses)
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderNumber)
                .ToList();

            _logger.LogInformation($"Found {lessons.Count} lessons for Course ID: {courseId}.");
            return lessons;
        }

        public IQueryable<Lesson> FetchLessonsByCourseForDetails(string courseId)
        {
            _logger.LogInformation($"Fetching lessons for Course ID: {courseId}");

            var lessonsQuery = _context.Lessons
                .Include(l => l.Course) // Tải thông tin khóa học liên quan
                .Include(l => l.Progresses) // Tải thông tin tiến độ liên quan
                .Include(l => l.Assignments) // Tải bài tập
                    .ThenInclude(a => a.Questions) // Tải câu hỏi của bài tập
                        .ThenInclude(q => q.Options) // Tải các lựa chọn của câu hỏi
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderNumber);

            _logger.LogInformation($"Found lessons query for Course ID: {courseId}.");
            return lessonsQuery; // Trả về IQueryable<Lesson>
        }

        public async Task<Lesson> GetByIdWithAssignmentsAsync(string lessonId)
        {
            return await _context.Lessons
                .Include(l => l.Assignments)
                .ThenInclude(a => a.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId);
        }

        public void Add(Lesson lesson)
        {
            _logger.LogInformation($"Adding new lesson with ID: {lesson.LessonId}");
            _context.Lessons.Add(lesson);
        }

        public void Update(Lesson lesson)
        {
            _logger.LogInformation($"Updating lesson with ID: {lesson.LessonId}");

            // Kiểm tra xem thực thể đã được theo dõi bởi context chưa
            var entry = _context.Entry(lesson);
            if (entry.State == EntityState.Detached)
            {
                // Nếu thực thể bị detached, lấy thực thể từ context
                var existingLesson = _context.Lessons
                    .FirstOrDefault(l => l.LessonId == lesson.LessonId);

                if (existingLesson != null)
                {
                    // Cập nhật các thuộc tính của thực thể hiện có
                    _context.Entry(existingLesson).CurrentValues.SetValues(lesson);
                    _logger.LogInformation($"Lesson {lesson.LessonId} attached and updated in context.");
                }
                else
                {
                    // Nếu không tìm thấy, gắn thực thể mới vào context
                    _context.Lessons.Update(lesson);
                    _logger.LogInformation($"Lesson {lesson.LessonId} not found in context, attached as new.");
                }
            }
            else
            {
                // Nếu thực thể đã được theo dõi, đánh dấu là Modified
                _context.Entry(lesson).State = EntityState.Modified;
                _logger.LogInformation($"Lesson {lesson.LessonId} marked as Modified.");
            }
        }

        public void Delete(string lessonId)
        {
            _logger.LogInformation($"Deleting lesson with ID: {lessonId}");
            var lesson = _context.Lessons
                .Include(l => l.Progresses)
                .FirstOrDefault(l => l.LessonId == lessonId);

            if (lesson != null)
            {
                // Xóa các Assignment liên quan đến Lesson
                var assignments = _context.Assignments.Where(a => a.LessonId == lessonId).ToList();
                foreach (var assignment in assignments)
                {
                    // Xóa các AssignmentQuestions liên quan
                    var questions = _context.AssignmentQuestions.Where(q => q.AssignmentId == assignment.AssignmentId).ToList();
                    foreach (var question in questions)
                    {
                        // Xóa các AssignmentQuestionOptions liên quan
                        var options = _context.AssignmentQuestionOptions.Where(o => o.QuestionId == question.QuestionId).ToList();
                        if (options.Any())
                            _context.AssignmentQuestionOptions.RemoveRange(options);
                    }
                    if (questions.Any())
                        _context.AssignmentQuestions.RemoveRange(questions);
                    // Xóa các AssignmentSubmissions liên quan
                    var submissions = _context.AssignmentSubmissions.Where(s => s.AssignmentId == assignment.AssignmentId).ToList();
                    if (submissions.Any())
                        _context.AssignmentSubmissions.RemoveRange(submissions);
                }
                if (assignments.Any())
                    _context.Assignments.RemoveRange(assignments);

                if (lesson.Progresses != null && lesson.Progresses.Any())
                {
                    _logger.LogInformation($"Removing {lesson.Progresses.Count} progresses for lesson {lessonId}.");
                    _context.Progresses.RemoveRange(lesson.Progresses);
                }
                _context.Lessons.Remove(lesson);
                _logger.LogInformation($"Lesson {lessonId} deleted successfully.");
            }
            else
            {
                _logger.LogWarning($"Lesson with ID: {lessonId} not found for deletion.");
            }
        }

        public void Save()
        {
            _logger.LogInformation("Saving changes to database.");
            _context.SaveChanges();
            _logger.LogInformation("Changes saved successfully.");
        }

        public async Task SaveAsync()
        {
            _logger.LogInformation("Saving changes to database asynchronously.");
            await _context.SaveChangesAsync();
            _logger.LogInformation("Changes saved successfully.");
        }
    }
}