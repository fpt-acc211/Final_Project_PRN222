using BusinessObjects;
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
        private readonly IDeckService _deckService;

        public StatisticsController(IQuizService quizService, IDeckService deckService)
        {
            _quizService = quizService;
            _deckService = deckService;
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

        public IActionResult Leaderboard(int deckId)
        {
            var deck = _deckService.GetDeckForStudy(deckId);
            if (deck is null) return NotFound();

            var histories = _quizService.GetTestHistoriesByDeck(deckId);

            var entries = histories
                .GroupBy(h => h.UserId)
                .Select(g =>
                {
                    var best = g.MaxBy(h => h.Percentage)!;
                    return new LeaderboardEntryViewModel
                    {
                        Username = best.User.Username,
                        BestPercentage = Math.Round(g.Max(h => h.Percentage), 1),
                        AttemptCount = g.Count(),
                        LastAttemptAt = g.Max(h => h.CreatedAt)
                    };
                })
                .OrderByDescending(e => e.BestPercentage)
                .ThenByDescending(e => e.LastAttemptAt)
                .Select((e, i) => { e.Rank = i + 1; return e; })
                .Take(20)
                .ToList();

            ViewBag.SubjectId = deck.SubjectId;

            var model = new LeaderboardViewModel
            {
                DeckId = deckId,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                Entries = entries
            };

            return View(model);
        }

        [Authorize(Policy = "ManageContent")]
        public IActionResult MentorStats()
        {
            var isAdmin = User.IsInRole(AppRoles.Admin);
            var histories = _quizService.GetTestHistoriesByContentOwner(CurrentUserId(), isAdmin);

            var subjectStats = histories
                .GroupBy(h => h.Deck.Subject.Name)
                .Select(g => new MentorSubjectStatViewModel
                {
                    SubjectName = g.Key,
                    TotalAttempts = g.Count(),
                    UniqueUsers = g.Select(h => h.UserId).Distinct().Count(),
                    AvgPercentage = Math.Round(g.Average(h => h.Percentage), 1),
                    BestPercentage = Math.Round(g.Max(h => h.Percentage), 1)
                })
                .OrderByDescending(s => s.TotalAttempts)
                .ThenBy(s => s.SubjectName)
                .ToList();

            var deckStats = histories
                .GroupBy(h => h.DeckId)
                .Select(g =>
                {
                    var first = g.First();
                    return new MentorDeckStatViewModel
                    {
                        DeckId = first.DeckId,
                        DeckName = first.Deck.Name,
                        SubjectName = first.Deck.Subject.Name,
                        TotalAttempts = g.Count(),
                        UniqueUsers = g.Select(h => h.UserId).Distinct().Count(),
                        AvgPercentage = Math.Round(g.Average(h => h.Percentage), 1),
                        BestPercentage = Math.Round(g.Max(h => h.Percentage), 1),
                        LastAttemptAt = g.Max(h => h.CreatedAt)
                    };
                })
                .OrderByDescending(d => d.TotalAttempts)
                .ThenBy(d => d.SubjectName)
                .ThenBy(d => d.DeckName)
                .ToList();

            return View(new MentorStatsViewModel
            {
                SubjectStats = subjectStats,
                DeckStats = deckStats
            });
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