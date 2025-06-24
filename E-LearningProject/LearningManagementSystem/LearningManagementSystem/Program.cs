using LearningManagementSystem.Data;
using LearningManagementSystem.Models;
using LearningManagementSystem.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.Google;
using LearningManagementSystem.Services; // Thêm namespace cho Google Authentication

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
});

// Đăng ký DbContext
builder.Services.AddDbContext<LMSContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký IPasswordHasher<User>
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddHttpClient<IGroqService, GroqService>();
builder.Services.AddScoped<IGroqService, GroqService>();


// Đăng ký Cookie Authentication và Google Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

// Thêm cấu hình Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireInstructorRole", policy =>
        policy.RequireRole("Instructor"));
    
    options.AddPolicy("RequireStudentRole", policy =>
        policy.RequireRole("Student"));
});

// Đăng ký các repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ICartItemRepository, CartItemRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IAssignmentQuestionRepository, AssignmentQuestionRepository>();
builder.Services.AddScoped<IAssignmentQuestionOptionRepository, AssignmentQuestionOptionRepository>();
builder.Services.AddScoped<IAssignmentSubmissionRepository, AssignmentSubmissionRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<ILessonRepository, LessonRepository>();

builder.Services.AddSingleton<EmailService>();

// Thêm hỗ trợ session (nếu cần cho giỏ hàng hoặc các tính năng khác)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(); // Ghi log ra console
    logging.AddDebug();   // Ghi log ra debug output (nếu chạy trong IDE)
    logging.SetMinimumLevel(LogLevel.Information); // Ghi tất cả log từ Information trở lên
    // Nếu muốn ghi log ra file, có thể thêm:
    // logging.AddFile("Logs/app-{Date}.log");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Thêm middleware Authentication
app.UseAuthorization();

app.UseSession(); // Thêm middleware Session

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LMSContext>();
    var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    // Tạo roles
    if (!roleRepo.GetAll().Any())
    {
        roleRepo.Add(new Role { RoleId = Guid.NewGuid().ToString(), RoleName = "Admin" });
        roleRepo.Add(new Role { RoleId = Guid.NewGuid().ToString(), RoleName = "Instructor" });
        roleRepo.Add(new Role { RoleId = Guid.NewGuid().ToString(), RoleName = "Student" });
        roleRepo.Save();
    }

    // Tạo tài khoản Admin
    string adminUsername = "admin";
    if (userRepo.GetByUserName(adminUsername) == null)
    {
        // Lấy RoleId của vai trò Admin
        var adminRole = roleRepo.GetByName("Admin");
        if (adminRole == null)
        {
            throw new Exception("Admin role not found. Please ensure roles are seeded correctly.");
        }

        var admin = new User
        {
            UserName = adminUsername,
            Email = "admin@elearning.com",
            FullName = "Administrator",
            RoleId = adminRole.RoleId, // Gán RoleId thực sự (GUID) từ vai trò Admin
        };
        admin.HashPassword(hasher, "Admin@123");
        userRepo.Add(admin);
        userRepo.Save();
    }
}

app.Run();