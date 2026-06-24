using BusinessObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.Infrastructure;
using QuizManagement.ViewModels.Account;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ILoginAttemptLogService _loginAttemptLogService;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AccountController(
            IUserService userService,
            ILoginAttemptService loginAttemptService,
            ILoginAttemptLogService loginAttemptLogService)
        {
            _userService = userService;
            _loginAttemptService = loginAttemptService;
            _loginAttemptLogService = loginAttemptLogService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            model.Email = model.Email.Trim();
            model.Username = model.Username.Trim();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (_userService.GetByEmail(model.Email) is not null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
                return View(model);
            }

            if (_userService.GetByUsername(model.Username) is not null)
            {
                ModelState.AddModelError(nameof(model.Username), "Tên người dùng này đã được sử dụng.");
                return View(model);
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = model.Username,
                Email = model.Email,
                Role = AppRoles.User,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            _userService.CreateUser(user);

            TempData["SuccessMessage"] = "Đăng ký thành công. Bạn có thể đăng nhập ngay.";
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.Email = model.Email.Trim();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check rate limit before any DB query
            if (_loginAttemptService.IsLockedOut(model.Email, ipAddress))
            {
                var remaining = _loginAttemptService.GetRemainingLockoutTime(model.Email, ipAddress);
                var minutes = (int)Math.Ceiling(remaining?.TotalMinutes ?? 1);
                ModelState.AddModelError(string.Empty,
                    $"Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần. Thử lại sau {minutes} phút.");
                return View(model);
            }

            var user = _userService.GetByEmail(model.Email);
            if (user?.PasswordHash is null)
            {
                _loginAttemptService.RecordFailedAttempt(model.Email, ipAddress);
                _loginAttemptLogService.Log(model.Email, ipAddress, false);
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _loginAttemptService.RecordFailedAttempt(model.Email, ipAddress);
                _loginAttemptLogService.Log(model.Email, ipAddress, false, user.Id);
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (user.IsDisabled)
            {
                _loginAttemptLogService.Log(model.Email, ipAddress, false, user.Id);
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Login success — clear failed attempts
            _loginAttemptService.ClearAttempts(model.Email, ipAddress);
            _loginAttemptLogService.Log(model.Email, ipAddress, true, user.Id);

            // Ensure SecurityStamp exists for new users migrated from old schema
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                user.SecurityStamp = Guid.NewGuid().ToString();
                _userService.UpdateProfile(user);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role ?? AppRoles.User),
                new("SecurityStamp", user.SecurityStamp)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
