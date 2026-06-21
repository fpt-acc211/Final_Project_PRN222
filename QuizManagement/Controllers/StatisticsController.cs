using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Statistics;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize(Policy = "ViewAnalytics")]
    public class StatisticsController : Controller
    {
        private readonly IQuizService _quizService;

        public StatisticsController(IQuizService quizService)
        {
            _quizService = quizService;
        }

        public IActionResult Index()
        {
            var histories = _quizService.GetTestHistoriesByUser(CurrentUserId())
                .OrderBy(history => history.CreatedAt)
                .ToList();

            var total = histories.Count;
            var model = new StatisticsViewModel
            {
                TotalAttempts = total,
                AveragePercentage = total > 0 ? Math.Round(histories.Average(h => h.Percentage), 1) : 0,
                BestPercentage = total > 0 ? Math.Round(histories.Max(h => h.Percentage), 1) : 0,
                LowestPercentage = total > 0 ? Math.Round(histories.Min(h => h.Percentage), 1) : 0,
                PassedAttempts = histories.Count(h => h.Percentage >= 50),
                FailedAttempts = histories.Count(h => h.Percentage < 50)
            };

            model.PassRate = total > 0 ? Math.Round((double)model.PassedAttempts / total * 100, 1) : 0;
            model.RecentScores = histories.TakeLast(12)
                .Select(history => new ScoreTrendPointViewModel
                {
                    Label = history.CreatedAt.ToLocalTime().ToString("dd/MM"),
                    Percentage = history.Percentage
                })
                .ToList();

            model.SubjectStats = histories
                .GroupBy(history => history.Deck.Subject.Name)
                .Select(group => BuildGroupPerformance(group.Key, group))
                .OrderByDescending(group => group.AveragePercentage)
                .ThenBy(group => group.Name)
                .ToList();

            model.DeckStats = histories
                .GroupBy(history => $"{history.Deck.Subject.Name} / {history.Deck.Name}")
                .Select(group => BuildGroupPerformance(group.Key, group))
                .OrderByDescending(group => group.AveragePercentage)
                .ThenBy(group => group.Name)
                .ToList();

            return View(model);
        }

        private static GroupPerformanceViewModel BuildGroupPerformance(string name, IEnumerable<BusinessObjects.TestHistory> histories)
        {
            var list = histories.ToList();
            return new GroupPerformanceViewModel
            {
                Name = name,
                Attempts = list.Count,
                AveragePercentage = Math.Round(list.Average(h => h.Percentage), 1),
                BestPercentage = Math.Round(list.Max(h => h.Percentage), 1),
                LastAttemptAt = list.Max(h => h.CreatedAt)
            };
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }
    }
}