using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using Repositories;
using Services;
using DataAccessObjects;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using QuizManagement.Infrastructure;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình file config local (nếu có) và logging
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. DbContext
builder.Services.AddDbContext<QuizManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Repositories
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<IDeckRepository, DeckRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// 3. Services
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IDeckService, DeckService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IQuestionImportService, QuestionImportService>();
builder.Services.AddSingleton<IDeckExportService, DeckExportService>();

// 4. Infrastructure (web-tier)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();

// 5. Data Protection (tránh mất session khi restart app)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

// 6. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // ManageUsers: chỉ Admin
    options.AddPolicy("ManageUsers", policy =>
        policy.RequireRole(AppRoles.Admin));

    // ManageContent: Admin + Mentor
    options.AddPolicy("ManageContent", policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Mentor));

    // ViewAnalytics: Admin + Mentor
    options.AddPolicy("ViewAnalytics", policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Mentor));

    // TakeQuiz: tất cả người dùng đã đăng nhập
    options.AddPolicy("TakeQuiz", policy =>
        policy.RequireAuthenticatedUser());
});

// 7. Cookie Authentication với SecurityStamp + IsDisabled validation
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId is null) return Task.CompletedTask;

                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var user = userService.GetById(userId);

                // Reject if user deleted or disabled
                if (user is null || user.IsDisabled)
                {
                    context.RejectPrincipal();
                    return Task.CompletedTask;
                }

                // Reject if SecurityStamp has changed (password changed / role changed)
                var stampInCookie = context.Principal?.FindFirstValue("SecurityStamp");
                if (user.SecurityStamp is not null && user.SecurityStamp != stampInCookie)
                {
                    context.RejectPrincipal();
                }

                return Task.CompletedTask;
            }
        };
    });

// 8. MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==================== KIỂM TRA KẾT NỐI DATABASE + SEED ====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<QuizManagementDbContext>();
        logger.LogInformation("Đang kiểm tra kết nối tới Database...");

        if (context.Database.CanConnect())
        {
            logger.LogInformation("Kết nối Database thành công!");

            // Seed admin account if none exists
            if (!context.Users.Any(u => u.Role == AppRoles.Admin))
            {
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<BusinessObjects.User>();
                var adminUser = new BusinessObjects.User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = "Admin",
                    Email = "admin@test.com",
                    Role = AppRoles.Admin,
                    IsDisabled = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");
                context.Users.Add(adminUser);
                context.SaveChanges();
                logger.LogInformation("Đã tạo tài khoản Admin: admin@test.com / Admin@123");
            }
        }
        else
        {
            logger.LogError("Không thể kết nối tới Database. Kiểm tra lại Connection String hoặc SQL Server.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Đã xảy ra lỗi khi kết nối Database!");
    }
}
// ========================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
