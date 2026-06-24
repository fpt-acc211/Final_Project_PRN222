using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Quiz;
using Services;
using System.Security.Claims;
using System.Text.Json;

namespace QuizManagement.Controllers
{
    [Authorize(Policy = "StudyContent")]
    public class QuizController : Controller
    {
        private static readonly TimeSpan QuizAttemptLifetime = TimeSpan.FromHours(8);
        private static readonly TimeSpan QuizSubmitGracePeriod = TimeSpan.FromSeconds(30);

        private readonly IDeckService _deckService;
        private readonly IQuizService _quizService;
        private readonly IDataProtector _quizAttemptProtector;

        public QuizController(
            IDeckService deckService,
            IQuizService quizService,
            IDataProtectionProvider dataProtectionProvider)
        {
            _deckService = deckService;
            _quizService = quizService;
            _quizAttemptProtector = dataProtectionProvider.CreateProtector("QuizManagement.QuizAttempt.v1");
        }

        public IActionResult Config(int deckId)
        {
            var deck = _deckService.GetDeckForStudy(deckId);
            if (deck is null)
            {
                return NotFound();
            }

            var availableCount = _quizService.GetAvailableQuestionCount(deckId);
            if (availableCount == 0)
            {
                TempData["ErrorMessage"] = "Bộ đề này chưa có câu hỏi nào để làm bài.";
                return RedirectToAction("Index", "Questions", new { deckId });
            }

            return View(new QuizConfigViewModel
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                AvailableQuestionCount = availableCount,
                QuestionCount = Math.Min(10, availableCount),
                TimeLimitMinutes = deck.TimeLimitMinutes
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Take(QuizConfigViewModel config)
        {
            var deck = _deckService.GetDeckForStudy(config.DeckId);
            if (deck is null)
            {
                return NotFound();
            }

            var availableCount = _quizService.GetAvailableQuestionCount(config.DeckId);
            config.AvailableQuestionCount = availableCount;
            config.DeckName = deck.Name;
            config.SubjectName = deck.Subject.Name;

            if (config.QuestionCount < 1 || config.QuestionCount > availableCount)
            {
                ModelState.AddModelError(nameof(config.QuestionCount),
                    $"Số câu hỏi phải từ 1 đến {availableCount}.");
                return View("Config", config);
            }

            if (!ModelState.IsValid)
            {
                return View("Config", config);
            }

            var questions = _quizService.GetQuestionsForQuiz(config.DeckId, config.QuestionCount);
            var questionIds = questions.Select(q => q.Id).ToList();
            var timeLimitSeconds = deck.TimeLimitMinutes > 0 ? deck.TimeLimitMinutes * 60 : 0;
            var issuedAtUtc = DateTimeOffset.UtcNow;
            var expiresAtUtc = timeLimitSeconds > 0
                ? issuedAtUtc.AddSeconds(timeLimitSeconds)
                : (DateTimeOffset?)null;

            var model = new QuizTakeViewModel
            {
                DeckId = config.DeckId,
                DeckName = deck.Name,
                SubjectName = deck.Subject.Name,
                AttemptToken = ProtectQuizAttempt(config.DeckId, CurrentUserId(), questionIds, issuedAtUtc, expiresAtUtc),
                TimeLimitSeconds = timeLimitSeconds,
                Questions = questions.Select(q => new QuizQuestionViewModel
                {
                    QuestionId = q.Id,
                    Content = q.Content,
                    QuestionType = q.QuestionType,
                    Answers = q.Answers.Select(a => new QuizAnswerOptionViewModel
                    {
                        AnswerId = a.Id,
                        Content = a.Content
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(QuizSubmitViewModel model)
        {
            var deck = _deckService.GetDeckForStudy(model.DeckId);
            if (deck is null)
            {
                return NotFound();
            }

            var currentUserId = CurrentUserId();
            var attempt = ReadQuizAttempt(model.AttemptToken);
            if (attempt is null || !IsValidQuizAttempt(attempt, model.DeckId, currentUserId))
            {
                TempData["ErrorMessage"] = "Phiên làm bài không hợp lệ hoặc đã hết hạn. Vui lòng bắt đầu lại bài quiz.";
                return RedirectToAction(nameof(Config), new { deckId = model.DeckId });
            }

            var selectedAnswersByQuestion = model.Questions
                .GroupBy(q => q.QuestionId)
                .ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(GetSelectedAnswerIds).Distinct().ToList());

            var history = _quizService.GradeAndSaveQuiz(
                model.DeckId,
                currentUserId,
                attempt.QuestionIds,
                selectedAnswersByQuestion);

            TempData["SuccessMessage"] = "Đã nộp bài thành công!";
            return RedirectToAction(nameof(Result), new { id = history.Id });
        }

        public IActionResult Result(int id)
        {
            var history = _quizService.GetTestHistoryById(id, CurrentUserId());
            if (history is null)
            {
                return NotFound();
            }

            var resultModel = BuildResultViewModel(history);
            return View(resultModel);
        }

        private static QuizResultViewModel BuildResultViewModel(TestHistory history)
        {
            var questionGroups = history.TestResultDetails
                .GroupBy(d => d.QuestionId)
                .ToList();

            var questionResults = questionGroups.Select(g =>
            {
                var firstDetail = g.First();
                var question = firstDetail.Question;
                var selectedAnswerIds = g
                    .Where(d => d.SelectedAnswerId.HasValue)
                    .Select(d => d.SelectedAnswerId!.Value)
                    .ToHashSet();

                return new QuizResultQuestionViewModel
                {
                    QuestionId = question.Id,
                    Content = question.Content,
                    Explanation = question.Explanation,
                    QuestionType = question.QuestionType,
                    IsCorrect = firstDetail.IsCorrect,
                    Answers = question.Answers
                        .OrderBy(a => a.Id)
                        .Select(a => new QuizResultAnswerViewModel
                        {
                            AnswerId = a.Id,
                            Content = a.Content,
                            IsCorrectAnswer = a.IsCorrect,
                            WasSelected = selectedAnswerIds.Contains(a.Id)
                        }).ToList()
                };
            }).ToList();

            return new QuizResultViewModel
            {
                TestHistoryId = history.Id,
                DeckName = history.Deck.Name,
                SubjectName = history.Deck.Subject.Name,
                Score = history.Score,
                Percentage = history.Percentage,
                CorrectCount = questionResults.Count(q => q.IsCorrect),
                TotalCount = questionResults.Count,
                CreatedAt = history.CreatedAt,
                Questions = questionResults
            };
        }

        private string ProtectQuizAttempt(
            int deckId,
            string userId,
            List<int> questionIds,
            DateTimeOffset issuedAtUtc,
            DateTimeOffset? expiresAtUtc)
        {
            var payload = new QuizAttemptPayload
            {
                DeckId = deckId,
                UserId = userId,
                QuestionIds = questionIds,
                IssuedAtUtc = issuedAtUtc,
                ExpiresAtUtc = expiresAtUtc
            };

            return _quizAttemptProtector.Protect(JsonSerializer.Serialize(payload));
        }

        private QuizAttemptPayload? ReadQuizAttempt(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var json = _quizAttemptProtector.Unprotect(token);
                return JsonSerializer.Deserialize<QuizAttemptPayload>(json);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidQuizAttempt(QuizAttemptPayload attempt, int deckId, string userId)
        {
            var now = DateTimeOffset.UtcNow;

            return attempt.DeckId == deckId
                && attempt.UserId == userId
                && attempt.QuestionIds.Count > 0
                && attempt.QuestionIds.Count <= 500
                && now - attempt.IssuedAtUtc <= QuizAttemptLifetime
                && (!attempt.ExpiresAtUtc.HasValue
                    || now <= attempt.ExpiresAtUtc.Value.Add(QuizSubmitGracePeriod));
        }

        private static IEnumerable<int> GetSelectedAnswerIds(QuizQuestionSubmitItem question)
        {
            if (question.SelectedAnswerId.HasValue)
            {
                yield return question.SelectedAnswerId.Value;
            }

            foreach (var answerId in question.SelectedAnswerIds)
            {
                yield return answerId;
            }
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }

        private sealed class QuizAttemptPayload
        {
            public int DeckId { get; set; }

            public string UserId { get; set; } = string.Empty;

            public List<int> QuestionIds { get; set; } = new();

            public DateTimeOffset IssuedAtUtc { get; set; }

            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }
    }
}
