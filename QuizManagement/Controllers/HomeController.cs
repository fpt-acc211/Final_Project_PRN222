using Microsoft.AspNetCore.Mvc;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using QuizManagement.ViewModels.Home;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IQuizService _quizService;
        private readonly ISubjectService _subjectService;

        public HomeController(
            ILogger<HomeController> logger,
            IQuizService quizService,
            ISubjectService subjectService)
        {
            _logger = logger;
            _quizService = quizService;
            _subjectService = subjectService;
        }

        public IActionResult Index()
        {
            var userId = CurrentUserId();

            var (totalQuizzes, averagePercentage, lastQuizDate) = _quizService.GetQuizStatistics(userId);
            var recentHistories = _quizService.GetRecentTestHistories(userId, 5);
            var subjects = _subjectService.GetAllSubjects().ToList();

            var model = new DashboardViewModel
            {
                TotalQuizzesTaken = totalQuizzes,
                AveragePercentage = averagePercentage,
                LastQuizDate = lastQuizDate,
                RecentHistories = recentHistories,
                Subjects = subjects
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }
    }
}
