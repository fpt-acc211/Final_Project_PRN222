using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using Repositories;
using Services;
using DataAccessObjects;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình cấu trúc file cấu hình local (nếu có) và logging
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. Đăng ký DbContext với Connection String từ appsettings
builder.Services.AddDbContext<QuizManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Đăng ký Dependency Injection cho Repository và Service
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ISubjectService, SubjectService>();

builder.Services.AddScoped<IDeckRepository, DeckRepository>();
builder.Services.AddScoped<IDeckService, DeckService>();

builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionService, QuestionService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IQuizRepository, QuizRepository>();
builder.Services.AddScoped<IQuizService, QuizService>();

builder.Services.AddScoped<IQuestionImportService, QuestionImportService>();
builder.Services.AddSingleton<IDeckExportService, DeckExportService>();

// 3. Cấu hình Cookie Authentication & Data Protection (Tránh lỗi mất session khi restart app)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// 4. Thêm dịch vụ MVC (Controllers với Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==================== PHẦN KIỂM TRA KẾT NỐI DATABASE ====================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<QuizManagementDbContext>();
        logger.LogInformation("⏳ Đang kiểm tra kết nối tới Database...");

        if (context.Database.CanConnect())
        {
            logger.LogInformation("✅ Kết nối Database thành công! Sẵn sàng chạy ứng dụng.");
        }
        else
        {
            logger.LogError("❌ Không thể kết nối tới Database. Vui lòng kiểm tra lại Connection String hoặc SQL Server.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "💥 Đã xảy ra lỗi nghiêm trọng khi cố gắng kết nối Database!");
    }
}
// ========================================================================

// Cấu hình HTTP request pipeline (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Giá trị HSTS mặc định là 30 ngày.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Thứ tự Middleware Authentication và Authorization phải chính xác
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();