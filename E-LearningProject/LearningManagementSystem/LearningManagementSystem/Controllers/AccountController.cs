using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using LearningManagementSystem.Models.ViewModels;
using System;
using LearningManagementSystem.Services;

namespace LearningManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly LMSContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<AccountController> _logger;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly EmailService _emailService;

        public AccountController(
            LMSContext context,
            IPasswordHasher<User> passwordHasher,
            ILogger<AccountController> logger,
            IEnrollmentRepository enrollmentRepository ,
            EmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _enrollmentRepository = enrollmentRepository;
            _emailService = emailService;
        }

        #region Đăng nhập (Login)
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string userName, string password, string returnUrl = null)
        {
            _logger.LogInformation($"Login attempt for UserName: {userName}");

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("UserName or Password is empty.");
                TempData["Error"] = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null)
            {
                _logger.LogWarning($"User not found for UserName: {userName}");
                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result != PasswordVerificationResult.Success)
            {
                _logger.LogWarning($"Invalid password for UserName: {userName}");
                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            if (user.Role == null)
            {
                _logger.LogError($"Role not found for UserName: {userName}");
                TempData["Error"] = "Không tìm thấy vai trò của người dùng. Vui lòng liên hệ quản trị viên.";
                return View();
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"User {userName} logged in successfully. Role: {user.Role.RoleName}");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role.RoleName == "Admin")
            {
                return RedirectToAction("Dashboard", "Admin", new { area = "" });
            }

            return RedirectToAction("Index", "Home");
        }
        #endregion

        #region Đăng ký (Register)
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string password, string confirmPassword)
        {
            _logger.LogInformation($"Register attempt for UserName: {model.UserName}");

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword) || password != confirmPassword)
            {
                _logger.LogWarning("Password and ConfirmPassword do not match.");
                ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
            }

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
            if (studentRole == null)
            {
                _logger.LogError("Role 'Student' not found in the system.");
                ModelState.AddModelError("", "Vai trò 'Student' không tồn tại trong hệ thống. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            model.RoleId = studentRole.RoleId;

            if (ModelState.ContainsKey("RoleId"))
            {
                ModelState.Remove("RoleId");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
                    if (existingUser != null)
                    {
                        _logger.LogWarning($"UserName {model.UserName} already exists.");
                        ModelState.AddModelError("UserName", "Tên đăng nhập đã tồn tại.");
                        return View(model);
                    }

                    var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    if (existingEmail != null)
                    {
                        _logger.LogWarning($"Email {model.Email} already exists.");
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(model);
                    }

                    // Gán avatar mặc định nếu không có avatar được cung cấp
                    if (string.IsNullOrEmpty(model.Avatar))
                    {
                        model.Avatar = "/images/defaultAvatar.png";
                    }

                    model.HashPassword(_passwordHasher, password);
                    _context.Users.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User {model.UserName} registered successfully.");
                    TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập để tiếp tục.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error occurred while registering user {model.UserName}: {ex.Message}");
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.");
                    return View(model);
                }
            }

            return View(model);
        }
        #endregion

        #region Đăng xuất (Logout)
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"User {userName} logged out successfully.");
            return RedirectToAction("Index", "Home");
        }
        #endregion

      #region Chỉnh sửa hồ sơ (EditProfile)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userName = User.Identity.Name;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("User not authenticated.");
            return RedirectToAction("Login", "Account");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            _logger.LogWarning($"User {userName} not found.");
            return NotFound("Không tìm thấy người dùng.");
        }

        var enrolledCourses = await _context.Enrollments
            .Where(e => e.UserName == userName)
            .Join(_context.Courses,
                  enrollment => enrollment.CourseId,
                  course => course.CourseId,
                  (enrollment, course) => course)
            .ToListAsync();

        var comments = await _context.Comments
            .Where(c => c.UserName == userName)
            .Join(_context.Courses,
                  comment => comment.CourseId,
                  course => course.CourseId,
                  (comment, course) => new UserCommentViewModel
                  {
                      CourseTitle = course.CourseName,
                      Content = comment.Content,
                      CommentDate = comment.CreatedDate
                  })
            .OrderByDescending(c => c.CommentDate)
            .ToListAsync();

        bool isAvatarExists = false;
        string avatarPath = null;
        if (!string.IsNullOrEmpty(user.Avatar))
        {
            avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/'));
            isAvatarExists = System.IO.File.Exists(avatarPath);
            if (!isAvatarExists)
            {
                _logger.LogWarning($"Avatar file not found at path: {avatarPath}");
                user.Avatar = null;
                await _context.SaveChangesAsync();
            }
        }

        var viewModel = new UserProfileEditViewModel
        {
            UserName = user.UserName,
            FullName = user.FullName,
            Email = user.Email,
            Bio = user.Bio,
            EnrolledCourses = enrolledCourses ?? new List<Course>(),
            Comments = comments ?? new List<UserCommentViewModel>()
        };

        ViewBag.Avatar = user.Avatar;
        ViewBag.IsAvatarExists = isAvatarExists;

        _logger.LogInformation($"User {userName} - Avatar path: {user.Avatar}, Bio: {user.Bio}, Avatar exists: {isAvatarExists}");

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(UserProfileEditViewModel model)
    {
        var userName = User.Identity.Name;
        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("User not authenticated.");
            return RedirectToAction("Login", "Account");
        }

        // Log dữ liệu nhận được từ form
        _logger.LogInformation($"Received form data - FullName: {model.FullName}, Email: {model.Email}, Bio: {model.Bio}");

        // Kiểm tra ModelState
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            _logger.LogWarning($"ModelState invalid. Errors: {string.Join(", ", errors)}");
            TempData["Error"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại: " + string.Join(", ", errors);

            var enrolledCourses = await _context.Enrollments
                .Where(e => e.UserName == userName)
                .Join(_context.Courses,
                      enrollment => enrollment.CourseId,
                      course => course.CourseId,
                      (enrollment, course) => course)
                .ToListAsync();

            var comments = await _context.Comments
                .Where(c => c.UserName == userName)
                .Join(_context.Courses,
                      comment => comment.CourseId,
                      course => course.CourseId,
                      (comment, course) => new UserCommentViewModel
                      {
                          CourseTitle = course.CourseName,
                          Content = comment.Content,
                          CommentDate = comment.CreatedDate
                      })
                .OrderByDescending(c => c.CommentDate)
                .ToListAsync();

            model.EnrolledCourses = enrolledCourses;
            model.Comments = comments;

            var userz = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            bool isAvatarExists = false;
            if (userz != null && !string.IsNullOrEmpty(userz.Avatar))
            {
                var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", userz.Avatar.TrimStart('/'));
                isAvatarExists = System.IO.File.Exists(avatarPath);
            }
            ViewBag.Avatar = userz?.Avatar;
            ViewBag.IsAvatarExists = isAvatarExists;

            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user == null)
        {
            _logger.LogWarning($"User {userName} not found.");
            return NotFound("Không tìm thấy người dùng.");
        }

        // Cập nhật thông tin
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Bio = model.Bio;
        _logger.LogInformation($"Updating user {userName} - FullName: {model.FullName}, Email: {model.Email}, Bio: {model.Bio}");

        // Lưu vào cơ sở dữ liệu
        try
        {
            _context.Update(user);
            int changes = await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userName} updated profile successfully. Changes saved: {changes}");
            TempData["Success"] = "Thông tin hồ sơ đã được cập nhật!";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update profile for user {userName}: {ex.Message}");
            TempData["Error"] = "Lỗi khi cập nhật thông tin hồ sơ: " + ex.Message;
        }

        return RedirectToAction("EditProfile");
    }

[Authorize]
[HttpGet]
public async Task<IActionResult> UpdateAvatar()
{
    var userName = User.Identity.Name;
    if (string.IsNullOrEmpty(userName))
    {
        _logger.LogWarning("User not authenticated.");
        return RedirectToAction("Login", "Account");
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if (user == null)
    {
        _logger.LogWarning($"User {userName} not found.");
        return NotFound("Không tìm thấy người dùng.");
    }

    bool isAvatarExists = false;
    string avatarPath = null;
    if (!string.IsNullOrEmpty(user.Avatar))
    {
        avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/'));
        isAvatarExists = System.IO.File.Exists(avatarPath);
        if (!isAvatarExists)
        {
            _logger.LogWarning($"Avatar file not found at path: {avatarPath}");
            user.Avatar = null;
            await _context.SaveChangesAsync();
        }
    }

    var viewModel = new UserProfileEditViewModel
    {
        UserName = user.UserName,
        FullName = user.FullName,
        Email = user.Email,
        Bio = user.Bio
    };

    ViewBag.Avatar = user.Avatar;
    ViewBag.IsAvatarExists = isAvatarExists;

    _logger.LogInformation($"User {userName} - Avatar path: {user.Avatar}, Avatar exists: {isAvatarExists}");

    return View(viewModel);
}

[Authorize]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateAvatar(UserProfileEditViewModel model, IFormFile avatarFile)
{
    var userName = User.Identity.Name;
    if (string.IsNullOrEmpty(userName))
    {
        _logger.LogWarning("User not authenticated.");
        TempData["Error"] = "Bạn cần đăng nhập để thực hiện hành động này.";
        return RedirectToAction("Login", "Account");
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
    if (user == null)
    {
        _logger.LogWarning($"User {userName} not found.");
        TempData["Error"] = "Không tìm thấy người dùng.";
        return RedirectToAction("EditProfile");
    }

    // Xử lý upload avatar nếu có
    if (avatarFile != null && avatarFile.Length > 0)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(avatarFile.FileName).ToLower();
        if (!allowedExtensions.Contains(extension))
        {
            TempData["Error"] = "Định dạng ảnh không được hỗ trợ. Vui lòng sử dụng .jpg, .jpeg, .png hoặc .gif.";
            ViewBag.Avatar = user.Avatar;
            ViewBag.IsAvatarExists = !string.IsNullOrEmpty(user.Avatar) && System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/')));
            return View(model);
        }

        if (avatarFile.Length > 5 * 1024 * 1024)
        {
            TempData["Error"] = "Kích thước ảnh vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.";
            ViewBag.Avatar = user.Avatar;
            ViewBag.IsAvatarExists = !string.IsNullOrEmpty(user.Avatar) && System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/')));
            return View(model);
        }

        // Xóa avatar cũ nếu có
        if (!string.IsNullOrEmpty(user.Avatar))
        {
            var oldAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/'));
            if (System.IO.File.Exists(oldAvatarPath))
            {
                System.IO.File.Delete(oldAvatarPath);
                _logger.LogInformation($"Old avatar deleted: {oldAvatarPath}");
            }
        }

        // Lưu avatar mới vào wwwroot/avatars
        var avatarDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/avatars");
        if (!Directory.Exists(avatarDirectory))
        {
            Directory.CreateDirectory(avatarDirectory);
        }

        var fileName = Guid.NewGuid().ToString() + extension;
        var filePath = Path.Combine(avatarDirectory, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatarFile.CopyToAsync(stream);
        }

        // Cập nhật đường dẫn avatar vào cơ sở dữ liệu
        user.Avatar = $"/avatars/{fileName}";
        _logger.LogInformation($"New avatar uploaded: {user.Avatar}");

        // Lưu vào cơ sở dữ liệu
        try
        {
            _context.Update(user);
            int changes = await _context.SaveChangesAsync();
            _logger.LogInformation($"User {userName} updated avatar successfully. Changes saved: {changes}");
            TempData["Success"] = "Ảnh đại diện đã được cập nhật!";
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to update avatar for user {userName}: {ex.Message}");
            TempData["Error"] = "Lỗi khi cập nhật ảnh đại diện: " + ex.Message;
            ViewBag.Avatar = user.Avatar;
            ViewBag.IsAvatarExists = true;
            return View(model);
        }
    }
    else
    {
        TempData["Error"] = "Vui lòng chọn một ảnh để cập nhật.";
        ViewBag.Avatar = user.Avatar;
        ViewBag.IsAvatarExists = !string.IsNullOrEmpty(user.Avatar) && System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/')));
        return View(model);
    }

    return RedirectToAction("UpdateAvatar");
}


        #endregion

        #region xác nhận mã OTP
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Message = "Vui lòng nhập email.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Message = "Không tìm thấy tài khoản với email này.";
                return View();
            }

            // Kiểm tra xem tài khoản có phải là tài khoản Google không
            if (user.Password.Length == 36 && user.Password.Contains("-"))
            {
                ViewBag.Message = "Tài khoản này được đăng ký qua Google. Vui lòng đăng nhập bằng Google.";
                return View();
            }

            // Tạo OTP (6 chữ số)
            Random random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            // Lưu OTP và email vào TempData
            TempData["Email"] = email;
            TempData["Otp"] = otp;
            TempData.Keep("Email");
            TempData.Keep("Otp");

            // Gửi OTP qua email
            var emailBody = $@"
        <h3>Xác minh OTP để đặt lại mật khẩu</h3>
        <p>Chào bạn,</p>
        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Dưới đây là mã OTP của bạn:</p>
        <h2>{otp}</h2>
        <p>Mã OTP này sẽ hết hạn sau 10 phút. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
        <p>Trân trọng,</p>
        <p>E-Learning System</p>";

            try
            {
                await _emailService.SendEmailAsync(email, "Mã OTP để đặt lại mật khẩu - E-Learning System", emailBody);
                TempData["Success"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư (hoặc thư rác).";
                return RedirectToAction("VerifyOtp");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send OTP email to {email}: {ex.Message}");
                ViewBag.Message = "Đã xảy ra lỗi khi gửi OTP. Vui lòng thử lại sau.";
                return View();
            }
        }

        // GET: VerifyOtp
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyOtp()
        {
            if (TempData["Email"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = TempData["Email"];
            TempData.Keep("Email");
            TempData.Keep("Otp");
            return View();
        }

        // POST: VerifyOtp
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string email, string otp)
        {
            if (TempData["Email"] == null || TempData["Otp"] == null)
            {
                TempData["Error"] = "Phiên xác minh đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            string storedEmail = TempData["Email"].ToString();
            string storedOtp = TempData["Otp"].ToString();

            if (email != storedEmail)
            {
                TempData["Error"] = "Email không khớp. Vui lòng thử lại.";
                ViewBag.Email = storedEmail;
                TempData.Keep("Email");
                TempData.Keep("Otp");
                return View();
            }

            if (otp != storedOtp)
            {
                TempData["Error"] = "Mã OTP không đúng. Vui lòng kiểm tra lại.";
                ViewBag.Email = storedEmail;
                TempData.Keep("Email");
                TempData.Keep("Otp");
                return View();
            }

            // OTP đúng, lưu email vào TempData để sử dụng trong ResetPassword
            TempData["VerifiedEmail"] = email;
            TempData.Keep("VerifiedEmail");
            TempData.Remove("Otp"); // Xóa OTP sau khi xác minh thành công
            return RedirectToAction("ResetPassword");
        }

        // GET: ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            if (TempData["VerifiedEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = TempData["VerifiedEmail"];
            TempData.Keep("VerifiedEmail");
            return View();
        }

        // POST: ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (TempData["VerifiedEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            string verifiedEmail = TempData["VerifiedEmail"].ToString();
            if (email != verifiedEmail)
            {
                TempData["Error"] = "Email không khớp. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                TempData["Error"] = "Vui lòng nhập mật khẩu mới và xác nhận mật khẩu.";
                ViewBag.Email = verifiedEmail;
                TempData.Keep("VerifiedEmail");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                ViewBag.Email = verifiedEmail;
                TempData.Keep("VerifiedEmail");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == verifiedEmail);
            if (user == null)
            {
                _logger.LogWarning($"User not found for verified email: {verifiedEmail}");
                return RedirectToAction("ForgotPassword");
            }

            // Kiểm tra lại tài khoản Google
            if (user.Password.Length == 36 && user.Password.Contains("-"))
            {
                TempData["Error"] = "Tài khoản này được đăng ký qua Google. Vui lòng đăng nhập bằng Google.";
                return RedirectToAction("ForgotPassword");
            }

            try
            {
                user.Password = _passwordHasher.HashPassword(user, newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successfully for email: {verifiedEmail}");
                TempData["Success"] = "Mật khẩu đã được đặt lại thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error resetting password for email {verifiedEmail}: {ex.Message}");
                TempData["Error"] = "Đã có lỗi xảy ra khi đặt lại mật khẩu. Vui lòng thử lại.";
                ViewBag.Email = verifiedEmail;
                TempData.Keep("VerifiedEmail");
                return View();
            }
        }
        #endregion

        #region Đổi mật khẩu (ChangePassword)
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Avatar = user.Avatar;
            ViewBag.IsAvatarExists = !string.IsNullOrEmpty(user.Avatar);

            var model = new ChangePasswordViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Avatar = user.Avatar;
            ViewBag.IsAvatarExists = !string.IsNullOrEmpty(user.Avatar);

            // Gán thủ công UserName từ User.Identity.Name
            model.UserName = User.Identity.Name;

            // Xóa lỗi validation liên quan đến UserName (nếu có)
            if (ModelState.ContainsKey("UserName"))
            {
                ModelState.Remove("UserName");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra mật khẩu hiện tại
            if (!user.VerifyPassword(_passwordHasher, model.CurrentPassword))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            try
            {
                // Cập nhật mật khẩu mới
                user.HashPassword(_passwordHasher, model.NewPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thay đổi mật khẩu thành công.";
                return RedirectToAction("ChangePassword");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi khi lưu mật khẩu: " + ex.Message);
                return View(model);
            }
        }

        #endregion

        #region Xóa tài khoản (DeleteAccount)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("User not authenticated when attempting to delete account.");
                return RedirectToAction("Login", "Account");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation($"Initiating account deletion process for user: {userName}");

                // Lấy user và tất cả dữ liệu liên quan, bao gồm Cart và CartItems
                var user = await _context.Users
                    .Include(u => u.Comments)
                    .Include(u => u.Enrollments)
                    .Include(u => u.Progresses)
                    .Include(u => u.Notifications)
                    .Include(u => u.Carts)
                        .ThenInclude(c => c.CartItems)
                    .Include(u => u.Payments)
                    .Include(u => u.AssignmentSubmissions)
                    .Include(u => u.CourseInstructors)
                    .FirstOrDefaultAsync(u => u.UserName == userName);

                if (user == null)
                {
                    _logger.LogWarning($"User {userName} not found when attempting to delete account.");
                    TempData["Error"] = "Không tìm thấy tài khoản.";
                    return RedirectToAction("EditProfile");
                }

                _logger.LogInformation($"Starting deletion process for user {userName}");

                // Xóa các AssignmentSubmissions
                if (user.AssignmentSubmissions?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.AssignmentSubmissions.Count} assignment submissions");
                    _context.AssignmentSubmissions.RemoveRange(user.AssignmentSubmissions);
                }

                // Xóa các Carts (CartItems sẽ tự động bị xóa nếu có cascade)
                if (user.Carts?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Carts.Count} carts for user {userName}");
                    _context.Carts.RemoveRange(user.Carts);
                }

                // Xóa các Payments
                if (user.Payments?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Payments.Count} payments");
                    _context.Payments.RemoveRange(user.Payments);
                }

                // Xóa các Notifications
                if (user.Notifications?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Notifications.Count} notifications");
                    _context.Notifications.RemoveRange(user.Notifications);
                }

                // Xóa các CourseInstructors
                if (user.CourseInstructors?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.CourseInstructors.Count} course instructor records");
                    _context.CourseInstructors.RemoveRange(user.CourseInstructors);
                }

                // Xóa các Enrollments
                if (user.Enrollments?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Enrollments.Count} enrollments");
                    _context.Enrollments.RemoveRange(user.Enrollments);
                }

                // Xóa các Comments
                if (user.Comments?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Comments.Count} comments");
                    _context.Comments.RemoveRange(user.Comments);
                }

                // Xóa các Progresses
                if (user.Progresses?.Any() == true)
                {
                    _logger.LogInformation($"Deleting {user.Progresses.Count} progress records");
                    _context.Progresses.RemoveRange(user.Progresses);
                }

                // Xóa các OrderDetails liên quan đến các PaymentId của user
                var paymentIds = user.Payments?.Select(p => p.PaymentId).ToList();
                if (paymentIds?.Any() == true)
                {
                    var orderDetails = _context.OrderDetails.Where(od => paymentIds.Contains(od.PaymentId));
                    if (orderDetails.Any())
                    {
                        _context.OrderDetails.RemoveRange(orderDetails);
                        _logger.LogInformation($"Deleted {orderDetails.Count()} order details for user {userName}");
                    }
                }

                // Xóa avatar nếu có
                if (!string.IsNullOrEmpty(user.Avatar) && user.Avatar != "/images/defaultAvatar.png")
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.Avatar.TrimStart('/'));
                    if (System.IO.File.Exists(avatarPath))
                    {
                        _logger.LogInformation($"Deleting avatar file: {avatarPath}");
                        System.IO.File.Delete(avatarPath);
                    }
                }

                // Xóa user
                _logger.LogInformation($"Deleting user record for {userName}");
                _context.Users.Remove(user);

                // Lưu tất cả thay đổi
                var changes = await _context.SaveChangesAsync();
                _logger.LogInformation($"Saved {changes} changes to database");

                // Commit transaction
                await transaction.CommitAsync();
                _logger.LogInformation($"Transaction committed successfully for user {userName}");

                // Đăng xuất
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation($"User {userName} signed out after account deletion");

                TempData["Success"] = "Tài khoản đã được xóa vĩnh viễn.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error during account deletion transaction for user {userName}: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                TempData["Error"] = "Đã có lỗi xảy ra khi xóa tài khoản. Vui lòng thử lại sau.";
                return RedirectToAction("EditProfile");
            }
        }
        #endregion

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                _logger.LogWarning($"External login error: {remoteError}");
                TempData["Error"] = "Đã xảy ra lỗi khi đăng nhập bằng Google. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (info == null || !info.Succeeded)
            {
                _logger.LogWarning("External authentication failed.");
                TempData["Error"] = "Xác thực Google thất bại. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var userName = email.Split('@')[0]; // Tạo username từ email
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);

            var user = await _context.Users
                .Include(u => u.Role) // Nạp Role để tránh NullReferenceException
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Tạo tài khoản mới nếu chưa tồn tại
                var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
                if (studentRole == null)
                {
                    _logger.LogError("Role 'Student' not found.");
                    TempData["Error"] = "Không tìm thấy vai trò 'Student'. Vui lòng liên hệ quản trị viên.";
                    return RedirectToAction("Login");
                }

                user = new User
                {
                    UserName = userName,
                    Email = email,
                    FullName = fullName ?? userName,
                    RoleId = studentRole.RoleId,
                    Password = Guid.NewGuid().ToString(), // Đặt mật khẩu ngẫu nhiên
                    Avatar = "/images/defaultAvatar.png"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"New user created via Google: {userName}");
            }

            // Kiểm tra thêm để đảm bảo Role không null
            if (user.Role == null)
            {
                _logger.LogError($"Role not found for user with email: {email}");
                TempData["Error"] = "Không tìm thấy vai trò của người dùng. Vui lòng liên hệ quản trị viên.";
                return RedirectToAction("Login");
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation($"User {userName} logged in via Google successfully.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        #region MyLearning
        [Authorize]
        public async Task<IActionResult> MyLearning()
        {
            var userName = User.Identity.Name;
            if (string.IsNullOrEmpty(userName))
            {
                _logger.LogWarning("User not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách các khóa học đã đăng ký
            var enrollments = await _context.Enrollments
                .Where(e => e.UserName == userName)
                .Include(e => e.Course)
                .ToListAsync();

            // Lấy tất cả progress của user này
            var progresses = await _context.Progresses
                .Where(p => p.UserName == userName)
                .Include(p => p.Lesson)
                .ToListAsync();

            var myLearningList = new List<MyLearningViewModel>();

            foreach (var enrollment in enrollments)
            {
                var course = enrollment.Course;
                if (course == null) continue;

                // Tổng số bài học của khóa học
                int totalLessons = course.Lessons?.Count ?? 0;
                // Số bài học đã hoàn thành
                int completedLessons = progresses
                    .Where(p => p.Lesson.CourseId == course.CourseId && p.CompletionStatus)
                    .Count();

                // Tính tiến độ %
                int progressPercent = (totalLessons > 0) ? (int)Math.Round(100.0 * completedLessons / totalLessons) : 0;

                // Lấy ngày hoàn thành gần nhất
                DateTime? lastAccessed = progresses
                    .Where(p => p.Lesson.CourseId == course.CourseId && p.CompletionDate.HasValue)
                    .OrderByDescending(p => p.CompletionDate)
                    .Select(p => p.CompletionDate)
                    .FirstOrDefault();

                myLearningList.Add(new MyLearningViewModel
                {
                    CourseId = course.CourseId,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    ImageUrl = course.ImageUrl,
                    EnrollmentDate = enrollment.EnrollmentDate,
                    Progress = progressPercent,
                    LastAccessed = lastAccessed ?? enrollment.EnrollmentDate
                });
            }

            // Sắp xếp theo lần truy cập gần nhất
            myLearningList = myLearningList.OrderByDescending(c => c.LastAccessed).ToList();

            return View(myLearningList);
        }
        #endregion

        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}