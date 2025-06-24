using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningManagementSystem.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly LMSContext _context;

        public PaymentController(LMSContext context)
        {
            _context = context;
        }

        // GET: Payment/History
        public async Task<IActionResult> History()
        {
            var username = User.Identity.Name;

            var payments = await _context.Payments
                .Include(p => p.Course)
                .Where(p => p.UserName == username)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // GET: Payment/PaymentDetail/5
        public async Task<IActionResult> PaymentDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var username = User.Identity.Name;

            var payment = await _context.Payments
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.UserName == username);

            if (payment == null)
            {
                return NotFound();
            }

            // Nếu là giao dịch mua nhiều khóa học, lấy chi tiết từ OrderDetails
            var orderDetails = await _context.OrderDetails
                .Include(od => od.Course)
                .Where(od => od.PaymentId == id)
                .ToListAsync();

            var viewModel = new PaymentDetailViewModel
            {
                Payment = payment,
                OrderDetails = orderDetails
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> ManagePayment(int page = 1)
        {
            var username = User.Identity.Name;
            var isAdmin = User.IsInRole("Admin");

            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.OrderDetails)
                    .ThenInclude(od => od.Course)
                        .ThenInclude(c => c.CourseInstructors)
                .AsQueryable();

            // Nếu là instructor, chỉ lấy các thanh toán liên quan đến khóa học của họ
            if (!isAdmin)
            {
                query = query.Where(p => p.OrderDetails.Any(od => 
                    od.Course.CourseInstructors.Any(ci => ci.UserName == username)));
            }

            int pageSize = 10;
            int totalPayments = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalPayments / pageSize);
            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(payments);
        }

       [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> UpdatePaymentStatus(string paymentId, string newStatus)
        {
            var username = User.Identity.Name;
            var isAdmin = User.IsInRole("Admin");

            var payment = await _context.Payments
                .Include(p => p.OrderDetails)
                    .ThenInclude(od => od.Course)
                        .ThenInclude(c => c.CourseInstructors)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

            if (payment == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền cập nhật
            if (!isAdmin)
            {
                var hasPermission = payment.OrderDetails.Any(od => 
                    od.Course.CourseInstructors.Any(ci => ci.UserName == username));
                
                if (!hasPermission)
                {
                    return Forbid();
                }
            }

            payment.PaymentStatus = newStatus;
            await _context.SaveChangesAsync();

            // Nếu trạng thái thanh toán thành công, tạo enrollment cho user và gửi thông báo
            if (newStatus == "Completed")
            {
                foreach (var orderDetail in payment.OrderDetails)
                {
                    // Kiểm tra xem user đã đăng ký khóa học này chưa
                    var existingEnrollment = await _context.Enrollments
                        .FirstOrDefaultAsync(e => e.UserName == payment.UserName && e.CourseId == orderDetail.CourseId);

                    if (existingEnrollment == null)
                    {
                        // Tạo enrollment mới
                        var enrollment = new Enrollment
                        {
                            EnrollmentId = Guid.NewGuid().ToString(),
                            UserName = payment.UserName,
                            CourseId = orderDetail.CourseId,
                            EnrollmentDate = DateTime.Now
                        };

                        _context.Enrollments.Add(enrollment);

                        // Tạo thông báo cho user
                        var notification = new Notification
                        {
                            NotificationId = Guid.NewGuid().ToString(),
                            UserName = payment.UserName,
                            Title = "Đăng ký khóa học thành công",
                            Content = $"Bạn đã được đăng ký thành công vào khóa học: {orderDetail.Course.CourseName}",
                            CreatedDate = DateTime.Now,
                            IsRead = false
                        };

                        _context.Notifications.Add(notification);
                    }
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Cập nhật trạng thái thanh toán thành công!";
            return RedirectToAction("ManagePayment");
        }
    }
}