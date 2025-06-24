using Microsoft.AspNetCore.Mvc;
using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

public class SearchController : Controller
{
    private readonly LMSContext _context;
    private readonly IEnrollmentRepository _enrollmentRepository;

    public SearchController(LMSContext context, IEnrollmentRepository enrollmentRepository)
    {
        _context = context;
        _enrollmentRepository = enrollmentRepository;
    }

    public IActionResult Index(string query, string sort = "name-asc")
    {
        // Sử dụng User.Identity.Name để lấy username của người dùng đã đăng nhập
        var userName = User.Identity.IsAuthenticated ? User.Identity.Name : null;

        var coursesQuery = _context.Courses
            .Include(c => c.Lessons)
            .Include(c => c.Comments)
            .Where(c => string.IsNullOrEmpty(query) || c.CourseName.Contains(query))
            .Select(c => new CourseListViewModel
            {
                CourseId = c.CourseId,
                CourseName = c.CourseName,
                Title = c.CourseName,
                Description = c.Description,
                CreatedDate = c.CreatedDate,
                ImageUrl = c.ImageUrl,
                Lessons = c.Lessons.ToList(),
                IsEnrolled = userName != null && _enrollmentRepository.GetAll()
                    .Any(e => e.UserName == userName && e.CourseId == c.CourseId),
                Price = c.Price,
                AverageRating = c.Comments.Any() ? c.Comments.Average(cm => cm.Rating) : null
            });

        // Sắp xếp danh sách khóa học dựa trên tham số sort
        List<CourseListViewModel> courses;
        switch (sort)
        {
            case "rating-desc": // Sắp xếp theo đánh giá cao (giảm dần)
                courses = coursesQuery
                    .OrderByDescending(c => c.AverageRating ?? 0) // Sắp xếp giảm dần, ưu tiên các khóa học có điểm cao, nếu null thì coi là 0
                    .ToList();
                break;
            case "name-asc": // Sắp xếp theo tên A-Z (tăng dần)
            default:
                courses = coursesQuery
                    .OrderBy(c => c.CourseName) // Sắp xếp tăng dần theo tên
                    .ToList();
                break;
        }

        ViewBag.Query = query;
        ViewBag.Sort = sort; // Truyền giá trị sort để view biết tiêu chí hiện tại
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> GetSearchSuggestions(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return Json(new List<string>());
        }

        var suggestions = await _context.Courses
            .Where(c => c.CourseName.Contains(query))
            .Select(c => c.CourseName)
            .Take(5) // Giới hạn 5 gợi ý
            .ToListAsync();

        return Json(suggestions);
    }
}