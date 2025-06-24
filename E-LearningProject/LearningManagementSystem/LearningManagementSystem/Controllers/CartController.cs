using Microsoft.AspNetCore.Mvc;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearningManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LearningManagementSystem.Utilities;

namespace LearningManagementSystem.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartItemRepository _cartItemRepository;
        private readonly LMSContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartItemRepository cartItemRepository,
            LMSContext context,
            ILogger<CartController> logger)
        {
            _cartItemRepository = cartItemRepository;
            _context = context;
            _logger = logger;
        }

        private async Task<Cart> GetOrCreateCartAsync(string userName)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserName == userName && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    CartId = Guid.NewGuid().ToString(),
                    UserName = userName,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now,
                    TotalAmount = 0,
                    Status = "Active",
                    CartItems = new List<CartItem>()
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity.Name;
            var cart = await GetOrCreateCartAsync(userName);
            var cartItems = cart.CartItems;

            var cartItemViewModels = new List<CartItemViewModel>();
            foreach (var item in cartItems)
            {
                string instructorName = "Unknown Instructor";
                double? averageRating = null;

                var course = await _context.Courses
                    .Include(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role) // Đảm bảo nạp Role
                    .Include(c => c.Comments)
                    .Include(c => c.Lessons)
                    .Include(c => c.Assignments)
                    .FirstOrDefaultAsync(c => c.CourseId == item.CourseId);

                CourseListViewModel courseViewModel = null;
                if (course != null)
                {
                    if (course.CourseInstructors?.Any() == true)
                    {
                        var instructor = course.CourseInstructors
                            .FirstOrDefault(ci => ci.User?.Role?.RoleName == "Instructor")?.User;
                        if (instructor != null)
                        {
                            instructorName = instructor.FullName ?? "Unknown Instructor";
                        }
                    }

                    if (course.Comments?.Any() == true)
                    {
                        averageRating = (double?)course.Comments.Average(cm => cm.Rating);
                    }

                    courseViewModel = new CourseListViewModel
                    {
                        CourseId = course.CourseId.ToString(),
                        CourseName = course.CourseName,
                        Description = course.Description,
                        CreatedDate = course.CreatedDate,
                        ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/default-course.jpg" : course.ImageUrl,
                        IsEnrolled = false,
                        Price = course.Price,
                        Title = course.CourseName,
                        Lessons = course.Lessons?.ToList(),
                        Assignments = course.Assignments?.ToList(),
                        AverageRating = averageRating
                    };
                }

                cartItemViewModels.Add(new CartItemViewModel
                {
                    CartItem = item,
                    InstructorName = instructorName,
                    Course = courseViewModel
                });
            }

            var cartCourseIds = cartItems.Select(ci => ci.CourseId).ToList();
            var recommendedCourses = await _context.Courses
                .Include(c => c.Comments)
                .Include(c => c.Lessons)
                .Include(c => c.Assignments)
                .Where(c => !cartCourseIds.Contains(c.CourseId))
                .OrderBy(c => c.CreatedDate)
                .Take(4)
                .ToListAsync();

            if (recommendedCourses.Count < 4)
            {
                var additionalCourses = await _context.Courses
                    .Include(c => c.Comments)
                    .Include(c => c.Lessons)
                    .Include(c => c.Assignments)
                    .Where(c => !cartCourseIds.Contains(c.CourseId) && !recommendedCourses.Select(rc => rc.CourseId).Contains(c.CourseId))
                    .OrderBy(c => c.CreatedDate)
                    .Take(4 - recommendedCourses.Count)
                    .ToListAsync();
                recommendedCourses.AddRange(additionalCourses);
            }

            var recommendedViewModels = new List<CourseListViewModel>();
            foreach (var course in recommendedCourses ?? new List<Course>())
            {
                double? avgRating = null;
                if (course.Comments?.Any() == true)
                {
                    avgRating = (double?)course.Comments.Average(cm => cm.Rating);
                }

                recommendedViewModels.Add(new CourseListViewModel
                {
                    CourseId = course.CourseId.ToString(),
                    CourseName = course.CourseName,
                    Description = course.Description,
                    CreatedDate = course.CreatedDate,
                    ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/default-course.jpg" : course.ImageUrl,
                    IsEnrolled = false,
                    Price = course.Price,
                    Title = course.CourseName,
                    Lessons = course.Lessons?.ToList(),
                    Assignments = course.Assignments?.ToList(),
                    AverageRating = avgRating
                });
            }

            ViewBag.RecommendedCourses = recommendedViewModels ?? new List<CourseListViewModel>();
            ViewBag.CartId = cart.CartId;

            return View(cartItemViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                TempData["Error"] = "Khóa học không hợp lệ.";
                return RedirectToAction("Index", "Search");
            }

            var userName = User.Identity.Name;
            var cart = await GetOrCreateCartAsync(userName);

            var existingCartItem = cart.CartItems.FirstOrDefault(ci => ci.CourseId == courseId);
            if (existingCartItem != null)
            {
                TempData["Error"] = "Khóa học đã có trong giỏ hàng.";
                return RedirectToAction("Index", "Home");
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["Error"] = "Khóa học không tồn tại.";
                return RedirectToAction("Index", "Search");
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserName == userName && e.CourseId == courseId);
            if (enrollment != null)
            {
                TempData["Error"] = "Bạn đã đăng ký khóa học này rồi.";
                return RedirectToAction("Index");
            }

            var cartItem = new CartItem
            {
                CartItemId = Guid.NewGuid().ToString(),
                CartId = cart.CartId,
                CourseId = courseId,
                AddedDate = DateTime.Now,
                Price = course.Price ?? 0m  // Use null-coalescing operator to default to 0 if Price is null
            };

            cart.CartItems.Add(cartItem);
            cart.TotalAmount += cartItem.Price;  // Update TotalAmount with the non-null price
            cart.LastModifiedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm khóa học vào giỏ hàng thành công!";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(string cartItemId)
        {
            if (string.IsNullOrEmpty(cartItemId))
            {
                TempData["Error"] = "Mục giỏ hàng không hợp lệ.";
                return RedirectToAction("Index");
            }

            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId && ci.Cart.UserName == User.Identity.Name);

            if (cartItem == null)
            {
                TempData["Error"] = "Mục giỏ hàng không tồn tại hoặc không thuộc về bạn.";
                return RedirectToAction("Index");
            }

            var cart = cartItem.Cart;
            cart.TotalAmount -= cartItem.Price;
            cart.LastModifiedDate = DateTime.Now;

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa khóa học khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> Checkout(string selectedCartItemIds)
        {
            // Debug log
            _logger.LogInformation($"Received selectedCartItemIds: {selectedCartItemIds}");

            // Parse the comma-separated string into array
            string[] selectedIds = null;
            if (!string.IsNullOrEmpty(selectedCartItemIds))
            {
                selectedIds = selectedCartItemIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }

            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một khóa học để thanh toán.";
                return RedirectToAction("Index");
            }

            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy giỏ hàng của người dùng
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Course)
                .FirstOrDefaultAsync(c => c.UserName == userName);

            if (cart == null || !cart.CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn hiện đang trống.";
                return RedirectToAction("Index");
            }

            // Lọc các CartItem theo selectedCartItemIds
            var cartItems = cart.CartItems
                .Where(ci => selectedIds.Contains(ci.CartItemId))
                .ToList();

            _logger.LogInformation($"Found {cartItems.Count} cart items from selected IDs");

            if (!cartItems.Any())
            {
                TempData["Error"] = "Không tìm thấy các khóa học đã chọn.";
                return RedirectToAction("Index");
            }

            // Xây dựng danh sách CartItemViewModel
            var cartItemViewModels = new List<CartItemViewModel>();
            foreach (var item in cartItems)
            {
                string instructorName = "Unknown Instructor";
                double? averageRating = null;

                // Lấy thông tin khóa học và giảng viên
                var course = await _context.Courses
                    .Include(c => c.CourseInstructors)
                        .ThenInclude(ci => ci.User)
                            .ThenInclude(u => u.Role)
                    .Include(c => c.Comments)
                    .Include(c => c.Lessons)
                    .Include(c => c.Assignments)
                    .FirstOrDefaultAsync(c => c.CourseId == item.CourseId);

                CourseListViewModel courseViewModel = null;
                if (course != null)
                {
                    // Lấy tên giảng viên
                    if (course.CourseInstructors?.Any() == true)
                    {
                        var instructor = course.CourseInstructors
                            .FirstOrDefault(ci => ci.User?.Role?.RoleName == "Instructor")?.User;
                        if (instructor != null)
                        {
                            instructorName = instructor.FullName ?? "Unknown Instructor";
                            _logger.LogInformation($"Found instructor for course {course.CourseId}: {instructorName}");
                        }
                        else
                        {
                            _logger.LogWarning($"No instructor found for course {course.CourseId} with role 'Instructor'");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No CourseInstructors found for course {course.CourseId}");
                    }

                    // Tính điểm đánh giá trung bình
                    if (course.Comments?.Any() == true)
                    {
                        averageRating = (double?)course.Comments.Average(cm => cm.Rating);
                    }

                    // Tạo CourseListViewModel
                    courseViewModel = new CourseListViewModel
                    {
                        CourseId = course.CourseId.ToString(),
                        CourseName = course.CourseName,
                        Description = course.Description,
                        CreatedDate = course.CreatedDate,
                        ImageUrl = string.IsNullOrEmpty(course.ImageUrl) ? "/images/default-course.jpg" : course.ImageUrl,
                        IsEnrolled = false,
                        Price = course.Price,
                        Title = course.CourseName,
                        Lessons = course.Lessons?.ToList(),
                        Assignments = course.Assignments?.ToList(),
                        AverageRating = averageRating
                    };

                    // Gán lại Course cho CartItem (để view có thể truy cập)
                    item.Course = course;
                }

                cartItemViewModels.Add(new CartItemViewModel
                {
                    CartItem = item,
                    InstructorName = instructorName,
                    Course = courseViewModel
                });
            }

            return View("Checkout", cartItemViewModels);
        }

        [HttpPost]
        public async Task<IActionResult> InitiateVnpayPayment(decimal amount, string selectedCartItemIds)
        {
            var username = User.Identity.Name;
            var cart = await GetOrCreateCartAsync(username);

            var existingPendingPayment = _context.Payments
                .FirstOrDefault(p => p.UserName == username && p.PaymentStatus == "Pending" && p.PaymentMethod == "VNPay");

            string paymentId;
            if (existingPendingPayment != null)
            {
                paymentId = existingPendingPayment.PaymentId;
            }
            else
            {
                string orderId = DateTime.Now.Ticks.ToString();
                paymentId = $"PAY_{orderId}";

                var selectedIds = selectedCartItemIds?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
                var cartItems = cart.CartItems
                    .Where(ci => selectedIds.Contains(ci.CartItemId))
                    .ToList();

                if (!cartItems.Any())
                {
                    TempData["Error"] = "Không có khóa học nào được chọn để thanh toán.";
                    return RedirectToAction("Checkout");
                }

                var payment = new Payment
                {
                    PaymentId = paymentId,
                    UserName = username,
                    Amount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Pending",
                    PaymentMethod = "VNPay",
                    TransactionType = "CoursePurchase"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderDetailId = Guid.NewGuid().ToString(),
                        PaymentId = paymentId,
                        CourseId = item.CourseId,
                        Price = item.Price
                    };
                    _context.OrderDetails.Add(orderDetail);
                }
                await _context.SaveChangesAsync();
            }

            var vnpay = new VnPayLibrary();
            var vnpayRequestData = new SortedList<string, string>();

            vnpayRequestData.Add("vnp_Version", VnPayConfig.vnp_Version);
            vnpayRequestData.Add("vnp_Command", VnPayConfig.vnp_Command);
            vnpayRequestData.Add("vnp_TmnCode", VnPayConfig.vnp_TmnCode);
            vnpayRequestData.Add("vnp_Amount", (amount * 100).ToString());
            vnpayRequestData.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpayRequestData.Add("vnp_CurrCode", VnPayConfig.vnp_CurrCode);
            vnpayRequestData.Add("vnp_IpAddr", VnPayLibrary.GetIpAddress(HttpContext));
            vnpayRequestData.Add("vnp_Locale", VnPayConfig.vnp_Locale);
            vnpayRequestData.Add("vnp_OrderInfo", $"Thanh toan khoa hoc - {username}");
            vnpayRequestData.Add("vnp_OrderType", "250000");
            vnpayRequestData.Add("vnp_ReturnUrl", Url.Action("VnPayReturn", "Cart", null, Request.Scheme) ?? VnPayConfig.vnp_Returnurl);
            vnpayRequestData.Add("vnp_TxnRef", paymentId);

            string paymentUrl = VnPayLibrary.CreateRequestUrl(VnPayConfig.vnp_Url, vnpayRequestData, VnPayConfig.vnp_HashSecret);

            return Redirect(paymentUrl);
        }

        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            SortedList<string, string> responseData = new SortedList<string, string>();
            foreach (var key in Request.Query.Keys)
            {
                if (!key.StartsWith("vnp_")) continue;

                string value = Request.Query[key];
                responseData.Add(key, value);
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            responseData.Remove("vnp_SecureHash");

            bool isValidSignature = VnPayLibrary.ValidateSignature(vnp_SecureHash, VnPayConfig.vnp_HashSecret, responseData);

            if (!isValidSignature)
            {
                _logger.LogWarning("VNPay signature validation failed.");
                TempData["Error"] = "Xác thực thanh toán thất bại.";
                return RedirectToAction("Checkout");
            }

            string paymentId = Request.Query["vnp_TxnRef"];
            string vnPayTransactionId = Request.Query["vnp_TransactionNo"];
            string responseCode = Request.Query["vnp_ResponseCode"];

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                _logger.LogWarning($"Payment with id {paymentId} not found.");
                TempData["Error"] = "Không tìm thấy thông tin thanh toán.";
                return RedirectToAction("Checkout");
            }

            if (responseCode == "00")
            {
                payment.PaymentStatus = "Completed";

                var username = payment.UserName;

                var orderDetails = await _context.OrderDetails
                    .Where(od => od.PaymentId == paymentId)
                    .ToListAsync();

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserName == username && c.Status == "Active");

                foreach (var detail in orderDetails)
                {
                    var existingEnrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserName == username && e.CourseId == detail.CourseId);

                    if (existingEnrollment == null)
                    {
                        var enrollment = new Enrollment
                        {
                            EnrollmentId = Guid.NewGuid().ToString(),
                            UserName = username,
                            CourseId = detail.CourseId,
                            EnrollmentDate = DateTime.Now,
                        };

                        _context.Enrollments.Add(enrollment);
                    }
                }

                if (cart != null)
                {
                    var selectedCourseIds = orderDetails.Select(od => od.CourseId).ToList();
                    var itemsToRemove = cart.CartItems.Where(ci => selectedCourseIds.Contains(ci.CourseId)).ToList();
                    cart.TotalAmount = cart.CartItems.Except(itemsToRemove).Sum(ci => ci.Price);
                    cart.LastModifiedDate = DateTime.Now;
                    _context.CartItems.RemoveRange(itemsToRemove);

                    if (!cart.CartItems.Any())
                    {
                        cart.Status = "Inactive";
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Payment {paymentId} completed successfully. Redirecting to PaymentSuccess.");
                return RedirectToAction("PaymentSuccess", new { id = paymentId });
            }
            else if (responseCode == "24") // 24: User cancel
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
                _logger.LogWarning($"Payment {paymentId} was cancelled by user (response code 24).");
                TempData["Error"] = "Bạn đã hủy giao dịch thanh toán.";

                var orderDetails = await _context.OrderDetails
                    .Where(od => od.PaymentId == paymentId)
                    .ToListAsync();

                var userName = payment.UserName;
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserName == userName && c.Status == "Active");

                if (cart != null)
                {
                    var selectedCourseIds = orderDetails.Select(od => od.CourseId).ToList();
                    var cartItems = cart.CartItems
                        .Where(ci => selectedCourseIds.Contains(ci.CourseId))
                        .ToList();
                    var selectedCartItemIds = string.Join(",", cartItems.Select(ci => ci.CartItemId));
                    TempData["SelectedCartItemIds"] = selectedCartItemIds;
                }

                return RedirectToAction("Checkout");
            }
            else
            {
                payment.PaymentStatus = "Failed";
                await _context.SaveChangesAsync();
                _logger.LogWarning($"Payment {paymentId} failed with response code {responseCode}.");
                TempData["Error"] = "Thanh toán thất bại. Vui lòng thử lại sau.";
                return RedirectToAction("Checkout");
            }
        }

        public async Task<IActionResult> PaymentSuccess(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("PaymentSuccess called with null or empty id.");
                return NotFound();
            }

            var username = User.Identity.Name;

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.UserName == username);

            if (payment == null)
            {
                _logger.LogWarning($"Payment with id {id} not found for user {username}.");
                return NotFound();
            }

            if (payment.PaymentStatus != "Completed")
            {
                _logger.LogWarning($"Payment {id} has status {payment.PaymentStatus}, expected 'Completed'.");
                return BadRequest("Giao dịch chưa hoàn tất hoặc không hợp lệ.");
            }

            var orderDetails = await _context.OrderDetails
                .Include(od => od.Course)
                .Where(od => od.PaymentId == id)
                .ToListAsync();

            var viewModel = new PaymentDetailViewModel
            {
                Payment = payment,
                OrderDetails = orderDetails,
                SuccessMessage = "Thanh toán của bạn đã được thực hiện thành công! Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi."
            };

            _logger.LogInformation($"PaymentSuccess view rendered for payment {id}.");
            return View(viewModel);
        }
    }
}