using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningManagementSystem.Controllers
{
    public class ChatController : Controller
    {
        private readonly IGroqService _groqService;
        private readonly LMSContext _context;

        public ChatController(IGroqService groqService, LMSContext context)
        {
            _groqService = groqService;
            _context = context;
        }

        public IActionResult Index()
        {
            var model = new ChatViewModel();
            // Thêm tin nhắn chào mừng
            model.Messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = "Xin chào! Tôi là chatbot tư vấn khóa học. Bạn cần tư vấn về khóa học nào ạ?"
            });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                var messages = request.Messages ?? new List<ChatMessage>();
                var userInput = request.UserInput?.ToLower() ?? "";

                // Biến để kiểm tra có điều kiện tìm kiếm hợp lệ không
                bool hasValidSearchCriteria = false;
                IQueryable<Course> query = _context.Courses;
                string filterDescription = "";

                // Kiểm tra các từ khóa cơ bản về khóa học
                bool containsCourseKeywords = userInput.Contains("gợi ý") || userInput.Contains("khóa học") ||
                                             userInput.Contains("course") || userInput.Contains("tìm khóa học");

                // Kiểm tra tìm kiếm khóa học theo tên cụ thể
                bool isSpecificCourseQuery = false;
                string courseKeyword = "";

                // Pattern: "khóa học [TÊN]" hoặc "course [TÊN]"
                if (userInput.Contains("khóa học ") || userInput.Contains("course "))
                {
                    var patterns = new[]
                    {
          @"khóa học\s+([a-zA-Z0-9#+.\-\s]+?)(?:\s+|$)",
          @"course\s+([a-zA-Z0-9#+.\-\s]+?)(?:\s+|$)"
      };

                    foreach (var pattern in patterns)
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(userInput, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            courseKeyword = match.Groups[1].Value.Trim();

                            // Loại bỏ các từ không liên quan đến tên khóa học
                            var excludeWords = new[] { "miễn phí", "free", "dưới", "trên", "từ", "đến", "không", "nào", "gì", "à", "về" };
                            var cleanKeyword = courseKeyword;

                            foreach (var word in excludeWords)
                            {
                                cleanKeyword = System.Text.RegularExpressions.Regex.Replace(cleanKeyword, @"\b" + word + @"\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                            }

                            // Chỉ coi là tìm kiếm theo tên nếu còn lại ít nhất 1 ký tự và không phải là từ khóa chung
                            if (!string.IsNullOrEmpty(cleanKeyword) && cleanKeyword.Length >= 1 &&
                                !cleanKeyword.ToLower().Equals("nào") && !cleanKeyword.ToLower().Equals("gì"))
                            {
                                courseKeyword = cleanKeyword;
                                isSpecificCourseQuery = true;
                                break;
                            }
                        }
                    }
                }

                if (containsCourseKeywords || isSpecificCourseQuery)
                {
                    // Nếu là câu hỏi về khóa học cụ thể, tìm theo tên khóa học
                    if (isSpecificCourseQuery && !string.IsNullOrEmpty(courseKeyword))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains(courseKeyword.ToLower()));
                        filterDescription = $" có tên chứa \"{courseKeyword}\"";
                        hasValidSearchCriteria = true;
                    }

                    // Lọc theo giá - CHỈ khi có từ khóa giá cụ thể
                    if (userInput.Contains("miễn phí") || userInput.Contains("free"))
                    {
                        query = query.Where(c => !c.Price.HasValue || c.Price == 0);
                        filterDescription = " miễn phí";
                        hasValidSearchCriteria = true;
                    }
                    else if (userInput.Contains("dưới") && (userInput.Contains("k") || userInput.Contains("000")))
                    {
                        // Tìm số tiền trong câu với pattern cụ thể
                        var priceMatch = System.Text.RegularExpressions.Regex.Match(userInput, @"dưới\s*(\d+)k");
                        if (priceMatch.Success)
                        {
                            var priceLimit = int.Parse(priceMatch.Groups[1].Value) * 1000;
                            query = query.Where(c => c.Price.HasValue && c.Price <= priceLimit);
                            filterDescription = $" dưới {priceLimit:N0}đ";
                            hasValidSearchCriteria = true;
                        }
                        else
                        {
                            // Kiểm tra các mức giá cụ thể
                            if (userInput.Contains("dưới 100k") || userInput.Contains("dưới 100"))
                            {
                                query = query.Where(c => c.Price.HasValue && c.Price <= 100000);
                                filterDescription = " dưới 100,000đ";
                                hasValidSearchCriteria = true;
                            }
                            else if (userInput.Contains("dưới 200k") || userInput.Contains("dưới 200"))
                            {
                                query = query.Where(c => c.Price.HasValue && c.Price <= 200000);
                                filterDescription = " dưới 200,000đ";
                                hasValidSearchCriteria = true;
                            }
                            else if (userInput.Contains("dưới 300k") || userInput.Contains("dưới 300"))
                            {
                                query = query.Where(c => c.Price.HasValue && c.Price <= 300000);
                                filterDescription = " dưới 300,000đ";
                                hasValidSearchCriteria = true;
                            }
                            else if (userInput.Contains("dưới 500k") || userInput.Contains("dưới 500"))
                            {
                                query = query.Where(c => c.Price.HasValue && c.Price <= 500000);
                                filterDescription = " dưới 500,000đ";
                                hasValidSearchCriteria = true;
                            }
                        }
                    }
                    else if (userInput.Contains("từ") && userInput.Contains("đến"))
                    {
                        // Lọc theo khoảng giá
                        var matches = System.Text.RegularExpressions.Regex.Matches(userInput, @"(\d+)k?");
                        if (matches.Count >= 2)
                        {
                            var minPrice = int.Parse(matches[0].Groups[1].Value);
                            var maxPrice = int.Parse(matches[1].Groups[1].Value);

                            if (userInput.Contains("k"))
                            {
                                minPrice *= 1000;
                                maxPrice *= 1000;
                            }

                            query = query.Where(c => c.Price.HasValue && c.Price >= minPrice && c.Price <= maxPrice);
                            filterDescription = $" từ {minPrice:N0}đ đến {maxPrice:N0}đ";
                            hasValidSearchCriteria = true;
                        }
                    }

                    // Lọc theo chủ đề/danh mục - CHỈ khi có từ khóa chủ đề cụ thể
                    if (userInput.Contains("lập trình") || userInput.Contains("programming"))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains("lập trình") ||
                                               c.CourseName.ToLower().Contains("programming") ||
                                               c.CourseName.ToLower().Contains("code") ||
                                               (c.Description != null && c.Description.ToLower().Contains("lập trình")));
                        filterDescription += " về lập trình";
                        hasValidSearchCriteria = true;
                    }
                    else if (userInput.Contains("web") && (userInput.Contains("phát triển") || userInput.Contains("thiết kế") || userInput.Contains("học")))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains("web") ||
                                               (c.Description != null && c.Description.ToLower().Contains("web")) ||
                                               c.CourseName.ToLower().Contains("html") ||
                                               c.CourseName.ToLower().Contains("css") ||
                                               c.CourseName.ToLower().Contains("javascript"));
                        filterDescription += " về phát triển web";
                        hasValidSearchCriteria = true;
                    }
                    else if (userInput.Contains("mobile") || userInput.Contains("app") || userInput.Contains("android") || userInput.Contains("ios"))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains("mobile") ||
                                               c.CourseName.ToLower().Contains("app") ||
                                               c.CourseName.ToLower().Contains("android") ||
                                               c.CourseName.ToLower().Contains("ios") ||
                                               (c.Description != null && c.Description.ToLower().Contains("mobile")));
                        filterDescription += " về phát triển ứng dụng di động";
                        hasValidSearchCriteria = true;
                    }
                    else if ((userInput.Contains("data") || userInput.Contains("dữ liệu")) && userInput.Contains("phân tích"))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains("data") ||
                                               c.CourseName.ToLower().Contains("dữ liệu") ||
                                               c.CourseName.ToLower().Contains("phân tích") ||
                                               (c.Description != null && c.Description.ToLower().Contains("data")));
                        filterDescription += " về phân tích dữ liệu";
                        hasValidSearchCriteria = true;
                    }
                    else if (userInput.Contains("ai") || userInput.Contains("machine learning") || userInput.Contains("trí tuệ nhân tạo"))
                    {
                        query = query.Where(c => c.CourseName.ToLower().Contains("ai") ||
                                               c.CourseName.ToLower().Contains("machine learning") ||
                                               c.CourseName.ToLower().Contains("trí tuệ nhân tạo") ||
                                               (c.Description != null && c.Description.ToLower().Contains("ai")));
                        filterDescription += " về trí tuệ nhân tạo";
                        hasValidSearchCriteria = true;
                    }
                    if (hasValidSearchCriteria ||
                        userInput.Contains("gợi ý khóa học") ||
                        userInput.Equals("khóa học") ||
                        userInput.Contains("tìm khóa học"))
                    {
                        var courses = await query.OrderBy(c => c.Price ?? 0).ThenBy(c => c.CourseName).Take(8).ToListAsync();

                        if (courses.Count == 0)
                        {
                            string noResultMessage;
                            if (isSpecificCourseQuery)
                            {
                                noResultMessage = $"Xin lỗi, hiện tại chưa có khóa học nào có tên chứa \"{courseKeyword}\". Bạn có thể:\n• Thử tìm với từ khóa khác\n• Xem danh sách khóa học hiện có bằng cách gõ \"gợi ý khóa học\"";
                            }
                            else if (hasValidSearchCriteria)
                            {
                                noResultMessage = $"Xin lỗi, không tìm thấy khóa học nào{filterDescription}. Bạn có thể thử với điều kiện khác như:\n• Khóa học dưới 500k\n• Khóa học lập trình\n• Khóa học miễn phí";
                            }
                            else
                            {
                                noResultMessage = "Hiện tại chưa có khóa học nào trong hệ thống.";
                            }
                            messages.Add(new ChatMessage { Role = "assistant", Content = noResultMessage });
                        }
                        else
                        {
                            var courseList = string.Join("<br>", courses.Select((c, i) =>
                                $@"<div style='display:flex;align-items:center;margin-bottom:12px;padding:10px;border:1px solid #e0e0e0;border-radius:8px;background:#f9f9f9;'>"
                                + (string.IsNullOrEmpty(c.ImageUrl) ? "" : $"<img src='{c.ImageUrl}' alt='img' style='width:50px;height:50px;object-fit:cover;border-radius:8px;margin-right:12px;' />")
                                + $"<div style='flex:1;'>"
                                + $"<a href='/Course/Details/{c.CourseId}' style='font-weight:bold;color:#1976d2;text-decoration:none;font-size:16px;'>{c.CourseName}</a>"
                                + $"<div style='margin-top:4px;color:#666;font-size:14px;'>{(string.IsNullOrEmpty(c.Description) ? "Mô tả đang cập nhật" : (c.Description.Length > 80 ? c.Description.Substring(0, 80) + "..." : c.Description))}</div>"
                                + $"<div style='margin-top:6px;color:#388e3c;font-weight:bold;font-size:15px;'>{(c.Price.HasValue && c.Price > 0 ? c.Price.Value.ToString("N0") + "đ" : "Miễn phí")}</div>"
                                + "</div></div>"
                            ));

                            string headerMessage;
                            if (isSpecificCourseQuery)
                            {
                                headerMessage = courses.Count == 1
                                    ? $"Có! Tìm thấy khóa học \"{courseKeyword}\":"
                                    : $"Tìm thấy {courses.Count} khóa học có tên chứa \"{courseKeyword}\":";
                            }
                            else if (hasValidSearchCriteria)
                            {
                                headerMessage = $"Tìm thấy {courses.Count} khóa học{filterDescription}:";
                            }
                            else
                            {
                                headerMessage = "Các khóa học đang có:";
                            }

                            var reply = $"<div style='font-weight:bold;margin-bottom:12px;color:#333;'>{headerMessage}</div>{courseList}<div style='margin-top:15px;padding:8px;background:#f0f8ff;border-radius:6px;color:#666;font-size:14px;'>💡 <b>Gợi ý tìm kiếm:</b><br>• \"Có khóa học Java không?\"<br>• \"Khóa học dưới 200k\"<br>• \"Khóa học thiết kế web miễn phí\"</div>";
                            messages.Add(new ChatMessage { Role = "assistant", Content = reply });
                        }
                        return Json(new { success = true, messages = messages });
                    }
                }

                // Thêm tin nhắn của user
                messages.Add(new ChatMessage { Role = "user", Content = request.UserInput });

                // Gọi Groq API
                var response = await _groqService.GetChatResponseAsync(messages);

                // Thêm phản hồi của bot
                messages.Add(new ChatMessage { Role = "assistant", Content = response });

                return Json(new { success = true, messages = messages });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public class ChatRequest
        {
            public string UserInput { get; set; }
            public List<ChatMessage> Messages { get; set; }
        }
    }
}